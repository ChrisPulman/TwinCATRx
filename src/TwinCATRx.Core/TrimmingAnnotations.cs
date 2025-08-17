// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CP.TwinCatRx.Core;

/// <summary>
/// Trimming/AOT annotations for methods that use reflection or dynamic code.
/// </summary>
public static class TrimmingAnnotations
{
    /// <summary>
    /// Emits a compiled assembly from source using Roslyn.
    /// </summary>
    /// <param name="code">Source code.</param>
    /// <param name="assemblyFileName">Output assembly path.</param>
    /// <returns>True if successful; otherwise false.</returns>
    [RequiresDynamicCode("Emits and loads assemblies dynamically.")]
    [RequiresUnreferencedCode("Dynamic compilation may access trimmed members.")]
    public static bool EmitAssembly(string code, string assemblyFileName) => CSharpLanguage.CreateAssembly(code, assemblyFileName);

    /// <summary>
    /// Loads an assembly from disk using reflection.
    /// </summary>
    /// <param name="path">Assembly path.</param>
    /// <returns>Loaded assembly or null.</returns>
    [RequiresDynamicCode("Loads an assembly at runtime via Assembly.Load which requires dynamic code.")]
    [RequiresUnreferencedCode("Uses reflection-based assembly loading and type access which may be trimmed.")]
    public static Assembly? LoadAssembly(string path) => path.AssemblyLoad();

    /// <summary>
    /// Resolves a type by name from an assembly path.
    /// </summary>
    /// <param name="assemblyPath">Assembly path.</param>
    /// <param name="typeName">Full type name.</param>
    /// <returns>Type or null.</returns>
    [RequiresDynamicCode("Accesses type by name using reflection which may require dynamic code.")]
    [RequiresUnreferencedCode("Uses reflection to access type by name which may be trimmed in AOT.")]
    public static Type? ResolveType(string assemblyPath, string typeName) => assemblyPath.GetType(typeName);
}
