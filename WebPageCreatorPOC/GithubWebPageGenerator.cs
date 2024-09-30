using Octokit;
using System.Collections.Concurrent;
using System.Text;

namespace WebPageCreatorPOC
{
  public sealed class GithubWebPageGenerator(GitHubClient client)
  {
    private static readonly ConcurrentDictionary<int,string> ConcurrentProjectDivDictionary = new();

    private static readonly string ProjectDivTemplate = @"<div data-date='[replace-data]' class=""project"">
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
      ConcurrentProjectDivDictionary.Clear();

      var repositories = await client.Repository.GetAllForCurrent(new RepositoryRequest
      {
        Type = RepositoryType.Owner,
        Sort = RepositorySort.Pushed,
        Direction = SortDirection.Descending,
      });

      var projectDivs = new StringBuilder();

      var tasks = new List<Task>();

      var orderIndex = 0;

      foreach (var repo in repositories.Where(i => !i.Private && !i.Fork)) 
      {
        var index = orderIndex;
        tasks.Add(Task.Run(async () => await CreateProjectDivAsync(index, repo)));
        orderIndex++;
      }

      await Task.WhenAll(tasks);

      foreach (var (index, content) in ConcurrentProjectDivDictionary.OrderBy(i => i.Key)) 
      {
        projectDivs.AppendLine(content);
      }
      
      await WriteToPageAsync(projectDivs.ToString(), templateName);
    }

    private async Task CreateProjectDivAsync(int orderNumber, Repository repo)
    {
      var projectDiv = ProjectDivTemplate;

      projectDiv = SetProjectName(repo, projectDiv);

      projectDiv = await SetProjectLanguagesAsync(repo, projectDiv);

      projectDiv = SetProjectDescription(repo, projectDiv);

      projectDiv = await SetLastCommitAsync(repo, projectDiv);

      projectDiv = SetGithubLink(repo, projectDiv);

      ConcurrentProjectDivDictionary.TryAdd(orderNumber, projectDiv);
    }

    private static async Task WriteToPageAsync(string content, string template)
    {
      var templateFile = await File.ReadAllTextAsync($"Template/{template}");

      var now = DateTime.Now;

      var indexPage = templateFile.Replace("<!--projects_replace-->", content)
        .Replace("[Last-generated]", $"{now.ToLongDateString()} : {now.ToLongTimeString()}");

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
        div = div.Replace("[replace-data]", commitDate.ToString("o"));

        // Will get update by js ^^^^
        var days = (now - commitDate).Days;
        var message = $"{commitDate.ToShortDateString()} <b class='last-commit-day'>({days} days ago)</b><br>{lastCommitMessage.Commit.Message}";
        return div.Replace("[Last-commit]",message);
      }


      // if it fails display no date
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
