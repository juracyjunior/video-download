using System.ComponentModel;

namespace VideoDownloaderDesktop;

public partial class DownloaderForm : Form
{
    private readonly BindingList<DownloadQueueItem> _downloadQueue = [];
    private readonly AppSettingsRepository _settingsRepository;
    private readonly CookieProvider _cookieProvider;
    private readonly DownloadService _downloadService;
    private bool _isDownloading;

    internal DownloaderForm(AppSettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
        _cookieProvider = new CookieProvider(settingsRepository);
        _downloadService = new DownloadService(new YtDlpClient(_cookieProvider), new DownloadErrorTranslator(_cookieProvider));
        InitializeComponent();
        gridDownloads.AutoGenerateColumns = false;
        gridDownloads.DataSource = _downloadQueue;
        var settings = _settingsRepository.Load();
        txtOutputFolder.Text = settings.LastOutputFolder ?? string.Empty;
        statusLabelVersion.Text = string.IsNullOrWhiteSpace(settings.Version) ? "Versão não configurada" : $"v{settings.Version.Trim()}";
        InitializeBrowserSelector();
        InitializeResolutionSelector();
        UpdateCookieControls();
    }

    private void InitializeBrowserSelector()
    {
        cboBrowser.Items.AddRange([new BrowserOption("chrome", "Google Chrome"), new BrowserOption("edge", "Microsoft Edge"), new BrowserOption("firefox", "Mozilla Firefox")]);
        cboBrowser.DisplayMember = nameof(BrowserOption.DisplayName);
        cboBrowser.ValueMember = nameof(BrowserOption.BrowserId);
        cboBrowser.SelectedItem = cboBrowser.Items.Cast<BrowserOption>().FirstOrDefault(option => option.BrowserId == _cookieProvider.GetBrowser()) ?? cboBrowser.Items[0];
    }

    private string SelectedBrowser => (cboBrowser.SelectedItem as BrowserOption)?.BrowserId ?? "chrome";

    private void InitializeResolutionSelector()
    {
        cboResolution.Items.AddRange([new ResolutionOption(2160), new ResolutionOption(1440), new ResolutionOption(1080), new ResolutionOption(720), new ResolutionOption(480), new ResolutionOption(360)]);
        cboResolution.DisplayMember = nameof(ResolutionOption.Label);
        cboResolution.ValueMember = nameof(ResolutionOption.Height);
        cboResolution.SelectedItem = cboResolution.Items.Cast<ResolutionOption>().First(option => option.Height == 1080);
    }

    private int SelectedResolution => (cboResolution.SelectedItem as ResolutionOption)?.Height ?? 1080;

    private void UpdateCookieControls()
    {
        var usesFile = _cookieProvider.TryGetFilePath(out var path);
        cboBrowser.Enabled = !usesFile && !_isDownloading;
        cboResolution.Enabled = !_isDownloading;
        lblBrowser.Enabled = !usesFile && !_isDownloading;
        lblBrowser.Text = usesFile ? "Cookies: arquivo" : "Cookies do navegador:";
        lblCookieStatus.Text = usesFile ? $"Arquivo de cookies carregado: {path}" : $"Nenhum arquivo encontrado em: {_cookieProvider.GetExpectedFilePath()}";
    }

    private void cboBrowser_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cboBrowser.SelectedItem is BrowserOption option) _cookieProvider.SaveBrowser(option.BrowserId);
    }

    private void btnBrowseFolder_Click(object sender, EventArgs e)
    {
        if (Directory.Exists(txtOutputFolder.Text)) folderBrowserDialog.SelectedPath = txtOutputFolder.Text;
        if (folderBrowserDialog.ShowDialog() != DialogResult.OK) return;
        txtOutputFolder.Text = folderBrowserDialog.SelectedPath;
        var settings = _settingsRepository.Load();
        settings.LastOutputFolder = folderBrowserDialog.SelectedPath;
        _settingsRepository.Save(settings);
    }

    private async void btnDownload_Click(object sender, EventArgs e)
    {
        if (_isDownloading) return;
        var request = new DownloadRequest(ParseUrls(txtUrl.Text), txtOutputFolder.Text.Trim(), txtFileName.Text.Trim(), SelectedBrowser, SelectedResolution);
        var validation = _downloadService.ValidateRequest(request);
        if (!validation.IsValid)
        {
            MessageBox.Show(validation.ErrorMessage, "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_cookieProvider.UsesBrowserCookies()) _cookieProvider.SaveBrowser(request.Browser);
        SetDownloadingUi(true);
        txtLog.Clear();
        _downloadQueue.Clear();
        foreach (var item in _downloadService.CreateQueue(request, AppendLog)) _downloadQueue.Add(item);
        RefreshDownloadGrid();
        AppendLog(CookieSourceMessage(request.Browser));
        if (_cookieProvider.UsesBrowserCookies()) LogBrowserProcessInfo(request.Browser);
        AppendLog($"Iniciando download de {_downloadQueue.Count} vídeo(s)...");

        var successes = 0;
        var errors = 0;
        try
        {
            for (var index = 0; index < _downloadQueue.Count; index++)
            {
                var item = _downloadQueue[index];
                AppendLog(string.Empty);
                AppendLog($"[{index + 1}/{_downloadQueue.Count}] Baixando como \"{item.FileName}\"...");
                AppendLog($"URL: {item.Url}");
                if (await DownloadItemAsync(item)) successes++; else errors++;
            }
        }
        finally
        {
            AppendLog(string.Empty);
            AppendLog("========== RESUMO ==========");
            AppendLog($"Total: {_downloadQueue.Count} | Sucessos: {successes} | Erros: {errors}");
            AppendLog("============================");
            SetDownloadingUi(false);
            RefreshDownloadGrid();
        }
    }

    private string CookieSourceMessage(string browser) => _cookieProvider.TryGetFilePath(out var path) ? $"Usando arquivo de cookies: {path}" : $"Usando cookies do navegador: {_cookieProvider.GetBrowserArgument(browser)}";

    private async Task<bool> DownloadItemAsync(DownloadQueueItem item)
    {
        UpdateItemStatus(item, DownloadItemStatus.Downloading, null);
        try
        {
            var result = await _downloadService.DownloadAsync(item, ConfirmOverwriteAsync, AppendLog);
            if (result.Success)
            {
                AppendLog($"Sucesso: {item.FileName}");
                UpdateItemStatus(item, DownloadItemStatus.Success, null);
                return true;
            }
            AppendLog($"Erro em \"{item.FileName}\": {result.ErrorMessage}");
            UpdateItemStatus(item, DownloadItemStatus.Error, result.ErrorMessage);
            return false;
        }
        catch (Exception exception)
        {
            AppendLog($"Erro em \"{item.FileName}\": {exception.Message}");
            UpdateItemStatus(item, DownloadItemStatus.Error, exception.Message);
            return false;
        }
    }

    private Task<bool> ConfirmOverwriteAsync(string path) => Task.FromResult(MessageBox.Show($"O arquivo já existe:\n{path}\n\nDeseja substituir?", "Arquivo existente", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes);

    private void gridDownloads_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != colRetry.Index || _isDownloading || e.RowIndex >= _downloadQueue.Count) return;
        var item = _downloadQueue[e.RowIndex];
        if (item.Status != DownloadItemStatus.Error) return;
        RetryItemAsync(item);
    }

    private async void RetryItemAsync(DownloadQueueItem item)
    {
        SetDownloadingUi(true);
        AppendLog($"Retentando \"{item.FileName}\"...");
        await DownloadItemAsync(item);
        SetDownloadingUi(false);
        RefreshDownloadGrid();
    }

    private void gridDownloads_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _downloadQueue.Count) return;
        var item = _downloadQueue[e.RowIndex];
        if (e.ColumnIndex == colStatus.Index)
        {
            e.Value = item.StatusText;
            e.FormattingApplied = true;
            gridDownloads.Rows[e.RowIndex].DefaultCellStyle.ForeColor = item.Status switch { DownloadItemStatus.Success => Color.DarkGreen, DownloadItemStatus.Error => Color.DarkRed, DownloadItemStatus.Downloading => Color.DarkBlue, _ => SystemColors.ControlText };
        }
        if (e.ColumnIndex == colRetry.Index)
        {
            e.Value = item.Status == DownloadItemStatus.Error ? "Retentar" : string.Empty;
            e.FormattingApplied = true;
        }
    }

    private void UpdateItemStatus(DownloadQueueItem item, DownloadItemStatus status, string? errorMessage)
    {
        item.Status = status;
        item.ErrorMessage = errorMessage;
        var index = _downloadQueue.IndexOf(item);
        if (index >= 0) _downloadQueue.ResetItem(index);
        gridDownloads.Invalidate();
    }

    private void RefreshDownloadGrid()
    {
        gridDownloads.Refresh();
        gridDownloads.Invalidate();
    }

    private void SetDownloadingUi(bool downloading)
    {
        _isDownloading = downloading;
        btnDownload.Enabled = !downloading;
        btnBrowseFolder.Enabled = !downloading;
        txtUrl.Enabled = !downloading;
        txtFileName.Enabled = !downloading;
        txtOutputFolder.Enabled = !downloading;
        UpdateCookieControls();
    }

    private static List<string> ParseUrls(string value) => value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private void LogBrowserProcessInfo(string browser)
    {
        var name = BrowserCookieHelper.GetBrowserDisplayName(browser);
        if (browser is "chrome" or "edge") AppendLog("Dica: Chrome/Edge no Windows podem falhar ao ler cookies. Use Firefox ou exporte todos os cookies para lib\\cookies.txt.");
        if (BrowserCookieHelper.HasVisibleBrowserWindow(browser))
        {
            AppendLog($"Aviso: o {name} está com janela aberta. Feche o navegador para evitar erro ao ler cookies.");
            return;
        }
        var processes = BrowserCookieHelper.GetBackgroundProcessCount(browser);
        if (processes > 0) AppendLog($"Aviso: há {processes} processo(s) do {name} em segundo plano. Se der erro de cookies, finalize-os no Gerenciador de Tarefas.");
    }

    private void AppendLog(string message)
    {
        if (txtLog.InvokeRequired)
        {
            txtLog.Invoke(() => AppendLog(message));
            return;
        }

        txtLog.AppendText(string.IsNullOrEmpty(message) ? Environment.NewLine : $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private sealed class BrowserOption(string browserId, string displayName)
    {
        public string BrowserId { get; } = browserId;
        public string DisplayName { get; } = displayName;
    }

    private sealed class ResolutionOption(int height)
    {
        public int Height { get; } = height;
        public string Label => $"{Height}p";
    }
}
