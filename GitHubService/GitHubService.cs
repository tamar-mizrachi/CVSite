using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Octokit;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace GitHubService
{
    public class GitHubService : IGitHubService
    {
        private readonly GitHubApiOptions _options;
        private readonly GitHubClient _client;
        private readonly IMemoryCache _cache;

        public GitHubService(IOptions<GitHubApiOptions> options, IMemoryCache cache)
        {
            _options = options.Value;
            _client = new GitHubClient(new ProductHeaderValue("CVSite"));
            _client.Credentials = new Credentials(_options.Token);
            _cache = cache;
        }

        public async Task<IEnumerable<RepositoryInfo>> GetPortfolioAsync()
        {
            return await _cache.GetOrCreateAsync("portfolio_cache", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                var repos = await _client.Repository.GetAllForUser(_options.Username);

                var repoInfos = new List<RepositoryInfo>();

                foreach (var repo in repos)
                {
                    var languages = await _client.Repository.GetAllLanguages(repo.Owner.Login, repo.Name);
                    var pulls = await _client.PullRequest.GetAllForRepository(repo.Id);
                    var commits = await _client.Repository.Commit.GetAll(repo.Id);
                    var lastCommit = commits.FirstOrDefault()?.Commit?.Committer?.Date.ToString("yyyy-MM-dd") ?? "N/A";
                    string mainLanguage = repo.Language;

                    //string mainLanguage = repo.Language;

                    if (languages != null && languages.Any())
                    {
                        mainLanguage = languages.FirstOrDefault()?.Name ?? repo.Language;
                    }


                    repoInfos.Add(new RepositoryInfo
                    {

                        Name = repo.Name,
                        HtmlUrl = repo.HtmlUrl,
                        Language = mainLanguage,
                        Stars = repo.StargazersCount,
                        PullRequests = pulls.Count,
                        LastCommitDate = lastCommit
                    });
                }

                return repoInfos;
            });
        }

        public async Task<IEnumerable<Repository>> SearchRepositoriesAsync(string name, string language, string username)
        {
            var request = new SearchRepositoriesRequest(name ?? "")
            {
                Language = string.IsNullOrEmpty(language) ? null : new Language(language),
                User = username
            };

            var result = await _client.Search.SearchRepo(request);
            return result.Items;
        }
    }
}
