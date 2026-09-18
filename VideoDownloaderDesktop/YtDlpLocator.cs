namespace VideoDownloaderDesktop;

internal static class YtDlpLocator
{
    private const string ExecutableName = "yt-dlp.exe";

    public static string GetExecutablePath()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "lib", ExecutableName);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"yt-dlp não encontrado em: {path}. Coloque {ExecutableName} na pasta lib do aplicativo.");
        }

        return path;
    }

    public static System.Diagnostics.ProcessStartInfo CreateProcessStartInfo()
    {
        return new System.Diagnostics.ProcessStartInfo
        {
            FileName = GetExecutablePath(),
            WorkingDirectory = Path.Combine(AppContext.BaseDirectory, "lib"),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
    }
}
