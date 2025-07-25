using BotCore.Interfaces;
using System.Reflection;
using System.Runtime.CompilerServices;
using TNO.IRC.BotEngine;

namespace BotCore.Roslyn;

public class BotWatcher
{
    private readonly string _botDirectory;
    private readonly Action<IBot> _onBotReload;
    private readonly FileSystemWatcher _watcher;

    public BotWatcher(string botDirectory, Action<IBot> onBotReload)
    {
        _botDirectory = botDirectory;
        _onBotReload = onBotReload;

        _watcher = new FileSystemWatcher(botDirectory, "*.cs")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };

        _watcher.Changed += ReloadBot;
        _watcher.Created += ReloadBot;
        _watcher.Renamed += ReloadBot;
        _watcher.EnableRaisingEvents = true;
    }

    private void ReloadBot(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (!File.Exists(e.FullPath)) return;

            var code = File.ReadAllText(e.FullPath);
            var compiler = new BotCompiler();
            var types = compiler.Compile(code, out var assembly, out var errors);

            if (errors.Any())
            {
                Console.WriteLine($"[HotReload Error] {Path.GetFileName(e.FullPath)}:");
                foreach (var error in errors)
                    Console.WriteLine("  " + error);
                return;
            }

            foreach (var type in types)
            {
                try
                {
                    RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                    if (Activator.CreateInstance(type) is IBot instance)
                    {
                        Console.WriteLine($"[HotReload] Reloaded bot: {instance.Name}");
                        _onBotReload(instance);
                    }
                }
                catch (Exception botEx)
                {
                    Console.WriteLine($"[HotReload Error] Failed to initialize {type.Name}: {botEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HotReload Exception] {ex.Message}");
        }
    }
}
