// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using CP.TwinCatRx.Core.Reactive;

namespace TwinCATRx.Tests.Core.Reactive;

/// <summary>Exercises deterministic Reactive Core code-generator branches without connecting to ADS.</summary>
public class ReactiveCodeGeneratorDeterministicCoverageTests
{
    /// <summary>The private array-length parser method name.</summary>
    private const string TryGetArrayLengthMethod = "TryGetArrayLength";

    /// <summary>The private fixed-string parser method name.</summary>
    private const string TryGetFixedStringLengthMethod = "TryGetFixedStringLength";

    /// <summary>The flattened size of the tested multidimensional array.</summary>
    private const int ExpectedDimensionLength = 9;

    /// <summary>The tested fixed PLC string length.</summary>
    private const int FixedStringLength = 80;

    /// <summary>The tested primitive array length.</summary>
    private const int ArrayLength = 4;

    /// <summary>Verifies every scalar PLC mapping in the Reactive Core variant.</summary>
    /// <param name="plcType">The PLC type.</param>
    /// <param name="expectedType">The expected CLR type.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments("BOOL", "System.Boolean")]
    [Arguments("BIT", "System.Boolean")]
    [Arguments("BIT8", "System.Boolean")]
    [Arguments("BYTE", "System.Byte")]
    [Arguments("BITARR8", "System.Byte")]
    [Arguments("USINT", "System.Byte")]
    [Arguments("UINT8", "System.Byte")]
    [Arguments("WORD", "System.UInt16")]
    [Arguments("BITARR16", "System.UInt16")]
    [Arguments("UINT16", "System.UInt16")]
    [Arguments("INT", "System.Int16")]
    [Arguments("INT16", "System.Int16")]
    [Arguments("UINT", "System.UInt16")]
    [Arguments("DWORD", "System.UInt32")]
    [Arguments("BITARR32", "System.UInt32")]
    [Arguments("UINT32", "System.UInt32")]
    [Arguments("DINT", "System.Int32")]
    [Arguments("INT32", "System.Int32")]
    [Arguments("UDINT", "System.UInt32")]
    [Arguments("INT8", "sbyte")]
    [Arguments("LINT", "long")]
    [Arguments("INT64", "long")]
    [Arguments("ULINT", "ulong")]
    [Arguments("UINT64", "ulong")]
    [Arguments("REAL", "System.Single")]
    [Arguments("FLOAT", "System.Single")]
    [Arguments("LREAL", "System.Double")]
    [Arguments("DOUBLE", "System.Double")]
    [Arguments("STRING(80)", "System.String")]
    public async Task PlcConverter_Maps_All_Scalar_Types(string plcType, string expectedType)
    {
        var result = CodeGenerator.PLCToCSharpTypeConverter(plcType);

        await TUnitAssert.That(result).IsEqualTo(expectedType);
    }

    /// <summary>Verifies special, array, fixed-string, and failure mappings.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task PlcConverter_Covers_Special_And_Failure_Branches()
    {
        await TUnitAssert.That(CodeGenerator.PLCToCSharpTypeConverter(null)).IsEqualTo("NULL");
        await TUnitAssert.That(CodeGenerator.PLCToCSharpTypeConverter("ARRAY [1..3] OF DINT")).IsEqualTo("System.Int32[],1..3");
        await TUnitAssert.That(CodeGenerator.PLCToCSharpTypeConverter("STRING(42)")).IsEqualTo("System.String,42");
        await TUnitAssert.That(() => CodeGenerator.PLCToCSharpTypeConverter("POINTER TO DINT")).Throws<UnsuportedTypeException>();
    }

    /// <summary>Verifies public guard clauses do not require ADS.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Public_Guards_Reject_Empty_Input()
    {
        using var generator = new CodeGenerator();
        var emptyNode = new FakeNode("Empty");

        await TUnitAssert.That(generator.CreateCSharpCode(emptyNode)).IsFalse();
        await TUnitAssert.That(generator.CreateCSharpCode(emptyNode, "ignored.cs")).IsFalse();
        await TUnitAssert.That(() => generator.CreateCSharpCodeString(null)).Throws<SimpleTypeException>();
        await TUnitAssert.That(generator.CreateCSharpCodeString(emptyNode)).IsEqualTo(string.Empty);
        await TUnitAssert.That(generator.CreateDll(emptyNode)).IsFalse();
        await TUnitAssert.That(generator.CreateDll(emptyNode, "ignored.dll")).IsFalse();
        await TUnitAssert.That(generator.CreateDll(string.Empty, "ignored.dll")).IsFalse();
        await TUnitAssert.That(generator.CreateDll("class C {}", string.Empty)).IsFalse();
        generator.Dispose();
    }

    /// <summary>Verifies raw source can be compiled without PLC access.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task CreateDll_Compiles_Valid_Source_And_Rejects_Invalid_Source()
    {
        var directory = Path.Combine(Path.GetTempPath(), "TwinCATRx_ReactiveCodeGen_" + Guid.NewGuid());
        _ = Directory.CreateDirectory(directory);
        try
        {
            using var generator = new CodeGenerator();
            var validPath = Path.Combine(directory, "Valid.dll");
            var invalidPath = Path.Combine(directory, "Invalid.dll");

            await TUnitAssert.That(generator.CreateDll("public sealed class GeneratedReactiveType { }", validPath)).IsTrue();
            await TUnitAssert.That(File.Exists(validPath)).IsTrue();
            await TUnitAssert.That(generator.CreateDll("public sealed class Broken {", invalidPath)).IsFalse();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>Verifies symbol searching traverses nodes case-insensitively.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task SearchSymbols_Covers_Traversal_And_Fallbacks()
    {
        using var generator = new CodeGenerator();
        var leaf = new FakeNode("Leaf");
        var root = new FakeNode("Root", leaf);
        _ = generator.SymbolList.Add(root);

        await TUnitAssert.That(generator.SearchSymbols(".root.leaf")).IsSameReferenceAs(leaf);
        await TUnitAssert.That(generator.SearchSymbols("ROOT")).IsSameReferenceAs(root);
        await TUnitAssert.That(generator.SearchSymbols(null).Text).IsEqualTo(string.Empty);
        await TUnitAssert.That(generator.SearchSymbols("Root.NotFound").Text).IsEqualTo(string.Empty);
        await TUnitAssert.That(generator.SearchSymbols("NotFound").Text).IsEqualTo(string.Empty);

        generator.Dispose();
        await TUnitAssert.That(generator.SymbolList).IsEmpty();
    }

    /// <summary>Exercises private pure parsing helpers for valid and invalid input.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Private_Array_And_String_Parsers_Cover_All_Branches()
    {
        var parseValid = InvokeWithOutParameters("TryParseArrayType", " ARRAY [0..2, 1..3] OF BOOL ");
        var parseInvalid = InvokeWithOutParameters("TryParseArrayType", "BOOL");
        var lengthValid = InvokeWithOutParameters(TryGetArrayLengthMethod, "0..2, 1..3");
        var lengthInvalidCount = InvokeWithOutParameters(TryGetArrayLengthMethod, "0");
        var lengthInvalidNumber = InvokeWithOutParameters(TryGetArrayLengthMethod, "a..2");
        var lengthInvalidOrder = InvokeWithOutParameters(TryGetArrayLengthMethod, "3..2");
        var fixedValid = InvokeWithOutParameters(TryGetFixedStringLengthMethod, "STRING(80)");
        var fixedNoParen = InvokeWithOutParameters(TryGetFixedStringLengthMethod, "STRING");
        var fixedNoClose = InvokeWithOutParameters(TryGetFixedStringLengthMethod, "STRING(80");
        var fixedWrongType = InvokeWithOutParameters(TryGetFixedStringLengthMethod, "WSTRING(80)");
        var fixedBadLength = InvokeWithOutParameters(TryGetFixedStringLengthMethod, "STRING(nope)");
#if NETFRAMEWORK
        var parsedElementType = parseValid.Last();
#else
        var parsedElementType = parseValid[^1];
#endif

        await TUnitAssert.That((bool)parseValid[0]!).IsTrue();
        await TUnitAssert.That((string)parseValid[1]!).IsEqualTo("0..2, 1..3");
        await TUnitAssert.That((string)parsedElementType!).IsEqualTo("BOOL");
        await TUnitAssert.That((bool)parseInvalid[0]!).IsFalse();
        await TUnitAssert.That((bool)lengthValid[0]!).IsTrue();
        await TUnitAssert.That((int)lengthValid[1]!).IsEqualTo(ExpectedDimensionLength);
        await TUnitAssert.That((bool)lengthInvalidCount[0]!).IsFalse();
        await TUnitAssert.That((bool)lengthInvalidNumber[0]!).IsFalse();
        await TUnitAssert.That((bool)lengthInvalidOrder[0]!).IsFalse();
        await TUnitAssert.That((bool)fixedValid[0]!).IsTrue();
        await TUnitAssert.That((int)fixedValid[1]!).IsEqualTo(FixedStringLength);
        await TUnitAssert.That((bool)fixedNoParen[0]!).IsFalse();
        await TUnitAssert.That((bool)fixedNoClose[0]!).IsFalse();
        await TUnitAssert.That((bool)fixedWrongType[0]!).IsFalse();
        await TUnitAssert.That((bool)fixedBadLength[0]!).IsFalse();
    }

    /// <summary>Verifies generated primitive member text.</summary>
    /// <param name="type">The converted type.</param>
    /// <param name="expected">Expected generated fragment.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments("System.Boolean", "UnmanagedType.I1")]
    [Arguments("System.String", "SizeConst = 81")]
    [Arguments("System.String[3],3", "System.String[] Value")]
    [Arguments("System.String,12", "SizeConst = 13")]
    [Arguments("System.Int32", "public System.Int32 Value;")]
    public async Task WritePrimitiveMember_Covers_All_Output_Shapes(string type, string expected)
    {
        var builder = new StringBuilder();
        var arguments = new object?[] { builder, type, "Value" };
        _ = GetPrivateMethod("WritePrimitiveMember").Invoke(null, arguments);

        await TUnitAssert.That(((StringBuilder)arguments[0]!).ToString()).Contains(expected);
    }

    /// <summary>Verifies array mapping and generated field helpers.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Primitive_Array_And_Field_Helpers_Cover_Both_Outcomes()
    {
        var mapped = InvokeWithOutParameters("TryGetPrimitiveArrayMapping", "BOOL", ArrayLength);
        var missing = InvokeWithOutParameters("TryGetPrimitiveArrayMapping", "CUSTOM", ArrayLength);
        var field = (string)GetPrivateMethod("BuildArrayField").Invoke(null, ["[Marshal]", "System.Int32", "Values", ArrayLength])!;
#if NETFRAMEWORK
        var marshalAttribute = mapped.Last();
#else
        var marshalAttribute = mapped[^1];
#endif

        await TUnitAssert.That((bool)mapped[0]!).IsTrue();
        await TUnitAssert.That((string)mapped[1]!).IsEqualTo("bool");
        await TUnitAssert.That((string)marshalAttribute!).Contains($"SizeConst = {ArrayLength}");
        await TUnitAssert.That((bool)missing[0]!).IsFalse();
        await TUnitAssert.That((string)missing[1]!).IsEqualTo(string.Empty);
        await TUnitAssert.That(field).Contains($"public System.Int32[] Values = new System.Int32[{ArrayLength}]");
    }

    /// <summary>Invokes a private static method whose remaining arguments are out parameters.</summary>
    /// <param name="name">The method name.</param>
    /// <param name="inputs">Input arguments.</param>
    /// <returns>The return value followed by the updated arguments.</returns>
    private static object?[] InvokeWithOutParameters(string name, params object?[] inputs)
    {
        var method = GetPrivateMethod(name);
        var arguments = new object?[method.GetParameters().Length];
        Array.Copy(inputs, arguments, inputs.Length);
        var result = method.Invoke(null, arguments);
        return [result, .. arguments.Skip(inputs.Length)];
    }

    /// <summary>Gets a private static Reactive Core CodeGenerator method.</summary>
    /// <param name="name">The method name.</param>
    /// <returns>The method.</returns>
    private static MethodInfo GetPrivateMethod(string name) =>
        typeof(CodeGenerator).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>A deterministic Reactive Core node implementation for traversal tests.</summary>
    private sealed class FakeNode : INodeEmulator
    {
        /// <summary>Initializes a new instance of the <see cref="FakeNode"/> class.</summary>
        /// <param name="text">The node text.</param>
        /// <param name="children">Child nodes.</param>
        public FakeNode(string text, params INodeEmulator[] children)
        {
            Text = text;
            Nodes = [.. children];
        }

        /// <inheritdoc/>
        public HashSet<INodeEmulator>? Nodes { get; }

        /// <inheritdoc/>
        public object? Tag { get; set; }

        /// <inheritdoc/>
        public string Text { get; set; }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
