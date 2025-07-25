using BotCore.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace TNO.IRC.BotEngine;

public class BotCompiler
{

    public IEnumerable<Type> Compile(string code, out Assembly? assembly, out List<string> errors)
    {
        errors = new();
        assembly = null;

        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        // Collect default runtime references
        var references = new List<MetadataReference>();
        var loadedAssemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => a.Location)
            .Distinct();

        foreach (var path in loadedAssemblies)
        {
            try
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
            catch (Exception ex)
            {
                errors.Add($"[BotCompiler] Failed to load reference: {path} - {ex.Message}");
            }
        }

        // Manually include known critical project DLLs
        string[] manualDlls =
        {
            "Bots\\BotCore.dll",
            "Bots\\LoboForge.TNOIRC.Shared.dll"
        };

        var baseDir = AppContext.BaseDirectory;
        foreach (var dll in manualDlls)
        {
            var fullPath = Path.Combine(baseDir, dll);
            if (File.Exists(fullPath))
            {
                references.Add(MetadataReference.CreateFromFile(fullPath));
            }
            else
            {
                errors.Add($"[BotCompiler] Warning: Required DLL not found: {fullPath}");
            }
        }

        var compilation = CSharpCompilation.Create(
            Path.GetRandomFileName(),
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            errors.AddRange(result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString()));
            return Enumerable.Empty<Type>();
        }

        ms.Position = 0;
        assembly = Assembly.Load(ms.ToArray());

        var botTypes = assembly
            .GetTypes()
            .Where(t =>
                t.Namespace == "BotScripts" &&
                typeof(IBot).IsAssignableFrom(t) &&
                t.IsClass && !t.IsAbstract)
            .ToList();

        return botTypes;
    }

}
