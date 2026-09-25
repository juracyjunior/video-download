using System.Text.Json;

namespace VideoDownloaderDesktop;

internal sealed class AppSettingsRepository
{
    private readonly string _configPath;

    public AppSettingsRepository(string? configPath = null)
    {
        _configPath = configPath ?? Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }

    public AppSettingsData Load()
    {
        if (!File.Exists(_configPath))
        {
            return new AppSettingsData();
        }

        try
        {
            return JsonSerializer.Deserialize<AppSettingsData>(File.ReadAllText(_configPath)) ?? new AppSettingsData();
        }
        catch (JsonException)
        {
            return new AppSettingsData();
        }
    }

    public void Save(AppSettingsData settings)
    {
        File.WriteAllText(_configPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}
