// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using CP.TwinCatRx.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TwinCATRx.SourceGenerators.Tests;

/// <summary>Exercises deterministic source-generator inputs through the Roslyn generator driver.</summary>
public class TwinCatReactiveStreamGeneratorTests
{
    /// <summary>The two post-initialization attribute sources.</summary>
    private const int AttributeSourceCount = 2;

    /// <summary>The two attribute sources plus one connection source.</summary>
    private const int AttributeAndConnectionSourceCount = 3;

    /// <summary>Verifies post-initialization attributes are emitted for an empty compilation.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Empty_Input_Emits_Both_Attribute_Surfaces()
    {
        var result = RunGenerator(string.Empty);

        await TUnitAssert.That(result.Diagnostics.Length).IsEqualTo(0);
        await TUnitAssert.That(result.GeneratedSources.Length).IsEqualTo(AttributeSourceCount);
        await TUnitAssert.That(GetSource(result, "TwinCatReactiveStreamAttribute.Lean.g.cs"))
            .Contains("namespace CP.TwinCatRx;");
        await TUnitAssert.That(GetSource(result, "TwinCatReactiveStreamAttribute.Reactive.g.cs"))
            .Contains("namespace CP.TwinCatRx.Reactive;");
    }

    /// <summary>Verifies invalid stream and connection values are ignored without generator diagnostics.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Invalid_Attribute_Values_Are_Ignored_Without_Diagnostics()
    {
        const string source = """
            using CP.TwinCatRx;

            [TwinCatReactiveStream("", typeof(int))]
            internal partial class EmptyStream;

            [TwinCatPlcConnection("address", 851)]
            internal partial class EmptyConnection
            {
                [DirectNotification("")]
                public int InvalidDirect { get; }

                [StructuredNotification("")]
                public int InvalidStructured { get; }

                [WriteOnly("")]
                public int InvalidWrite { get; }
            }
            """;

        var result = RunGenerator(source);

        await TUnitAssert.That(result.Diagnostics.Length).IsEqualTo(0);
        await TUnitAssert.That(result.GeneratedSources.Length).IsEqualTo(AttributeAndConnectionSourceCount);
        var generated = GetGeneratedSource(result, "TwinCatPlcConnection");
        await TUnitAssert.That(generated).Contains("internal partial class EmptyConnection");
        await TUnitAssert.That(generated).DoesNotContain("InvalidDirectObservable");
        await TUnitAssert.That(generated).DoesNotContain("InvalidStructuredObservable");
        await TUnitAssert.That(generated).DoesNotContain("WriteInvalidWrite");
    }

    /// <summary>Verifies lean legacy streams sanitize identifiers, honor explicit names, and escape literals.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Lean_Legacy_Stream_Emits_Sanitized_And_Escaped_Bindings()
    {
        const string source = """
            using CP.TwinCatRx;

            namespace Generator.Samples;

            [TwinCatReactiveStream("!!!", typeof(string))]
            [TwinCatReactiveStream("42 speed", typeof(int))]
            [TwinCatReactiveStream("MAIN.Path\\Segment\"Quoted", typeof(double), Id = "id\\part\"quoted", PropertyName = "MotorSpeed", ObservableName = "MotorChanges")]
            public partial class LeanStreams;
            """;

        var result = RunGenerator(source);
        var generated = GetGeneratedSource(result, "Lean.TwinCatReactiveStream");

        await TUnitAssert.That(result.Diagnostics.Length).IsEqualTo(0);
        await TUnitAssert.That(generated).Contains("namespace Generator.Samples;");
        await TUnitAssert.That(generated).Contains("public string? Value");
        await TUnitAssert.That(generated).Contains("public int? Value42speed");
        await TUnitAssert.That(generated).Contains("public double? MotorSpeed");
        await TUnitAssert.That(generated).Contains("MotorChanges => _motorSpeedSubject;");
        await TUnitAssert.That(generated).Contains("MAIN.Path\\\\Segment\\\"Quoted");
        await TUnitAssert.That(generated).Contains("id\\\\part\\\"quoted");
        await TUnitAssert.That(generated).Contains("global::CP.TwinCatRx.IRxTcAdsClient");
    }

    /// <summary>Verifies Reactive legacy bindings use the System.Reactive-compatible API aliases.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Reactive_Legacy_Stream_Uses_Reactive_Api_Surface()
    {
        const string source = """
            [CP.TwinCatRx.Reactive.TwinCatReactiveStream(".Reactive", typeof(long), Id = "reactive")]
            internal partial class ReactiveStreams;
            """;

        var result = RunGenerator(source);
        var generated = GetGeneratedSource(result, "Reactive.TwinCatReactiveStream");

        await TUnitAssert.That(result.Diagnostics.Length).IsEqualTo(0);
        await TUnitAssert.That(generated).DoesNotContain("namespace ");
        await TUnitAssert.That(generated).Contains("global::CP.TwinCatRx.Reactive.IRxTcAdsClient");
        await TUnitAssert.That(generated).Contains("global::CP.TwinCatRx.Reactive.ObservableBridgeExtensions");
        await TUnitAssert.That(generated).Contains("global::CP.TwinCatRx.Reactive.TwinCatRxExtensions");
        await TUnitAssert.That(generated).Contains("Observe<long>(client, \".Reactive\", \"reactive\")");
    }

    /// <summary>Verifies lean PLC connections emit direct, structured, write-only, and settings branches.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Lean_Connection_Emits_All_Property_Variants()
    {
        const string source = """
            using CP.TwinCatRx;

            namespace Generator.Samples;

            [TwinCatPlcConnection("1.2.3.4.5.6", 851, SettingsId = "Line\"One")]
            public partial class LeanConnection
            {
                [DirectNotification(".Direct", CycleTime = 25, ArraySize = 3, Id = "direct-id", ObservableName = "DirectChanges", CanWrite = false)]
                public int Direct { get; }

                [StructuredNotification(".Root", "Nested.Value", CycleTime = 50, WriteAddress = ".Override", Id = "structured-id")]
                public int Structured { get; }

                [StructuredNotification(".Root", MemberAddress = "Nested.Other")]
                public int StructuredNamed { get; }

                [WriteOnly(".Root.Nested.Command", ArraySize = 2, Id = "write-id")]
                public int Command { get; }

                [WriteOnly(".Separate")]
                public int Separate { get; }

                [DirectNotification(".Static")]
                public static int StaticValue { get; }
            }
            """;

        var result = RunGenerator(source);
        var generated = GetGeneratedSource(result, "Lean.TwinCatPlcConnection");

        await TUnitAssert.That(result.Diagnostics.Length).IsEqualTo(0);
        await TUnitAssert.That(generated).Contains("using CP.Collections;");
        await TUnitAssert.That(generated).Contains("using CP.TwinCatRx.Core;");
        await TUnitAssert.That(generated).Contains("AdsAddress = \"1.2.3.4.5.6\"");
        await TUnitAssert.That(generated).Contains("Port = 851");
        await TUnitAssert.That(generated).Contains("SettingsId = \"Line\\\"One\"");
        await TUnitAssert.That(generated).Contains("AddNotification(\".Direct\", cycleTime: 25, arraySize: 3)");
        await TUnitAssert.That(generated).Contains("AddNotification(\".Root\", cycleTime: 50, arraySize: -1)");
        await TUnitAssert.That(generated).Contains("AddWriteVariable(\".Root.Nested.Command\", arraySize: 2)");
        await TUnitAssert.That(generated).Contains("DirectChanges => _directSubject;");
        await TUnitAssert.That(generated).Contains("TwinCatRxHashTableExtensions.Observe<int>(structure0, \"Nested.Value\")");
        await TUnitAssert.That(generated).Contains("WriteCommand(int value)");
        await TUnitAssert.That(generated).Contains("AddTwinCatRxStructuredWrite");
        await TUnitAssert.That(generated).Contains("TryWriteTwinCatRxStructure");
        await TUnitAssert.That(generated).DoesNotContain("StaticValueObservable");
        await TUnitAssert.That(generated).DoesNotContain("WriteDirect(global::System.Int32 value)");
    }

    /// <summary>Verifies Reactive PLC connections consistently select Reactive aliases and generated members.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Reactive_Connection_Uses_Reactive_Api_Surface()
    {
        const string source = """
            namespace Generator.ReactiveSamples
            {
                [CP.TwinCatRx.Reactive.TwinCatPlcConnection("reactive-address", 852)]
                internal partial class ReactiveConnection
                {
                    [CP.TwinCatRx.Reactive.DirectNotification(".Value", WriteAddress = ".WriteValue")]
                    public int Value { get; }

                    [CP.TwinCatRx.Reactive.StructuredNotification(".Structure", "Nested.Value")]
                    public int NestedValue { get; }

                    [CP.TwinCatRx.Reactive.WriteOnly(".Only")]
                    public string Only { get; }
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = GetGeneratedSource(result, "Reactive.TwinCatPlcConnection");

        await TUnitAssert.That(result.Diagnostics.Length).IsEqualTo(0);
        await TUnitAssert.That(generated).Contains("using CP.Collections.Reactive;");
        await TUnitAssert.That(generated).Contains("using CP.TwinCatRx.Core.Reactive;");
        await TUnitAssert.That(generated).Contains("global::CP.TwinCatRx.Reactive.IRxTcAdsClient");
        await TUnitAssert.That(generated).Contains("global::CP.TwinCatRx.Reactive.RxTcAdsClient");
        await TUnitAssert.That(generated).Contains("AdsAddress = \"reactive-address\"");
        await TUnitAssert.That(generated).Contains("Port = 852");
        await TUnitAssert.That(generated).Contains("SettingsId = \"ReactiveConnection\"");
        await TUnitAssert.That(generated).Contains("WriteValue(int value)");
        await TUnitAssert.That(generated).Contains("WriteOnly(string value)");
        await TUnitAssert.That(generated).Contains("ReadValue()");
        await TUnitAssert.That(generated).DoesNotContain("ReadNestedValue()");
        await TUnitAssert.That(generated).DoesNotContain("ReadOnly()");
    }

    /// <summary>Verifies edge-case names and addresses traverse deterministic fallback branches.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Edge_Case_Names_And_Addresses_Use_Fallbacks()
    {
        const string source = """
            using CP.TwinCatRx;

            [TwinCatReactiveStream(".BlankName", typeof(int), PropertyName = "", ObservableName = "")]
            internal partial class BlankNameStream;

            [TwinCatPlcConnection("edge", 853)]
            internal partial class EdgeConnection
            {
                [DirectNotification(".Direct", WriteAddress = " ", Id = "direct")]
                public int Direct { get; }

                [StructuredNotification(".Root.", "Member")]
                public int RootMember { get; }

                [StructuredNotification(".Other", ".Member")]
                public int OtherMember { get; }

                [WriteOnly(".Root.Child")]
                public int RootCommand { get; }

                public int Plain { get; }
            }

            internal partial class Container
            {
                [TwinCatReactiveStream(".Nested", typeof(byte))]
                private partial class NestedStream;
            }
            """;

        var result = RunGenerator(source);
        var generated = GetGeneratedSource(result, "EdgeConnection.Lean.TwinCatPlcConnection");

        await TUnitAssert.That(result.Diagnostics.Length).IsEqualTo(0);
        await TUnitAssert.That(generated).Contains("client.Write(\".Direct\", typedValue, id: \"direct\")");
        await TUnitAssert.That(generated).Contains("AddTwinCatRxStructuredWrite(structuredWrites, \".Root.\", \"Member\", \".Root.Member\"");
        await TUnitAssert.That(generated).Contains("AddTwinCatRxStructuredWrite(structuredWrites, \".Other\", \".Member\", \".Other.Member\"");
        await TUnitAssert.That(generated).Contains("AddTwinCatRxStructuredWrite(structuredWrites, \".Root.\", \"Child\", \".Root.Child\"");
        await TUnitAssert.That(generated).DoesNotContain("PlainObservable");
        await TUnitAssert.That(GetGeneratedSource(result, "NestedStream.Lean.TwinCatReactiveStream"))
            .Contains("internal partial class NestedStream");
    }

    /// <summary>Runs the incremental generator against one deterministic source document.</summary>
    /// <param name="source">The source document.</param>
    /// <returns>The single generator result.</returns>
    private static GeneratorRunResult RunGenerator(string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [syntaxTree],
            GetPlatformReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TwinCatReactiveStreamGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);
        var runResult = driver.GetRunResult();
        if (diagnostics.Length != 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, diagnostics));
        }

        return runResult.Results[0];
    }

    /// <summary>Gets the current runtime reference assemblies for an in-memory compilation.</summary>
    /// <returns>The metadata references.</returns>
    private static ImmutableArray<MetadataReference> GetPlatformReferences()
    {
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        var references = ImmutableArray.CreateBuilder<MetadataReference>();
        foreach (var path in trustedAssemblies.Split(Path.PathSeparator))
        {
            references.Add(MetadataReference.CreateFromFile(path));
        }

        return references.ToImmutable();
    }

    /// <summary>Gets a generated source by exact hint name.</summary>
    /// <param name="result">The generator result.</param>
    /// <param name="hintName">The source hint name.</param>
    /// <returns>The generated source text.</returns>
    private static string GetSource(GeneratorRunResult result, string hintName)
    {
        foreach (var source in result.GeneratedSources)
        {
            if (source.HintName == hintName)
            {
                return source.SourceText.ToString();
            }
        }

        throw new InvalidOperationException($"Generated source '{hintName}' was not found.");
    }

    /// <summary>Gets the first generated source whose hint name contains a suffix.</summary>
    /// <param name="result">The generator result.</param>
    /// <param name="hintNamePart">The source hint-name fragment.</param>
    /// <returns>The generated source text.</returns>
    private static string GetGeneratedSource(GeneratorRunResult result, string hintNamePart)
    {
        foreach (var source in result.GeneratedSources)
        {
            if (source.HintName.Contains(hintNamePart, StringComparison.Ordinal))
            {
                return source.SourceText.ToString();
            }
        }

        throw new InvalidOperationException($"Generated source containing '{hintNamePart}' was not found.");
    }
}
