using LoboForge.TNOIRC;
using LoboForge.TNOIRC.Services;
using System.Reflection;
using System.Text.Json;

public static class ConfigService
{
    public static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LoboForge.TNOIRC", "config.json");

    public static void Load()
    {
        var dllPath = Path.Combine(AppContext.BaseDirectory, "GhostRootBot.dll");
        if (File.Exists(dllPath))
        {
            Assembly.LoadFrom(dllPath);
        }

        if (File.Exists(ConfigPath))
        {
            var json = File.ReadAllText(ConfigPath);
            Common.Config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        else
        {
            Common.Config = new AppConfig();
        }

        ApplyTorSettings();
    }

    public static void Save()
    {
        var dir = Path.GetDirectoryName(ConfigPath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(Common.Config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    private static void ApplyTorSettings()
    {
        TorSocks5Helper.TorExecutablePath = Common.Config.TorExecutablePath;
        if (Common.Config.TorSocksPort > 0)
            TorSocks5Helper.SocksPort = Common.Config.TorSocksPort;
    }
}
