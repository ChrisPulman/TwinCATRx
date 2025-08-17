// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CP.TwinCatRx.Core;

/// <summary>
/// AOT annotations for reflection-based helpers.
/// </summary>
public static class AotAnnotations
{
    /// <summary>
    /// Loads an assembly from a file path using reflection. Requires dynamic code and may be affected by trimming.
    /// </summary>
    /// <param name="path">The full path to the assembly file.</param>
    /// <returns>The loaded assembly or null if the file does not exist.</returns>
    [RequiresDynamicCode("Loads an assembly at runtime via Assembly.Load which requires dynamic code.")]
    [RequiresUnreferencedCode("Uses reflection-based assembly loading and type access which may be trimmed.")]
    public static Assembly? LoadAssembly(string path) => path.AssemblyLoad();

    /// <summary>
    /// Gets a type by name from an assembly path using reflection. Requires dynamic code and may be affected by trimming.
    /// </summary>
    /// <param name="assemblyPath">The assembly path.</param>
    /// <param name="typeName">Full type name to resolve.</param>
    /// <returns>The resolved type or null.</returns>
    [RequiresDynamicCode("Accesses type by name using reflection which may require dynamic code.")]
    [RequiresUnreferencedCode("Uses reflection to access type by name which may be trimmed in AOT.")]
    public static Type? GetTypeFromAssembly(string assemblyPath, string typeName) => assemblyPath.GetType(typeName);
}
