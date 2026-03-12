using System.Collections.Concurrent;
using System.Net.Http.Json;
using HackerNewsApi.Models;
using HackerNewsApi.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HackerNewsApi.Services;

public sealed class HackerNewsService(HttpClient http, IMemoryCache cache, IOptions<HackerNewsOptions> options) : IHackerNewsService
{
    private readonly HackerNewsOptions _options = options.Value;

    private TimeSpan IdsCacheDuration => TimeSpan.FromSeconds(_options.IdsCacheDurationSeconds);

    private MemoryCacheEntryOptions ItemCacheOptions => new MemoryCacheEntryOptions()
        .SetSlidingExpiration(TimeSpan.FromSeconds(_options.ItemSlidingExpirationSeconds))
        .SetAbsoluteExpiration(TimeSpan.FromSeconds(_options.ItemAbsoluteExpirationSeconds));

    public async Task<IReadOnlyList<StoryResponse>> GetBestStoriesAsync(int count, CancellationToken ct = default)
    {
        var ids = await GetBestStoryIdsAsync(ct);

        var top = ids.Take(count);

        var stories = new ConcurrentBag<StoryResponse>();
        await Parallel.ForEachAsync(top, new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.Resilience.BulkheadMaxConcurrency,
            CancellationToken = ct,
        }, async (id, token) =>
        {
            var story = await GetStoryAsync(id, token);
            if (story is not null)
                stories.Add(story);
        });

        return stories
            .OrderByDescending(s => s.Score)
            .ToList();
    }

    private async Task<int[]> GetBestStoryIdsAsync(CancellationToken ct)
    {
        if (cache.TryGetValue<int[]>("best_story_ids", out var cached) && cached is not null)
            return cached;

        var ids = await http.GetFromJsonAsync<int[]>(_options.BestStoriesUrl, ct)
            ?? Array.Empty<int>();

        cache.Set("best_story_ids", ids, IdsCacheDuration);
        return ids;
    }

    private async Task<StoryResponse?> GetStoryAsync(int id, CancellationToken ct)
    {
        var cacheKey = $"story_{id}";

        if (cache.TryGetValue<StoryResponse>(cacheKey, out var cached))
            return cached;

        var item = await http.GetFromJsonAsync<HackerNewsItem>(
            string.Format(_options.ItemUrl, id), ct);

        if (item is null)
            return null;

        var story = StoryResponse.FromItem(item);
        cache.Set(cacheKey, story, ItemCacheOptions);
        return story;
    }
}
