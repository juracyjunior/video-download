namespace VideoDownloaderDesktop;

internal sealed class CookieProvider
{
    private readonly AppSettingsRepository _settingsRepository;

    public CookieProvider(AppSettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public string GetBrowser() => string.IsNullOrWhiteSpace(_settingsRepository.Load().CookiesFromBrowser) ? "chrome" : _settingsRepository.Load().CookiesFromBrowser!.Trim();

    public void SaveBrowser(string browser)
    {
        var settings = _settingsRepository.Load();
        settings.CookiesFromBrowser = browser;
        _settingsRepository.Save(settings);
    }

    public string GetBrowserArgument(string browser)
    {
        var profile = _settingsRepository.Load().CookiesBrowserProfile?.Trim();
        return $"{browser}:{(string.IsNullOrWhiteSpace(profile) ? "Default" : profile)}";
    }

    public string GetExpectedFilePath()
    {
        var configured = _settingsRepository.Load().CookiesFile?.Trim();
        var path = string.IsNullOrWhiteSpace(configured) ? Path.Combine("lib", "cookies.txt") : configured;
        path = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        return Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    public bool TryGetFilePath(out string path)
    {
        path = GetExpectedFilePath();
        return File.Exists(path);
    }

    public bool UsesBrowserCookies() => !TryGetFilePath(out _);

    public void AddArguments(System.Diagnostics.ProcessStartInfo processStartInfo, string browser)
    {
        if (TryGetFilePath(out var cookiesFile))
        {
            processStartInfo.ArgumentList.Add("--cookies");
            processStartInfo.ArgumentList.Add(cookiesFile);
            return;
        }

        processStartInfo.ArgumentList.Add("--cookies-from-browser");
        processStartInfo.ArgumentList.Add(GetBrowserArgument(browser));
    }
}
