// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CSharp;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;

namespace CP.TwinCatRx.Core
{
    /// <summary>
    /// Code Generator.
    /// </summary>
    /// <seealso cref="ICodeGenerator"/>
    internal class CodeGenerator : ICodeGenerator
    {
        /// <summary>
        /// The type list.
        /// </summary>
        private readonly Hashtable _typeList = new();

        /// <summary>
        /// The ads client.
        /// </summary>
        private AdsClient? _adsClient;

        /// <summary>
        /// The disposed value.
        /// </summary>
        private bool _disposedValue;

        /// <summary>
        /// The symbol loader.
        /// </summary>
        private ISymbolLoader? _symbolLoader;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGenerator"/> class.
        /// </summary>
        public CodeGenerator() => _disposedValue = false;

        /// <summary>
        /// Gets the symbol list.
        /// </summary>
        /// <value>The symbol list.</value>
        public HashSet<INodeEmulator> SymbolList { get; } = new HashSet<INodeEmulator>();

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
        /// <param name="pLCType">Type of the PLC.</param>
        /// <returns>A Value.</returns>
        /// <exception cref="Exception">
        /// This Type (" + PLCType + ")is not supported in this version, Please contact A I C
        /// Solutions for details of next version release.
        /// </exception>
        public static string PLCToCSharpTypeConverter(string? pLCType)
        {
            switch (pLCType)
            {
                case "STRING(80)":
                    return "string";

                case "BIT":
                case "BIT8":
                case "BOOL":
                    return "bool";

                case "WORD":
                case "BITARR16":
                case "UINT16":
                case "UINT":
                    return "ushort";

                case "INT8":
                    return "sbyte";

                case "INT16":
                case "INT":
                    return "short";

                case "INT32":
                case "DINT":
                    return "int";

                case "BITARR32":
                case "DWORD":
                case "UINT32":
                case "UDINT":
                    return "uint";

                case "UINT64":
                case "ULINT":
                    return "ulong";

                case "INT64":
                case "LINT":
                    return "long";

                case "FLOAT":
                case "REAL":
                    return "float";

                case "DOUBLE":
                case "LREAL":
                    return "double";

                case "BITARR8":
                case "USINT":
                case "UINT8":
                case "BYTE":
                    return "byte";

                case null:
                    return "NULL";

                default:
                    if (pLCType.Contains("STRING("))
                    {
                        var s = pLCType.Replace("STRING(", string.Empty);
                        s = s.Replace(")", string.Empty);
                        return $"string,{s}";
                    }

                    if (pLCType.Contains("OF BOOL"))
                    {
                        return "bool[]";
                    }

                    if (pLCType.Contains("OF BIT"))
                    {
                        return "bool[]";
                    }

                    if (pLCType.Contains("OF BIT8"))
                    {
                        return "bool[]";
                    }

                    if (pLCType.Contains("OF BYTE"))
                    {
                        return "byte[]";
                    }

                    if (pLCType.Contains("OF REAL"))
                    {
                        return "System.Single[]";
                    }

                    if (pLCType.Contains("OF FLOAT"))
                    {
                        return "System.Single[]";
                    }

                    if (pLCType.Contains("OF INT"))
                    {
                        return "System.Int16[]";
                    }

                    if (pLCType.Contains("OF INT16"))
                    {
                        return "System.Int16[]";
                    }

                    if (pLCType.Contains("OF DINT"))
                    {
                        return "System.Int32[]";
                    }

                    if (pLCType.Contains("OF INT32"))
                    {
                        return "System.Int32[]";
                    }

                    throw new UnsuportedTypeException("This Type (" + pLCType + ")is not supported in this version, Please contact us for details of next version release");
            }
        }

        /// <summary>
        /// Creates a C# code file based on the structure of the Node presented the output is saved
        /// to the File Name provided.
        /// </summary>
        /// <param name="selectedTN">The selected tn.</param>
        /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
        /// <returns>
        /// Result as a Boolean.
        /// </returns>
        public bool CreateCSharpCode(INodeEmulator selectedTN, bool isTwinCat3 = false) => CreateCSharpCode(selectedTN, string.Empty, isTwinCat3, "TwinCATRx");

        /// <summary>
        /// Creates a C# code file based on the structure of the Node presented the output is saved
        /// to the File Name provided.
        /// </summary>
        /// <param name="selectedTN">The selected tn.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
        /// <param name="classNamespace">The class namespace.</param>
        /// <returns>
        /// Result as a Boolean.
        /// </returns>
        public bool CreateCSharpCode(INodeEmulator selectedTN, string fileName, bool isTwinCat3 = false, string classNamespace = "TwinCATRx")
        {
            if (selectedTN?.Nodes?.Count != 0)
            {
                _typeList.Clear();
                var sb = new StringBuilder();
                CreateCsharpCodeFile(ref sb, selectedTN, classNamespace, isTwinCat3);
                if (sb.ToString().Length > 1)
                {
                    try
                    {
                        ((ISymbol?)selectedTN?.Tag)?.InstanceName.Remove(0, 1);
                        using (Stream s = File.Open(fileName, FileMode.Create))
                        using (var sw = new StreamWriter(s))
                        using (var cSharpCodeProvider = new CSharpCodeProvider())
                        {
                            var csu = new CodeSnippetCompileUnit(sb.ToString());
                            cSharpCodeProvider.CreateGenerator(sw).GenerateCodeFromCompileUnit(csu, sw, new CodeGeneratorOptions()
                            {
                                BracingStyle = "C",
                                IndentString = "   "
                            });
                        }

                        return true;
                    }
                    catch
                    {
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Creates the c sharp code string.
        /// </summary>
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

        /// <summary>
        /// Creates a dll based on the structure of the Node presented the output is saved as the
        /// File Name provided.
        /// </summary>
        /// <param name="selectedTN">The selected tn.</param>
        /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
        /// <returns>
        /// Result as a Boolean.
        /// </returns>
        public bool CreateDll(INodeEmulator selectedTN, bool isTwinCat3 = false) => CreateDll(selectedTN, string.Empty, isTwinCat3, "TwinCATRx");

        /// <summary>
        /// Creates a dll based on the structure of the Node presented the output is saved as the
        /// File Name provided.
        /// </summary>
        /// <param name="selectedTN">The selected tn.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="isTwinCat3">if set to <c>true</c> [is twincat3].</param>
        /// <param name="classNamespace">The class namespace.</param>
        /// <returns>
        /// Result as a Boolean.
        /// </returns>
        public bool CreateDll(INodeEmulator? selectedTN, string fileName, bool isTwinCat3 = false, string classNamespace = "TwinCATRx")
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                File.Delete(fileName);
                if (selectedTN?.Nodes?.Count != 0)
                {
                    var sb = new StringBuilder();
                    _typeList.Clear();
                    CreateCsharpCodeFile(ref sb, selectedTN, classNamespace, isTwinCat3);
                    if (sb.ToString().Length > 1)
                    {
                        try
                        {
                            return CSharpLanguage.CreateAssembly(sb.ToString(), fileName);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Creates the DLL.
        /// </summary>
        /// <param name="cSharpSourceCode">The c sharp source code.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>A Value.</returns>
        public bool CreateDll(string cSharpSourceCode, string fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                File.Delete(fileName);
                if (cSharpSourceCode?.Length > 1)
                {
                    try
                    {
                        return CSharpLanguage.CreateAssembly(cSharpSourceCode, fileName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Loads Symbols form specified PLC ADS Address and Returns a HashSet(Of NodeEmulator).
        /// </summary>
        /// <param name="aDSAddress">The ads address.</param>
        /// <returns>
        /// HashSet(Of NodeEmulator).
        /// </returns>
        public HashSet<INodeEmulator> LoadSymbols(string aDSAddress) => LoadSymbols(aDSAddress, 801);

        /// <summary>
        /// Loads Symbols form specified PLC ADS Address and Returns a HashSet(Of NodeEmulator).
        /// </summary>
        /// <param name="aDSAddress">The ads address.</param>
        /// <param name="port">The port.</param>
        /// <returns>
        /// HashSet(Of NodeEmulator).
        /// </returns>
        public HashSet<INodeEmulator> LoadSymbols(string aDSAddress, int port)
        {
            try
            {
                _adsClient = new AdsClient();
                _adsClient.Connect(aDSAddress, port);
                _symbolLoader = SymbolLoaderFactory.Create(_adsClient, SymbolLoaderSettings.Default);
                BuildSymbolList();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _adsClient?.Dispose();
                _adsClient = new AdsClient();
            }

            return SymbolList;
        }

        /// <summary>
        /// Loads Symbols form specified PLC ADS port and Returns a HashSet(Of NodeEmulator).
        /// </summary>
        /// <param name="port">The port.</param>
        /// <returns>A Value.</returns>
        public HashSet<INodeEmulator> LoadSymbols(int port)
        {
            try
            {
                _adsClient = new();
                _adsClient.Connect(port);
                _symbolLoader = SymbolLoaderFactory.Create(_adsClient, SymbolLoaderSettings.Default);
                BuildSymbolList();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _adsClient?.Dispose();
                _adsClient = new();
            }

            return SymbolList;
        }

        /// <summary>
        /// Reads the symbol.
        /// </summary>
        /// <param name="aDSAddress">The ads address.</param>
        /// <param name="port">The port.</param>
        /// <param name="variable">The variable.</param>
        /// <param name="variableType">Type of the variable.</param>
        /// <returns>A Value.</returns>
        public object ReadSymbol(string aDSAddress, int port, string variable, Type variableType)
        {
            var obj = RuntimeHelpers.GetObjectValue(new object());
            try
            {
                _adsClient = new();
                _adsClient.Connect(aDSAddress, port);
                obj = RuntimeHelpers.GetObjectValue(_adsClient.ReadAny(_adsClient.CreateVariableHandle(variable), variableType));
            }
            finally
            {
                _adsClient!.Dispose();
                _adsClient = new();
            }

            return obj;
        }

        /// <summary>
        /// Searches for first element with the nearest match in SymbolList for Symbol Name entered.
        /// </summary>
        /// <param name="symbolName">Name of the symbol.</param>
        /// <returns>
        /// NodeEmulator.
        /// </returns>
        public INodeEmulator SearchSymbols(string? symbolName)
        {
            if (symbolName?.StartsWith(".", StringComparison.InvariantCulture) == true)
            {
                symbolName = symbolName.Remove(0, 1);
            }

            var symbols = symbolName?.Split('.');

            // get first symbol
            var ret = SymbolList.First(x => x.Text.ToUpperInvariant().Equals(symbols?[0]?.ToUpperInvariant(), StringComparison.Ordinal));
            if (symbols?.Length > 1)
            {
                for (var i = 1; i < symbols.Length; i++)
                {
                    ret = ret!.Nodes?.First(x => x.Text.ToUpperInvariant().Equals(symbols[i]?.ToUpperInvariant(), StringComparison.Ordinal));
                }
            }

            return ret ?? new NodeEmulator();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
        /// unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _adsClient?.Dispose();
                    SymbolList?.Clear();
                }
            }

            _disposedValue = true;
        }

        /// <summary>
        /// Writes the c sharp class members.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <param name="selectedTN">The selected tn.</param>
        private static void WriteCSharpClassMembers(ref StringBuilder sb, INodeEmulator selectedTN)
        {
            HashSet<INodeEmulator>.Enumerator enumerator = default;
            try
            {
                enumerator = selectedTN.Nodes!.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var t = enumerator.Current;
                    if (t.Tag is not TwinCAT.TypeSystem.ISymbol)
                    {
                        continue;
                    }

                    var symbol = (ISymbol)t.Tag;
                    var str = symbol.InstanceName;
                    if (symbol.Category != DataTypeCategory.Array && symbol.Category != DataTypeCategory.String && symbol.Category != DataTypeCategory.Primitive && !symbol.TypeName.Contains("ARRAY ["))
                    {
                        sb.Append("public ").Append(symbol.TypeName).Append(' ').Append(str).Append(" = new ").Append(symbol.TypeName).AppendLine("();");
                    }
                    else
                    {
                        var c_type = PLCToCSharpTypeConverter(symbol.TypeName);
                        if (c_type == "bool")
                        {
                            sb.AppendLine("[MarshalAs(UnmanagedType.I1)]")
                                .Append("public ").Append(c_type).Append(' ').Append(str).AppendLine(";");
                        }
                        else if (c_type == "string")
                        {
                            sb.AppendLine("[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 81)]")
                                .Append("public ").Append(c_type).Append(' ').Append(str).AppendLine(";");
                        }
                        else if (c_type.Contains("string,"))
                        {
                            var num = int.Parse(c_type.Split(',')[1]);
                            sb.Append("[MarshalAs(UnmanagedType.ByValTStr, SizeConst = ").Append(num + 1).AppendLine(")]")
                                .Append("public string ").Append(str).AppendLine(";");
                        }
                        else if (c_type.Contains("System.Single[],"))
                        {
                            var nums = c_type.Split(',')[1];
                            var num = int.Parse(nums.Split('.')[2]);
                            sb.Append("[MarshalAs(UnmanagedType.ByValArray, SizeConst = ").Append(num + 1).AppendLine(")]")
                                .Append("public System.Single[] ").Append(str).Append(" = new ").Append("System.Single[").Append(num).AppendLine("];");
                        }
                        else if (c_type.Contains("System.Int16[],"))
                        {
                            var nums = c_type.Split(',')[1];
                            var num = int.Parse(nums.Split('.')[2]);
                            sb.Append("[MarshalAs(UnmanagedType.ByValArray, SizeConst = ").Append(num + 1).AppendLine(")]")
                                .Append("public System.Int16[] ").Append(str).Append(" = new ").Append("System.Int16[").Append(num).AppendLine("];");
                        }
                        else if (c_type.Contains("System.Int32[],"))
                        {
                            var nums = c_type.Split(',')[1];
                            var num = int.Parse(nums.Split('.')[2]);
                            sb.Append("[MarshalAs(UnmanagedType.ByValArray, SizeConst = ").Append(num + 1).AppendLine(")]")
                                .Append("public System.Int32[] ").Append(str).Append(" = new ").Append("System.Int32[").Append(num).AppendLine("];");
                        }
                        else if (c_type.Contains("bool[],"))
                        {
                            var nums = c_type.Split(',')[1];
                            var num = int.Parse(nums.Split('.')[2]);
                            sb.Append("[MarshalAs(UnmanagedType.ByValArray, SizeConst = ").Append(num + 1).AppendLine(")]")
                                .Append("public bool[] ").Append(str).Append(" = new ").Append("bool[").Append(num).AppendLine("];");
                        }
                        else
                        {
                            sb.Append("public ").Append(c_type).Append(' ').Append(str).AppendLine(";");
                        }
                    }
                }
            }
            finally
            {
                enumerator.Dispose();
            }
        }

        /// <summary>
        /// Builds the symbol list.
        /// </summary>
        private void BuildSymbolList()
        {
            SymbolList.Clear();
            try
            {
                if (_symbolLoader != null)
                {
                    foreach (var symbol in _symbolLoader.Symbols)
                    {
                        SymbolList.Add(CreateNewNode(symbol));
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Creates the csharp code file.
        /// </summary>
        /// <param name="sb">The _SB.</param>
        /// <param name="selectedTN">The selected tn.</param>
        /// <param name="classNamespace">The class namespace.</param>
        /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
        /// <exception cref="Exception">You cannot create a structure from simple types. Please add as a single tag in your program.</exception>
        private void CreateCsharpCodeFile(ref StringBuilder sb, INodeEmulator? selectedTN, string classNamespace, bool isTwinCat3)
        {
            if (selectedTN?.Nodes?.Count <= 0)
            {
                throw new SimpleTypeException("You cannot create a structure from simple types. Please add as a single tag in your program");
            }

            sb.AppendLine("using System;")
                .AppendLine("using System.Runtime.InteropServices;")
                .AppendLine(string.Empty)
                .Append("namespace ").AppendLine(classNamespace)
                .AppendLine("{");
            WriteCSharpClass(ref sb, selectedTN, isTwinCat3);
            try
            {
                if (selectedTN?.Nodes != null)
                {
                    foreach (var t in selectedTN.Nodes)
                    {
                        WriteCSharpClasses(ref sb, t, isTwinCat3);
                        var symbol = (ISymbol?)t.Tag;
                        if (t.Nodes?.Count <= 0 || symbol?.TypeName.Contains("ARRAY [") == true)
                        {
                            continue;
                        }

                        WriteCSharpClass(ref sb, t, isTwinCat3);
                    }
                }
            }
            catch
            {
            }

            sb.AppendLine("}");
        }

        /// <summary>
        /// Creates the new node.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>A Value.</returns>
        private INodeEmulator CreateNewNode(ISymbol symbol)
        {
            INodeEmulator node = new NodeEmulator
            {
                Text = symbol.InstanceName,
                Tag = symbol
            };
            foreach (var subsymbol in symbol.SubSymbols)
            {
                node.Nodes?.Add(CreateNewNode(subsymbol));
            }

            return node;
        }

        /// <summary>
        /// Finds the next node.
        /// </summary>
        /// <param name="selectedTN">The selected tn.</param>
        /// <returns>A Value.</returns>
        private INodeEmulator? FindNextNode(INodeEmulator selectedTN)
        {
            try
            {
                foreach (var t in selectedTN.Nodes!.Where(x => x?.Nodes?.Count > 0 && x?.Tag is ISymbol))
                {
                    var symbol = (ISymbol?)t.Tag;
                    if (_typeList.ContainsKey(symbol!.TypeName!) || symbol!.TypeName!.Contains("ARRAY ["))
                    {
                        continue;
                    }

                    return t;
                }
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// Writes the c sharp class.
        /// </summary>
        /// <param name="sb">The _SB.</param>
        /// <param name="selectedTN">The selected tn.</param>
        /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
        private void WriteCSharpClass(ref StringBuilder sb, INodeEmulator? selectedTN, bool isTwinCat3)
        {
            if (selectedTN != null)
            {
                var symbol = (ISymbol?)selectedTN.Tag;
                if (!_typeList.ContainsKey(symbol!.TypeName))
                {
                    sb.AppendLine("[Serializable]")
                        .Append("[StructLayout(LayoutKind.Sequential, Pack = ").Append(isTwinCat3 ? "0" : "1").AppendLine(")]")
                        .Append("public class ").AppendLine(symbol?.TypeName)
                        .AppendLine("{");
                    _typeList.Add(symbol!.TypeName, symbol!.InstanceName);
                    sb.Append("public ").Append(symbol?.TypeName).AppendLine("()")
                        .AppendLine("{")
                        .AppendLine("}");
                    WriteCSharpClassMembers(ref sb, selectedTN);
                    sb.AppendLine("}")
                        .AppendLine(string.Empty);
                }
            }
        }

        /// <summary>
        /// Writes the c sharp classes.
        /// </summary>
        /// <param name="sb">The _SB.</param>
        /// <param name="selectedTN">The selected tn.</param>
        /// <param name="isTwinCat3">if set to <c>true</c> [is twin cat3].</param>
        private void WriteCSharpClasses(ref StringBuilder sb, INodeEmulator selectedTN, bool isTwinCat3)
        {
            var b = false;
            try
            {
                while (!b)
                {
                    var selectedTN1 = FindNextNode(selectedTN);
                    if (selectedTN1 != null)
                    {
                        WriteCSharpClass(ref sb, selectedTN1, isTwinCat3);
                        WriteCSharpClasses(ref sb, selectedTN1, isTwinCat3);
                    }
                    else
                    {
                        b = true;
                    }
                }
            }
            catch
            {
            }
        }
    }
}
