using HackerNewsApi.Options;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace HackerNewsApi.Extensions;

internal static class HttpClientBuilderExtensions
{
    internal static IHttpClientBuilder AddHackerNewsResilience(this IHttpClientBuilder builder)
    {
        builder.AddResilienceHandler("hn-pipeline", (pipeline, context) =>
        {
            var opts = context.ServiceProvider
                .GetRequiredService<IOptions<HackerNewsOptions>>().Value.Resilience;

            pipeline.AddTimeout(TimeSpan.FromSeconds(opts.TimeoutSeconds));

            pipeline.AddConcurrencyLimiter(opts.BulkheadMaxConcurrency, opts.BulkheadQueueLimit);

            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = opts.RetryCount,
                Delay = TimeSpan.FromSeconds(opts.RetryBaseDelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
            });

            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = opts.CircuitBreakerFailureRatio,
                MinimumThroughput = opts.CircuitBreakerMinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(opts.CircuitBreakerSamplingDurationSeconds),
                BreakDuration = TimeSpan.FromSeconds(opts.CircuitBreakerBreakDurationSeconds),
            });
        });
        return builder;
    }
}
