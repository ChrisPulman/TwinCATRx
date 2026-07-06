// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using CP.TwinCatRx.Core;

namespace TwinCATRx.Tests.Core;

/// <summary>Tests for CodeGenerator static mapping and simple behavior.</summary>
public class CodeGeneratorTests
{
    /// <summary>PLC to C# type converter maps known types.</summary>
    /// <param name="plc">PLC type.</param>
    /// <param name="expectedStartsWith">Expected start of mapped type.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments("BOOL", "System.Boolean")]
    [Arguments("DINT", "System.Int32")]
    [Arguments("REAL", "System.Single")]
    [Arguments("LREAL", "System.Double")]
    [Arguments("BYTE", "System.Byte")]
    [Arguments("STRING(80)", "System.String")]
    [Arguments("ARRAY [0..10] OF BOOL", "System.Boolean[]")]
    [Arguments("ARRAY [0..10] OF BYTE", "System.Byte[]")]
    public async Task PLCToCSharpTypeConverter_Maps_Known_Types(string plc, string expectedStartsWith)
    {
        var result = CodeGenerator.PLCToCSharpTypeConverter(plc);
        await TUnitAssert.That(result.StartsWith(expectedStartsWith, StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>Private CreateCsharpCodeFile throws for simple type node (no children).</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task CreateCsharpCodeFile_Throws_On_SimpleType()
    {
        var cg = new CodeGenerator();
        var method = typeof(CodeGenerator).GetMethod("CreateCsharpCodeFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await TUnitAssert.That(method).IsNotNull();
        var sb = new System.Text.StringBuilder();
        var fake = new FakeNodeEmulator();
        var ex = await TUnitAssert.That(() => method!.Invoke(cg, [sb, fake, "TwinCATRx", false])).Throws<System.Reflection.TargetInvocationException>();
        await TUnitAssert.That(ex).IsNotNull();
        await TUnitAssert.That(ex!.InnerException is SimpleTypeException).IsTrue();
    }

    /// <summary>CreateCSharpCodeString returns empty for simple nodes.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task CreateCSharpCodeString_Returns_Empty_For_Empty_Node()
    {
        var cg = new CodeGenerator();
        var simple = new FakeNodeEmulator();
        var code = cg.CreateCSharpCodeString(simple);
        await TUnitAssert.That(code).IsEqualTo(string.Empty);
    }

    /// <summary>Minimal node emulator used by code generator tests.</summary>
    private sealed class FakeNodeEmulator : INodeEmulator
    {
        /// <inheritdoc/>
        public HashSet<INodeEmulator>? Nodes { get; } = new HashSet<INodeEmulator>();

        /// <inheritdoc/>
        public object? Tag { get; set; }

        /// <inheritdoc/>
        public string Text { get; set; } = string.Empty;

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
