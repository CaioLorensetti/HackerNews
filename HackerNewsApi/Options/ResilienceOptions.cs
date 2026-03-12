namespace HackerNewsApi.Options;

public sealed class ResilienceOptions
{
    public int RetryCount { get; init; } = 3;
    public double RetryBaseDelaySeconds { get; init; } = 1;

    public double CircuitBreakerFailureRatio { get; init; } = 0.5;
    public int CircuitBreakerMinimumThroughput { get; init; } = 5;
    public double CircuitBreakerSamplingDurationSeconds { get; init; } = 30;
    public double CircuitBreakerBreakDurationSeconds { get; init; } = 15;

    public int BulkheadMaxConcurrency { get; init; } = 10;
    public int BulkheadQueueLimit { get; init; } = 20;

    public double TimeoutSeconds { get; init; } = 10;
}
