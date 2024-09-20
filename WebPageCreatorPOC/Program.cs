using Microsoft.Extensions.Configuration;
using Octokit;
using System.Text;

// Replace with your GitHub personal access token
var configuration = new ConfigurationBuilder()
                        .AddUserSecrets<Program>()
                        .Build();

var personalAccessToken = configuration["AccessToken"];

// Initialize the GitHub client with the token
var client = new GitHubClient(new ProductHeaderValue("MyApp"));
var tokenAuth = new Credentials(personalAccessToken);
client.Credentials = tokenAuth;

var languageDictionary = new Dictionary<string, string>() 
{
            { "C#", "language-csharp" },
            { "JavaScript", "language-javascript" },
            { "Python", "language-python" },
            { "Java", "language-java" },
            { "C++", "language-cpp" },
            { "HTML", "language-html" },
            { "CSS", "language-css" },
            { "Ruby", "language-ruby" },
            { "Go", "language-go" },
            { "PHP", "language-php" },
            { "TypeScript", "language-typescript" },
            { "Swift", "language-swift" },
            { "Rust", "language-rust" },
            { "Kotlin", "language-kotlin" },
            { "Shell", "language-shell" },
            { "Scala", "language-scala" },
            { "Perl", "language-perl" },
            { "Lua", "language-lua" },
            { "Objective-C", "language-objectivec" },
            { "R", "language-r" },
            { "Haskell", "language-haskell" },
            { "Elixir", "language-elixir" },
            { "Dart", "language-dart" },
            { "PowerShell", "language-powershell" },
            { "Matlab", "language-matlab" },
            { "CoffeeScript", "language-coffeescript" },
            { "Vim script", "language-vimscript" },
            { "TeX", "language-tex" },
            { "C", "language-c" },
            { "Dockerfile", "language-dockerfile" },
            { "Markdown", "language-markdown" },
            { "F#", "language-fsharp" },
            { "Erlang", "language-erlang" },
            { "V", "language-v" }
};

var projectTemplate = @"<div class=""project"">
      <h2>Name: <span id=""project-name-1"">[Project Name Placeholder]</span></h2>
      <div class=""languages"">
        [ProjectLanguages]
      </div>
      <p class=""description"" id=""project-desc-1"">
        [This is a description placeholder]
      </p>
      <p class=""pull-request"" id=""project-pr-1"">
        <b>Last Commit</b>: [Placeholder for the last pull commit message.]
      </p>
      <p class=""github-link"">
         <a href=""[github link]"">Github Link</a>
      </p>
    </div>";

var projectStringBuilder = new StringBuilder();

// Fetch all repositories for the current authenticated user
var repositories = await client.Repository.GetAllForCurrent();

foreach (var repo in repositories.Where(i => !i.Private && !i.Fork))
{
  // Set name
  var projectString = projectTemplate.Replace("[Project Name Placeholder]", repo.Name);

  // Fetch the languages used in each repository
  // Skip for now
  var languages = await client.Repository.GetAllLanguages(repo.Owner.Login, repo.Name);

  if (languages.Count > 0)
  {
    // <span class=""language language-csharp"">C#</span>
    var projectLanguages = new StringBuilder();

    var totalBytes = languages.Sum(i => i.NumberOfBytes);

    foreach (var language in languages)
    {
      var percentage = (((float)language.NumberOfBytes / (float)totalBytes) * 100f).ToString("0.00");

      Console.WriteLine(percentage.ToString());

      if (!languageDictionary.ContainsKey(language.Name))
      {
        projectLanguages.AppendLine(@$"<span class=""language language-unknown"">{language.Name} ({percentage}%)</span>");
      }
      else
      { 
        projectLanguages.AppendLine(@$"<span class=""language {languageDictionary[language.Name]}"">{language.Name} ({percentage}%)</span>");
      }
    }

    if (projectLanguages.Length > 0)
    {
      projectString = projectString.Replace("[ProjectLanguages]", projectLanguages.ToString());
    }
    else
    {
      projectString =  projectString.Replace("[ProjectLanguages]", @$"<span class=""language language-unknown"">No Language probally configuration</span>");
    }
  }
  else
  {
    projectString = projectString.Replace("[ProjectLanguages]", @$"<span class=""language language-unknown"">No Language probally configuration</span>");
  }
  var description = repo.Description;

  if (!string.IsNullOrEmpty(description))
  {
    projectString = projectString.Replace("[This is a description placeholder]", $"<h3>{description}</h3>");
  }
  else
  {
    projectString = projectString.Replace("[This is a description placeholder]", "<h3>No description found</h3>");
  }

  var branch = await client.Git.Reference.Get(repo.Owner.Login, repo.Name, $"heads/{repo.DefaultBranch}");

  var lastCommitMessage = await client.Repository.Commit.Get(repo.Owner.Login, repo.Name, branch.Object.Sha);

  if (lastCommitMessage != null)
  {
    var commit = lastCommitMessage.Commit.Message;
    projectString = projectString.Replace("[Placeholder for the last pull commit message.]", commit);
  }
  else
  {
    projectString = projectString.Replace("[Placeholder for the last pull commit message.]", "None");
  }

  projectString = projectString.Replace("[github link]", repo.HtmlUrl);
  projectStringBuilder.AppendLine(projectString);
}

var copy = File.ReadAllText("Template/template-index.html");

var content = copy.Replace("<!--REPLACE_ME-->",projectStringBuilder.ToString());

File.WriteAllText($"Template/index.html",content);

Console.WriteLine("Done");