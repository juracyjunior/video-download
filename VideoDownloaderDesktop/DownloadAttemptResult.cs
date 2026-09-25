namespace VideoDownloaderDesktop;

internal sealed record DownloadAttemptResult(bool Success, string Log, string? OutputPath = null)
{
    public static DownloadAttemptResult Failed(string log) => new(false, log);
}

internal sealed record DownloadExecutionResult(bool Success, string? ErrorMessage)
{
    public static DownloadExecutionResult Completed() => new(true, null);
    public static DownloadExecutionResult Failed(string message) => new(false, message);
}
