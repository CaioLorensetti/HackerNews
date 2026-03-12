using System.Diagnostics.Metrics;

namespace HackerNewsApi.Diagnostics;

public sealed class HackerNewsMetrics
{
    public const string MeterName = "HackerNewsApi";

    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Counter<long> _upstreamCalls;

    public HackerNewsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _cacheHits = meter.CreateCounter<long>(
            "hackernews.cache.hits",
            description: "Cache hits, tagged by cache_type (ids | item).");

        _cacheMisses = meter.CreateCounter<long>(
            "hackernews.cache.misses",
            description: "Cache misses, tagged by cache_type (ids | item).");

        _upstreamCalls = meter.CreateCounter<long>(
            "hackernews.upstream.calls",
            description: "Total outbound calls to the Hacker News API.");
    }

    public void RecordCacheHit(string cacheType) =>
        _cacheHits.Add(1, new KeyValuePair<string, object?>("cache_type", cacheType));

    public void RecordCacheMiss(string cacheType) =>
        _cacheMisses.Add(1, new KeyValuePair<string, object?>("cache_type", cacheType));

    public void RecordUpstreamCall() =>
        _upstreamCalls.Add(1);
}
