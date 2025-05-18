using Octokit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitHubService
{
    public interface IGitHubService
    {
        Task<IEnumerable<RepositoryInfo>> GetPortfolioAsync();
        Task<IEnumerable<object>> SearchRepositoriesAsync(string name, string language, string username);
    }

    public class RepositoryInfo
    {
        public string Name { get; set; }
        public string HtmlUrl { get; set; }
        public string Language { get; set; }
        public int Stars { get; set; }
        public int PullRequests { get; set; }
        public string LastCommitDate { get; set; }
    }
}
