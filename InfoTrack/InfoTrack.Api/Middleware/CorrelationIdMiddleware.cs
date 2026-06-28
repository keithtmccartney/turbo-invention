using System.Diagnostics;

namespace InfoTrack.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        }

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using var scope = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Correlation")
            .BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId });

        Activity.Current?.SetTag("correlation.id", correlationId);

        await next(context);
    }
}

public static class CorrelationIdExtensions
{
    public static string GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var value) && value is string correlationId
            ? correlationId
            : Guid.NewGuid().ToString("N");
}
