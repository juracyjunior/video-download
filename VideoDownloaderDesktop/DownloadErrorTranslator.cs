namespace VideoDownloaderDesktop;

internal sealed class DownloadErrorTranslator
{
    private readonly CookieProvider _cookieProvider;

    public DownloadErrorTranslator(CookieProvider cookieProvider)
    {
        _cookieProvider = cookieProvider;
    }

    public string Translate(string log, string browser)
    {
        var usingCookiesFile = _cookieProvider.TryGetFilePath(out _);
        if (BrowserCookieHelper.TryGetCookieDatabaseErrorMessage(log, browser, out var message)
            || BrowserCookieHelper.TryGetDpapiErrorMessage(log, out message)
            || BrowserCookieHelper.TryGetInstagramNotFoundMessage(log, usingCookiesFile, out message)
            || BrowserCookieHelper.TryGetAuthErrorMessage(log, browser, usingCookiesFile, out message))
        {
            return message;
        }

        return DownloadMessages.GenericDownloadFailure;
    }
}
