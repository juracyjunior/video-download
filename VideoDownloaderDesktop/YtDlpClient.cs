using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace VideoDownloaderDesktop;

internal sealed class YtDlpClient
{
    private readonly CookieProvider _cookieProvider;

    public YtDlpClient(CookieProvider cookieProvider)
    {
        _cookieProvider = cookieProvider;
    }

    public async Task<DownloadAttemptResult> GetExpectedOutputPathAsync(DownloadQueueItem item, YtDlpOptions options)
    {
        var format = await ResolveFormatAsync(item, options);
        if (!format.Success) return DownloadAttemptResult.Failed(format.Log);

        var processStartInfo = CreateDownloadProcessStartInfo(item, options, format.Selector!);
        processStartInfo.ArgumentList.Add("--no-download");
        processStartInfo.ArgumentList.Add("--print");
        processStartInfo.ArgumentList.Add("filename");
        processStartInfo.ArgumentList.Add(item.Url);

        var result = await RunToEndAsync(processStartInfo);
        var outputPath = result.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(outputPath)
            ? new DownloadAttemptResult(true, result.Log, outputPath)
            : DownloadAttemptResult.Failed(result.Log);
    }

    public async Task<DownloadAttemptResult> DownloadAsync(DownloadQueueItem item, YtDlpOptions options, Action<string> reportLog)
    {
        var format = await ResolveFormatAsync(item, options);
        if (!format.Success) return DownloadAttemptResult.Failed(format.Log);

        var processStartInfo = CreateDownloadProcessStartInfo(item, options, format.Selector!);
        processStartInfo.ArgumentList.Add(item.Url);
        using var process = new Process { StartInfo = processStartInfo };
        var log = new StringBuilder();
        void Receive(string? line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            log.AppendLine(line);
            reportLog(line);
        }

        process.OutputDataReceived += (_, eventArgs) => Receive(eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => Receive(eventArgs.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        return process.ExitCode == 0 ? new DownloadAttemptResult(true, log.ToString()) : DownloadAttemptResult.Failed(log.ToString());
    }

    private async Task<FormatSelection> ResolveFormatAsync(DownloadQueueItem item, YtDlpOptions options)
    {
        if (item.SelectedFormatSelector is { } selector) return new FormatSelection(true, selector, string.Empty);

        var processStartInfo = CreateMetadataProcessStartInfo(item, options);
        var result = await RunToEndAsync(processStartInfo);
        if (result.ExitCode != 0) return FormatSelection.Failed(result.Log);

        try
        {
            using var document = JsonDocument.Parse(result.Output);
            var formats = document.RootElement.GetProperty("formats")
                .EnumerateArray()
                .Select(CreateVideoFormat)
                .Where(format => format is not null)
                .Cast<AvailableVideoFormat>()
                .ToList();

            var availableResolutions = formats.Select(format => format.Resolution).Distinct().OrderBy(resolution => resolution).ToList();
            var chosenResolution = availableResolutions.FirstOrDefault(resolution => resolution >= item.RequestedHeight);
            if (chosenResolution == 0) chosenResolution = availableResolutions.LastOrDefault();
            if (chosenResolution == 0) return FormatSelection.Failed("Nenhuma resolução de vídeo foi encontrada.");

            var chosenFormat = formats
                .Where(format => format.Resolution == chosenResolution)
                .OrderByDescending(format => format.IsMp4)
                .ThenByDescending(format => format.IsDirectDownload)
                .ThenByDescending(format => format.Bitrate)
                .First();

            item.SelectedHeight = chosenResolution;
            item.SelectedFormatSelector = FormatSelection.CreateSelector(chosenFormat);
            item.SelectedFormatId = chosenFormat.Id;
            return FormatSelection.For(chosenFormat);
        }
        catch (JsonException)
        {
            return FormatSelection.Failed(result.Log);
        }
    }

    private ProcessStartInfo CreateMetadataProcessStartInfo(DownloadQueueItem item, YtDlpOptions options)
    {
        var processStartInfo = CreateBaseProcessStartInfo(item, options);
        processStartInfo.ArgumentList.Add("--dump-single-json");
        processStartInfo.ArgumentList.Add("--no-download");
        processStartInfo.ArgumentList.Add(item.Url);
        return processStartInfo;
    }

    private ProcessStartInfo CreateDownloadProcessStartInfo(DownloadQueueItem item, YtDlpOptions options, string formatSelector)
    {
        var processStartInfo = CreateBaseProcessStartInfo(item, options);
        processStartInfo.ArgumentList.Add("-f");
        processStartInfo.ArgumentList.Add(formatSelector);
        processStartInfo.ArgumentList.Add("--merge-output-format");
        processStartInfo.ArgumentList.Add("mp4");
        processStartInfo.ArgumentList.Add("-o");
        processStartInfo.ArgumentList.Add(Path.Combine(item.OutputFolder, $"{item.FileName}.%(ext)s"));
        return processStartInfo;
    }

    private ProcessStartInfo CreateBaseProcessStartInfo(DownloadQueueItem item, YtDlpOptions options)
    {
        var processStartInfo = YtDlpLocator.CreateProcessStartInfo();
        processStartInfo.ArgumentList.Add("--no-update");
        if (options.NoContinue) processStartInfo.ArgumentList.Add("--no-continue");
        if (options.ForceOverwrite) processStartInfo.ArgumentList.Add("--force-overwrites");
        if (!string.IsNullOrWhiteSpace(options.ExtractorArguments))
        {
            processStartInfo.ArgumentList.Add("--extractor-args");
            processStartInfo.ArgumentList.Add(options.ExtractorArguments);
        }
        if (options.UseCookies) _cookieProvider.AddArguments(processStartInfo, item.Browser);
        return processStartInfo;
    }

    private static async Task<ProcessResult> RunToEndAsync(ProcessStartInfo processStartInfo)
    {
        using var process = Process.Start(processStartInfo) ?? throw new InvalidOperationException("Não foi possível iniciar o processo do yt-dlp.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;
        return new ProcessResult(process.ExitCode, output, $"{output}{Environment.NewLine}{error}");
    }

    private static AvailableVideoFormat? CreateVideoFormat(JsonElement format)
    {
        if (!format.TryGetProperty("format_id", out var id)
            || !format.TryGetProperty("vcodec", out var codec)
            || string.Equals(codec.GetString(), "none", StringComparison.OrdinalIgnoreCase)
            || !format.TryGetProperty("width", out var width)
            || !format.TryGetProperty("height", out var height)
            || !width.TryGetInt32(out var widthValue)
            || !height.TryGetInt32(out var heightValue))
        {
            return null;
        }

        var formatNote = format.TryGetProperty("format_note", out var note) ? note.GetString() : null;
        var formatDescription = format.TryGetProperty("format", out var description) ? description.GetString() : null;
        var resolution = ReadResolution(formatNote) ?? ReadResolution(formatDescription) ?? Math.Min(widthValue, heightValue);
        var extension = format.TryGetProperty("ext", out var ext) ? ext.GetString() : null;
        var protocol = format.TryGetProperty("protocol", out var protocolValue) ? protocolValue.GetString() : null;
        var audioCodec = format.TryGetProperty("acodec", out var audio) ? audio.GetString() : null;
        var bitrate = format.TryGetProperty("tbr", out var tbr) && tbr.TryGetDouble(out var bitrateValue) ? bitrateValue : 0;
        return new AvailableVideoFormat(id.GetString()!, resolution, string.Equals(extension, "mp4", StringComparison.OrdinalIgnoreCase), protocol?.Contains("https", StringComparison.OrdinalIgnoreCase) == true, !string.Equals(audioCodec, "none", StringComparison.OrdinalIgnoreCase), bitrate);
    }

    private static int? ReadResolution(string? formatNote)
    {
        var match = Regex.Match(formatNote ?? string.Empty, @"(?<!\d)(\d{3,4})p");
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Log);

    private sealed record AvailableVideoFormat(string Id, int Resolution, bool IsMp4, bool IsDirectDownload, bool HasAudio, double Bitrate);

    private sealed record FormatSelection(bool Success, string? Selector, string Log)
    {
        public static FormatSelection For(AvailableVideoFormat format) => new(true, CreateSelector(format), string.Empty);
        public static string CreateSelector(AvailableVideoFormat format) => format.HasAudio ? format.Id : $"{format.Id}+bestaudio[ext=m4a]/{format.Id}+bestaudio/{format.Id}";
        public static FormatSelection Failed(string log) => new(false, null, log);
    }
}
