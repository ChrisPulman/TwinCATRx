// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace CP.TwinCatRx.Core;

/// <summary>
/// AOT annotations for dynamic compilation helpers.
/// </summary>
public static class CSharpLanguageAot
{
    /// <summary>
    /// Creates a dynamic assembly via Roslyn emit. Requires dynamic code and may be affected by trimming.
    /// </summary>
    /// <param name="code">C# source code.</param>
    /// <param name="assemblyFileName">Output assembly file path.</param>
    /// <returns>True if created successfully; otherwise false.</returns>
    [RequiresDynamicCode("Emits and loads assemblies dynamically.")]
    [RequiresUnreferencedCode("Dynamic compilation may access trimmed members.")]
    public static bool CreateAssemblyAotSafe(string code, string assemblyFileName) => CSharpLanguage.CreateAssembly(code, assemblyFileName);
}
