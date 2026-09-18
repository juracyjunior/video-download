using System.ComponentModel;
using System.Text.RegularExpressions;

namespace VideoDownloaderDesktop;

public partial class frmApp : Form
{
    private static readonly Regex TrailingNumberRegex = new(@"^(.*?)(\d+)$", RegexOptions.Compiled);
    private readonly BindingList<DownloadQueueItem> _downloadQueue = [];
    private bool _isDownloading;

    public frmApp()
    {
        InitializeComponent();
        InitializeDownloadGrid();
        txtOutputFolder.Text = AppSettings.GetLastOutputFolder() ?? string.Empty;
        statusLabelVersion.Text = AppSettings.GetVersionLabel();
        InitializeBrowserSelector();
        UpdateBrowserSelectorState();
        UpdateCookieFileStatus();
    }

    private void InitializeDownloadGrid()
    {
        gridDownloads.AutoGenerateColumns = false;
        gridDownloads.DataSource = _downloadQueue;
    }

    private void InitializeBrowserSelector()
    {
        cboBrowser.Items.Clear();
        cboBrowser.Items.Add(new BrowserOption("chrome", "Google Chrome"));
        cboBrowser.Items.Add(new BrowserOption("edge", "Microsoft Edge"));
        cboBrowser.Items.Add(new BrowserOption("firefox", "Mozilla Firefox"));
        cboBrowser.DisplayMember = nameof(BrowserOption.DisplayName);
        cboBrowser.ValueMember = nameof(BrowserOption.BrowserId);

        var savedBrowser = AppSettings.GetCookiesFromBrowser();
        cboBrowser.SelectedItem = cboBrowser.Items
            .Cast<BrowserOption>()
            .FirstOrDefault(item => item.BrowserId == savedBrowser)
            ?? cboBrowser.Items[0];
    }

    private string GetSelectedBrowser()
    {
        return (cboBrowser.SelectedItem as BrowserOption)?.BrowserId ?? "chrome";
    }

    private void UpdateBrowserSelectorState()
    {
        var usesFile = AppSettings.TryGetCookiesFilePath(out _);

        cboBrowser.Enabled = !usesFile && !_isDownloading;
        lblBrowser.Enabled = !usesFile && !_isDownloading;
        lblBrowser.Text = usesFile ? "Cookies: arquivo" : "Cookies do navegador:";
    }

    private void UpdateCookieFileStatus()
    {
        lblCookieStatus.Text = AppSettings.TryGetCookiesFilePath(out var path)
            ? $"Arquivo de cookies carregado: {path}"
            : $"Nenhum arquivo encontrado em: {AppSettings.GetExpectedCookiesFilePath()}";
    }

    private void cboBrowser_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cboBrowser.SelectedItem is BrowserOption option)
        {
            AppSettings.SaveCookiesFromBrowser(option.BrowserId);
        }
    }

    private sealed class BrowserOption(string browserId, string displayName)
    {
        public string BrowserId { get; } = browserId;
        public string DisplayName { get; } = displayName;
    }

    private void btnBrowseFolder_Click(object sender, EventArgs e)
    {
        if (Directory.Exists(txtOutputFolder.Text))
        {
            folderBrowserDialog.SelectedPath = txtOutputFolder.Text;
        }

        if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
        {
            txtOutputFolder.Text = folderBrowserDialog.SelectedPath;
            AppSettings.SaveLastOutputFolder(folderBrowserDialog.SelectedPath);
        }
    }

    private async void btnDownload_Click(object sender, EventArgs e)
    {
        if (_isDownloading)
        {
            return;
        }

        var videoUrls = ParseUrlList(txtUrl.Text);
        var outputFolder = txtOutputFolder.Text.Trim();
        var customFileName = txtFileName.Text.Trim();

        if (videoUrls.Count == 0)
        {
            MessageBox.Show("Informe ao menos uma URL do vídeo (uma por linha).", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            MessageBox.Show("Informe a pasta de destino.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!Directory.Exists(outputFolder))
        {
            MessageBox.Show("A pasta de destino não existe.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(customFileName))
        {
            MessageBox.Show("Informe o nome do arquivo.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (customFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            MessageBox.Show("O nome do arquivo contém caracteres inválidos.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selectedBrowser = GetSelectedBrowser();

        if (AppSettings.UsesBrowserCookies())
        {
            AppSettings.SaveCookiesFromBrowser(selectedBrowser);
        }

        SetDownloadingUi(true);
        txtLog.Clear();
        BuildDownloadQueue(videoUrls, outputFolder, customFileName, selectedBrowser);

        AppendLog(AppSettings.GetCookiesSourceMessage(selectedBrowser));
        if (AppSettings.TryGetCookiesFilePath(out var cookiesPath))
        {
            if (videoUrls.Any(CookieFileHelper.IsInstagramUrl))
            {
                AppendLog("Instagram: vídeos públicos serão baixados sem cookies (cookies podem causar erro 404).");
                AppendLog(CookieFileHelper.DescribeInstagramCookies(cookiesPath));
            }

            if (videoUrls.Any(CookieFileHelper.IsYouTubeUrl))
            {
                AppendLog("YouTube: tenta primeiro sem cookies; se falhar, tenta de novo com cookies.");
            }
        }
        else if (AppSettings.UsesBrowserCookies())
        {
            LogBrowserProcessInfo(selectedBrowser);
        }

        AppendLog($"Iniciando download de {_downloadQueue.Count} vídeo(s)...");
        AppendLog($"Nome base: {customFileName} (ex.: {BuildSequentialFileName(customFileName, 0)}, {BuildSequentialFileName(customFileName, 1)}, ...)");

        var successCount = 0;
        var errorCount = 0;

        try
        {
            for (var index = 0; index < _downloadQueue.Count; index++)
            {
                var item = _downloadQueue[index];
                AppendLog(string.Empty);
                AppendLog($"[{index + 1}/{_downloadQueue.Count}] Baixando como \"{item.FileName}\"...");
                AppendLog($"URL: {item.Url}");

                var success = await DownloadQueueItemAsync(item, askOverwrite: true);
                if (success)
                {
                    successCount++;
                }
                else
                {
                    errorCount++;
                }
            }
        }
        finally
        {
            AppendLog(string.Empty);
            AppendLog("========== RESUMO ==========");
            AppendLog($"Total: {_downloadQueue.Count} | Sucessos: {successCount} | Erros: {errorCount}");
            AppendLog("============================");

            SetDownloadingUi(false);
            RefreshDownloadGrid();
        }
    }

    private void BuildDownloadQueue(List<string> videoUrls, string outputFolder, string customFileName, string browser)
    {
        _downloadQueue.Clear();

        for (var index = 0; index < videoUrls.Count; index++)
        {
            var (videoUrl, urlAdjusted) = CookieFileHelper.AdjustInstagramReelsUrl(videoUrls[index]);
            if (urlAdjusted)
            {
                AppendLog($"Ajuste automático ({BuildSequentialFileName(customFileName, index)}): /reels/ → /reel/");
            }

            _downloadQueue.Add(new DownloadQueueItem
            {
                FileName = BuildSequentialFileName(customFileName, index),
                Url = videoUrl,
                OutputFolder = outputFolder,
                Browser = browser,
                Status = DownloadItemStatus.Pending
            });
        }

        RefreshDownloadGrid();
    }

    private async Task<bool> DownloadQueueItemAsync(DownloadQueueItem item, bool askOverwrite)
    {
        UpdateItemStatus(item, DownloadItemStatus.Downloading, null);

        try
        {
            var forceOverwrite = false;
            var expectedPath = await GetExpectedOutputPathAsync(item.Url, item.OutputFolder, item.FileName, item.Browser);
            if (!string.IsNullOrWhiteSpace(expectedPath) && File.Exists(expectedPath))
            {
                if (askOverwrite)
                {
                    var replace = MessageBox.Show(
                        $"O arquivo já existe:\n{expectedPath}\n\nDeseja substituir?",
                        "Arquivo existente",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (replace != DialogResult.Yes)
                    {
                        AppendLog($"Ignorado: arquivo já existe ({item.FileName}).");
                        UpdateItemStatus(item, DownloadItemStatus.Error, "Arquivo já existe (não substituído)");
                        return false;
                    }
                }

                forceOverwrite = true;
            }

            await RunYtDlpAsync(item.Url, item.OutputFolder, item.FileName, forceOverwrite, item.Browser);
            AppendLog($"Sucesso: {item.FileName}");
            UpdateItemStatus(item, DownloadItemStatus.Success, null);
            return true;
        }
        catch (Exception ex)
        {
            AppendLog($"Erro em \"{item.FileName}\": {ex.Message}");
            UpdateItemStatus(item, DownloadItemStatus.Error, ex.Message);
            return false;
        }
    }

    private void UpdateItemStatus(DownloadQueueItem item, DownloadItemStatus status, string? errorMessage)
    {
        void Apply()
        {
            item.Status = status;
            item.ErrorMessage = errorMessage;

            var index = _downloadQueue.IndexOf(item);
            if (index >= 0)
            {
                _downloadQueue.ResetItem(index);
            }

            gridDownloads.Invalidate();
        }

        if (InvokeRequired)
        {
            Invoke(Apply);
            return;
        }

        Apply();
    }

    private void RefreshDownloadGrid()
    {
        if (gridDownloads.InvokeRequired)
        {
            gridDownloads.Invoke(RefreshDownloadGrid);
            return;
        }

        gridDownloads.Refresh();
        gridDownloads.Invalidate();
    }

    private async void gridDownloads_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != colRetry.Index)
        {
            return;
        }

        if (_isDownloading)
        {
            MessageBox.Show("Aguarde o fim dos downloads em andamento antes de retentar.", "Aguarde", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (e.RowIndex >= _downloadQueue.Count)
        {
            return;
        }

        var item = _downloadQueue[e.RowIndex];
        if (item.Status != DownloadItemStatus.Error)
        {
            return;
        }

        SetDownloadingUi(true);
        AppendLog(string.Empty);
        AppendLog($"Retentando \"{item.FileName}\"...");
        AppendLog($"URL: {item.Url}");

        try
        {
            await DownloadQueueItemAsync(item, askOverwrite: true);
        }
        finally
        {
            SetDownloadingUi(false);
            RefreshDownloadGrid();
        }
    }

    private void gridDownloads_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _downloadQueue.Count)
        {
            return;
        }

        var item = _downloadQueue[e.RowIndex];

        if (e.ColumnIndex == colStatus.Index)
        {
            e.Value = item.StatusText;
            e.FormattingApplied = true;

            var row = gridDownloads.Rows[e.RowIndex];
            row.DefaultCellStyle.ForeColor = item.Status switch
            {
                DownloadItemStatus.Success => Color.DarkGreen,
                DownloadItemStatus.Error => Color.DarkRed,
                DownloadItemStatus.Downloading => Color.DarkBlue,
                _ => SystemColors.ControlText
            };
        }
        else if (e.ColumnIndex == colRetry.Index)
        {
            e.Value = item.Status == DownloadItemStatus.Error ? "Retentar" : string.Empty;
            e.FormattingApplied = true;
        }
    }

    private void SetDownloadingUi(bool downloading)
    {
        _isDownloading = downloading;
        btnDownload.Enabled = !downloading;
        btnBrowseFolder.Enabled = !downloading;
        txtUrl.Enabled = !downloading;
        txtFileName.Enabled = !downloading;
        txtOutputFolder.Enabled = !downloading;
        UpdateBrowserSelectorState();
    }

    private static List<string> ParseUrlList(string text)
    {
        return text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static string BuildSequentialFileName(string baseName, int index)
    {
        var match = TrailingNumberRegex.Match(baseName);
        if (!match.Success)
        {
            return index == 0 ? baseName : $"{baseName}{index}";
        }

        var prefix = match.Groups[1].Value;
        var numberText = match.Groups[2].Value;
        var startNumber = long.Parse(numberText);
        var nextNumber = startNumber + index;
        return $"{prefix}{nextNumber.ToString().PadLeft(numberText.Length, '0')}";
    }

    private void LogBrowserProcessInfo(string browser)
    {
        var browserName = BrowserCookieHelper.GetBrowserDisplayName(browser);
        var isChromium = browser is "chrome" or "edge";

        if (isChromium)
        {
            AppendLog("Dica: Chrome/Edge no Windows podem falhar ao ler cookies. Use Firefox ou exporte todos os cookies para lib\\cookies.txt.");
        }

        if (BrowserCookieHelper.HasVisibleBrowserWindow(browser))
        {
            AppendLog($"Aviso: o {browserName} está com janela aberta. Feche o navegador para evitar erro ao ler cookies.");
            return;
        }

        var backgroundCount = BrowserCookieHelper.GetBackgroundProcessCount(browser);
        if (backgroundCount > 0)
        {
            AppendLog($"Aviso: há {backgroundCount} processo(s) do {browserName} em segundo plano. Se der erro de cookies, finalize-os no Gerenciador de Tarefas.");
        }
    }

    private static string BuildOutputTemplate(string outputFolder, string fileName)
    {
        return Path.Combine(outputFolder, $"{fileName}.%(ext)s");
    }

    private static void AddCommonArguments(System.Diagnostics.ProcessStartInfo psi, string browser, bool useCookies, string videoUrl)
    {
        psi.ArgumentList.Add("--no-update");

        if (CookieFileHelper.IsYouTubeUrl(videoUrl))
        {
            // URLs assinadas do YouTube expiram; retomar .part antigo causa 403.
            psi.ArgumentList.Add("--no-continue");
            psi.ArgumentList.Add("--extractor-args");
            psi.ArgumentList.Add("youtube:player_client=tv,web");
        }

        if (!useCookies)
        {
            return;
        }

        if (AppSettings.TryGetCookiesFilePath(out var cookiesFile))
        {
            psi.ArgumentList.Add("--cookies");
            psi.ArgumentList.Add(cookiesFile);
            return;
        }

        psi.ArgumentList.Add("--cookies-from-browser");
        psi.ArgumentList.Add(AppSettings.GetCookiesFromBrowserArgument(browser));
    }

    private static bool ShouldUseCookiesOnFirstAttempt(string videoUrl)
    {
        // Instagram/YouTube públicos funcionam melhor sem cookies.
        return !CookieFileHelper.IsInstagramUrl(videoUrl) && !CookieFileHelper.IsYouTubeUrl(videoUrl);
    }

    private static void AddFormatArguments(System.Diagnostics.ProcessStartInfo psi)
    {
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("bestvideo+bestaudio/best");
        psi.ArgumentList.Add("--merge-output-format");
        psi.ArgumentList.Add("mp4");
    }

    private async Task<string?> GetExpectedOutputPathAsync(string videoUrl, string outputFolder, string customFileName, string browser)
    {
        var useCookies = ShouldUseCookiesOnFirstAttempt(videoUrl);
        var result = await TryGetExpectedOutputPathAsync(videoUrl, outputFolder, customFileName, browser, useCookies);

        if (result.Success)
        {
            return result.Path;
        }

        if (CookieFileHelper.IsYouTubeUrl(videoUrl) && !useCookies)
        {
            if (CookieFileHelper.IsHttp403(result.Log))
            {
                AppendLog("YouTube: 403 na verificação. Tentando novamente com cookies...");
            }
            else
            {
                AppendLog("YouTube: verificação falhou. Tentando novamente com cookies...");
            }

            var cookieRetry = await TryGetExpectedOutputPathAsync(videoUrl, outputFolder, customFileName, browser, useCookies: true);
            if (cookieRetry.Success)
            {
                return cookieRetry.Path;
            }

            ThrowIfKnownError(cookieRetry.Log, browser, usingCookiesFile: AppSettings.TryGetCookiesFilePath(out _));
            return null;
        }

        if (CookieFileHelper.IsInstagramUrl(videoUrl) && !useCookies && CookieFileHelper.RequiresAuthRetry(result.Log))
        {
            AppendLog("Instagram: tentando novamente com cookies...");
            var retry = await TryGetExpectedOutputPathAsync(videoUrl, outputFolder, customFileName, browser, useCookies: true);
            if (retry.Success)
            {
                return retry.Path;
            }

            ThrowIfKnownError(retry.Log, browser, usingCookiesFile: true);
            return null;
        }

        ThrowIfKnownError(result.Log, browser, AppSettings.TryGetCookiesFilePath(out _));
        return null;
    }

    private async Task<(bool Success, string? Path, string Log)> TryGetExpectedOutputPathAsync(
        string videoUrl, string outputFolder, string customFileName, string browser, bool useCookies)
    {
        var psi = YtDlpLocator.CreateProcessStartInfo();

        AddCommonArguments(psi, browser, useCookies, videoUrl);
        AddFormatArguments(psi);
        psi.ArgumentList.Add("--no-download");
        psi.ArgumentList.Add("--print");
        psi.ArgumentList.Add("filename");
        psi.ArgumentList.Add("-o");
        psi.ArgumentList.Add(BuildOutputTemplate(outputFolder, customFileName));
        psi.ArgumentList.Add(videoUrl);

        using var process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Não foi possível iniciar o processo do yt-dlp.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;
        var log = $"{output}{Environment.NewLine}{error}";

        if (process.ExitCode != 0)
        {
            return (false, null, log);
        }

        var line = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return (string.IsNullOrWhiteSpace(line) ? false : true, line, log);
    }

    private static void ThrowIfKnownError(string log, string browser, bool usingCookiesFile)
    {
        if (BrowserCookieHelper.TryGetCookieDatabaseErrorMessage(log, browser, out var cookieMessage)
            || BrowserCookieHelper.TryGetDpapiErrorMessage(log, out cookieMessage)
            || BrowserCookieHelper.TryGetInstagramNotFoundMessage(log, usingCookiesFile, out cookieMessage)
            || BrowserCookieHelper.TryGetAuthErrorMessage(log, browser, usingCookiesFile, out cookieMessage))
        {
            throw new InvalidOperationException(cookieMessage);
        }
    }

    private async Task RunYtDlpAsync(string videoUrl, string outputFolder, string customFileName, bool forceOverwrite, string browser)
    {
        var useCookies = ShouldUseCookiesOnFirstAttempt(videoUrl);
        if (CookieFileHelper.IsInstagramUrl(videoUrl) && !useCookies)
        {
            AppendLog("Instagram: baixando sem cookies.");
        }

        if (CookieFileHelper.IsYouTubeUrl(videoUrl) && !useCookies)
        {
            AppendLog("YouTube: baixando sem cookies e sem retomar arquivo parcial.");
        }

        var (success, log) = await TryRunYtDlpAsync(videoUrl, outputFolder, customFileName, forceOverwrite, browser, useCookies);
        if (success)
        {
            return;
        }

        if (CookieFileHelper.IsYouTubeUrl(videoUrl) && !useCookies)
        {
            if (CookieFileHelper.IsHttp403(log))
            {
                AppendLog("YouTube: 403 ao baixar. Reiniciando o arquivo (sem retomar o .part).");
                var youtubeRetry = await TryRunYtDlpAsync(videoUrl, outputFolder, customFileName, forceOverwrite: true, browser, useCookies: false);
                if (youtubeRetry.Success)
                {
                    return;
                }
            }

            AppendLog("YouTube: tentando novamente com cookies...");
            var cookieRetry = await TryRunYtDlpAsync(videoUrl, outputFolder, customFileName, forceOverwrite: true, browser, useCookies: true);
            if (cookieRetry.Success)
            {
                return;
            }

            ThrowIfKnownError(cookieRetry.Log, browser, usingCookiesFile: AppSettings.TryGetCookiesFilePath(out _));
            throw new InvalidOperationException("Falha no YouTube mesmo com cookies.");
        }

        if (CookieFileHelper.IsInstagramUrl(videoUrl) && !useCookies && CookieFileHelper.RequiresAuthRetry(log))
        {
            AppendLog("Instagram: tentando novamente com cookies...");
            var retrySuccess = await TryRunYtDlpAsync(videoUrl, outputFolder, customFileName, forceOverwrite, browser, useCookies: true);
            if (retrySuccess.Success)
            {
                return;
            }

            ThrowIfKnownError(retrySuccess.Log, browser, usingCookiesFile: true);
            throw new InvalidOperationException("Falha no download do Instagram mesmo com cookies.");
        }

        ThrowIfKnownError(log, browser, useCookies && AppSettings.TryGetCookiesFilePath(out _));
        throw new InvalidOperationException("Falha no download.");
    }

    private async Task<(bool Success, string Log)> TryRunYtDlpAsync(
        string videoUrl, string outputFolder, string customFileName, bool forceOverwrite, string browser, bool useCookies)
    {
        var psi = YtDlpLocator.CreateProcessStartInfo();

        AddCommonArguments(psi, browser, useCookies, videoUrl);
        AddFormatArguments(psi);
        if (forceOverwrite)
        {
            psi.ArgumentList.Add("--force-overwrites");
        }

        psi.ArgumentList.Add("-o");
        psi.ArgumentList.Add(BuildOutputTemplate(outputFolder, customFileName));
        psi.ArgumentList.Add(videoUrl);

        using var process = new System.Diagnostics.Process { StartInfo = psi, EnableRaisingEvents = true };
        var outputLog = new System.Text.StringBuilder();

        void LogLine(string? line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            outputLog.AppendLine(line);
            AppendLog(line);
        }

        process.OutputDataReceived += (_, args) => LogLine(args.Data);
        process.ErrorDataReceived += (_, args) => LogLine(args.Data);

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Não foi possível iniciar o processo do yt-dlp.");
            }
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new InvalidOperationException(
                $"Não foi possível executar o yt-dlp em {YtDlpLocator.GetExecutablePath()}.", ex);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();
        var log = outputLog.ToString();

        if (process.ExitCode == 0)
        {
            return (true, log);
        }

        return (false, log);
    }

    private void AppendLog(string message)
    {
        if (txtLog.InvokeRequired)
        {
            txtLog.Invoke(() => AppendLog(message));
            return;
        }

        if (string.IsNullOrEmpty(message))
        {
            txtLog.AppendText(Environment.NewLine);
            return;
        }

        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void statusLabelVersion_Click(object sender, EventArgs e)
    {

    }
}
