using GitHubService;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CVSite.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GitHubController : ControllerBase
    {
        private readonly IGitHubService _service;

        public GitHubController(IGitHubService service)
        {
            _service = service;
        }

        [HttpGet("portfolio")]
        public async Task<IActionResult> GetPortfolio()
        {
            var data = await _service.GetPortfolioAsync();
            return Ok(data);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string name = null, string language = null, string username = null)
        {
            var data = await _service.SearchRepositoriesAsync(name, language, username);
            return Ok(data);
        }
    }
}
