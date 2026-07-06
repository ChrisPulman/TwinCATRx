// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CSharp;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;

namespace CP.TwinCatRx.Core;

/// <summary>Code Generator.</summary>
/// <seealso cref="ICodeGenerator"/>
public class CodeGenerator : ICodeGenerator
{
    /// <summary>Separates PLC array lower and upper bounds.</summary>
    private static readonly string[] RangeSeparator = [".."];

    /// <summary>Maps PLC array element markers to C# array type names.</summary>
    private static readonly (string PlcType, string CSharpType)[] ArrayTypeMappings =
    [
        ("OF STRING", typeof(string[]) + string.Empty),
        ("OF BOOL", typeof(bool[]) + string.Empty),
        ("OF BIT", typeof(bool[]) + string.Empty),
        ("OF BIT8", typeof(bool[]) + string.Empty),
        ("OF BYTE", typeof(byte[]) + string.Empty),
        ("OF REAL", "System.Single[]"),
        ("OF LREAL", "System.Double[]"),
        ("OF FLOAT", "System.Single[]"),
        ("OF INT", "System.Int16[]"),
        ("OF INT16", "System.Int16[]"),
        ("OF DINT", "System.Int32[]"),
        ("OF INT32", "System.Int32[]")
    ];

    /// <summary>Maps primitive PLC type names to C# type names.</summary>
    private static readonly Dictionary<string, string> PrimitiveTypeMappings = new(StringComparer.Ordinal)
    {
        ["STRING(80)"] = typeof(string).ToString(),
        ["BIT"] = typeof(bool).ToString(),
        ["BIT8"] = typeof(bool).ToString(),
        ["BOOL"] = typeof(bool).ToString(),
        ["WORD"] = typeof(ushort).ToString(),
        ["BITARR16"] = typeof(ushort).ToString(),
        ["UINT16"] = typeof(ushort).ToString(),
        ["UINT"] = typeof(ushort).ToString(),
        ["INT8"] = "sbyte",
        ["INT16"] = typeof(short).ToString(),
        ["INT"] = typeof(short).ToString(),
        ["INT32"] = typeof(int).ToString(),
        ["DINT"] = typeof(int).ToString(),
        ["BITARR32"] = typeof(uint).ToString(),
        ["DWORD"] = typeof(uint).ToString(),
        ["UINT32"] = typeof(uint).ToString(),
        ["UDINT"] = typeof(uint).ToString(),
        ["UINT64"] = "ulong",
        ["ULINT"] = "ulong",
        ["INT64"] = "long",
        ["LINT"] = "long",
        ["FLOAT"] = typeof(float).ToString(),
        ["REAL"] = typeof(float).ToString(),
        ["DOUBLE"] = typeof(double).ToString(),
        ["LREAL"] = typeof(double).ToString(),
        ["BITARR8"] = typeof(byte).ToString(),
        ["USINT"] = typeof(byte).ToString(),
        ["UINT8"] = typeof(byte).ToString(),
        ["BYTE"] = typeof(byte).ToString()
    };

    /// <summary>Maps primitive PLC array element names to C# type and marshal subtype names.</summary>
    private static readonly Dictionary<string, (string CSharpType, string MarshalSubType)> PrimitiveArrayMappings =
        new(StringComparer.Ordinal)
        {
            ["BIT"] = ("bool", "U1"),
            ["BIT8"] = ("bool", "U1"),
            ["BOOL"] = ("bool", "U1"),
            ["BITARR8"] = ("byte", "U1"),
            ["USINT"] = ("byte", "U1"),
            ["UINT8"] = ("byte", "U1"),
            ["BYTE"] = ("byte", "U1"),
            ["WORD"] = ("ushort", "I2"),
            ["BITARR16"] = ("ushort", "I2"),
            ["UINT16"] = ("ushort", "I2"),
            ["UINT"] = ("ushort", "I2"),
            ["INT16"] = ("short", "I2"),
            ["INT"] = ("short", "I2"),
            ["BITARR32"] = ("uint", "I4"),
            ["DWORD"] = ("uint", "I4"),
            ["UINT32"] = ("uint", "I4"),
            ["UDINT"] = ("uint", "I4"),
            ["INT32"] = ("int", "I4"),
            ["DINT"] = ("int", "I4"),
            ["FLOAT"] = ("float", "R4"),
            ["REAL"] = ("float", "R4"),
            ["DOUBLE"] = ("double", "R8"),
            ["LREAL"] = ("double", "R8")
        };

    /// <summary>Stores generated type names while code is emitted.</summary>
    private readonly Hashtable _typeList = [];

    /// <summary>Stores the current ADS client.</summary>
    private AdsClient? _adsClient;

    /// <summary>Tracks whether this instance has been disposed.</summary>
    private bool _disposedValue;

    /// <summary>Stores the current symbol loader.</summary>
    private ISymbolLoader? _symbolLoader;

    /// <summary>Initializes a new instance of the <see cref="CodeGenerator"/> class.</summary>
    public CodeGenerator() => _disposedValue = false;

    /// <summary>Gets the symbol list.</summary>
    /// <value>The symbol list.</value>
    public HashSet<INodeEmulator> SymbolList { get; } = [];

    /// <summary>
    /// PLCs to c sharp type converter. BIT BOOL System.Boolean bool Boolean For info about
    /// specific PLC data type, see: TwinCAT PLC Control - Data Types BIT8 BOOL System.Boolean
    /// bool Boolean BITARR8 BYTE System.Byte byte Byte BITARR16 WORD System.UInt16 ushort -
    /// BITARR32 DWORD System.UInt32 uint - INT8 SINT System.SByte sbyte - INT16 INT System.Int16
    /// short Short INT32 DINT System.Int32 int Integer INT64 LINT System.Int64 long Long Integer
    /// type with size of 8 bytes.Currently not supported by TwinCAT PLC. UINT8 USINT System.Byte
    /// byte Byte UINT16 UINT System.UInt16 ushort - UINT32 UDINT System.UInt32 uint - UINT64
    /// ULINT System.UInt64 ulong - Unsigned integer type with size of 8 bytes.Currently not
    /// supported by TwinCAT PLC. FLOAT REAL System.Single float Single DOUBLE LREAL
    /// System.Double double Double.
    /// </summary>
    /// <param name="plcType">Type of the PLC.</param>
    /// <returns>A Value.</returns>
    /// <exception cref="Exception">
    /// This Type (" + PLCType + ")is not supported in this version, Please contact us for details of next version.
    /// </exception>
    public static string PLCToCSharpTypeConverter(string? plcType)
    {
        if (plcType is null)
        {
            return "NULL";
        }

        if (PrimitiveTypeMappings.TryGetValue(plcType, out var primitiveType))
        {
            return primitiveType;
        }

        if (TryConvertArrayType(plcType, out var arrayType))
        {
            return arrayType;
        }

        if (TryConvertStringType(plcType, out var stringType))
        {
            return stringType;
        }

        throw new UnsuportedTypeException("This Type (" + plcType + ")is not supported in this version, Please contact us for details of next version");
    }

    /// <summary>Creates a C# code file based on the selected node structure.</summary>
    /// <param name="selectedTN">The selected tn.</param>
    /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
    /// <returns>
    /// Result as a Boolean.
    /// </returns>
    public bool CreateCSharpCode(INodeEmulator selectedTN, bool isTwinCat3 = false) =>
        CreateCSharpCode(selectedTN, string.Empty, isTwinCat3, "TwinCATRx");

    /// <summary>Creates a C# code file based on the selected node structure.</summary>
    /// <param name="selectedTN">The selected tn.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
    /// <param name="classNamespace">The class namespace.</param>
    /// <returns>
    /// Result as a Boolean.
    /// </returns>
    public bool CreateCSharpCode(INodeEmulator selectedTN, string fileName, bool isTwinCat3 = false, string classNamespace = "TwinCATRx")
    {
        if (selectedTN?.Nodes?.Count <= 0)
        {
            return false;
        }

        _typeList.Clear();
        var sb = new StringBuilder();
        CreateCsharpCodeFile(ref sb, selectedTN, classNamespace, isTwinCat3);
        var sourceCode = sb.ToString();
        if (sourceCode.Length <= 1)
        {
            return false;
        }

        try
        {
            using Stream stream = File.Open(fileName, FileMode.Create);
            using var writer = new StreamWriter(stream);
            using var codeProvider = new CSharpCodeProvider();
            var compileUnit = new CodeSnippetCompileUnit(sourceCode);
            var options = new CodeGeneratorOptions
            {
                BracingStyle = "C",
                IndentString = "   "
            };
            codeProvider.CreateGenerator(writer).GenerateCodeFromCompileUnit(compileUnit, writer, options);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }

    /// <summary>Creates the C# code string.</summary>
    /// <param name="selectedTN">The selected tn.</param>
    /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
    /// <param name="classNamespace">The class namespace.</param>
    /// <returns>A Value.</returns>
    public string CreateCSharpCodeString(INodeEmulator? selectedTN, bool isTwinCat3 = false, string classNamespace = "TwinCATRx")
    {
        if (selectedTN?.Nodes?.Count != 0)
        {
            _typeList.Clear();
            var sb = new StringBuilder();
            CreateCsharpCodeFile(ref sb, selectedTN, classNamespace, isTwinCat3);
            return sb.ToString().Length <= 1 ? string.Empty : sb.ToString();
        }

        return string.Empty;
    }

    /// <summary>Creates a DLL based on the selected node structure.</summary>
    /// <param name="selectedTN">The selected tn.</param>
    /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
    /// <returns>
    /// Result as a Boolean.
    /// </returns>
#if NET8_0_OR_GREATER
    [RequiresDynamicCode("Emits and loads assemblies dynamically via Roslyn/Mono.Cecil.")]
    [RequiresUnreferencedCode("Dynamic compilation may access trimmed members.")]
#endif
    public bool CreateDll(INodeEmulator selectedTN, bool isTwinCat3 = false) =>
        CreateDll(selectedTN, string.Empty, isTwinCat3, "TwinCATRx");

    /// <summary>Creates a DLL based on the selected node structure.</summary>
    /// <param name="selectedTN">The selected tn.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="isTwinCat3">if set to <c>true</c> [is twincat3].</param>
    /// <param name="classNamespace">The class namespace.</param>
    /// <returns>
    /// Result as a Boolean.
    /// </returns>
#if NET8_0_OR_GREATER
    [RequiresDynamicCode("Emits and loads assemblies dynamically via Roslyn/Mono.Cecil.")]
    [RequiresUnreferencedCode("Dynamic compilation may access trimmed members.")]
#endif
    public bool CreateDll(INodeEmulator? selectedTN, string fileName, bool isTwinCat3 = false, string classNamespace = "TwinCATRx")
    {
        if (string.IsNullOrWhiteSpace(fileName) || selectedTN?.Nodes?.Count <= 0)
        {
            return false;
        }

        File.Delete(fileName);
        var sb = new StringBuilder();
        _typeList.Clear();
        CreateCsharpCodeFile(ref sb, selectedTN, classNamespace, isTwinCat3);
        var sourceCode = sb.ToString();
        if (sourceCode.Length <= 1)
        {
            return false;
        }

        try
        {
            return CSharpLanguage.CreateAssembly(sourceCode, fileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }

    /// <summary>Creates the DLL from raw source.</summary>
    /// <param name="sourceCode">The C# source code.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <returns>A Value.</returns>
#if NET8_0_OR_GREATER
    [RequiresDynamicCode("Emits and loads assemblies dynamically via Roslyn/Mono.Cecil.")]
    [RequiresUnreferencedCode("Dynamic compilation may access trimmed members.")]
#endif
    public bool CreateDll(string sourceCode, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || sourceCode is null || sourceCode.Length <= 1)
        {
            return false;
        }

        File.Delete(fileName);
        try
        {
            return CSharpLanguage.CreateAssembly(sourceCode, fileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }

    /// <summary>Performs application-defined tasks associated with freeing resources.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Loads symbols from the specified PLC ADS address.</summary>
    /// <param name="adsAddress">The ADS address.</param>
    /// <returns>
    /// HashSet(Of NodeEmulator).
    /// </returns>
    public HashSet<INodeEmulator> LoadSymbols(string adsAddress) => LoadSymbols(adsAddress, 801);

    /// <summary>Loads symbols from the specified PLC ADS address and port.</summary>
    /// <param name="adsAddress">The ADS address.</param>
    /// <param name="port">The port.</param>
    /// <returns>
    /// HashSet(Of NodeEmulator).
    /// </returns>
    public HashSet<INodeEmulator> LoadSymbols(string adsAddress, int port)
    {
        _adsClient = new();
        _adsClient.Connect(adsAddress, port);
        _symbolLoader = SymbolLoaderFactory.Create(_adsClient, SymbolLoaderSettings.Default);
        BuildSymbolList();
        _adsClient.Dispose();
        _adsClient = new();
        return SymbolList;
    }

    /// <summary>Loads symbols from the specified PLC ADS port.</summary>
    /// <param name="port">The port.</param>
    /// <returns>A Value.</returns>
    public HashSet<INodeEmulator> LoadSymbols(int port)
    {
        _adsClient = new();
        _adsClient.Connect(port);
        _symbolLoader = SymbolLoaderFactory.Create(_adsClient, SymbolLoaderSettings.Default);
        BuildSymbolList();
        _adsClient.Dispose();
        _adsClient = new();
        return SymbolList;
    }

    /// <summary>Reads the symbol.</summary>
    /// <param name="adsAddress">The ADS address.</param>
    /// <param name="port">The port.</param>
    /// <param name="variable">The variable.</param>
    /// <param name="variableType">Type of the variable.</param>
    /// <returns>A Value.</returns>
    public object ReadSymbol(string adsAddress, int port, string variable, Type variableType)
    {
        var obj = RuntimeHelpers.GetObjectValue(new object());
        try
        {
            _adsClient = new();
            _adsClient.Connect(adsAddress, port);
            obj = RuntimeHelpers.GetObjectValue(_adsClient.ReadAny(_adsClient.CreateVariableHandle(variable), variableType));
        }
        finally
        {
            _adsClient!.Dispose();
            _adsClient = new();
        }

        return obj;
    }

    /// <summary>Searches for the nearest matching symbol list element.</summary>
    /// <param name="symbolName">Name of the symbol.</param>
    /// <returns>
    /// NodeEmulator.
    /// </returns>
    public INodeEmulator SearchSymbols(string? symbolName)
    {
        if (string.IsNullOrWhiteSpace(symbolName))
        {
            return new NodeEmulator();
        }

        var normalizedSymbolName = symbolName!;
        if (normalizedSymbolName.StartsWith(".", StringComparison.Ordinal))
        {
            normalizedSymbolName = normalizedSymbolName.Remove(0, 1);
        }

        var symbols = normalizedSymbolName.Split('.');
        var ret = FindNode(SymbolList, symbols[0]);
        for (var i = 1; i < symbols.Length && ret is not null; i++)
        {
            ret = FindNode(ret.Nodes, symbols[i]);
        }

        return ret ?? new NodeEmulator();
    }

    /// <summary>Releases unmanaged and optionally managed resources.</summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
    /// unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue && disposing)
        {
            _adsClient?.Dispose();
            SymbolList?.Clear();
        }

        _disposedValue = true;
    }

    /// <summary>Finds a child node by text.</summary>
    /// <param name="nodes">The nodes to search.</param>
    /// <param name="text">The text to match.</param>
    /// <returns>The matching node.</returns>
    private static INodeEmulator? FindNode(IEnumerable<INodeEmulator>? nodes, string text)
    {
        if (nodes is null)
        {
            return null;
        }

        foreach (var node in nodes)
        {
            if (string.Equals(node.Text, text, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }
        }

        return null;
    }

    /// <summary>Converts a PLC array type name to a C# array type name.</summary>
    /// <param name="plcType">The PLC type name.</param>
    /// <param name="arrayType">The converted array type.</param>
    /// <returns><c>true</c> when the type was converted.</returns>
    private static bool TryConvertArrayType(string plcType, out string arrayType)
    {
        foreach (var mapping in ArrayTypeMappings)
        {
            if (!plcType.Contains(mapping.PlcType))
            {
                continue;
            }

            var bounds = plcType.Replace("ARRAY [", string.Empty);
            bounds = bounds.Replace("] " + mapping.PlcType, string.Empty);
            arrayType = mapping.CSharpType + "," + bounds;
            return true;
        }

        arrayType = string.Empty;
        return false;
    }

    /// <summary>Converts a PLC fixed-length string type name to a C# string type name.</summary>
    /// <param name="plcType">The PLC type name.</param>
    /// <param name="stringType">The converted string type.</param>
    /// <returns><c>true</c> when the type was converted.</returns>
    private static bool TryConvertStringType(string plcType, out string stringType)
    {
        if (!plcType.Contains("STRING("))
        {
            stringType = string.Empty;
            return false;
        }

        var size = plcType.Replace("STRING(", string.Empty);
        size = size.Replace(")", string.Empty);
        stringType = $"System.String,{size}";
        return true;
    }

    /// <summary>Writes the C# class members.</summary>
    /// <param name="sb">The sb.</param>
    /// <param name="selectedTN">The selected tn.</param>
    /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
    private static void WriteCSharpClassMembers(ref StringBuilder sb, INodeEmulator selectedTN, bool isTwinCat3)
    {
        foreach (var node in selectedTN.Nodes!)
        {
            if (node.Tag is ISymbol symbol)
            {
                WriteCSharpClassMember(ref sb, symbol, isTwinCat3);
            }
        }
    }

    /// <summary>Writes one C# class member.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="symbol">The symbol to write.</param>
    /// <param name="isTwinCat3">Whether TwinCAT 3 packing should be used.</param>
    private static void WriteCSharpClassMember(ref StringBuilder sb, ISymbol symbol, bool isTwinCat3)
    {
        var memberName = symbol.InstanceName;
        if (IsGeneratedStructure(symbol))
        {
            _ = sb.Append("public ").Append(symbol.TypeName).Append(' ').Append(memberName).Append(" = new ").Append(symbol.TypeName).AppendLine("();");
            return;
        }

        var stringArrayWrapper = new StringBuilder();
        var arrayOfStruct = CreateArrayOFStructure(symbol, stringArrayWrapper, isTwinCat3);
        if (!string.IsNullOrWhiteSpace(arrayOfStruct))
        {
            _ = sb.Append(stringArrayWrapper).Append(arrayOfStruct);
            return;
        }

        WritePrimitiveMember(ref sb, PLCToCSharpTypeConverter(symbol.TypeName), memberName);
    }

    /// <summary>Gets whether a symbol should be emitted as a generated structure instance.</summary>
    /// <param name="symbol">The symbol to inspect.</param>
    /// <returns><c>true</c> when the symbol is a generated structure.</returns>
    private static bool IsGeneratedStructure(ISymbol symbol) =>
        symbol.Category != DataTypeCategory.Array
        && symbol.Category != DataTypeCategory.String
        && symbol.Category != DataTypeCategory.Primitive
        && !symbol.TypeName.Contains("ARRAY [");

    /// <summary>Writes one primitive C# class member.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="csharpType">The C# type.</param>
    /// <param name="memberName">The member name.</param>
    private static void WritePrimitiveMember(ref StringBuilder sb, string csharpType, string memberName)
    {
        if (csharpType == "System.Boolean")
        {
            _ = sb.AppendLine("[MarshalAs(UnmanagedType.I1)]")
                .Append("public ").Append(csharpType).Append(' ').Append(memberName).AppendLine(";");
            return;
        }

        if (csharpType == "System.String")
        {
            _ = sb.AppendLine("[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 81)]")
                .Append("public ").Append(csharpType).Append(' ').Append(memberName).AppendLine(";");
            return;
        }

        if (csharpType.Contains("System.String[", StringComparison.Ordinal))
        {
            var length = int.Parse(csharpType.Split(',')[1]);
            _ = sb.Append("[MarshalAs(UnmanagedType.ByValTStr, SizeConst = ").Append(length + 1).AppendLine(")] ")
                .Append("public System.String[] ").Append(memberName).Append(" = new ").Append("System.String[").Append(length).AppendLine("];");
            return;
        }

        if (csharpType.Contains("System.String,", StringComparison.Ordinal))
        {
            var length = int.Parse(csharpType.Split(',')[1]);
            _ = sb.Append("[MarshalAs(UnmanagedType.ByValTStr, SizeConst = ").Append(length + 1).AppendLine(")] ")
                .Append("public string ").Append(memberName).AppendLine(";");
            return;
        }

        _ = sb.Append("public ").Append(csharpType).Append(' ').Append(memberName).AppendLine(";");
    }

    /// <summary>Creates the new node.</summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>A Value.</returns>
    private static NodeEmulator CreateNewNode(ISymbol symbol)
    {
        var node = new NodeEmulator
        {
            Text = symbol.InstanceName,
            Tag = symbol
        };
        foreach (var subSymbol in symbol.SubSymbols)
        {
            node.Nodes?.Add(CreateNewNode(subSymbol));
        }

        return node;
    }

    /// <summary>Creates a generated field for a PLC array type.</summary>
    /// <param name="symbol">The source symbol.</param>
    /// <param name="wrapperBuilder">The wrapper builder for fixed string arrays.</param>
    /// <param name="isTwinCat3">Whether TwinCAT 3 packing should be used.</param>
    /// <returns>The generated field source.</returns>
    private static string CreateArrayOFStructure(ISymbol symbol, StringBuilder wrapperBuilder, bool isTwinCat3)
    {
        if (!TryParseArrayType(symbol.TypeName, out var dimensions, out var elementType)
            || !TryGetArrayLength(dimensions, out var totalLength))
        {
            return string.Empty;
        }

        var instanceName = symbol.InstanceName?.Trim();
        if (string.IsNullOrWhiteSpace(instanceName) || string.IsNullOrWhiteSpace(elementType) || totalLength <= 0)
        {
            return string.Empty;
        }

        var csharpType = elementType;
        var marshalAttribute = $"[MarshalAs(UnmanagedType.ByValArray, SizeConst = {totalLength})]";
        if (TryGetPrimitiveArrayMapping(elementType, totalLength, out var primitiveType, out var primitiveMarshalAttribute))
        {
            csharpType = primitiveType;
            marshalAttribute = primitiveMarshalAttribute;
        }
        else if (TryCreateStringArrayWrapper(elementType, wrapperBuilder, isTwinCat3, out var wrapperName))
        {
            csharpType = wrapperName;
        }

        return BuildArrayField(marshalAttribute, csharpType, instanceName!, totalLength);
    }

    /// <summary>Parses a PLC array type into dimensions and element type.</summary>
    /// <param name="typeName">The PLC type name.</param>
    /// <param name="dimensions">The parsed dimensions.</param>
    /// <param name="elementType">The parsed element type.</param>
    /// <returns><c>true</c> when parsing succeeds.</returns>
    private static bool TryParseArrayType(string typeName, out string dimensions, out string elementType)
    {
        var trimmedTypeName = typeName.Trim();
        var arrayIndex = trimmedTypeName.IndexOf("ARRAY [", StringComparison.OrdinalIgnoreCase);
        var ofIndex = trimmedTypeName.IndexOf("] OF ", StringComparison.OrdinalIgnoreCase);
        if (arrayIndex < 0 || ofIndex < 0)
        {
            dimensions = string.Empty;
            elementType = string.Empty;
            return false;
        }

        dimensions = trimmedTypeName.Substring(arrayIndex + "ARRAY [".Length, ofIndex - (arrayIndex + "ARRAY [".Length)).Trim();
        elementType = trimmedTypeName.Substring(ofIndex + "] OF ".Length).Trim();
        return true;
    }

    /// <summary>Gets the flattened element count for PLC array dimensions.</summary>
    /// <param name="dimensions">The PLC dimensions.</param>
    /// <param name="totalLength">The flattened element count.</param>
    /// <returns><c>true</c> when the length was calculated.</returns>
    private static bool TryGetArrayLength(string dimensions, out int totalLength)
    {
        totalLength = 1;
        foreach (var dimension in dimensions.Split(','))
        {
            var bounds = dimension.Trim().Split(RangeSeparator, StringSplitOptions.None);
            if (bounds.Length != 2
                || !int.TryParse(bounds[0].Trim(), out var lower)
                || !int.TryParse(bounds[1].Trim(), out var upper)
                || upper < lower)
            {
                totalLength = 0;
                return false;
            }

            totalLength *= upper - lower + 1;
        }

        return true;
    }

    /// <summary>Gets the C# type and marshal attribute for primitive PLC array elements.</summary>
    /// <param name="elementType">The PLC element type.</param>
    /// <param name="totalLength">The flattened element count.</param>
    /// <param name="csharpType">The C# type.</param>
    /// <param name="marshalAttribute">The marshal attribute source.</param>
    /// <returns><c>true</c> when the element type is primitive.</returns>
    private static bool TryGetPrimitiveArrayMapping(string elementType, int totalLength, out string csharpType, out string marshalAttribute)
    {
        if (!PrimitiveArrayMappings.TryGetValue(elementType.ToUpperInvariant(), out var mapping))
        {
            csharpType = string.Empty;
            marshalAttribute = string.Empty;
            return false;
        }

        csharpType = mapping.CSharpType;
        marshalAttribute = $"[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.{mapping.MarshalSubType}, SizeConst = {totalLength})]";
        return true;
    }

    /// <summary>Creates a wrapper struct for fixed-length PLC string arrays.</summary>
    /// <param name="elementType">The PLC element type.</param>
    /// <param name="wrapperBuilder">The wrapper builder.</param>
    /// <param name="isTwinCat3">Whether TwinCAT 3 packing should be used.</param>
    /// <param name="wrapperName">The wrapper type name.</param>
    /// <returns><c>true</c> when a string wrapper was created or found.</returns>
    private static bool TryCreateStringArrayWrapper(string elementType, StringBuilder wrapperBuilder, bool isTwinCat3, out string wrapperName)
    {
        if (!TryGetFixedStringLength(elementType, out var stringLength))
        {
            wrapperName = string.Empty;
            return false;
        }

        wrapperName = $"STRING_{stringLength}_WRAPPER";
        if (wrapperBuilder.ToString().Contains($"struct {wrapperName}"))
        {
            return true;
        }

        _ = wrapperBuilder.AppendLine($"[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = {(isTwinCat3 ? 0 : 1)})]")
            .AppendLine($"public struct {wrapperName}")
            .AppendLine("{")
            .AppendLine($"    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = {stringLength + 1})]")
            .AppendLine("    public string Value;")
            .AppendLine("}")
            .AppendLine()
            .AppendLine("public static string[] ToStringArray(" + wrapperName + "[] wrappers)")
            .AppendLine("{")
            .AppendLine("    if (wrappers == null) return Array.Empty<string>();")
            .AppendLine("    var result = new string[wrappers.Length];")
            .AppendLine("    for (int i = 0; i < wrappers.Length; i++)")
            .AppendLine("        result[i] = wrappers[i].Value;")
            .AppendLine("    return result;")
            .AppendLine("}")
            .AppendLine();
        return true;
    }

    /// <summary>Tries to read the declared length from a fixed-length PLC string type.</summary>
    /// <param name="elementType">The PLC element type.</param>
    /// <param name="stringLength">The parsed string length.</param>
    /// <returns><c>true</c> when the type is a fixed-length string.</returns>
    private static bool TryGetFixedStringLength(string elementType, out int stringLength)
    {
        stringLength = 0;
        var openParenIndex = elementType.IndexOf('(');
        if (openParenIndex < 0)
        {
            return false;
        }

        var closeParenIndex = elementType.IndexOf(')', openParenIndex + 1);
        if (closeParenIndex <= openParenIndex)
        {
            return false;
        }

        var typeName = elementType[..openParenIndex].Trim();
        if (!string.Equals(typeName, "STRING", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var lengthText = elementType[(openParenIndex + 1)..closeParenIndex].Trim();
        return int.TryParse(lengthText, out stringLength);
    }

    /// <summary>Builds the generated array field source.</summary>
    /// <param name="marshalAttribute">The marshal attribute source.</param>
    /// <param name="csharpType">The C# type.</param>
    /// <param name="instanceName">The PLC instance name.</param>
    /// <param name="totalLength">The flattened element count.</param>
    /// <returns>The generated array field source.</returns>
    private static string BuildArrayField(string marshalAttribute, string csharpType, string instanceName, int totalLength)
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine(marshalAttribute)
            .Append("public ")
            .Append(csharpType)
            .Append("[] ")
            .Append(instanceName)
            .Append(" = new ")
            .Append(csharpType)
            .Append('[')
            .Append(totalLength)
            .AppendLine("];");
        return sb.ToString();
    }

    /// <summary>Builds the symbol list.</summary>
    private void BuildSymbolList()
    {
        SymbolList.Clear();
        if (_symbolLoader is null)
        {
            return;
        }

        foreach (var symbol in _symbolLoader.Symbols)
        {
            _ = SymbolList.Add(CreateNewNode(symbol));
        }
    }

    /// <summary>Creates the C# code file.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="selectedTN">The selected tn.</param>
    /// <param name="classNamespace">The class namespace.</param>
    /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
    /// <exception cref="Exception">You cannot create a structure from simple types. Please add as a single tag in your program.</exception>
    private void CreateCsharpCodeFile(ref StringBuilder sb, INodeEmulator? selectedTN, string classNamespace, bool isTwinCat3)
    {
        var selectedNodes = selectedTN?.Nodes;
        if (selectedNodes is null || selectedNodes.Count == 0)
        {
            throw new SimpleTypeException("You cannot create a structure from simple types. Please add as a single tag in your program");
        }

        _ = sb.AppendLine("using System;")
            .AppendLine("using System.Runtime.InteropServices;")
            .AppendLine(string.Empty)
            .Append("namespace ").AppendLine(classNamespace)
            .AppendLine("{");
        WriteCSharpClass(ref sb, selectedTN!, isTwinCat3);

        foreach (var node in selectedNodes)
        {
            WriteCSharpClasses(ref sb, node, isTwinCat3);
            var symbol = (ISymbol?)node.Tag;
            if (node.Nodes?.Count <= 0 || symbol?.TypeName.Contains("ARRAY [") == true)
            {
                continue;
            }

            WriteCSharpClass(ref sb, node, isTwinCat3);
        }

        _ = sb.AppendLine("}");
    }

    /// <summary>Finds the next node.</summary>
    /// <param name="selectedTN">The selected tn.</param>
    /// <returns>A Value.</returns>
    private INodeEmulator? FindNextNode(INodeEmulator selectedTN)
    {
        if (selectedTN.Nodes is null)
        {
            return null;
        }

        foreach (var node in selectedTN.Nodes)
        {
            if (node.Nodes?.Count <= 0 || node.Tag is not ISymbol symbol)
            {
                continue;
            }

            if (!_typeList.ContainsKey(symbol.TypeName!) && !symbol.TypeName!.Contains("ARRAY ["))
            {
                return node;
            }
        }

        return null;
    }

    /// <summary>Writes the C# class.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="selectedTN">The selected tn.</param>
    /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
    private void WriteCSharpClass(ref StringBuilder sb, INodeEmulator? selectedTN, bool isTwinCat3)
    {
        if (selectedTN is null)
        {
            return;
        }

        var symbol = (ISymbol?)selectedTN.Tag;
        if (_typeList.ContainsKey(symbol!.TypeName))
        {
            return;
        }

        _ = sb.AppendLine("[Serializable]")
            .Append("[StructLayout(LayoutKind.Sequential, Pack = ").Append(isTwinCat3 ? "0" : "1").AppendLine(")]")
            .Append("public class ").AppendLine(symbol?.TypeName)
            .AppendLine("{");
        _typeList.Add(symbol!.TypeName, symbol!.InstanceName);
        _ = sb.Append("public ").Append(symbol?.TypeName).AppendLine("()")
            .AppendLine("{")
            .AppendLine("}");
        WriteCSharpClassMembers(ref sb, selectedTN, isTwinCat3);
        _ = sb.AppendLine("}")
            .AppendLine(string.Empty);
    }

    /// <summary>Writes the C# classes.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="selectedTN">The selected tn.</param>
    /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
    private void WriteCSharpClasses(ref StringBuilder sb, INodeEmulator selectedTN, bool isTwinCat3)
    {
        while (true)
        {
            var nextNode = FindNextNode(selectedTN);
            if (nextNode is null)
            {
                return;
            }

            WriteCSharpClass(ref sb, nextNode, isTwinCat3);
            WriteCSharpClasses(ref sb, nextNode, isTwinCat3);
        }
    }
}
