using HackerNewsApi.Models;

namespace HackerNewsApi.Services;

public interface IHackerNewsService
{
    Task<IReadOnlyList<StoryResponse>> GetBestStoriesAsync(int count, CancellationToken ct = default);
}
