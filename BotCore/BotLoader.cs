using BotCore.Interfaces;
using LoboForge.TNOIRC.Shared.Models;
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

        Console.WriteLine($"[BotLoader] Compiling all bots in: {_botPath}");
        var types = _compiler.CompileAll(_botPath, out var assembly, out var errors);

        if (errors.Any())
        {
            Console.WriteLine($"[BotLoader] Errors during compilation:");
            foreach (var err in errors)
                Console.WriteLine($"  {err}");
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

}
