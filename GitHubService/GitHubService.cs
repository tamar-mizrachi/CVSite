using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Octokit;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Octokit.Internal;

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
                    // שפות בפורמט Dictionary<string, long> (שפה + שורות קוד)
                    var languages = await _client.Repository.GetAllLanguages(repo.Owner.Login, repo.Name);

                    // מספר Pull Requests פתוחים (אפשר גם לסנן לפי מצב אם רוצים)
                    var pulls = await _client.PullRequest.GetAllForRepository(repo.Id);

                    // ניסיון לקבל את הקומיט האחרון - משתמשים בדף ראשון של קומיטים (HEAD)
                    string lastCommitDate = "N/A";
                    try
                    {
                        var commits = await _client.Repository.Commit.GetAll(repo.Owner.Login, repo.Name, new CommitRequest { Sha = repo.DefaultBranch }, new ApiOptions { PageSize = 1 });
                        var lastCommit = commits.FirstOrDefault();
                        if (lastCommit != null)
                            lastCommitDate = lastCommit.Commit.Committer.Date.ToString("yyyy-MM-dd");
                    }
                    catch (ApiException ex)
                    {
                        // במקרה שהריפוזיטורי ריק או בעיה אחרת - אפשר ללוג או להשאיר N/A
                    }

                    // קביעת שפת פיתוח עיקרית מהשפות שנמצאו (השפה עם הכי הרבה קוד)
                    string mainLanguage = repo.Language; // ברירת מחדל
                    if (languages != null && languages.Any())
                    {
                        mainLanguage = languages.OrderByDescending(l => l.Name).First().Name;
                    }

                    repoInfos.Add(new RepositoryInfo
                    {
                        Name = repo.Name,
                        HtmlUrl = repo.HtmlUrl,
                        Language = mainLanguage,
                        Stars = repo.StargazersCount,
                        PullRequests = pulls.Count,
                        LastCommitDate = lastCommitDate
                    });
                }

                return repoInfos;
            });
        }

        public async Task<IEnumerable<object>> SearchRepositoriesAsync(string name, string language, string username)
        {
            var request = new SearchRepositoriesRequest(name ?? "")
            {
                Language = string.IsNullOrEmpty(language) ? null : Enum.TryParse<Language>(language, true, out var lang) ? lang : (Language?)null,
                User = string.IsNullOrEmpty(username) ? null : username,
                // אפשר להוסיף סינון נוסף כמו Sort, Order וכו'
            };

            var result = await _client.Search.SearchRepo(request);
            var result1 = result.Items.Select(repo => new
            {
                repo.Name,
                repo.Description,
                repo.Language,
                repo.StargazersCount,
                repo.HtmlUrl
            });

            return result1;
        }
    }
}
