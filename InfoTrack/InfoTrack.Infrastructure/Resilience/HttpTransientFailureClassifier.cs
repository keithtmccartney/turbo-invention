using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Polly.Timeout;

namespace InfoTrack.Infrastructure.Resilience;

/// <summary>
/// Centralises retry eligibility so every outbound pipeline applies the same rules.
/// Parsing failures and client errors are never retried — only transport/transient HTTP faults.
/// </summary>
public static class HttpTransientFailureClassifier
{
    private static readonly HashSet<HttpStatusCode> TransientStatusCodes =
    [
        HttpStatusCode.RequestTimeout,           // 408
        HttpStatusCode.TooManyRequests,          // 429
        HttpStatusCode.InternalServerError,      // 500
        HttpStatusCode.BadGateway,               // 502
        HttpStatusCode.ServiceUnavailable,       // 503
        HttpStatusCode.GatewayTimeout,           // 504
    ];

    private static readonly HashSet<HttpStatusCode> NonRetryableClientErrors =
    [
        HttpStatusCode.BadRequest,               // 400
        HttpStatusCode.Unauthorized,             // 401
        HttpStatusCode.Forbidden,                // 403
        HttpStatusCode.NotFound,                 // 404
    ];

    public static bool IsTransientHttpStatus(HttpStatusCode statusCode) =>
        TransientStatusCodes.Contains(statusCode);

    public static bool IsNonRetryableClientError(HttpStatusCode statusCode) =>
        NonRetryableClientErrors.Contains(statusCode);

    public static bool ShouldRetryHttpResponse(HttpResponseMessage response) =>
        IsTransientHttpStatus(response.StatusCode);

    public static bool ShouldRetryException(Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        return exception switch
        {
            HttpRequestException => true,
            SocketException => true,
            TimeoutRejectedException => true,
            TaskCanceledException canceled when !cancellationToken.IsCancellationRequested => true,
            _ => false,
        };
    }

    public static string DescribeHttpStatus(HttpStatusCode statusCode) =>
        IsNonRetryableClientError(statusCode)
            ? "non-retryable client error"
            : IsTransientHttpStatus(statusCode)
                ? "transient HTTP status"
                : "non-retryable HTTP status";

    public static string DescribeException(Exception exception) =>
        exception switch
        {
            HttpRequestException httpEx => $"HTTP transport failure ({httpEx.Message})",
            SocketException socketEx => $"socket failure ({socketEx.SocketErrorCode})",
            TimeoutRejectedException => "attempt timeout",
            TaskCanceledException => "request timeout",
            _ => exception.GetType().Name,
        };
}
