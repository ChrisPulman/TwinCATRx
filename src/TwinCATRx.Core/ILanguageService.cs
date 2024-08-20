// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace CP.TwinCatRx.Core;

/// <summary>
/// I Language Service.
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Parses the text.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="kind">The kind.</param>
    /// <returns>A SyntaxTree.</returns>
    SyntaxTree ParseText(string code, SourceCodeKind kind);

    /// <summary>
    /// Creates the library compilation.
    /// </summary>
    /// <param name="assemblyName">Name of the assembly.</param>
    /// <param name="enableOptimisations">if set to <c>true</c> [enable optimisations].</param>
    /// <returns>A Compilation.</returns>
    Compilation CreateLibraryCompilation(string assemblyName, bool enableOptimisations);
}
