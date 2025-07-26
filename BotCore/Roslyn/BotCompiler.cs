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

        // Start with all currently loaded assemblies
        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .ToList();

        // 🔧 Manually ensure critical framework references are added
        var requiredFrameworkAssemblies = new[]
        {
            typeof(object).Assembly,                    // System.Private.CoreLib
            typeof(Console).Assembly,                   // System.Console
            typeof(List<>).Assembly,                    // System.Collections
            typeof(StringComparison).Assembly,          // System.Runtime (needed!)
            typeof(Enumerable).Assembly                 // System.Linq
        };

        foreach (var asm in requiredFrameworkAssemblies)
        {
            if (!assemblies.Contains(asm))
                assemblies.Add(asm);
        }

        var references = new List<MetadataReference>();
        foreach (var asm in assemblies.Distinct())
        {
            try
            {
                references.Add(MetadataReference.CreateFromFile(asm.Location));
            }
            catch (Exception ex)
            {
                errors.Add($"[BotCompiler] Failed to load reference: {asm.Location} - {ex.Message}");
            }
        }

        // 📦 Include known project DLLs if needed (already handled here)
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
