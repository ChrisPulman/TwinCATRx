// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
    /// <param name="plc">PLC type string.</param>
    /// <param name="expectedStartsWith">Expected starting substring of result.</param>
    [Theory]
    [InlineData("BOOL", "System.Boolean")]
    [InlineData("DINT", "System.Int32")]
    [InlineData("REAL", "System.Single")]
    [InlineData("LREAL", "System.Double")]
    [InlineData("BYTE", "System.Byte")]
    [InlineData("STRING(80)", "System.String")]
    [InlineData("ARRAY [0..10] OF BOOL", "System.Boolean[]")]
    [InlineData("ARRAY [0..10] OF BYTE", "System.Byte[]")]
    public void PLCToCSharpTypeConverter_Maps_Known_Types(string plc, string expectedStartsWith)
    {
        var result = CodeGenerator.PLCToCSharpTypeConverter(plc);
        result.Should().StartWith(expectedStartsWith);
    }

    /// <summary>
    /// Private CreateCsharpCodeFile throws for simple type node (no children).
    /// </summary>
    [Fact]
    public void CreateCsharpCodeFile_Throws_On_SimpleType()
    {
        var cg = new CodeGenerator();
        var method = typeof(CodeGenerator).GetMethod("CreateCsharpCodeFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Should().NotBeNull();
        var sb = new System.Text.StringBuilder();

        // Create a minimal fake INodeEmulator with no children via reflection emit using proxy/Delegate
        var proxyType = typeof(INodeEmulator);
        var fake = new FakeNodeEmulator();
        Action act = () => method!.Invoke(cg, new object?[] { sb, fake, "TwinCATRx", false });
        act.Should().Throw<System.Reflection.TargetInvocationException>()
           .WithInnerException<SimpleTypeException>();
    }

    /// <summary>
    /// CreateCSharpCodeString returns empty for simple nodes.
    /// </summary>
    [Fact]
    public void CreateCSharpCodeString_Returns_Empty_For_Empty_Node()
    {
        var cg = new CodeGenerator();
        var simple = new FakeNodeEmulator();
        var code = cg.CreateCSharpCodeString(simple);
        code.Should().BeEmpty();
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
