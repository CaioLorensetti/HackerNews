namespace HackerNewsApi.Options;

public sealed class HackerNewsOptions
{
    public const string SectionName = "HackerNews";

    public string BestStoriesUrl { get; init; } = string.Empty;
    public string ItemUrl { get; init; } = string.Empty;
    public int IdsCacheDurationSeconds { get; init; } = 300;
    public int ItemCacheDurationSeconds { get; init; } = 900;
}
