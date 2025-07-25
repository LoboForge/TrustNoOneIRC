using BotCore.Interfaces;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TNO.IRC.BotEngine;

public class BotLoader
{
    private readonly string _botPath;
    private readonly BotCompiler _compiler = new();

    public BotLoader(string botPath)
    {
        _botPath = botPath;
    }

    public void LoadAll()
    {
        if (!Directory.Exists(_botPath))
        {
            Console.WriteLine($"[BotLoader] Bot path does not exist: {_botPath}");
            return;
        }

        foreach (var file in Directory.GetFiles(_botPath, "*.cs"))
        {
            Console.WriteLine($"[BotLoader] Loading: {Path.GetFileName(file)}");

            try
            {
                var code = File.ReadAllText(file);
                var types = _compiler.Compile(code, out var assembly, out var errors);

                if (errors.Any())
                {
                    Console.WriteLine($"[BotLoader] Errors in {Path.GetFileName(file)}:");
                    foreach (var err in errors)
                        Console.WriteLine($"  {err}");
                    continue;
                }

                foreach (var type in types)
                {
                    try
                    {
                        RuntimeHelpers.RunClassConstructor(type.TypeHandle);

                        if (Activator.CreateInstance(type) is IBot instance)
                        {
                            Console.WriteLine($"[BotLoader] Initialized bot: {instance.Name}");
                        }
                    }
                    catch (Exception botEx)
                    {
                        Console.WriteLine($"[BotLoader] Error initializing {type.Name}: {botEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BotLoader] Exception loading {file}: {ex.Message}");
            }
        }
    }
}
