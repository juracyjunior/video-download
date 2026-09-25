namespace VideoDownloaderDesktop;

internal sealed record YtDlpOptions(bool UseCookies, bool ForceOverwrite = false, bool NoContinue = false, string? ExtractorArguments = null);
