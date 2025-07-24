using System.Text.Json;
using TNO.mIRC;

public static class ConfigService
{
    public static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TNO.mIRC", "config.json");

    public static void Load()
    {
        if (File.Exists(ConfigPath))
        {
            var json = File.ReadAllText(ConfigPath);
            Common.Config = JsonSerializer.Deserialize<AppConfig>(json);
        }
        else
        {
            Common.Config = new AppConfig();
        }
    }

    public static void Save()
    {
        var dir = Path.GetDirectoryName(ConfigPath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(Common.Config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}
