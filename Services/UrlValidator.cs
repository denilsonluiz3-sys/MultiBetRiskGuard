using System.Net;

namespace MULTI_Bet_playing_Demo.Services;

public static class UrlValidator
{
    private static readonly HashSet<string> BlockedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "javascript", "data", "file", "content", "about", "blob", "intent", "market", "android-app", "chrome", "chrome-extension"
    };

    public static bool TryNormalize(string? input, out string normalized, out string error)
    {
        normalized = string.Empty;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(input)) { error = "URL vazia."; return false; }

        var raw = input.Trim();
        var schemePart = raw.Split(':', 2)[0];
        if (BlockedSchemes.Contains(schemePart)) { error = "Scheme de URL não permitido."; return false; }
        if (!raw.Contains("://", StringComparison.Ordinal)) raw = "https://" + raw;
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)) { error = "URL inválida."; return false; }
        if (uri.Scheme is not ("https" or "http")) { error = "Use apenas http:// ou https://."; return false; }
        if (string.IsNullOrWhiteSpace(uri.Host) || uri.Host.Contains(' ')) { error = "Host inválido."; return false; }
        if (IsLocalOrPrivateHost(uri)) { error = "Endereços locais ou privados não são permitidos."; return false; }
        normalized = uri.AbsoluteUri;
        return true;
    }

    public static bool IsHttpsPreferred(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Scheme == "https";

    private static bool IsLocalOrPrivateHost(Uri uri)
    {
        if (uri.IsLoopback) return true;
        if (!IPAddress.TryParse(uri.Host, out var address))
            return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                   uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
                   uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase);

        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)) return true;

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            var first = b[0]; var second = b[1];
            return first == 10 || (first == 172 && second is >= 16 and <= 31) ||
                   (first == 192 && second == 168) || (first == 169 && second == 254) ||
                   first == 127 || first == 0 || first >= 224;
        }
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast || address.IsIPv6UniqueLocal;
        return false;
    }
}
