using HackerNewsApi.Diagnostics;
using HackerNewsApi.Extensions;
using HackerNewsApi.Options;
using HackerNewsApi.Services;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.Configure<HackerNewsOptions>(
    builder.Configuration.GetSection(HackerNewsOptions.SectionName));
builder.Services.AddSingleton<HackerNewsMetrics>();
builder.Services.AddHttpClient<IHackerNewsService, HackerNewsService>()
    .AddHackerNewsResilience();

var app = builder.Build();

app.MapGet("/beststories/{n:int}", async (int n, IHackerNewsService service, CancellationToken ct) =>
{
    if (n <= 0)
        return Results.BadRequest("n must be a positive digit.");

    try
    {
        var stories = await service.GetBestStoriesAsync(n, ct);
        return Results.Ok(stories);
    }
    catch (Exception ex) when (ex is TimeoutRejectedException
                                   or BrokenCircuitException
                                   or RateLimiterRejectedException)
    {
        return Results.StatusCode(503);
    }
});

app.Run();
