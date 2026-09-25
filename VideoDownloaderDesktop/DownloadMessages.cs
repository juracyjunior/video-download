namespace VideoDownloaderDesktop;

internal static class DownloadMessages
{
    public const string MissingUrls = "Informe ao menos uma URL do vídeo (uma por linha).";
    public const string MissingOutputFolder = "Informe a pasta de destino.";
    public const string OutputFolderDoesNotExist = "A pasta de destino não existe.";
    public const string MissingFileName = "Informe o nome do arquivo.";
    public const string InvalidFileName = "O nome do arquivo contém caracteres inválidos.";
    public const string ExistingFileNotReplaced = "Arquivo já existe (não substituído)";
    public const string GenericDownloadFailure = "Falha no download.";
    public const string InstagramRetryWithCookies = "Instagram: tentando novamente com cookies...";
    public const string InstagramDownloadWithoutCookies = "Instagram: baixando sem cookies.";
    public const string YouTubeValidationRetryWithCookies = "YouTube: verificação falhou. Tentando novamente com cookies...";
    public const string YouTubeDownloadWithoutCookies = "YouTube: baixando sem cookies e sem retomar arquivo parcial.";
    public const string YouTubeRestartAfterForbidden = "YouTube: 403 ao baixar. Reiniciando o arquivo.";
    public const string YouTubeRetryWithCookies = "YouTube: tentando novamente com cookies...";

    public static string InvalidUrl(int lineNumber) => $"A URL na linha {lineNumber} não é válida.";
    public static string AdjustedInstagramUrl(string fileName) => $"Ajuste automático ({fileName}): /reels/ → /reel/";
    public static string SelectedResolution(int selectedHeight, int requestedHeight) => selectedHeight == requestedHeight ? $"Resolução selecionada: {selectedHeight}p." : $"Resolução {requestedHeight}p indisponível; usando a mais próxima: {selectedHeight}p.";
    public static string SelectedFormat(string formatId) => $"Formato de vídeo selecionado: ID {formatId}.";
}
