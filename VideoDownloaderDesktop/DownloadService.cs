using System.Text.RegularExpressions;

namespace VideoDownloaderDesktop;

internal sealed class DownloadService
{
    private static readonly Regex TrailingNumberRegex = new(@"^(.*?)(\d+)$", RegexOptions.Compiled);
    private readonly YtDlpClient _client;
    private readonly DownloadErrorTranslator _errorTranslator;
    private readonly IReadOnlyList<IDownloadSitePolicy> _sitePolicies;

    public DownloadService(YtDlpClient client, DownloadErrorTranslator errorTranslator)
    {
        _client = client;
        _errorTranslator = errorTranslator;
        _sitePolicies = [new InstagramDownloadSitePolicy(), new YouTubeDownloadSitePolicy(), new DefaultDownloadSitePolicy()];
    }

    public ValidationResult ValidateRequest(DownloadRequest request)
    {
        if (request.Urls.Count == 0) return ValidationResult.Failure(DownloadMessages.MissingUrls);
        if (string.IsNullOrWhiteSpace(request.OutputFolder)) return ValidationResult.Failure(DownloadMessages.MissingOutputFolder);
        if (!Directory.Exists(request.OutputFolder)) return ValidationResult.Failure(DownloadMessages.OutputFolderDoesNotExist);
        if (string.IsNullOrWhiteSpace(request.BaseFileName)) return ValidationResult.Failure(DownloadMessages.MissingFileName);
        if (request.BaseFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return ValidationResult.Failure(DownloadMessages.InvalidFileName);

        for (var index = 0; index < request.Urls.Count; index++)
        {
            if (!Uri.TryCreate(request.Urls[index], UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return ValidationResult.Failure(DownloadMessages.InvalidUrl(index + 1));
            }
        }

        return ValidationResult.Success();
    }

    public IReadOnlyList<DownloadQueueItem> CreateQueue(DownloadRequest request, Action<string> report)
    {
        return request.Urls.Select((url, index) => CreateQueueItem(url, request, index, report)).ToList();
    }

    public async Task<DownloadExecutionResult> DownloadAsync(DownloadQueueItem item, Func<string, Task<bool>> confirmOverwrite, Action<string> report)
    {
        var policy = GetPolicy(item.Url);
        var outputPath = await policy.ResolveOutputPathAsync(_client, item, report);
        if (!outputPath.Success) return DownloadExecutionResult.Failed(_errorTranslator.Translate(outputPath.Log, item.Browser));
        if (item.SelectedHeight is { } selectedHeight) report(DownloadMessages.SelectedResolution(selectedHeight, item.RequestedHeight));
        if (!string.IsNullOrWhiteSpace(item.SelectedFormatId)) report(DownloadMessages.SelectedFormat(item.SelectedFormatId));

        var forceOverwrite = false;
        if (File.Exists(outputPath.OutputPath))
        {
            if (!await confirmOverwrite(outputPath.OutputPath!)) return DownloadExecutionResult.Failed(DownloadMessages.ExistingFileNotReplaced);
            forceOverwrite = true;
        }

        var download = await policy.DownloadAsync(_client, item, forceOverwrite, report);
        return download.Success ? DownloadExecutionResult.Completed() : DownloadExecutionResult.Failed(_errorTranslator.Translate(download.Log, item.Browser));
    }

    private static DownloadQueueItem CreateQueueItem(string url, DownloadRequest request, int index, Action<string> report)
    {
        var (adjustedUrl, adjusted) = CookieFileHelper.AdjustInstagramReelsUrl(url);
        var fileName = BuildSequentialFileName(request.BaseFileName, index);
        if (adjusted) report(DownloadMessages.AdjustedInstagramUrl(fileName));

        return new DownloadQueueItem { FileName = fileName, Url = adjustedUrl, OutputFolder = request.OutputFolder, Browser = request.Browser, RequestedHeight = request.RequestedHeight };
    }

    private IDownloadSitePolicy GetPolicy(string url) => _sitePolicies.First(policy => policy.AppliesTo(url));

    public static string BuildSequentialFileName(string baseName, int index)
    {
        var match = TrailingNumberRegex.Match(baseName);
        if (!match.Success) return index == 0 ? baseName : $"{baseName}{index}";
        var number = long.Parse(match.Groups[2].Value) + index;
        return $"{match.Groups[1].Value}{number.ToString().PadLeft(match.Groups[2].Value.Length, '0')}";
    }
}
