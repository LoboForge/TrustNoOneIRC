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

        var references = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));

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
            foreach (var diag in result.Diagnostics)
                errors.Add(diag.ToString());
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
