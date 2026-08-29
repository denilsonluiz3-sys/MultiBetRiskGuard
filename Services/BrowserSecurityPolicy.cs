namespace MULTI_Bet_playing_Demo.Services;

/// <summary>Centralized defense-in-depth policy for browser navigation.</summary>
public static class BrowserSecurityPolicy
{
    public static bool IsAllowedNavigation(string? url, out string normalized)
    {
        normalized = string.Empty;
        if (!UrlValidator.TryNormalize(url, out normalized, out _)) return false;
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme is "https" or "http" && !string.IsNullOrWhiteSpace(uri.Host);
    }

    public static bool IsAllowedExternalTarget(string? url)
    {
        return IsAllowedNavigation(url, out _);
    }
}
