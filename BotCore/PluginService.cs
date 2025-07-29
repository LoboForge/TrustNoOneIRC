using BotCore.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Runtime.CompilerServices;
using TNO.IRC.BotEngine;

namespace LoboForge.TNOIRC.BotEngine
{
    public class PluginService
    {
        private readonly List<IBotMetadata> _bots = new();
        private readonly List<string> _errors = new();
        private readonly string _defaultBotDirectory = Path.Combine(AppContext.BaseDirectory, "Bots");

        /// <summary>
        /// Reloads all bot scripts from the specified folder or the default /Bots directory.
        /// Also discovers DLL-based bots (class libraries).
        /// </summary>
        /// <param name="folderOverride">Optional path to a bot folder. If null or empty, defaults to /Bots.</param>
        public (List<IBotMetadata> Bots, List<string> Errors) ReloadBots(string? folderOverride = null)
        {
            _bots.Clear();
            _errors.Clear();

            var botPath = !string.IsNullOrWhiteSpace(folderOverride)
                ? Path.GetFullPath(folderOverride)
                : _defaultBotDirectory;

            if (!Directory.Exists(botPath))
                Directory.CreateDirectory(botPath);

            // ---- 1. Roslyn CODE-ON-DEMAND Bots (.cs) ----
            var compiler = new BotCompiler();
            var codTypes = compiler.CompileAll(botPath, out var codAssembly, out var compileErrors);

            if (compileErrors.Any())
                _errors.AddRange(compileErrors.Select(e => $"[COD Compilation] {e}"));

            foreach (var type in codTypes)
            {
                try
                {
                    RuntimeHelpers.RunClassConstructor(type.TypeHandle);

                    if (Activator.CreateInstance(type) is IBot instance)
                    {
                        var metadata = new BotMetadata
                        {
                            Name = instance.Name,
                            Enabled = true,
                            Instance = instance,
                            SourceAssembly = codAssembly!,
                            SourceType = "COD"
                        };
                        _bots.Add(metadata);
                        instance.OnStart();
                    }
                }
                catch (Exception botEx)
                {
                    _errors.Add($"[COD Compilation] Error initializing {type.Name}: {botEx.Message}");
                }
            }

            // ---- 2. DLL Bots ----
            foreach (var dll in Directory.GetFiles(botPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dll);
                    var botTypes = assembly.GetTypes()
                        .Where(t => typeof(IBot).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

                    foreach (var type in botTypes)
                    {
                        try
                        {
                            RuntimeHelpers.RunClassConstructor(type.TypeHandle);

                            if (Activator.CreateInstance(type) is IBot instance)
                            {
                                var metadata = new BotMetadata
                                {
                                    Name = instance.Name,
                                    Enabled = true,
                                    Instance = instance,
                                    SourceAssembly = assembly,
                                    SourceType = "DLL"
                                };
                                _bots.Add(metadata);
                                instance.OnStart();
                            }
                        }
                        catch (Exception botEx)
                        {
                            _errors.Add($"[DLL Load] Error initializing {type.FullName}: {botEx.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _errors.Add($"[DLL Load] Failed to load {dll}: {ex.Message}");
                }
            }

            return (_bots.ToList(), _errors.ToList());
        }

        /// <summary>
        /// Enables or disables a bot instance based on its Enabled state.
        /// </summary>
        public void ApplyBotState(IBotMetadata bot)
        {
            if (bot.Enabled)
            {
                bot.Instance?.OnStart();
            }
            else
            {
                bot.Instance?.OnStop();
            }
        }

        /// <summary>
        /// Optional ad-hoc compile helper (currently unused).
        /// </summary>
        private Assembly? Compile(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location));

            var compilation = CSharpCompilation.Create(
                Path.GetRandomFileName(),
                new[] { tree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            if (!result.Success)
            {
                foreach (var diag in result.Diagnostics)
                {
                    _errors.Add(diag.ToString());
                }
                return null;
            }

            ms.Seek(0, SeekOrigin.Begin);
            return Assembly.Load(ms.ToArray());
        }
    }
}
