using HackerNewsApi.Options;
using HackerNewsApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.Configure<HackerNewsOptions>(
    builder.Configuration.GetSection(HackerNewsOptions.SectionName));
builder.Services.AddHttpClient<IHackerNewsService, HackerNewsService>();

var app = builder.Build();

app.MapGet("/beststories/{n:int}", async (int n, IHackerNewsService service, CancellationToken ct) =>
{
    if (n <= 0)
        return Results.BadRequest("n must be a positive digit.");

    var stories = await service.GetBestStoriesAsync(n, ct);
    return Results.Ok(stories);
});

app.Run();
