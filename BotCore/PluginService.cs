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

            foreach (var file in Directory.GetFiles(botPath, "*.cs", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var code = File.ReadAllText(file);
                    var compiler = new BotCompiler();
                    var types = compiler.Compile(code, out var assembly, out var compileErrors);

                    if (compileErrors.Any())
                    {
                        _errors.AddRange(compileErrors.Select(e => $"[{Path.GetFileName(file)}] {e}"));
                        continue;
                    }

                    foreach (var type in types)
                    {
                        RuntimeHelpers.RunClassConstructor(type.TypeHandle);

                        if (Activator.CreateInstance(type) is IBot instance)
                        {
                            var metadata = new BotMetadata
                            {
                                Name = instance.Name,
                                Enabled = true,
                                Instance = instance,
                                SourceAssembly = assembly!
                            };

                            _bots.Add(metadata);
                            instance.OnStart(); // Auto-start on load
                        }
                    }
                }
                catch (Exception ex)
                {
                    _errors.Add($"[{Path.GetFileName(file)}] {ex.Message}");
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
