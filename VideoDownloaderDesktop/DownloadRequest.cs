namespace VideoDownloaderDesktop;

internal sealed record DownloadRequest(IReadOnlyList<string> Urls, string OutputFolder, string BaseFileName, string Browser, int RequestedHeight);

internal sealed record ValidationResult(bool IsValid, string? ErrorMessage)
{
    public static ValidationResult Success() => new(true, null);
    public static ValidationResult Failure(string message) => new(false, message);
}
