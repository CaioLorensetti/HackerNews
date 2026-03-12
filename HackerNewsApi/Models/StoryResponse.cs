namespace HackerNewsApi.Models;

public sealed class StoryResponse
{
    public string Title { get; init; } = string.Empty;
    public string? Uri { get; init; }
    public string? PostedBy { get; init; }
    public DateTimeOffset Time { get; init; }
    public int Score { get; init; }
    public int CommentCount { get; init; }

    public static StoryResponse FromItem(HackerNewsItem item) => new()
    {
        Title = item.Title ?? string.Empty,
        Uri = item.Url,
        PostedBy = item.By,
        Time = DateTimeOffset.FromUnixTimeSeconds(item.Time),
        Score = item.Score,
        CommentCount = item.Descendants,
    };
}
