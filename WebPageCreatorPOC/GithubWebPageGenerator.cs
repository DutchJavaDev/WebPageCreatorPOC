using Octokit;
using System.Text;

namespace WebPageCreatorPOC
{
  public sealed class GithubWebPageGenerator(GitHubClient client)
  {
    private static string ProjectDivTemplate = @"<div class=""project"">
      <h2>Name: <span id=""project-name-1"">[Project-Name-Placeholder]</span></h2>
      <div class=""languages"">
        [ProjectLanguages]
      </div>
      <p class=""description"" id=""project-desc-1"">
        [Description]
      </p>
      <p class=""pull-request"" id=""project-pr-1"">
        <b>Last Commit</b>: [Last-commit]
      </p>
      <p class=""github-link"">
         <a href=""[Github-link]"">Github Link</a>
      </p>
    </div>";

    public async Task GenerateAsync(string templateName = "template-index.html")
    {
      var repositories = await client.Repository.GetAllForCurrent(new RepositoryRequest
      {
        Type = RepositoryType.Owner,
        Sort = RepositorySort.Pushed,
        Direction = SortDirection.Descending,
      });

      var projectDivs = new StringBuilder();

      // No control over the order
      //var tasks = new List<Task>();

      foreach (var repo in repositories.Where(i => !i.Private && !i.Fork)) 
      {
        //tasks.Add(Task.Run(async () => {
          var projectDiv = ProjectDivTemplate;

          projectDiv = SetProjectName(repo, projectDiv);

          projectDiv = await SetProjectLanguagesAsync(repo, projectDiv);

          projectDiv = SetProjectDescription(repo, projectDiv);

          projectDiv = await SetLastCommitAsync(repo, projectDiv);

          projectDiv = SetGithubLink(repo, projectDiv);

          projectDivs.Append(projectDiv);
        //}));
      }

      //await Task.WhenAll(tasks);

      await WriteToPageAsync(projectDivs.ToString(), templateName);
    }

    private static async Task WriteToPageAsync(string content, string template)
    {
      var templateFile = await File.ReadAllTextAsync($"Template/{template}");

      var indexPage = templateFile.Replace("<!--projects_replace-->", content)
        .Replace("[Last-generated]", DateTime.Now.ToShortDateString());

      await File.WriteAllTextAsync("Template/index.html", indexPage);
    }

    private static string SetProjectName(Repository repo, string div)
    {
      if(repo.Archived)
      {
        return div.Replace("[Project-Name-Placeholder]", $"<span style='color:yellow'>[Archived]</span> {repo.Name}");
      }

      return div.Replace("[Project-Name-Placeholder]", repo.Name);
    }

    private static string SetGithubLink(Repository repo, string div)
    {
      return div.Replace("[Github-link]",repo.HtmlUrl);
    }

    private static string SetProjectDescription(Repository repo, string div)
    {
      var description = repo.Description;

      if (!string.IsNullOrEmpty(description))
      {
        return div.Replace("[Description]", $"<h3>{description}</h3>");
      }

      return div.Replace("[Description]", "<h3>This project has no description</h3>");
    }

    private async Task<string> SetLastCommitAsync(Repository repo, string div)
    {
      var branch = await client.Git.Reference.Get(repo.Owner.Login, repo.Name, $"heads/{repo.DefaultBranch}");

      var lastCommitMessage = await client.Repository.Commit.Get(repo.Owner.Login, repo.Name, branch.Object.Sha);

      if (lastCommitMessage != null) 
      {
        var now = DateTime.Now;
        var commitDate = lastCommitMessage.Commit.Author.Date.DateTime;
        var days = (now - commitDate).Days;
        var message = $"{commitDate.ToShortDateString()} ({days} days ago)<br>{lastCommitMessage.Commit.Message}";
        return div.Replace("[Last-commit]",message);
      }

      return div.Replace("[Last-commit]","No last commit message found");
    }

    private async Task<string> SetProjectLanguagesAsync(Repository repo, string div)
    {
      var languages = await client.Repository.GetAllLanguages(repo.Owner.Login, repo.Name);

      var totalProjectBytes = languages.Sum(i => i.NumberOfBytes);

      var languageSpans = new StringBuilder();

      if (languages.Any())
      {
        foreach (var language in languages)
        {
          var percentage = (((float)language.NumberOfBytes / (float)totalProjectBytes) * 100f).ToString("0.00");

          if (LanguageDictionary.TryGetLanguageHTMLClass(language.Name, out string htmlClass))
          {
            languageSpans.AppendLine(@$"<span class=""language {htmlClass}"">{language.Name} ({percentage}%)</span>");
          }
          else 
          {
            languageSpans.Append(@$"<span class=""language language-unknown"">{language.Name} ({percentage}%)</span>");
          }
        }

        return div.Replace("[ProjectLanguages]", languageSpans.ToString());
      }

      return div.Replace("[ProjectLanguages]", @"<span class=""language language-unknown"">No Language found, probally configuration project</span>");
    }
  }
}
