namespace VideoDownloaderDesktop;

partial class DownloaderForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        lblUrl = new Label();
        txtUrl = new TextBox();
        lblOutputFolder = new Label();
        txtOutputFolder = new TextBox();
        lblFileName = new Label();
        txtFileName = new TextBox();
        btnBrowseFolder = new Button();
        btnDownload = new Button();
        lblBrowser = new Label();
        cboBrowser = new ComboBox();
        lblResolution = new Label();
        cboResolution = new ComboBox();
        lblCookieStatus = new Label();
        lblQueue = new Label();
        gridDownloads = new DataGridView();
        colFileName = new DataGridViewTextBoxColumn();
        colUrl = new DataGridViewTextBoxColumn();
        colStatus = new DataGridViewTextBoxColumn();
        colRetry = new DataGridViewButtonColumn();
        txtLog = new TextBox();
        statusStrip = new StatusStrip();
        statusLabelApp = new ToolStripStatusLabel();
        statusLabelVersion = new ToolStripStatusLabel();
        statusLabelDeveloper = new ToolStripStatusLabel();
        folderBrowserDialog = new FolderBrowserDialog();
        ((System.ComponentModel.ISupportInitialize)gridDownloads).BeginInit();
        statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // lblUrl
        // 
        lblUrl.AutoSize = true;
        lblUrl.Location = new Point(12, 15);
        lblUrl.Name = "lblUrl";
        lblUrl.Size = new Size(95, 15);
        lblUrl.TabIndex = 0;
        lblUrl.Text = "URLs dos vídeos:";
        // 
        // txtUrl
        // 
        txtUrl.AcceptsReturn = true;
        txtUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtUrl.Location = new Point(139, 12);
        txtUrl.Multiline = true;
        txtUrl.Name = "txtUrl";
        txtUrl.PlaceholderText = "Uma URL por linha";
        txtUrl.ScrollBars = ScrollBars.Vertical;
        txtUrl.Size = new Size(630, 90);
        txtUrl.TabIndex = 1;
        // 
        // lblOutputFolder
        // 
        lblOutputFolder.AutoSize = true;
        lblOutputFolder.Location = new Point(12, 115);
        lblOutputFolder.Name = "lblOutputFolder";
        lblOutputFolder.Size = new Size(96, 15);
        lblOutputFolder.TabIndex = 2;
        lblOutputFolder.Text = "Pasta de destino:";
        // 
        // txtOutputFolder
        // 
        txtOutputFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtOutputFolder.Location = new Point(139, 112);
        txtOutputFolder.Name = "txtOutputFolder";
        txtOutputFolder.PlaceholderText = "ex.: C:\\temp";
        txtOutputFolder.Size = new Size(535, 23);
        txtOutputFolder.TabIndex = 3;
        // 
        // lblFileName
        // 
        lblFileName.AutoSize = true;
        lblFileName.Location = new Point(12, 147);
        lblFileName.Name = "lblFileName";
        lblFileName.Size = new Size(103, 15);
        lblFileName.TabIndex = 4;
        lblFileName.Text = "Nome do arquivo:";
        // 
        // txtFileName
        // 
        txtFileName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtFileName.Location = new Point(139, 144);
        txtFileName.Name = "txtFileName";
        txtFileName.PlaceholderText = "Base do nome (ex.: react100)";
        txtFileName.Size = new Size(535, 23);
        txtFileName.TabIndex = 5;
        // 
        // btnBrowseFolder
        // 
        btnBrowseFolder.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseFolder.Location = new Point(680, 111);
        btnBrowseFolder.Name = "btnBrowseFolder";
        btnBrowseFolder.Size = new Size(89, 25);
        btnBrowseFolder.TabIndex = 6;
        btnBrowseFolder.Text = "Selecionar...";
        btnBrowseFolder.UseVisualStyleBackColor = true;
        btnBrowseFolder.Click += btnBrowseFolder_Click;
        // 
        // btnDownload
        // 
        btnDownload.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnDownload.Location = new Point(680, 142);
        btnDownload.Name = "btnDownload";
        btnDownload.Size = new Size(89, 25);
        btnDownload.TabIndex = 7;
        btnDownload.Text = "Baixar";
        btnDownload.UseVisualStyleBackColor = true;
        btnDownload.Click += btnDownload_Click;
        // 
        // lblBrowser
        // 
        lblBrowser.AutoSize = true;
        lblBrowser.Location = new Point(12, 179);
        lblBrowser.Name = "lblBrowser";
        lblBrowser.Size = new Size(128, 15);
        lblBrowser.TabIndex = 8;
        lblBrowser.Text = "Cookies do navegador:";
        // 
        // cboBrowser
        // 
        cboBrowser.DropDownStyle = ComboBoxStyle.DropDownList;
        cboBrowser.FormattingEnabled = true;
        cboBrowser.Location = new Point(139, 176);
        cboBrowser.Name = "cboBrowser";
        cboBrowser.Size = new Size(179, 23);
        cboBrowser.TabIndex = 9;
        cboBrowser.SelectedIndexChanged += cboBrowser_SelectedIndexChanged;
        lblResolution.AutoSize = true;
        lblResolution.Location = new Point(342, 179);
        lblResolution.Name = "lblResolution";
        lblResolution.Size = new Size(65, 15);
        lblResolution.TabIndex = 10;
        lblResolution.Text = "Resolução:";
        cboResolution.DropDownStyle = ComboBoxStyle.DropDownList;
        cboResolution.FormattingEnabled = true;
        cboResolution.Location = new Point(417, 176);
        cboResolution.Name = "cboResolution";
        cboResolution.Size = new Size(120, 23);
        cboResolution.TabIndex = 11;
        // 
        // 
        // lblCookieStatus
        // 
        lblCookieStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblCookieStatus.AutoEllipsis = true;
        lblCookieStatus.ForeColor = SystemColors.GrayText;
        lblCookieStatus.Location = new Point(12, 205);
        lblCookieStatus.Name = "lblCookieStatus";
        lblCookieStatus.Size = new Size(757, 15);
        lblCookieStatus.TabIndex = 12;
        lblCookieStatus.Text = "Arquivos de cookies";
        // 
        // lblQueue
        // 
        lblQueue.AutoSize = true;
        lblQueue.Location = new Point(12, 228);
        lblQueue.Name = "lblQueue";
        lblQueue.Size = new Size(105, 15);
        lblQueue.TabIndex = 13;
        lblQueue.Text = "Fila de downloads:";
        // 
        // gridDownloads
        // 
        gridDownloads.AllowUserToAddRows = false;
        gridDownloads.AllowUserToDeleteRows = false;
        gridDownloads.AllowUserToResizeRows = false;
        gridDownloads.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        gridDownloads.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        gridDownloads.BackgroundColor = SystemColors.Window;
        gridDownloads.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        gridDownloads.Columns.AddRange(new DataGridViewColumn[] { colFileName, colUrl, colStatus, colRetry });
        gridDownloads.Location = new Point(12, 246);
        gridDownloads.MultiSelect = false;
        gridDownloads.Name = "gridDownloads";
        gridDownloads.ReadOnly = true;
        gridDownloads.RowHeadersVisible = false;
        gridDownloads.RowTemplate.Height = 28;
        gridDownloads.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridDownloads.Size = new Size(757, 130);
        gridDownloads.TabIndex = 14;
        gridDownloads.CellContentClick += gridDownloads_CellContentClick;
        gridDownloads.CellFormatting += gridDownloads_CellFormatting;
        // 
        // colFileName
        // 
        colFileName.DataPropertyName = "FileName";
        colFileName.FillWeight = 90F;
        colFileName.HeaderText = "Arquivo";
        colFileName.Name = "colFileName";
        colFileName.ReadOnly = true;
        // 
        // colUrl
        // 
        colUrl.DataPropertyName = "Url";
        colUrl.FillWeight = 220F;
        colUrl.HeaderText = "URL";
        colUrl.Name = "colUrl";
        colUrl.ReadOnly = true;
        // 
        // colStatus
        // 
        colStatus.DataPropertyName = "StatusText";
        colStatus.FillWeight = 70F;
        colStatus.HeaderText = "Status";
        colStatus.Name = "colStatus";
        colStatus.ReadOnly = true;
        // 
        // colRetry
        // 
        colRetry.FillWeight = 55F;
        colRetry.HeaderText = "Ação";
        colRetry.Name = "colRetry";
        colRetry.ReadOnly = true;
        colRetry.Text = "Retentar";
        // 
        // txtLog
        // 
        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtLog.BackColor = SystemColors.Info;
        txtLog.Location = new Point(12, 385);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Both;
        txtLog.Size = new Size(757, 150);
        txtLog.TabIndex = 15;
        txtLog.WordWrap = false;
        // 
        // statusStrip
        // 
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabelApp, statusLabelVersion, statusLabelDeveloper });
        statusStrip.Location = new Point(0, 541);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(781, 22);
        statusStrip.SizingGrip = false;
        statusStrip.TabIndex = 9;
        statusStrip.Text = "statusStrip";
        // 
        // statusLabelApp
        // 
        statusLabelApp.Name = "statusLabelApp";
        statusLabelApp.Size = new Size(52, 17);
        statusLabelApp.Text = "Sovidow";
        // 
        // statusLabelVersion
        // 
        statusLabelVersion.Name = "statusLabelVersion";
        statusLabelVersion.Size = new Size(413, 17);
        statusLabelVersion.Spring = true;
        statusLabelVersion.Text = "v";
        // 
        // statusLabelDeveloper
        // 
        statusLabelDeveloper.Name = "statusLabelDeveloper";
        statusLabelDeveloper.Size = new Size(118, 17);
        statusLabelDeveloper.Text = "desenvolvido por jroi";
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(781, 563);
        Controls.Add(statusStrip);
        Controls.Add(txtLog);
        Controls.Add(gridDownloads);
        Controls.Add(lblQueue);
        Controls.Add(lblCookieStatus);
        Controls.Add(cboResolution);
        Controls.Add(lblResolution);
        Controls.Add(cboBrowser);
        Controls.Add(lblBrowser);
        Controls.Add(btnDownload);
        Controls.Add(btnBrowseFolder);
        Controls.Add(txtFileName);
        Controls.Add(lblFileName);
        Controls.Add(txtOutputFolder);
        Controls.Add(lblOutputFolder);
        Controls.Add(txtUrl);
        Controls.Add(lblUrl);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimumSize = new Size(796, 602);
        Name = "DownloaderForm";
        ShowIcon = false;
        SizeGripStyle = SizeGripStyle.Hide;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "SoViDow";
        ((System.ComponentModel.ISupportInitialize)gridDownloads).EndInit();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
    private Label lblUrl;
    private TextBox txtUrl;
    private Label lblOutputFolder;
    private TextBox txtOutputFolder;
    private Label lblFileName;
    private TextBox txtFileName;
    private Button btnBrowseFolder;
    private Button btnDownload;
    private Label lblBrowser;
    private ComboBox cboBrowser;
    private Label lblResolution;
    private ComboBox cboResolution;
    private Label lblCookieStatus;
    private Label lblQueue;
    private DataGridView gridDownloads;
    private DataGridViewTextBoxColumn colFileName;
    private DataGridViewTextBoxColumn colUrl;
    private DataGridViewTextBoxColumn colStatus;
    private DataGridViewButtonColumn colRetry;
    private TextBox txtLog;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel statusLabelApp;
    private ToolStripStatusLabel statusLabelVersion;
    private ToolStripStatusLabel statusLabelDeveloper;
    private FolderBrowserDialog folderBrowserDialog;

    #endregion
}
