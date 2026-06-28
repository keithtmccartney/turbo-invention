namespace InfoTrack.Domain.Discovery;

public static class LocationSlug
{
    public static string FromName(string name) =>
        name.Trim().ToLowerInvariant().Replace(' ', '-');

    public static string Normalize(string slug) =>
        slug.Trim().ToLowerInvariant();
}
