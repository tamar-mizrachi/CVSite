using GitHubService;
//using GitHubServiceNamespace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GitHubApiOptions>(builder.Configuration.GetSection("GitHub"));
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IGitHubService, GitHubService.GitHubService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
