// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using CP.TwinCatRx.Core;

namespace TwinCATRx.Tests.Core;

/// <summary>
/// Tests for CodeGenerator static mapping and simple behavior.
/// </summary>
public class CodeGeneratorTests
{
    /// <summary>
    /// PLC to C# type converter maps known types.
    /// </summary>
    /// <param name="plc">PLC type.</param>
    /// <param name="expectedStartsWith">Expected start of mapped type.</param>
    [Test]
    [Arguments("BOOL", "System.Boolean")]
    [Arguments("DINT", "System.Int32")]
    [Arguments("REAL", "System.Single")]
    [Arguments("LREAL", "System.Double")]
    [Arguments("BYTE", "System.Byte")]
    [Arguments("STRING(80)", "System.String")]
    [Arguments("ARRAY [0..10] OF BOOL", "System.Boolean[]")]
    [Arguments("ARRAY [0..10] OF BYTE", "System.Byte[]")]
    public void PLCToCSharpTypeConverter_Maps_Known_Types(string plc, string expectedStartsWith)
    {
        var result = CodeGenerator.PLCToCSharpTypeConverter(plc);
        TestAssert.StartsWith(expectedStartsWith, result);
    }

    /// <summary>
    /// Private CreateCsharpCodeFile throws for simple type node (no children).
    /// </summary>
    [Test]
    public void CreateCsharpCodeFile_Throws_On_SimpleType()
    {
        var cg = new CodeGenerator();
        var method = typeof(CodeGenerator).GetMethod("CreateCsharpCodeFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        TestAssert.NotNull(method);
        var sb = new System.Text.StringBuilder();
        var fake = new FakeNodeEmulator();
        var ex = TestAssert.Throws<System.Reflection.TargetInvocationException>(() => method!.Invoke(cg, new object?[] { sb, fake, "TwinCATRx", false }));
        TestAssert.True(ex.InnerException is SimpleTypeException);
    }

    /// <summary>
    /// CreateCSharpCodeString returns empty for simple nodes.
    /// </summary>
    [Test]
    public void CreateCSharpCodeString_Returns_Empty_For_Empty_Node()
    {
        var cg = new CodeGenerator();
        var simple = new FakeNodeEmulator();
        var code = cg.CreateCSharpCodeString(simple);
        TestAssert.Equal(string.Empty, code);
    }

    private sealed class FakeNodeEmulator : INodeEmulator
    {
        public HashSet<INodeEmulator>? Nodes { get; } = new HashSet<INodeEmulator>();

        public object? Tag { get; set; }

        public string Text { get; set; } = string.Empty;

        public void Dispose()
        {
        }
    }
}
