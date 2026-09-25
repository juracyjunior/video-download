namespace VideoDownloaderDesktop;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new DownloaderForm(new AppSettingsRepository()));
    }    
}
