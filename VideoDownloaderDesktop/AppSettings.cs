using System.Text.Json;

namespace VideoDownloaderDesktop;

internal static class AppSettings
{
    private static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static string GetDefaultCookiesRelativePath() => Path.Combine("lib", "cookies.txt");

    public static string GetVersionLabel()
    {
        var version = Load().Version?.Trim();
        return string.IsNullOrWhiteSpace(version) ? "Versão não configurada" : $"v{version}";
    }

    public static string? GetLastOutputFolder()
    {
        var folder = Load().LastOutputFolder?.Trim();
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return null;
        }

        return folder;
    }

    public static void SaveLastOutputFolder(string folderPath)
    {
        var settings = Load();
        settings.LastOutputFolder = folderPath;
        Save(settings);
    }

    public static void SaveCookiesFromBrowser(string browser)
    {
        var settings = Load();
        settings.CookiesFromBrowser = browser;
        Save(settings);
    }

    public static string GetCookiesFromBrowser()
    {
        var browser = Load().CookiesFromBrowser?.Trim();
        return string.IsNullOrWhiteSpace(browser) ? "chrome" : browser;
    }

    public static string GetCookiesFromBrowserArgument(string? browser = null)
    {
        browser = string.IsNullOrWhiteSpace(browser) ? GetCookiesFromBrowser() : browser.Trim();
        var profile = Load().CookiesBrowserProfile?.Trim();
        if (string.IsNullOrWhiteSpace(profile))
        {
            profile = "Default";
        }

        return $"{browser}:{profile}";
    }

    public static string GetCookiesSourceMessage(string? browser = null)
    {
        if (TryGetCookiesFilePath(out var path))
        {
            return $"Usando arquivo de cookies: {path}";
        }

        return $"Usando cookies do navegador: {GetCookiesFromBrowserArgument(browser)}";
    }

    public static string GetExpectedCookiesFilePath()
    {
        var configured = Load().CookiesFile?.Trim();
        return ResolveCookiesFilePath(configured);
    }

    public static bool TryGetCookiesFilePath(out string path)
    {
        path = GetExpectedCookiesFilePath();
        return File.Exists(path);
    }

    private static string ResolveCookiesFilePath(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = GetDefaultCookiesRelativePath();
        }
        else
        {
            configured = configured
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
        }

        return Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configured));
    }

    public static bool UsesBrowserCookies() => !TryGetCookiesFilePath(out _);

    private static AppConfig Load()
    {
        if (!File.Exists(ConfigPath))
        {
            return new AppConfig();
        }

        try
        {
            return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath)) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    private static void Save(AppConfig settings)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(settings, options));
    }

    private sealed class AppConfig
    {
        public string? Version { get; set; }
        public string? LastOutputFolder { get; set; }
        public string? CookiesFromBrowser { get; set; }
        public string? CookiesBrowserProfile { get; set; }
        public string? CookiesFile { get; set; }
    }
}
