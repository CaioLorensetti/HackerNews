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
  "ItemAbsoluteExpirationSeconds": 900,
  "Resilience": {
    "RetryCount": 3,
    "RetryBaseDelaySeconds": 1,
    "CircuitBreakerFailureRatio": 0.5,
    "CircuitBreakerMinimumThroughput": 5,
    "CircuitBreakerSamplingDurationSeconds": 30,
    "CircuitBreakerBreakDurationSeconds": 15,
    "BulkheadMaxConcurrency": 10,
    "BulkheadQueueLimit": 20
  }
}
```

## Resilience pipeline

Outbound calls to Hacker News go through a three-layer pipeline defined in `Extensions/HttpClientBuilderExtensions.cs`. The layers are applied outermost-first, so each one wraps everything inside it.

### Concurrency limiter (bulkhead)

The outermost layer caps the number of in-flight requests to Hacker News at any point in time (`BulkheadMaxConcurrency`, default: 10). Requests that exceed that limit are queued up to `BulkheadQueueLimit` (default: 20); anything beyond the queue is rejected immediately with an exception.

The reason this sits outside the retry layer is important: without it, a retry policy that fires three times per request would triple the effective concurrency against the upstream, which is exactly what a bulkhead is meant to prevent.

### Retry with exponential backoff and jitter

The middle layer retries on transient failures — 5xx responses, 408 Request Timeout, and network-level errors. It attempts up to `RetryCount` additional tries (default: 3) with delays that grow exponentially from `RetryBaseDelaySeconds` (default: 1 s). Jitter is added to each delay so that a burst of concurrent clients retrying at the same time does not produce a synchronized thundering-herd against a recovering upstream.

### Circuit breaker

The innermost layer monitors the ratio of failed calls over a rolling `CircuitBreakerSamplingDurationSeconds` window (default: 30 s). When the failure ratio reaches `CircuitBreakerFailureRatio` (default: 0.5, i.e. 50%) and at least `CircuitBreakerMinimumThroughput` calls (default: 5) have been made in that window, the circuit opens and all subsequent calls fail immediately for `CircuitBreakerBreakDurationSeconds` (default: 15 s). After the break the circuit half-opens, lets one probe request through, and closes again on success or re-opens on failure.

This prevents the retry layer from continuing to hammer a struggling or down upstream during an outage. Requests fail fast while the circuit is open, which frees threads on the API side and gives Hacker News time to recover.

## Running locally

```bash
cd HackerNewsApi
dotnet run
```
