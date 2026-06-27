using System.Security.Cryptography;
using System.Text;

namespace InfoTrack.Domain.Common;

public static class SolicitorIdentity
{
    public static string CreateKey(string firmName, string? address, string? phone)
    {
        var normalised = $"{Normalise(firmName)}|{Normalise(address)}|{Normalise(phone)}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalised));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Normalise(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
}
