using Microsoft.Extensions.Configuration;
using Octokit;
using WebPageCreatorPOC;

// Replace with your GitHub personal access token
var configuration = new ConfigurationBuilder()
                        .AddUserSecrets<Program>()
                        .Build();

var personalAccessToken = configuration["AccessToken"];

// Initialize the GitHub client with the token
var client = new GitHubClient(new ProductHeaderValue("MyApp"));
var tokenAuth = new Credentials(personalAccessToken);
client.Credentials = tokenAuth;

var generator = new GithubWebPageGenerator(client);

await generator.GenerateAsync();
