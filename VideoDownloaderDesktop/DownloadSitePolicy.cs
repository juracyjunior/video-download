namespace VideoDownloaderDesktop;

internal interface IDownloadSitePolicy
{
    bool AppliesTo(string videoUrl);
    Task<DownloadAttemptResult> ResolveOutputPathAsync(YtDlpClient client, DownloadQueueItem item, Action<string> report);
    Task<DownloadAttemptResult> DownloadAsync(YtDlpClient client, DownloadQueueItem item, bool forceOverwrite, Action<string> report);
}

internal sealed class DefaultDownloadSitePolicy : IDownloadSitePolicy
{
    public bool AppliesTo(string videoUrl) => true;

    public Task<DownloadAttemptResult> ResolveOutputPathAsync(YtDlpClient client, DownloadQueueItem item, Action<string> report) =>
        client.GetExpectedOutputPathAsync(item, new YtDlpOptions(UseCookies: true));

    public Task<DownloadAttemptResult> DownloadAsync(YtDlpClient client, DownloadQueueItem item, bool forceOverwrite, Action<string> report) =>
        client.DownloadAsync(item, new YtDlpOptions(UseCookies: true, ForceOverwrite: forceOverwrite), report);
}

internal sealed class InstagramDownloadSitePolicy : IDownloadSitePolicy
{
    public bool AppliesTo(string videoUrl) => CookieFileHelper.IsInstagramUrl(videoUrl);

    public async Task<DownloadAttemptResult> ResolveOutputPathAsync(YtDlpClient client, DownloadQueueItem item, Action<string> report)
    {
        var firstAttempt = await client.GetExpectedOutputPathAsync(item, new YtDlpOptions(UseCookies: false));
        if (firstAttempt.Success || !CookieFileHelper.RequiresAuthRetry(firstAttempt.Log)) return firstAttempt;

        report(DownloadMessages.InstagramRetryWithCookies);
        return await client.GetExpectedOutputPathAsync(item, new YtDlpOptions(UseCookies: true));
    }

    public async Task<DownloadAttemptResult> DownloadAsync(YtDlpClient client, DownloadQueueItem item, bool forceOverwrite, Action<string> report)
    {
        report(DownloadMessages.InstagramDownloadWithoutCookies);
        var firstAttempt = await client.DownloadAsync(item, new YtDlpOptions(UseCookies: false, ForceOverwrite: forceOverwrite), report);
        if (firstAttempt.Success || !CookieFileHelper.RequiresAuthRetry(firstAttempt.Log)) return firstAttempt;

        report(DownloadMessages.InstagramRetryWithCookies);
        return await client.DownloadAsync(item, new YtDlpOptions(UseCookies: true, ForceOverwrite: forceOverwrite), report);
    }
}

internal sealed class YouTubeDownloadSitePolicy : IDownloadSitePolicy
{
    private static readonly YtDlpOptions InitialOptions = new(UseCookies: false, NoContinue: true);

    public bool AppliesTo(string videoUrl) => CookieFileHelper.IsYouTubeUrl(videoUrl);

    public async Task<DownloadAttemptResult> ResolveOutputPathAsync(YtDlpClient client, DownloadQueueItem item, Action<string> report)
    {
        var firstAttempt = await client.GetExpectedOutputPathAsync(item, InitialOptions);
        if (firstAttempt.Success) return firstAttempt;

        report(DownloadMessages.YouTubeValidationRetryWithCookies);
        return await client.GetExpectedOutputPathAsync(item, InitialOptions with { UseCookies = true });
    }

    public async Task<DownloadAttemptResult> DownloadAsync(YtDlpClient client, DownloadQueueItem item, bool forceOverwrite, Action<string> report)
    {
        report(DownloadMessages.YouTubeDownloadWithoutCookies);
        var firstAttempt = await client.DownloadAsync(item, InitialOptions with { ForceOverwrite = forceOverwrite }, report);
        if (firstAttempt.Success) return firstAttempt;

        if (CookieFileHelper.IsHttp403(firstAttempt.Log))
        {
            report(DownloadMessages.YouTubeRestartAfterForbidden);
            var restartAttempt = await client.DownloadAsync(item, InitialOptions with { ForceOverwrite = true }, report);
            if (restartAttempt.Success) return restartAttempt;
        }

        report(DownloadMessages.YouTubeRetryWithCookies);
        return await client.DownloadAsync(item, InitialOptions with { UseCookies = true, ForceOverwrite = true }, report);
    }
}
