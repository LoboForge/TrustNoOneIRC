using BotCore.Interfaces;
using LoboForge.TNOIRC.Shared.Models;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TNO.IRC.BotEngine;

public class BotLoader
{
    private readonly string _botPath;
    private readonly BotCompiler _compiler = new();

    /// <summary>
    /// If true, load bots from referenced DLLs/namespaces (dev/debug mode).
    /// If true, load bots from .cs files in _botPath (Roslyn script mode).
    /// </summary>
    public bool UseDllBots { get; set; } = true;
    public bool UseScriptBots { get; set; } = true;
    /// Namespace to filter for DLL bots (set to null for any).
    public string? DllNamespaceFilter { get; set; } = "BotScripts";

    public BotLoader(string botPath)
    {
        _botPath = botPath;
    }

    public void LoadAll()
    {
        // --- 1. Load Bots from Referenced DLLs (for Debugging/Development)
        if (UseDllBots)
        {
            Console.WriteLine("[BotLoader] Discovering bots in loaded assemblies...");
            var dllBots = DiscoverBotsFromAssemblies(DllNamespaceFilter);
            foreach (var type in dllBots)
            {
                try
                {
                    RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                    if (Activator.CreateInstance(type) is IBot instance)
                    {
                        Console.WriteLine($"[BotLoader][DLL] Initialized bot: {instance.Name}");
                    }
                }
                catch (Exception botEx)
                {
                    Console.WriteLine($"[BotLoader][DLL] Error initializing {type.Name}: {botEx.Message}");
                }
            }
        }

        // --- 2. Load Bots from Script Folder (.cs files, Roslyn)
        if (UseScriptBots)
        {
            if (!Directory.Exists(_botPath))
            {
                Console.WriteLine($"[BotLoader][Roslyn] Bot path does not exist: {_botPath}");
                return;
            }
            Console.WriteLine($"[BotLoader][Roslyn] Compiling all bots in: {_botPath}");
            var types = _compiler.CompileAll(_botPath, out var assembly, out var errors);

            if (errors.Any())
            {
                Console.WriteLine($"[BotLoader][Roslyn] Errors during compilation:");
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
                        Console.WriteLine($"[BotLoader][Roslyn] Initialized bot: {instance.Name}");
                    }
                }
                catch (Exception botEx)
                {
                    Console.WriteLine($"[BotLoader][Roslyn] Error initializing {type.Name}: {botEx.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Discovers all IBot implementations from all loaded assemblies, optionally filtering by namespace.
    /// </summary>
    public static List<Type> DiscoverBotsFromAssemblies(string? targetNamespace = null)
    {
        var botTypes = new List<Type>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var types = asm.GetTypes().Where(t =>
                    t.IsClass && !t.IsAbstract &&
                    typeof(IBot).IsAssignableFrom(t) &&
                    (targetNamespace == null || t.Namespace == targetNamespace));
                botTypes.AddRange(types);
            }
            catch
            {
                // Some assemblies (e.g. dynamic) may throw; skip them.
            }
        }
        return botTypes;
    }
}
