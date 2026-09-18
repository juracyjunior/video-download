namespace VideoDownloaderDesktop;

internal static class CookieFileHelper
{
    public static bool IsInstagramUrl(string videoUrl)
    {
        return Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri)
            && uri.Host.Contains("instagram.com", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsYouTubeUrl(string videoUrl)
    {
        if (!Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase);
    }

    public static (string Url, bool WasAdjusted) AdjustInstagramReelsUrl(string videoUrl)
    {
        if (!IsInstagramUrl(videoUrl)
            || !videoUrl.Contains("/reels/", StringComparison.OrdinalIgnoreCase))
        {
            return (videoUrl, false);
        }

        return (videoUrl.Replace("/reels/", "/reel/", StringComparison.OrdinalIgnoreCase), true);
    }

    public static bool RequiresAuthRetry(string output)
    {
        return output.Contains("login required", StringComparison.OrdinalIgnoreCase)
            || output.Contains("empty media response", StringComparison.OrdinalIgnoreCase)
            || output.Contains("Instagram sent an empty", StringComparison.OrdinalIgnoreCase)
            || output.Contains("--cookies for the authentication", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsHttp403(string output)
    {
        return output.Contains("HTTP Error 403", StringComparison.OrdinalIgnoreCase)
            || output.Contains("403: Forbidden", StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasInstagramSession(string cookiesFilePath)
    {
        return TryFindCookieValue(cookiesFilePath, "instagram.com", "sessionid", out var sessionId)
            && !string.IsNullOrWhiteSpace(sessionId);
    }

    public static string CreateFilteredCookiesFile(string sourceCookiesPath, string siteHost)
    {
        var lines = File.ReadAllLines(sourceCookiesPath);
        var selected = new List<string>
        {
            "# Netscape HTTP Cookie File",
            "# Filtered by Social Video Downloader"
        };

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split('\t');
            if (parts.Length < 7)
            {
                continue;
            }

            var domain = parts[0].Trim();
            var name = parts[5].Trim();
            if (!domain.Contains(siteHost, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Prefer cookies importantes; inclui o restante do domínio para manter sessão completa.
            var key = $"{domain}|{parts[2]}|{name}";
            if (!seen.Add(key))
            {
                continue;
            }

            selected.Add(line);
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"svd-{siteHost.Replace('.', '-')}-cookies.txt");
        File.WriteAllLines(tempPath, selected);
        return tempPath;
    }

    public static string DescribeInstagramCookies(string cookiesFilePath)
    {
        if (!File.Exists(cookiesFilePath))
        {
            return "Arquivo de cookies não encontrado.";
        }

        var hasSession = TryFindCookieValue(cookiesFilePath, "instagram.com", "sessionid", out var sessionId);
        var hasUser = TryFindCookieValue(cookiesFilePath, "instagram.com", "ds_user_id", out var userId);

        if (!hasSession)
        {
            return "Cookies do Instagram sem sessionid. Exporte novamente estando logado em instagram.com.";
        }

        var preview = sessionId!.Length <= 24 ? sessionId : sessionId[..24] + "...";
        return hasUser
            ? $"Cookies Instagram OK (usuário {userId}, sessionid {preview})"
            : $"Cookies Instagram com sessionid ({preview}), mas sem ds_user_id.";
    }

    private static bool TryFindCookieValue(string cookiesFilePath, string siteHost, string cookieName, out string? value)
    {
        value = null;
        foreach (var line in File.ReadLines(cookiesFilePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split('\t');
            if (parts.Length < 7)
            {
                continue;
            }

            if (!parts[0].Contains(siteHost, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!parts[5].Equals(cookieName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = parts[6].Trim();
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }
}
