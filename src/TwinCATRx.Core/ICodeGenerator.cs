// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

#if REACTIVE_SHIM
namespace CP.TwinCatRx.Core.Reactive;
#else
namespace CP.TwinCatRx.Core;
#endif

/// <summary>Interface for Code Generator.</summary>
/// <seealso cref="IDisposable"/>
public interface ICodeGenerator : IDisposable
{
    /// <summary>Gets the symbol list.</summary>
    /// <value>The symbol list.</value>
    HashSet<INodeEmulator> SymbolList { get; }

    /// <summary>Creates the c sharp code.</summary>
    /// <param name="selectedTN">The selected tn.</param>
    /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
    /// <returns>A Value.</returns>
    bool CreateCSharpCode(INodeEmulator selectedTN, bool isTwinCat3 = false);

    /// <summary>Creates the c sharp code.</summary>
    /// <param name="selectedTN">The selected tn.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
    /// <param name="classNamespace">The class namespace.</param>
    /// <returns>A Value.</returns>
    bool CreateCSharpCode(INodeEmulator selectedTN, string fileName, bool isTwinCat3 = false, string classNamespace = CodeGeneratorDefaults.Namespace);

    /// <summary>Creates the c sharp code string.</summary>
    /// <param name="selectedTN">The selected tn.</param>
    /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
    /// <param name="classNamespace">The class namespace.</param>
    /// <returns>A Value.</returns>
    string CreateCSharpCodeString(INodeEmulator? selectedTN, bool isTwinCat3 = false, string classNamespace = CodeGeneratorDefaults.Namespace);

    /// <summary>Creates the DLL.</summary>
    /// <param name="selectedTN">The selected tn.</param>
    /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
    /// <returns>A Value.</returns>
#if NET8_0_OR_GREATER
    [RequiresDynamicCode("Emits and loads assemblies dynamically via Roslyn/Mono.Cecil.")]
    [RequiresUnreferencedCode("Dynamic compilation may access trimmed members.")]
#endif
    bool CreateDll(INodeEmulator selectedTN, bool isTwinCat3 = false);

    /// <summary>Creates the DLL.</summary>
    /// <param name="sourceCode">The C# source code.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <returns>A Value.</returns>
#if NET8_0_OR_GREATER
    [RequiresDynamicCode("Emits and loads assemblies dynamically via Roslyn/Mono.Cecil.")]
    [RequiresUnreferencedCode("Dynamic compilation may access trimmed members.")]
#endif
    bool CreateDll(string sourceCode, string fileName);

    /// <summary>Creates the DLL.</summary>
    /// <param name="selectedTN">The selected tn.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
    /// <param name="classNamespace">The class namespace.</param>
    /// <returns>A Value.</returns>
#if NET8_0_OR_GREATER
    [RequiresDynamicCode("Emits and loads assemblies dynamically via Roslyn/Mono.Cecil.")]
    [RequiresUnreferencedCode("Dynamic compilation may access trimmed members.")]
#endif
    bool CreateDll(INodeEmulator? selectedTN, string fileName, bool isTwinCat3 = false, string classNamespace = CodeGeneratorDefaults.Namespace);

    /// <summary>Loads the symbols.</summary>
    /// <param name="adsAddress">The ADS address.</param>
    /// <returns>A Value.</returns>
    HashSet<INodeEmulator> LoadSymbols(string adsAddress);

    /// <summary>Loads the symbols.</summary>
    /// <param name="port">The port.</param>
    /// <returns>A Value.</returns>
    HashSet<INodeEmulator> LoadSymbols(int port);

    /// <summary>Loads the symbols.</summary>
    /// <param name="adsAddress">The ADS address.</param>
    /// <param name="port">The port.</param>
    /// <returns>A Value.</returns>
    HashSet<INodeEmulator> LoadSymbols(string adsAddress, int port);

    /// <summary>Reads the symbol.</summary>
    /// <param name="adsAddress">The ADS address.</param>
    /// <param name="port">The port.</param>
    /// <param name="variable">The variable.</param>
    /// <param name="variableType">Type of the variable.</param>
    /// <returns>A Value.</returns>
    object ReadSymbol(string adsAddress, int port, string variable, Type variableType);

    /// <summary>Searches the symbols.</summary>
    /// <param name="symbolName">Name of the symbol.</param>
    /// <returns>A Value.</returns>
    INodeEmulator SearchSymbols(string? symbolName);
}
