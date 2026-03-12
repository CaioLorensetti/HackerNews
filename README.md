# HackerNews Best Stories API

A .NET 10 Minimal Web API that retrieves details of the first n "best stories" from the [Hacker News API](https://hacker-news.firebaseio.com).

## Endpoint

```
GET /beststories/{n}
```

Returns the first `n` best stories sorted by score in descending order.

### Response shape

```json
[
  {
    "title": "Don't post generated/AI-edited comments. HN is for conversation between humans",
    "uri": "https://news.ycombinator.com/newsguidelines.html#generated",
    "postedBy": "usefulposter",
    "time": "2026-03-11T19:29:29+00:00",
    "score": 4070,
    "commentCount": 1554
  },
]
```

## Caching strategy

Two separate cache policies protect the Hacker News API from being overloaded under high request volume.

### Best story IDs — absolute expiration

The list of best story IDs (`/v0/beststories.json`) is cached with a plain absolute expiration controlled by `IdsCacheDurationSeconds` (default: 300 s). An absolute policy is appropriate here because the list itself changes on Hacker News' side; keeping it longer than necessary would serve a stale ranking.

### Individual story details — sliding + absolute expiration

Each story item (`/v0/item/{id}.json`) is cached with two overlapping policies:

- **Sliding expiration** (`ItemSlidingExpirationSeconds`, default: 300 s) — the TTL is reset on every cache hit. A story that is being frequently requested stays in memory without triggering a round-trip to Hacker News.
- **Absolute expiration** (`ItemAbsoluteExpirationSeconds`, default: 900 s) — regardless of access frequency, the entry is evicted after this ceiling. This prevents perpetually hot items from serving data that has grown significantly stale.

A story that is never requested again evicts after 5 minutes of inactivity. A story that is requested continuously evicts at most 15 minutes after it was first cached.

### Configuration

All durations are configurable in `appsettings.json` without recompiling:

```json
"HackerNews": {
  "BestStoriesUrl": "https://hacker-news.firebaseio.com/v0/beststories.json",
  "ItemUrl": "https://hacker-news.firebaseio.com/v0/item/{0}.json",
  "IdsCacheDurationSeconds": 300,
  "ItemSlidingExpirationSeconds": 300,
  "ItemAbsoluteExpirationSeconds": 900
}
```

## Running locally

```bash
cd HackerNewsApi
dotnet run
```
