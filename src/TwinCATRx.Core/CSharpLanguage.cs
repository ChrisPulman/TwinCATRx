// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;

namespace CP.TwinCatRx.Core;

/// <summary>
/// C Sharp Language.
/// </summary>
/// <seealso cref="ILanguageService" />
internal class CSharpLanguage : ILanguageService
{
    private static readonly IReadOnlyCollection<MetadataReference> _references =
    [
      MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
      MetadataReference.CreateFromFile(typeof(ValueTuple<>).GetTypeInfo().Assembly.Location)
    ];

    private static readonly LanguageVersion MaxLanguageVersion = Enum
        .GetValues(typeof(LanguageVersion))
        .Cast<LanguageVersion>()
        .Max();

    /// <summary>
    /// Creates the assembly.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="assemblyFileName">Name of the assembly file.</param>
    /// <returns>A bool.</returns>
    public static bool CreateAssembly(string code, string assemblyFileName)
    {
        var sourceLanguage = new CSharpLanguage();
        var syntaxTree = sourceLanguage.ParseText(code, SourceCodeKind.Regular);
        var compilation = sourceLanguage
          .CreateLibraryCompilation(
              assemblyName: assemblyFileName?.Replace(".dll", string.Empty)!,
              enableOptimisations: false)
          .AddReferences(_references)
          .AddSyntaxTrees(syntaxTree);

        if (string.IsNullOrWhiteSpace(assemblyFileName))
        {
            return false;
        }

        using (var stream = new FileStream(assemblyFileName, FileMode.Create))
        {
            var emitResult = compilation.Emit(stream);

            if (emitResult.Success)
            {
                stream.Seek(0, SeekOrigin.Begin);
                _ = AssemblyDefinition.ReadAssembly(stream);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Parses the text.
    /// </summary>
    /// <param name="code">The source code.</param>
    /// <param name="kind">The kind.</param>
    /// <returns>The Syntax Tree.</returns>
    public SyntaxTree ParseText(string code, SourceCodeKind kind)
    {
        var options = new CSharpParseOptions(languageVersion: MaxLanguageVersion, kind: kind);

        // Return a syntax tree of our source code
        return CSharpSyntaxTree.ParseText(code, options);
    }

    /// <summary>
    /// Creates the library compilation.
    /// </summary>
    /// <param name="assemblyName">Name of the assembly.</param>
    /// <param name="enableOptimisations">if set to <c>true</c> [enable optimisations].</param>
    /// <returns>The Compilation.</returns>
    public Compilation CreateLibraryCompilation(string assemblyName, bool enableOptimisations)
    {
        var options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: enableOptimisations ? OptimizationLevel.Release : OptimizationLevel.Debug,
            allowUnsafe: true);

        return CSharpCompilation.Create(assemblyName, references: _references, options: options);
    }
}
