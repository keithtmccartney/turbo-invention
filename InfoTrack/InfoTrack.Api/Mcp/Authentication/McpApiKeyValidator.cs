using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace InfoTrack.Api.Mcp.Authentication;

/// <summary>
/// Validates <c>Authorization: Bearer &lt;ApiKey&gt;</c> for MCP endpoints using constant-time comparison.
/// </summary>
public sealed class McpApiKeyValidator(IOptions<McpOptions> options)
{
    public bool TryValidateAuthorizationHeader(
        string? authorizationHeader,
        out string? failureReason)
    {
        var mcpOptions = options.Value;
        if (!mcpOptions.Enabled)
        {
            failureReason = "MCP is disabled.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(mcpOptions.ApiKey))
        {
            failureReason = "MCP API key is not configured.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(authorizationHeader)
            || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            failureReason = "Authorization header must use Bearer scheme.";
            return false;
        }

        var providedKey = authorizationHeader["Bearer ".Length..].Trim();
        var expectedBytes = Encoding.UTF8.GetBytes(mcpOptions.ApiKey);
        var providedBytes = Encoding.UTF8.GetBytes(providedKey);

        if (expectedBytes.Length != providedBytes.Length
            || !CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
        {
            failureReason = "Invalid MCP API key.";
            return false;
        }

        failureReason = null;
        return true;
    }
}
