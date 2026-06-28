using Microsoft.Extensions.Http;

namespace InfoTrack.Tests.Integration;

/// <summary>
/// Replaces the primary handler for named HttpClients in integration tests.
/// </summary>
public sealed class StubHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
{
    private readonly List<(string ClientNamePart, HttpMessageHandler Handler)> _handlers = [];

    public void RegisterHandler(string clientNamePart, HttpMessageHandler handler) =>
        _handlers.Add((clientNamePart, handler));

    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) =>
        builder =>
        {
            next(builder);

            foreach (var (clientNamePart, handler) in _handlers)
            {
                if (builder.Name is not null &&
                    builder.Name.Contains(clientNamePart, StringComparison.Ordinal))
                {
                    builder.PrimaryHandler = handler;
                    break;
                }
            }
        };
}
