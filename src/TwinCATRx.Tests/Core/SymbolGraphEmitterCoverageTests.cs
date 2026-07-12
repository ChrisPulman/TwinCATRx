// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NETFRAMEWORK
using System.Diagnostics.CodeAnalysis;
#endif
using System.Reflection;
using System.Text;
using TwinCAT.TypeSystem;
using LeanCodeGenerator = CP.TwinCatRx.Core.CodeGenerator;
using ReactiveCodeGenerator = CP.TwinCatRx.Core.Reactive.CodeGenerator;

namespace TwinCATRx.Tests.Core;

/// <summary>Exercises symbol graph traversal and C# emission without loading live ADS symbols.</summary>
public class SymbolGraphEmitterCoverageTests
{
    /// <summary>The number of members in the deterministic root structure.</summary>
    private const int ExpectedRootChildCount = 6;

    /// <summary>The number of bits in the fake symbol's byte-sized storage.</summary>
    private const int ByteBitSize = 8;

    /// <summary>Verifies complete graph emission in both Core package variants.</summary>
    /// <param name="reactive">Whether to use the System.Reactive package variant.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Symbol_Graph_Emits_All_Member_Shapes(bool reactive)
    {
        var generatorType = reactive ? typeof(ReactiveCodeGenerator) : typeof(LeanCodeGenerator);
        var generator = Activator.CreateInstance(generatorType)!;
        var rootSymbol = CreateSymbolGraph();
        var rootNode = CreateNodeGraph(generatorType.Assembly, rootSymbol);

        var source = (string)generatorType.GetMethod("CreateCSharpCodeString")!
            .Invoke(generator, [rootNode, false, "Generated.Symbols"])!;

        await TUnitAssert.That(source).Contains("namespace Generated.Symbols");
        await TUnitAssert.That(source).Contains("public class RootType");
        await TUnitAssert.That(source).Contains("public class NestedType");
        await TUnitAssert.That(source).Contains("[MarshalAs(UnmanagedType.I1)]");
        await TUnitAssert.That(source).Contains("public System.Boolean Enabled;");
        await TUnitAssert.That(source).Contains("[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 81)]");
        await TUnitAssert.That(source).Contains("public System.String Name;");
        await TUnitAssert.That(source).Contains("public NestedType Nested = new NestedType();");
        await TUnitAssert.That(source).Contains("ArraySubType = UnmanagedType.I2, SizeConst = 3");
        await TUnitAssert.That(source).Contains("public short[] Values = new short[3];");
        await TUnitAssert.That(source).Contains("public struct STRING_10_WRAPPER");
        await TUnitAssert.That(source).Contains("public STRING_10_WRAPPER[] Labels = new STRING_10_WRAPPER[2];");
        await TUnitAssert.That(source).Contains("public ItemType[] Items = new ItemType[2];");
        await TUnitAssert.That(source).Contains("public System.Int32 Value;");

        ((IDisposable)generator).Dispose();
        ((IDisposable)rootNode).Dispose();
    }

    /// <summary>Verifies private graph creation and next-node traversal in both Core package variants.</summary>
    /// <param name="reactive">Whether to use the System.Reactive package variant.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Private_Graph_Helpers_Create_And_Traverse_Nodes(bool reactive)
    {
        var generatorType = reactive ? typeof(ReactiveCodeGenerator) : typeof(LeanCodeGenerator);
        var generator = Activator.CreateInstance(generatorType)!;
        var rootSymbol = CreateSymbolGraph();
        var createNode = generatorType.GetMethod("CreateNewNode", BindingFlags.Static | BindingFlags.NonPublic)!;
        var generatedRoot = createNode.Invoke(null, [rootSymbol])!;
        var nodeType = generatedRoot.GetType();
        var nodes = nodeType.GetProperty("Nodes")!.GetValue(generatedRoot)!;
        var nodeCount = (int)nodes.GetType().GetProperty("Count")!.GetValue(nodes)!;

        await TUnitAssert.That(nodeType.GetProperty("Text")!.GetValue(generatedRoot)).IsEqualTo("Root");
        await TUnitAssert.That(nodeType.GetProperty("Tag")!.GetValue(generatedRoot)).IsSameReferenceAs(rootSymbol);
        await TUnitAssert.That(nodeCount).IsEqualTo(ExpectedRootChildCount);

        var findNext = generatorType.GetMethod("FindNextNode", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var next = findNext.Invoke(generator, [generatedRoot]);
        await TUnitAssert.That(next).IsNotNull();
        await TUnitAssert.That(nodeType.GetProperty("Text")!.GetValue(next)).IsEqualTo("Nested");

        _ = generatorType.GetMethod("CreateCSharpCodeString")!
            .Invoke(generator, [generatedRoot, true, "Generated.Traversal"]);
        await TUnitAssert.That(findNext.Invoke(generator, [generatedRoot])).IsNull();

        ((IDisposable)generator).Dispose();
        ((IDisposable)generatedRoot).Dispose();
    }

    /// <summary>Verifies malformed and empty array symbol branches in both package variants.</summary>
    /// <param name="reactive">Whether to use the System.Reactive package variant.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Array_Emitter_Rejects_Invalid_Symbol_Shapes(bool reactive)
    {
        var generatorType = reactive ? typeof(ReactiveCodeGenerator) : typeof(LeanCodeGenerator);
        var method = generatorType.GetMethod("CreateArrayOFStructure", BindingFlags.Static | BindingFlags.NonPublic)!;
        var wrapper = new StringBuilder();
        var notArray = (string)method.Invoke(null, [new FakeSymbol("Value", "DINT", DataTypeCategory.Primitive), wrapper, false])!;
        var reversed = (string)method.Invoke(null, [new FakeSymbol("Values", "ARRAY [4..1] OF INT", DataTypeCategory.Array), wrapper, false])!;
        var nameless = (string)method.Invoke(null, [new FakeSymbol(" ", "ARRAY [0..1] OF INT", DataTypeCategory.Array), wrapper, false])!;

        await TUnitAssert.That(notArray).IsEmpty();
        await TUnitAssert.That(reversed).IsEmpty();
        await TUnitAssert.That(nameless).IsEmpty();
        await TUnitAssert.That(wrapper.ToString()).IsEmpty();
    }

    /// <summary>Creates a deterministic symbol tree with scalar, structure, and array members.</summary>
    /// <returns>The root symbol.</returns>
    private static FakeSymbol CreateSymbolGraph()
    {
        var nested = new FakeSymbol(
            "Nested",
            "NestedType",
            DataTypeCategory.Struct,
            new FakeSymbol("Value", "DINT", DataTypeCategory.Primitive));
        return new FakeSymbol(
            "Root",
            "RootType",
            DataTypeCategory.Struct,
            new FakeSymbol("Enabled", "BOOL", DataTypeCategory.Primitive),
            new FakeSymbol("Name", "STRING(80)", DataTypeCategory.String),
            nested,
            new FakeSymbol("Values", "ARRAY [1..3] OF INT", DataTypeCategory.Array),
            new FakeSymbol("Labels", "ARRAY [0..1] OF STRING(10)", DataTypeCategory.Array),
            new FakeSymbol("Items", "ARRAY [0..1] OF ItemType", DataTypeCategory.Array));
    }

    /// <summary>Creates the assembly-specific internal node graph for a symbol.</summary>
    /// <param name="assembly">The Core variant assembly.</param>
    /// <param name="symbol">The symbol to wrap.</param>
    /// <returns>The internal node instance.</returns>
    private static object CreateNodeGraph(Assembly assembly, ISymbol symbol)
    {
        var nodeType = assembly.GetType(assembly == typeof(LeanCodeGenerator).Assembly
            ? "CP.TwinCatRx.Core.NodeEmulator"
            : "CP.TwinCatRx.Core.Reactive.NodeEmulator")!;
        var node = Activator.CreateInstance(nodeType)!;
        nodeType.GetProperty("Text")!.SetValue(node, symbol.InstanceName);
        nodeType.GetProperty("Tag")!.SetValue(node, symbol);
        var nodes = nodeType.GetProperty("Nodes")!.GetValue(node)!;
        var add = nodes.GetType().GetMethod("Add")!;
        foreach (var child in symbol.SubSymbols)
        {
            _ = add.Invoke(nodes, [CreateNodeGraph(assembly, child)]);
        }

        return node;
    }

    /// <summary>Minimal deterministic TwinCAT symbol.</summary>
    private sealed class FakeSymbol : ISymbol
    {
        /// <summary>Initializes a new instance of the <see cref="FakeSymbol"/> class.</summary>
        /// <param name="instanceName">The instance name.</param>
        /// <param name="typeName">The PLC type name.</param>
        /// <param name="category">The type category.</param>
        /// <param name="children">The child symbols.</param>
        public FakeSymbol(string instanceName, string typeName, DataTypeCategory category, params ISymbol[] children)
        {
            InstanceName = instanceName;
            InstancePath = instanceName;
            TypeName = typeName;
            Category = category;
            SubSymbols = new FakeSymbolCollection(children);
        }

        /// <inheritdoc/>
        public DataTypeCategory Category { get; }

        /// <inheritdoc/>
        public ISymbol? Parent => null;

        /// <inheritdoc/>
        public ISymbolCollection<ISymbol> SubSymbols { get; }

        /// <inheritdoc/>
        public bool IsContainerType => Category is DataTypeCategory.Struct or DataTypeCategory.Array;

        /// <inheritdoc/>
        public bool IsPrimitiveType => Category == DataTypeCategory.Primitive;

        /// <inheritdoc/>
        public bool IsPersistent => false;

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public bool IsRecursive => false;

        /// <inheritdoc/>
        public IDataType DataType => null!;

        /// <inheritdoc/>
        public string TypeName { get; }

        /// <inheritdoc/>
        public string InstanceName { get; }

        /// <inheritdoc/>
        public string InstancePath { get; }

        /// <inheritdoc/>
        public bool IsStatic => false;

        /// <inheritdoc/>
        public bool IsReference => false;

        /// <inheritdoc/>
        public bool IsPointer => false;

        /// <inheritdoc/>
        public string Comment => string.Empty;

        /// <inheritdoc/>
        public bool IsProperty => false;

        /// <inheritdoc/>
        public ITypeAttributeCollection Attributes => null!;

        /// <inheritdoc/>
        public Encoding ValueEncoding => Encoding.UTF8;

        /// <inheritdoc/>
        public int Size => ByteSize;

        /// <inheritdoc/>
        public bool IsBitType => false;

        /// <inheritdoc/>
        public int BitSize => ByteBitSize;

        /// <inheritdoc/>
        public int ByteSize => 1;

        /// <inheritdoc/>
        public bool IsByteAligned => true;
    }

    /// <summary>List-backed implementation of the TwinCAT symbol collection marker.</summary>
    private sealed class FakeSymbolCollection : List<ISymbol>, ISymbolCollection<ISymbol>
    {
        /// <summary>Initializes a new instance of the <see cref="FakeSymbolCollection"/> class.</summary>
        /// <param name="symbols">The symbols to copy.</param>
        public FakeSymbolCollection(IEnumerable<ISymbol> symbols)
            : base(symbols)
        {
        }

        /// <inheritdoc/>
        /// <inheritdoc/>
        public InstanceCollectionMode Mode => InstanceCollectionMode.Names;

        /// <inheritdoc/>
        public ISymbol this[string instancePath] => GetInstance(instancePath);

        /// <inheritdoc/>
        public bool Contains(string instancePath) => this.Any(symbol => symbol.InstancePath == instancePath);

        /// <inheritdoc/>
        public bool ContainsName(string instanceName) => this.Any(symbol => symbol.InstanceName == instanceName);

        /// <inheritdoc/>
        public ISymbol GetInstance(string instancePath) => this.First(symbol => symbol.InstancePath == instancePath);

        /// <inheritdoc/>
        public IList<ISymbol> GetInstanceByName(string instanceName) => this.Where(symbol => symbol.InstanceName == instanceName).ToList();

        /// <inheritdoc/>
#if NETFRAMEWORK
        public bool TryGetInstance(string instancePath, out ISymbol symbol)
#else
        public bool TryGetInstance(string instancePath, [NotNullWhen(true)] out ISymbol? symbol)
#endif
        {
            symbol = this.FirstOrDefault(candidate => candidate.InstancePath == instancePath)!;
            return symbol is not null;
        }

        /// <inheritdoc/>
#if NETFRAMEWORK
        public bool TryGetInstanceByName(string instanceName, out IList<ISymbol> symbols)
#else
        public bool TryGetInstanceByName(string instanceName, [NotNullWhen(true)] out IList<ISymbol>? symbols)
#endif
        {
            symbols = GetInstanceByName(instanceName);
            return symbols.Count > 0;
        }
    }
}
