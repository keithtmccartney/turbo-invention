using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace InfoTrack.Api.RateLimiting;

public static class InboundRateLimitingExtensions
{
    public static IServiceCollection AddInfoTrackInboundRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<InboundRateLimitOptions>(configuration.GetSection(InboundRateLimitOptions.SectionName));

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiterOptions.OnRejected = OnRejectedAsync;

            limiterOptions.AddPolicy(InboundRateLimitPolicies.ApiReads, CreateReadPolicy);
            limiterOptions.AddPolicy(InboundRateLimitPolicies.ApiWrites, CreateWritePolicy);
        });

        return services;
    }

    private static RateLimitPartition<string> CreateReadPolicy(HttpContext httpContext) =>
        CreatePolicy(httpContext, options => (options.ReadPermitLimit, options.ReadWindowSeconds, options.ReadQueueLimit));

    private static RateLimitPartition<string> CreateWritePolicy(HttpContext httpContext) =>
        CreatePolicy(httpContext, options => (options.WritePermitLimit, options.WriteWindowSeconds, options.WriteQueueLimit));

    private static RateLimitPartition<string> CreatePolicy(
        HttpContext httpContext,
        Func<InboundRateLimitOptions, (int PermitLimit, int WindowSeconds, int QueueLimit)> selector)
    {
        var options = httpContext.RequestServices.GetRequiredService<IOptionsMonitor<InboundRateLimitOptions>>().CurrentValue;

        if (!options.Enabled)
        {
            return RateLimitPartition.GetNoLimiter(ResolveClientPartitionKey(httpContext));
        }

        var (permitLimit, windowSeconds, queueLimit) = selector(options);
        return CreatePartition(httpContext, permitLimit, windowSeconds, queueLimit);
    }

    private static RateLimitPartition<string> CreatePartition(
        HttpContext httpContext,
        int permitLimit,
        int windowSeconds,
        int queueLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            ResolveClientPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = Math.Max(1, permitLimit),
                Window = TimeSpan.FromSeconds(Math.Max(1, windowSeconds)),
                QueueLimit = Math.Max(0, queueLimit),
            });

    internal static string ResolveClientPartitionKey(HttpContext httpContext)
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var clientIp = forwardedFor.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(clientIp))
            {
                return clientIp;
            }
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static async ValueTask OnRejectedAsync(
        OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        var options = httpContext.RequestServices.GetRequiredService<IOptions<InboundRateLimitOptions>>().Value;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            httpContext.Response.Headers.RetryAfter =
                options.ReadWindowSeconds.ToString(CultureInfo.InvariantCulture);
        }

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await httpContext.Response.WriteAsJsonAsync(
            new
            {
                title = "Too many requests",
                detail = "Inbound rate limit exceeded. Retry after the Retry-After interval.",
                status = StatusCodes.Status429TooManyRequests,
            },
            cancellationToken);
    }
}

public static class InboundRateLimitingEndpointExtensions
{
    public static TBuilder WithApiReadRateLimit<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireRateLimiting(InboundRateLimitPolicies.ApiReads);

    public static TBuilder WithApiWriteRateLimit<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireRateLimiting(InboundRateLimitPolicies.ApiWrites);
}
