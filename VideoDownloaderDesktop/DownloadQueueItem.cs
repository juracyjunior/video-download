namespace VideoDownloaderDesktop;

internal enum DownloadItemStatus
{
    Pending,
    Downloading,
    Success,
    Error
}

internal sealed class DownloadQueueItem
{
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string OutputFolder { get; set; } = string.Empty;
    public string Browser { get; set; } = "chrome";
    public int RequestedHeight { get; set; }
    public int? SelectedHeight { get; set; }
    public string? SelectedFormatSelector { get; set; }
    public string? SelectedFormatId { get; set; }
    public DownloadItemStatus Status { get; set; } = DownloadItemStatus.Pending;
    public string? ErrorMessage { get; set; }

    public string StatusText => Status switch
    {
        DownloadItemStatus.Pending => "Pendente",
        DownloadItemStatus.Downloading => "Baixando...",
        DownloadItemStatus.Success => "Sucesso",
        DownloadItemStatus.Error => "Erro",
        _ => Status.ToString()
    };

    public string RetryText => Status == DownloadItemStatus.Error ? "Retentar" : string.Empty;
}
