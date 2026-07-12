// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CP.TwinCatRx.SourceGenerators;

/// <summary>Generates TwinCAT reactive stream binding members.</summary>
[Generator(LanguageNames.CSharp)]
public sealed class TwinCatReactiveStreamGenerator : IIncrementalGenerator
{
    /// <summary>Stores the legacy stream attribute metadata name.</summary>
    private const string TwinCatReactiveStreamAttributeName = "CP.TwinCatRx.TwinCatReactiveStreamAttribute";

    /// <summary>Stores the Reactive legacy stream attribute metadata name.</summary>
    private const string ReactiveTwinCatReactiveStreamAttributeName = "CP.TwinCatRx.Reactive.TwinCatReactiveStreamAttribute";

    /// <summary>Stores the PLC connection attribute metadata name.</summary>
    private const string TwinCatPlcConnectionAttributeName = "CP.TwinCatRx.TwinCatPlcConnectionAttribute";

    /// <summary>Stores the Reactive PLC connection attribute metadata name.</summary>
    private const string ReactiveTwinCatPlcConnectionAttributeName = "CP.TwinCatRx.Reactive.TwinCatPlcConnectionAttribute";

    /// <summary>Stores the direct notification attribute metadata name.</summary>
    private const string DirectNotificationAttributeName = "CP.TwinCatRx.DirectNotificationAttribute";

    /// <summary>Stores the Reactive direct notification attribute metadata name.</summary>
    private const string ReactiveDirectNotificationAttributeName = "CP.TwinCatRx.Reactive.DirectNotificationAttribute";

    /// <summary>Stores the structured notification attribute metadata name.</summary>
    private const string StructuredNotificationAttributeName = "CP.TwinCatRx.StructuredNotificationAttribute";

    /// <summary>Stores the Reactive structured notification attribute metadata name.</summary>
    private const string ReactiveStructuredNotificationAttributeName = "CP.TwinCatRx.Reactive.StructuredNotificationAttribute";

    /// <summary>Stores the write-only attribute metadata name.</summary>
    private const string WriteOnlyAttributeName = "CP.TwinCatRx.WriteOnlyAttribute";

    /// <summary>Stores the Reactive write-only attribute metadata name.</summary>
    private const string ReactiveWriteOnlyAttributeName = "CP.TwinCatRx.Reactive.WriteOnlyAttribute";

    /// <summary>Stores the lean library namespace.</summary>
    private const string LeanLibraryNamespace = "CP.TwinCatRx";

    /// <summary>Stores the Reactive library namespace.</summary>
    private const string ReactiveLibraryNamespace = "CP.TwinCatRx.Reactive";

    /// <summary>Stores the lean core namespace.</summary>
    private const string LeanCoreNamespace = "CP.TwinCatRx.Core";

    /// <summary>Stores the Reactive core namespace.</summary>
    private const string ReactiveCoreNamespace = "CP.TwinCatRx.Core.Reactive";

    /// <summary>Stores the lean collections namespace.</summary>
    private const string LeanCollectionsNamespace = "CP.Collections";

    /// <summary>Stores the Reactive collections namespace.</summary>
    private const string ReactiveCollectionsNamespace = "CP.Collections.Reactive";

    /// <summary>Stores the generated using-directive prefix.</summary>
    private const string UsingDirectivePrefix = "using ";

    /// <summary>Stores the direct notification tag kind.</summary>
    private const string DirectKind = "Direct";

    /// <summary>Stores the structured notification tag kind.</summary>
    private const string StructuredKind = "Structured";

    /// <summary>Stores the write-only tag kind.</summary>
    private const string WriteOnlyKind = "WriteOnly";

    /// <summary>Stores the observable-name attribute argument.</summary>
    private const string ObservableNameArgument = "ObservableName";

    /// <summary>Stores the suffix used for generated observable members.</summary>
    private const string ObservableSuffix = "Observable";

    /// <summary>Stores the array-size attribute argument.</summary>
    private const string ArraySizeArgument = "ArraySize";

    /// <summary>Stores a generated class-level opening brace.</summary>
    private const string ClassOpenBrace = "    {";

    /// <summary>Stores a generated class-level closing brace.</summary>
    private const string ClassCloseBrace = "    }";

    /// <summary>Stores a generated block-level opening brace.</summary>
    private const string BlockOpenBrace = "        {";

    /// <summary>Stores a generated block-level closing brace.</summary>
    private const string BlockCloseBrace = "        }";

    /// <summary>Stores a generated nested-block opening brace.</summary>
    private const string NestedBlockOpenBrace = "            {";

    /// <summary>Stores a generated nested-block closing brace.</summary>
    private const string NestedBlockCloseBrace = "            }";

    /// <summary>Stores the modern-framework conditional-compilation directive.</summary>
    private const string Net5OrGreaterDirective = "#if NET5_0_OR_GREATER";

    /// <summary>Stores the conditional-compilation terminator.</summary>
    private const string EndIfDirective = "#endif";

    /// <summary>Stores the generated named identifier argument prefix.</summary>
    private const string IdArgumentPrefix = ", id: \"";

    /// <summary>Stores the generated tag comparison prefix.</summary>
    private const string OrdinalTagComparisonPrefix = " || string.Equals(tag, \"";

    /// <summary>Stores the generated ordinal tag comparison suffix.</summary>
    private const string OrdinalTagComparisonSuffix = "\", StringComparison.OrdinalIgnoreCase)";

    /// <summary>Stores an indented generated false return statement.</summary>
    private const string IndentedReturnFalse = "            return false;";

    /// <summary>Stores the default PLC notification cycle time in milliseconds.</summary>
    private const int DefaultCycleTime = 100;

    /// <summary>Defines the attributes consumed by this source generator.</summary>
    private const string AttributeSource = """
// <auto-generated/>
#nullable enable
namespace CP.TwinCatRx;

[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal sealed class TwinCatReactiveStreamAttribute : System.Attribute
{
    public TwinCatReactiveStreamAttribute(string variable, System.Type dataType)
    {
        Variable = variable;
        DataType = dataType;
    }

    public string Variable { get; }

    public System.Type DataType { get; }

    public string? Id { get; set; }

    public string? PropertyName { get; set; }

    public string? ObservableName { get; set; }
}

[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class TwinCatPlcConnectionAttribute : System.Attribute
{
    public TwinCatPlcConnectionAttribute(string adsAddress, int port)
    {
        AdsAddress = adsAddress;
        Port = port;
    }

    public string AdsAddress { get; }

    public int Port { get; }

    public string? SettingsId { get; set; }
}

[System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class DirectNotificationAttribute : System.Attribute
{
    public DirectNotificationAttribute(string address)
    {
        Address = address;
    }

    public string Address { get; }

    public int CycleTime { get; set; } = 100;

    public int ArraySize { get; set; } = -1;

    public string? Id { get; set; }

    public string? ObservableName { get; set; }

    public bool CanWrite { get; set; } = true;

    public string? WriteAddress { get; set; }
}

[System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class StructuredNotificationAttribute : System.Attribute
{
    public StructuredNotificationAttribute(string address)
    {
        Address = address;
    }

    public StructuredNotificationAttribute(string address, string memberAddress)
    {
        Address = address;
        MemberAddress = memberAddress;
    }

    public string Address { get; }

    public string? MemberAddress { get; set; }

    public int CycleTime { get; set; } = 100;

    public int ArraySize { get; set; } = -1;

    public string? Id { get; set; }

    public string? ObservableName { get; set; }

    public bool CanWrite { get; set; } = true;

    public string? WriteAddress { get; set; }
}

[System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class WriteOnlyAttribute : System.Attribute
{
    public WriteOnlyAttribute(string address)
    {
        Address = address;
    }

    public string Address { get; }

    public int ArraySize { get; set; } = -1;

    public string? Id { get; set; }
}
""";

    /// <summary>Identifies the generated TwinCATRx API surface.</summary>
    private enum ApiSurface
    {
        /// <summary>The lean ReactiveUI.Primitives surface.</summary>
        Lean,

        /// <summary>The System.Reactive-compatible surface.</summary>
        Reactive,
    }

    /// <summary>Initializes the incremental generator pipeline.</summary>
    /// <param name="context">The generator initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ctx.AddSource("TwinCatReactiveStreamAttribute.Lean.g.cs", SourceText.From(AttributeSource, Encoding.UTF8));
            ctx.AddSource(
                "TwinCatReactiveStreamAttribute.Reactive.g.cs",
                SourceText.From(AttributeSource.Replace("namespace CP.TwinCatRx;", "namespace CP.TwinCatRx.Reactive;"), Encoding.UTF8));
        });

        var legacyCandidates = context.SyntaxProvider.ForAttributeWithMetadataName(
                TwinCatReactiveStreamAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => GetLegacyStream(ctx))
            .Where(static stream => stream is not null);

        var connectionCandidates = context.SyntaxProvider.ForAttributeWithMetadataName(
                TwinCatPlcConnectionAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => GetConnection(ctx))
            .Where(static connection => connection is not null);

        var reactiveLegacyCandidates = context.SyntaxProvider.ForAttributeWithMetadataName(
                ReactiveTwinCatReactiveStreamAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => GetLegacyStream(ctx))
            .Where(static stream => stream is not null);

        var reactiveConnectionCandidates = context.SyntaxProvider.ForAttributeWithMetadataName(
                ReactiveTwinCatPlcConnectionAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => GetConnection(ctx))
            .Where(static connection => connection is not null);

        context.RegisterSourceOutput(legacyCandidates.Collect(), static (ctx, streams) => ExecuteLegacy(ctx, streams!));
        context.RegisterSourceOutput(connectionCandidates.Collect(), static (ctx, connections) => ExecuteConnections(ctx, connections!));
        context.RegisterSourceOutput(reactiveLegacyCandidates.Collect(), static (ctx, streams) => ExecuteLegacy(ctx, streams!));
        context.RegisterSourceOutput(reactiveConnectionCandidates.Collect(), static (ctx, connections) => ExecuteConnections(ctx, connections!));
    }

    /// <summary>Creates a stream specification from an attributed class.</summary>
    /// <param name="context">The attributed generator context.</param>
    /// <returns>The stream specification, or <c>null</c> when the attribute is invalid.</returns>
    private static LegacyStreamSpec? GetLegacyStream(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol || context.Attributes.Length == 0)
        {
            return null;
        }

        var surface = GetApiSurface(context.Attributes[0]);
        var specs = new List<LegacyReactivePropertySpec>();
        foreach (var attribute in context.Attributes)
        {
            if (attribute.ConstructorArguments.Length != 2)
            {
                continue;
            }

            var variable = attribute.ConstructorArguments[0].Value as string;
            if (string.IsNullOrWhiteSpace(variable) || attribute.ConstructorArguments[1].Value is not INamedTypeSymbol dataType)
            {
                continue;
            }

            var propertyName = GetNamedString(attribute, "PropertyName") ?? SanitizeIdentifier(variable!);
            var observableName = GetNamedString(attribute, ObservableNameArgument) ?? (propertyName + ObservableSuffix);
            specs.Add(new LegacyReactivePropertySpec(variable!, dataType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), GetNamedString(attribute, "Id"), propertyName, observableName));
        }

        return specs.Count == 0
            ? null
            : new LegacyStreamSpec(GetNamespace(classSymbol), classSymbol.Name, GetAccessibility(classSymbol), surface, specs);
    }

    /// <summary>Creates a PLC connection specification from an attributed class.</summary>
    /// <param name="context">The attributed generator context.</param>
    /// <returns>The connection specification, or <c>null</c> when the attribute is invalid.</returns>
    private static ConnectionSpec? GetConnection(GeneratorAttributeSyntaxContext context)
    {
        if (!TryGetConnectionValues(context, out var classSymbol, out var adsAddress, out var port, out var settingsId))
        {
            return null;
        }

        var surface = GetApiSurface(context.Attributes[0]);
        var properties = new List<PlcPropertySpec>();
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property || property.IsStatic)
            {
                continue;
            }

            var propertySpec = GetPlcProperty(property, surface);
            if (propertySpec is not null)
            {
                properties.Add(propertySpec);
            }
        }

        return new ConnectionSpec(GetNamespace(classSymbol), classSymbol.Name, GetAccessibility(classSymbol), adsAddress, port, settingsId, properties)
        {
            Surface = surface,
        };
    }

    /// <summary>Tries to read class-level PLC connection values.</summary>
    /// <param name="context">The attributed generator context.</param>
    /// <param name="classSymbol">The class symbol.</param>
    /// <param name="adsAddress">The ADS address.</param>
    /// <param name="port">The ADS port.</param>
    /// <param name="settingsId">The settings identifier.</param>
    /// <returns><c>true</c> when connection values were read.</returns>
    private static bool TryGetConnectionValues(GeneratorAttributeSyntaxContext context, out INamedTypeSymbol classSymbol, out string adsAddress, out int port, out string settingsId)
    {
        classSymbol = null!;
        adsAddress = string.Empty;
        port = 0;
        settingsId = string.Empty;
        if (context.TargetSymbol is not INamedTypeSymbol targetClass || context.Attributes.Length == 0)
        {
            return false;
        }

        var connectionAttribute = context.Attributes[0];
        if (connectionAttribute.ConstructorArguments.Length != 2 || connectionAttribute.ConstructorArguments[0].Value is not string targetAddress || connectionAttribute.ConstructorArguments[1].Value is not int targetPort)
        {
            return false;
        }

        classSymbol = targetClass;
        adsAddress = targetAddress;
        port = targetPort;
        settingsId = GetNamedString(connectionAttribute, "SettingsId") ?? targetClass.Name;
        return true;
    }

    /// <summary>Creates a PLC property specification from an attributed property.</summary>
    /// <param name="property">The property symbol.</param>
    /// <param name="surface">The API surface selected by the connection attribute.</param>
    /// <returns>The property specification, or <c>null</c> when no supported attribute is present.</returns>
    private static PlcPropertySpec? GetPlcProperty(IPropertySymbol property, ApiSurface surface)
    {
        var directAttributeName = surface == ApiSurface.Reactive ? ReactiveDirectNotificationAttributeName : DirectNotificationAttributeName;
        var structuredAttributeName = surface == ApiSurface.Reactive ? ReactiveStructuredNotificationAttributeName : StructuredNotificationAttributeName;
        var writeOnlyAttributeName = surface == ApiSurface.Reactive ? ReactiveWriteOnlyAttributeName : WriteOnlyAttributeName;
        foreach (var attribute in property.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();
            if (attributeName == directAttributeName)
            {
                return GetDirectProperty(property, attribute);
            }

            if (attributeName == structuredAttributeName)
            {
                return GetStructuredProperty(property, attribute);
            }

            if (attributeName == writeOnlyAttributeName)
            {
                return GetWriteOnlyProperty(property, attribute);
            }
        }

        return null;
    }

    /// <summary>Creates a direct notification property specification.</summary>
    /// <param name="property">The property symbol.</param>
    /// <param name="attribute">The attribute data.</param>
    /// <returns>The property specification, or <c>null</c> when invalid.</returns>
    private static PlcPropertySpec? GetDirectProperty(IPropertySymbol property, AttributeData attribute)
    {
        var address = GetConstructorString(attribute, 0);
        return string.IsNullOrWhiteSpace(address)
            ? null
            : new PlcPropertySpec(
                new PlcPropertyIdentity(
                    property.Name,
                    property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    GetNamedString(attribute, ObservableNameArgument) ?? (property.Name + ObservableSuffix)),
                new PlcAddressSpec(
                    DirectKind,
                    address!,
                    null,
                    GetNamedString(attribute, "WriteAddress"),
                    GetNamedString(attribute, "Id")),
                new PlcNotificationSpec(
                    GetNamedInt(attribute, "CycleTime", DefaultCycleTime),
                    GetNamedInt(attribute, ArraySizeArgument, -1)),
                GetNamedBool(attribute, "CanWrite", true));
    }

    /// <summary>Creates a structured notification property specification.</summary>
    /// <param name="property">The property symbol.</param>
    /// <param name="attribute">The attribute data.</param>
    /// <returns>The property specification, or <c>null</c> when invalid.</returns>
    private static PlcPropertySpec? GetStructuredProperty(IPropertySymbol property, AttributeData attribute)
    {
        var address = GetConstructorString(attribute, 0);
        var memberAddress = GetConstructorString(attribute, 1) ?? GetNamedString(attribute, "MemberAddress");
        return string.IsNullOrWhiteSpace(address)
            ? null
            : new PlcPropertySpec(
                new PlcPropertyIdentity(
                    property.Name,
                    property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    GetNamedString(attribute, ObservableNameArgument) ?? (property.Name + ObservableSuffix)),
                new PlcAddressSpec(
                    StructuredKind,
                    address!,
                    memberAddress,
                    GetNamedString(attribute, "WriteAddress"),
                    GetNamedString(attribute, "Id")),
                new PlcNotificationSpec(
                    GetNamedInt(attribute, "CycleTime", DefaultCycleTime),
                    GetNamedInt(attribute, ArraySizeArgument, -1)),
                GetNamedBool(attribute, "CanWrite", true));
    }

    /// <summary>Creates a write-only property specification.</summary>
    /// <param name="property">The property symbol.</param>
    /// <param name="attribute">The attribute data.</param>
    /// <returns>The property specification, or <c>null</c> when invalid.</returns>
    private static PlcPropertySpec? GetWriteOnlyProperty(IPropertySymbol property, AttributeData attribute)
    {
        var address = GetConstructorString(attribute, 0);
        return string.IsNullOrWhiteSpace(address)
            ? null
            : new PlcPropertySpec(
                new PlcPropertyIdentity(
                    property.Name,
                    property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    property.Name + ObservableSuffix),
                new PlcAddressSpec(
                    WriteOnlyKind,
                    address!,
                    null,
                    null,
                    GetNamedString(attribute, "Id")),
                new PlcNotificationSpec(
                    DefaultCycleTime,
                    GetNamedInt(attribute, ArraySizeArgument, -1)),
                true);
    }

    /// <summary>Emits generated source for all collected legacy stream specifications.</summary>
    /// <param name="context">The source production context.</param>
    /// <param name="streams">The collected stream specifications.</param>
    private static void ExecuteLegacy(SourceProductionContext context, ImmutableArray<LegacyStreamSpec?> streams)
    {
        var groups = new Dictionary<string, List<LegacyStreamSpec>>();
        foreach (var stream in streams)
        {
            if (stream is null)
            {
                continue;
            }

            var key = stream.Surface + ":" + stream.Namespace + "." + stream.ClassName;
            if (!groups.TryGetValue(key, out var group))
            {
                group = [];
                groups[key] = group;
            }

            group.Add(stream);
        }

        foreach (var group in groups.Values)
        {
            var spec = group[0];
            var properties = new List<LegacyReactivePropertySpec>();
            foreach (var stream in group)
            {
                properties.AddRange(stream.Properties);
            }

            context.AddSource(GetHintName(spec.Namespace, spec.ClassName, spec.Surface + ".TwinCatReactiveStream"), SourceText.From(GenerateLegacy(spec, properties), Encoding.UTF8));
        }
    }

    /// <summary>Emits generated source for all collected PLC connection specifications.</summary>
    /// <param name="context">The source production context.</param>
    /// <param name="connections">The collected connection specifications.</param>
    private static void ExecuteConnections(SourceProductionContext context, ImmutableArray<ConnectionSpec?> connections)
    {
        foreach (var connection in connections)
        {
            if (connection is null)
            {
                continue;
            }

            context.AddSource(GetHintName(connection.Namespace, connection.ClassName, connection.Surface + ".TwinCatPlcConnection"), SourceText.From(GenerateConnection(connection), Encoding.UTF8));
        }
    }

    /// <summary>Generates the legacy reactive stream binding source.</summary>
    /// <param name="spec">The target class specification.</param>
    /// <param name="properties">The reactive properties to generate.</param>
    /// <returns>The generated C# source.</returns>
    private static string GenerateLegacy(LegacyStreamSpec spec, IReadOnlyList<LegacyReactivePropertySpec> properties)
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine("// <auto-generated/>")
            .AppendLine("#nullable enable")
            .AppendLine("using System;")
            .Append(UsingDirectivePrefix).Append("TwinCatRxClientContract = global::").Append(GetLibraryNamespace(spec.Surface)).AppendLine(".IRxTcAdsClient;")
            .Append(UsingDirectivePrefix).Append("TwinCatRxObservableBridge = global::").Append(GetLibraryNamespace(spec.Surface)).AppendLine(".ObservableBridgeExtensions;")
            .Append(UsingDirectivePrefix).Append("TwinCatRxApiExtensions = global::").Append(GetLibraryNamespace(spec.Surface)).AppendLine(".TwinCatRxExtensions;")
            .AppendLine("using ReactiveUI.Primitives.Disposables;")
            .AppendLine();

        AppendNamespace(sb, spec.Namespace);

        _ = sb.Append(spec.Accessibility).Append(" partial class ").AppendLine(spec.ClassName)
            .AppendLine("{");

        foreach (var property in properties)
        {
            _ = sb.Append("    private readonly global::ReactiveUI.Primitives.Signals.BehaviorSignal<").Append(property.TypeName).Append("?> _").Append(ToCamel(property.PropertyName)).AppendLine("Subject = new(default);")
                .Append("    private ").Append(property.TypeName).Append("? _").Append(ToCamel(property.PropertyName)).AppendLine(";")
                .Append("    public ").Append(property.TypeName).Append("? ").AppendLine(property.PropertyName)
                .AppendLine(ClassOpenBrace)
                .Append("        get => _").Append(ToCamel(property.PropertyName)).AppendLine(";")
                .AppendLine("        private set")
                .AppendLine(BlockOpenBrace)
                .Append("            _").Append(ToCamel(property.PropertyName)).AppendLine(" = value;")
                .Append("            _").Append(ToCamel(property.PropertyName)).AppendLine("Subject.OnNext(value);")
                .AppendLine(BlockCloseBrace)
                .AppendLine(ClassCloseBrace)
                .Append("    public IObservable<").Append(property.TypeName).Append("?> ").Append(property.ObservableName).Append(" => _").Append(ToCamel(property.PropertyName)).AppendLine("Subject;")
                .AppendLine();
        }

        _ = sb.AppendLine("    public IDisposable BindTwinCatRx(TwinCatRxClientContract client)")
            .AppendLine(ClassOpenBrace)
            .AppendLine("        if (client == null)")
            .AppendLine(BlockOpenBrace)
            .AppendLine("            throw new ArgumentNullException(nameof(client));")
            .AppendLine(BlockCloseBrace)
            .AppendLine()
            .AppendLine("        var subscriptions = new MultipleDisposable();");

        foreach (var property in properties)
        {
            _ = sb.Append("        subscriptions.Add(TwinCatRxObservableBridge.SubscribeTo(TwinCatRxApiExtensions.Observe<").Append(property.TypeName).Append(">(client, \"").Append(Escape(property.Variable)).Append('"');
            if (property.Id is not null)
            {
                _ = sb.Append(", \"").Append(Escape(property.Id)).Append('"');
            }

            _ = sb.Append("), value => ").Append(property.PropertyName).AppendLine(" = value));");
        }

        _ = sb.AppendLine("        return subscriptions;")
            .AppendLine(ClassCloseBrace)
            .AppendLine("}");

        return sb.ToString();
    }

    /// <summary>Generates the PLC connection binding source.</summary>
    /// <param name="spec">The target class specification.</param>
    /// <returns>The generated C# source.</returns>
    private static string GenerateConnection(ConnectionSpec spec)
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine("// <auto-generated/>")
            .AppendLine("#nullable enable")
            .AppendLine("using System;")
            .AppendLine(Net5OrGreaterDirective)
            .AppendLine("using System.Diagnostics.CodeAnalysis;")
            .AppendLine(EndIfDirective)
            .Append(UsingDirectivePrefix).Append(GetCollectionsNamespace(spec.Surface)).AppendLine(";")
            .Append(UsingDirectivePrefix).Append(GetCoreNamespace(spec.Surface)).AppendLine(";")
            .Append(UsingDirectivePrefix).Append("TwinCatRxHashTable = global::").Append(GetCollectionsNamespace(spec.Surface)).AppendLine(".HashTableRx;")
            .Append(UsingDirectivePrefix).Append("TwinCatRxHashTableExtensions = global::").Append(GetCollectionsNamespace(spec.Surface)).AppendLine(".HashTableRxExtensions;")
            .Append(UsingDirectivePrefix).Append("TwinCatRxClientContract = global::").Append(GetLibraryNamespace(spec.Surface)).AppendLine(".IRxTcAdsClient;")
            .Append(UsingDirectivePrefix).Append("TwinCatRxClient = global::").Append(GetLibraryNamespace(spec.Surface)).AppendLine(".RxTcAdsClient;")
            .Append(UsingDirectivePrefix).Append("TwinCatRxObservableBridge = global::").Append(GetLibraryNamespace(spec.Surface)).AppendLine(".ObservableBridgeExtensions;")
            .Append(UsingDirectivePrefix).Append("TwinCatRxApiExtensions = global::").Append(GetLibraryNamespace(spec.Surface)).AppendLine(".TwinCatRxExtensions;")
            .Append(UsingDirectivePrefix).Append("TwinCatRxSettings = global::").Append(GetCoreNamespace(spec.Surface)).AppendLine(".Settings;")
            .AppendLine("using ReactiveUI.Primitives.Disposables;")
            .AppendLine();

        AppendNamespace(sb, spec.Namespace);

        _ = sb.Append(spec.Accessibility).Append(" partial class ").AppendLine(spec.ClassName)
            .AppendLine("{")
            .AppendLine("    private TwinCatRxClientContract? _twinCatRxClient;")
            .AppendLine("    private readonly System.Collections.Generic.Dictionary<string, TwinCatRxHashTable> _twinCatRxStructures = new(StringComparer.OrdinalIgnoreCase);")
            .AppendLine();

        AppendConnectionFields(sb, spec.Properties);
        AppendConnectionProperties(sb, spec.Properties);
        AppendSettingsFactory(sb, spec);
        AppendConnectMethod(sb);
        AppendBindingMethod(sb, spec.Properties);
        AppendNotificationMethods(sb, spec.Properties);
        AppendWriteMethods(sb, spec.Properties);

        _ = sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>Appends generated signal fields.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="properties">The PLC property specifications.</param>
    private static void AppendConnectionFields(StringBuilder sb, IReadOnlyList<PlcPropertySpec> properties)
    {
        foreach (var property in properties)
        {
            if (property.Kind == WriteOnlyKind)
            {
                continue;
            }

            _ = sb.Append("    private readonly global::ReactiveUI.Primitives.Signals.BehaviorSignal<").Append(property.TypeName).Append("> ").Append(property.SubjectField).AppendLine(" = new(default!);");
        }

        if (!HasNotificationProperties(properties))
        {
            return;
        }

        _ = sb.AppendLine();
    }

    /// <summary>Appends generated client and observable properties.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="properties">The PLC property specifications.</param>
    private static void AppendConnectionProperties(StringBuilder sb, IReadOnlyList<PlcPropertySpec> properties)
    {
        _ = sb.AppendLine("    public TwinCatRxClientContract? TwinCatRxClient => _twinCatRxClient;")
            .AppendLine();

        foreach (var property in properties)
        {
            if (property.Kind == WriteOnlyKind)
            {
                continue;
            }

            _ = sb.Append("    public IObservable<").Append(property.TypeName).Append("> ").Append(property.ObservableName).Append(" => ").Append(property.SubjectField).AppendLine(";")
                .AppendLine();
        }
    }

    /// <summary>Appends the settings factory method.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="spec">The PLC connection specification.</param>
    private static void AppendSettingsFactory(StringBuilder sb, ConnectionSpec spec)
    {
        var notifications = GetNotificationRegistrations(spec.Properties);
        var writeVariables = GetWriteRegistrations(spec.Properties);

        _ = sb.AppendLine("    public TwinCatRxSettings CreateTwinCatRxSettings()")
            .AppendLine(ClassOpenBrace)
            .AppendLine("        var settings = new TwinCatRxSettings")
            .AppendLine(BlockOpenBrace)
            .Append("            AdsAddress = \"").Append(Escape(spec.AdsAddress)).AppendLine("\",")
            .Append("            Port = ").Append(spec.Port.ToString(CultureInfo.InvariantCulture)).AppendLine(",")
            .Append("            SettingsId = \"").Append(Escape(spec.SettingsId)).AppendLine("\"")
            .AppendLine("        };");

        foreach (var notification in notifications)
        {
            _ = sb.Append("        settings.AddNotification(\"").Append(Escape(notification.Variable)).Append("\", cycleTime: ").Append(notification.CycleTime.ToString(CultureInfo.InvariantCulture)).Append(", arraySize: ").Append(notification.ArraySize.ToString(CultureInfo.InvariantCulture)).AppendLine(");");
        }

        foreach (var writeVariable in writeVariables)
        {
            _ = sb.Append("        settings.AddWriteVariable(\"").Append(Escape(writeVariable.Variable)).Append("\", arraySize: ").Append(writeVariable.ArraySize.ToString(CultureInfo.InvariantCulture)).AppendLine(");");
        }

        _ = sb.AppendLine("        return settings;")
            .AppendLine(ClassCloseBrace)
            .AppendLine();
    }

    /// <summary>Appends the owned client connection method.</summary>
    /// <param name="sb">The string builder.</param>
    private static void AppendConnectMethod(StringBuilder sb)
    {
        _ = sb.AppendLine(Net5OrGreaterDirective)
            .AppendLine("    [RequiresDynamicCode(\"RxTcAdsClient generates PLC structure types at runtime.\")]")
            .AppendLine("    [RequiresUnreferencedCode(\"RxTcAdsClient uses reflection to materialize PLC structure types.\")]")
            .AppendLine(EndIfDirective)
            .AppendLine("    public IDisposable ConnectTwinCatRx()")
            .AppendLine(ClassOpenBrace)
            .AppendLine("        var client = new TwinCatRxClient();")
            .AppendLine("        var cleanup = new MultipleDisposable();")
            .AppendLine("        cleanup.Add(client);")
            .AppendLine("        cleanup.Add(BindTwinCatRx(client));")
            .AppendLine("        client.Connect(CreateTwinCatRxSettings());")
            .AppendLine("        return cleanup;")
            .AppendLine(ClassCloseBrace)
            .AppendLine();
    }

    /// <summary>Appends the client binding method.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="properties">The PLC property specifications.</param>
    private static void AppendBindingMethod(StringBuilder sb, IReadOnlyList<PlcPropertySpec> properties)
    {
        _ = sb.AppendLine(Net5OrGreaterDirective)
            .AppendLine("    [RequiresUnreferencedCode(\"Structured notifications use HashTableRx structure materialization.\")]")
            .AppendLine(EndIfDirective)
            .AppendLine("    public IDisposable BindTwinCatRx(TwinCatRxClientContract client)")
            .AppendLine(ClassOpenBrace)
            .AppendLine("        if (client == null)")
            .AppendLine(BlockOpenBrace)
            .AppendLine("            throw new ArgumentNullException(nameof(client));")
            .AppendLine(BlockCloseBrace)
            .AppendLine()
            .AppendLine("        _twinCatRxClient = client;")
            .AppendLine("        _twinCatRxStructures.Clear();")
            .AppendLine("        var subscriptions = new MultipleDisposable();")
            .AppendLine("        subscriptions.Add(Scope.Create(_twinCatRxStructures.Clear));");

        AppendStructuredBindings(sb, properties);
        AppendDirectBindings(sb, properties);

        _ = sb.AppendLine("        return subscriptions;")
            .AppendLine(ClassCloseBrace)
            .AppendLine();

        foreach (var property in properties)
        {
            if (property.Kind == WriteOnlyKind)
            {
                continue;
            }

            _ = sb.Append("    private void ").Append(property.SetterName).Append('(').Append(property.TypeName).AppendLine(" value)")
                .AppendLine(ClassOpenBrace)
                .Append("        ").Append(property.PropertyName).AppendLine(" = value;")
                .Append("        ").Append(property.SubjectField).AppendLine(".OnNext(value);")
                .AppendLine(ClassCloseBrace)
                .AppendLine();
        }
    }

    /// <summary>Appends structured notification subscriptions.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="properties">The PLC property specifications.</param>
    private static void AppendStructuredBindings(StringBuilder sb, IReadOnlyList<PlcPropertySpec> properties)
    {
        var structureVariables = GetStructuredVariables(properties);
        for (var i = 0; i < structureVariables.Count; i++)
        {
            var variable = structureVariables[i];
            var structureName = GetStructureLocalName(i);
            _ = sb.Append("        var ").Append(structureName).Append(" = TwinCatRxApiExtensions.CreateStruct(client, \"").Append(Escape(variable)).AppendLine("\")")
                .Append("            ?? throw new InvalidOperationException(\"The PLC structure '").Append(Escape(variable)).AppendLine("' could not be created.\");")
                .Append("        _twinCatRxStructures[\"").Append(Escape(variable)).Append("\"] = ").Append(structureName).AppendLine(";")
                .Append("        subscriptions.Add(").Append(structureName).AppendLine(");");

            foreach (var property in properties)
            {
                if (property.Kind != StructuredKind || !string.Equals(property.Address, variable, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(property.MemberAddress))
                {
                    continue;
                }

                _ = sb.Append("        subscriptions.Add(TwinCatRxObservableBridge.SubscribeTo(TwinCatRxHashTableExtensions.Observe<").Append(property.TypeName).Append(">(").Append(structureName).Append(", \"").Append(Escape(property.MemberAddress!)).Append("\"), ").Append(property.SetterName).AppendLine("));");
            }
        }
    }

    /// <summary>Appends direct notification subscriptions.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="properties">The PLC property specifications.</param>
    private static void AppendDirectBindings(StringBuilder sb, IReadOnlyList<PlcPropertySpec> properties)
    {
        foreach (var property in properties)
        {
            if (property.Kind == WriteOnlyKind || (property.Kind == StructuredKind && !string.IsNullOrWhiteSpace(property.MemberAddress)))
            {
                continue;
            }

            _ = sb.Append("        subscriptions.Add(TwinCatRxObservableBridge.SubscribeTo(TwinCatRxApiExtensions.Observe<").Append(property.TypeName).Append(">(client, \"").Append(Escape(property.Address)).Append('"');
            if (property.Id is not null)
            {
                _ = sb.Append(", \"").Append(Escape(property.Id)).Append('"');
            }

            _ = sb.Append("), ").Append(property.SetterName).AppendLine("));");
        }
    }

    /// <summary>Appends read methods for notification properties.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="properties">The PLC property specifications.</param>
    private static void AppendNotificationMethods(StringBuilder sb, IReadOnlyList<PlcPropertySpec> properties)
    {
        foreach (var property in properties)
        {
            if (property.Kind == WriteOnlyKind || property.Kind == StructuredKind)
            {
                continue;
            }

            _ = sb.Append("    public void ").Append(property.ReadMethodName).AppendLine("()")
                .AppendLine(ClassOpenBrace)
                .AppendLine("        var client = RequireTwinCatRxClient();")
                .Append("        client.Read(\"").Append(Escape(property.Address)).Append('"');

            if (property.ArraySize > 0)
            {
                _ = sb.Append(", arrayLength: ").Append(property.ArraySize.ToString(CultureInfo.InvariantCulture));
            }

            if (property.Id is not null)
            {
                _ = sb.Append(IdArgumentPrefix).Append(Escape(property.Id)).Append('"');
            }

            _ = sb.AppendLine(");")
                .AppendLine(ClassCloseBrace)
                .AppendLine();
        }
    }

    /// <summary>Appends write methods for write-capable properties.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="properties">The PLC property specifications.</param>
    private static void AppendWriteMethods(StringBuilder sb, IReadOnlyList<PlcPropertySpec> properties)
    {
        var writeProperties = GetWriteProperties(properties);
        var structuredVariables = GetStructuredVariables(properties);
        var structuredWriteProperties = GetStructuredWriteProperties(writeProperties, structuredVariables);
        foreach (var property in writeProperties)
        {
            AppendRequiresUnreferencedCodeAttribute(sb);
            _ = sb.Append("    public void ").Append(property.WriteMethodName).Append('(').Append(property.TypeName).AppendLine(" value)")
                .AppendLine(ClassOpenBrace)
                .Append("        WriteTwinCatRx((nameof(").Append(property.PropertyName).AppendLine("), value));")
                .AppendLine(ClassCloseBrace)
                .AppendLine();
        }

        AppendRequiresUnreferencedCodeAttribute(sb);
        _ = sb.AppendLine("    public void WriteTwinCatRx(params (string Tag, object? Value)[] values)")
            .AppendLine(ClassOpenBrace)
            .AppendLine("        if (values == null)")
            .AppendLine(BlockOpenBrace)
            .AppendLine("            throw new ArgumentNullException(nameof(values));")
            .AppendLine(BlockCloseBrace)
            .AppendLine()
            .AppendLine("        var client = RequireTwinCatRxClient();")
            .AppendLine("        var structuredWrites = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<(string MemberAddress, string WriteAddress, object Value, string? Id)>>(StringComparer.OrdinalIgnoreCase);")
            .AppendLine("        foreach (var value in values)")
            .AppendLine(BlockOpenBrace)
            .AppendLine("            if (TryAddTwinCatRxStructuredWrite(value.Tag, value.Value, structuredWrites))")
            .AppendLine(NestedBlockOpenBrace)
            .AppendLine("                continue;")
            .AppendLine(NestedBlockCloseBrace)
            .AppendLine()
            .AppendLine("            WriteTwinCatRxValue(client, value.Tag, value.Value);")
            .AppendLine(BlockCloseBrace)
            .AppendLine()
            .AppendLine("        WriteTwinCatRxStructures(client, structuredWrites);")
            .AppendLine(ClassCloseBrace)
            .AppendLine()
            .AppendLine("    private TwinCatRxClientContract RequireTwinCatRxClient() =>")
            .AppendLine("        _twinCatRxClient ?? throw new InvalidOperationException(\"The generated TwinCATRx class is not bound to a PLC client.\");")
            .AppendLine();

        AppendStructuredWriteCollector(sb, structuredWriteProperties);
        AppendStructuredWriteFlusher(sb);

        AppendRequiresUnreferencedCodeAttribute(sb);
        _ = sb.AppendLine("    private void WriteTwinCatRxValue(TwinCatRxClientContract client, string tag, object? value)")
            .AppendLine(ClassOpenBrace)
            .AppendLine("        var checkedValue = value ?? throw new ArgumentNullException(nameof(value), \"TwinCATRx write values cannot be null.\");");

        foreach (var property in writeProperties)
        {
            if (GetStructuredWriteTarget(property, structuredVariables) is null)
            {
                AppendWriteBranch(sb, property);
            }
        }

        _ = sb.AppendLine("        throw new ArgumentOutOfRangeException(nameof(tag), tag, \"Unknown TwinCATRx generated write tag.\");")
            .AppendLine(ClassCloseBrace)
            .AppendLine();

        AppendStructuredWriteHelper(sb);
    }

    /// <summary>Appends one write dispatch branch.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="property">The write-capable property specification.</param>
    private static void AppendWriteBranch(StringBuilder sb, PlcPropertySpec property)
    {
        var writeAddress = GetWriteAddress(property);
        _ = sb.Append("        if (string.Equals(tag, nameof(").Append(property.PropertyName).Append("), StringComparison.OrdinalIgnoreCase)")
            .Append(OrdinalTagComparisonPrefix).Append(Escape(writeAddress)).Append(OrdinalTagComparisonSuffix);

        if (property.Kind == StructuredKind && !string.IsNullOrWhiteSpace(property.MemberAddress))
        {
            _ = sb.Append(OrdinalTagComparisonPrefix).Append(Escape(property.MemberAddress!)).Append(OrdinalTagComparisonSuffix);
        }

        _ = sb.AppendLine(")")
            .AppendLine(BlockOpenBrace)
            .Append("            var typedValue = (").Append(property.TypeName).AppendLine(")checkedValue;");

        if (property.Kind == WriteOnlyKind)
        {
            _ = sb.Append("            ").Append(property.PropertyName).AppendLine(" = typedValue;");
        }

        _ = sb.Append("            client.Write(\"").Append(Escape(writeAddress)).Append("\", typedValue");
        if (property.Id is not null)
        {
            _ = sb.Append(", id: \"").Append(Escape(property.Id)).Append('"');
        }

        _ = sb.AppendLine(");")
            .AppendLine("            return;")
            .AppendLine(BlockCloseBrace)
            .AppendLine();
    }

    /// <summary>Appends structured write collection logic.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="properties">The structured write properties.</param>
    private static void AppendStructuredWriteCollector(StringBuilder sb, IReadOnlyList<StructuredWritePropertySpec> properties)
    {
        AppendRequiresUnreferencedCodeAttribute(sb);
        _ = sb.AppendLine("    private bool TryAddTwinCatRxStructuredWrite(string tag, object? value, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<(string MemberAddress, string WriteAddress, object Value, string? Id)>> structuredWrites)")
            .AppendLine(ClassOpenBrace)
            .AppendLine("        var checkedValue = value ?? throw new ArgumentNullException(nameof(value), \"TwinCATRx write values cannot be null.\");");

        foreach (var property in properties)
        {
            AppendStructuredWriteCollectorBranch(sb, property);
        }

        _ = sb.AppendLine("        return false;")
            .AppendLine(ClassCloseBrace)
            .AppendLine();
    }

    /// <summary>Appends one structured write collection branch.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="structuredProperty">The structured write property.</param>
    private static void AppendStructuredWriteCollectorBranch(StringBuilder sb, StructuredWritePropertySpec structuredProperty)
    {
        var property = structuredProperty.Property;
        var structuredTarget = structuredProperty.Target;
        var writeAddress = GetWriteAddress(property);
        _ = sb.Append("        if (string.Equals(tag, nameof(").Append(property.PropertyName).Append("), StringComparison.OrdinalIgnoreCase)")
            .Append(" || string.Equals(tag, \"").Append(Escape(writeAddress)).Append("\", StringComparison.OrdinalIgnoreCase)")
            .Append(" || string.Equals(tag, \"").Append(Escape(structuredTarget.MemberAddress)).Append("\", StringComparison.OrdinalIgnoreCase)")
            .AppendLine(")")
            .AppendLine(BlockOpenBrace)
            .Append("            var typedValue = (").Append(property.TypeName).AppendLine(")checkedValue;");

        if (property.Kind == WriteOnlyKind)
        {
            _ = sb.Append("            ").Append(property.PropertyName).AppendLine(" = typedValue;");
        }

        _ = sb.Append("            AddTwinCatRxStructuredWrite(structuredWrites, \"").Append(Escape(structuredTarget.RootAddress)).Append("\", \"").Append(Escape(structuredTarget.MemberAddress)).Append("\", \"").Append(Escape(writeAddress)).Append("\", typedValue, ");
        AppendNullableStringLiteral(sb, property.Id);
        _ = sb.AppendLine(");")
            .AppendLine("            return true;")
            .AppendLine(BlockCloseBrace)
            .AppendLine();
    }

    /// <summary>Appends structured write flushing logic.</summary>
    /// <param name="sb">The string builder.</param>
    private static void AppendStructuredWriteFlusher(StringBuilder sb)
    {
        AppendRequiresUnreferencedCodeAttribute(sb);
        _ = sb.AppendLine("    private void WriteTwinCatRxStructures(TwinCatRxClientContract client, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<(string MemberAddress, string WriteAddress, object Value, string? Id)>> structuredWrites)")
            .AppendLine(ClassOpenBrace)
            .AppendLine("        foreach (var structuredWrite in structuredWrites)")
            .AppendLine(BlockOpenBrace)
            .AppendLine("            if (TryWriteTwinCatRxStructure(structuredWrite.Key, structuredWrite.Value))")
            .AppendLine(NestedBlockOpenBrace)
            .AppendLine("                continue;")
            .AppendLine(NestedBlockCloseBrace)
            .AppendLine()
            .AppendLine("            foreach (var value in structuredWrite.Value)")
            .AppendLine(NestedBlockOpenBrace)
            .AppendLine("                client.Write(value.WriteAddress, value.Value, id: value.Id);")
            .AppendLine(NestedBlockCloseBrace)
            .AppendLine(BlockCloseBrace)
            .AppendLine(ClassCloseBrace)
            .AppendLine();
    }

    /// <summary>Appends the structured bulk write helper.</summary>
    /// <param name="sb">The string builder.</param>
    private static void AppendStructuredWriteHelper(StringBuilder sb)
    {
        AppendRequiresUnreferencedCodeAttribute(sb);
        _ = sb.AppendLine("    private static void AddTwinCatRxStructuredWrite(System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<(string MemberAddress, string WriteAddress, object Value, string? Id)>> structuredWrites, string variable, string memberAddress, string writeAddress, object value, string? id)")
            .AppendLine(ClassOpenBrace)
            .AppendLine("        if (!structuredWrites.TryGetValue(variable, out var values))")
            .AppendLine(BlockOpenBrace)
            .AppendLine("            values = [];")
            .AppendLine("            structuredWrites[variable] = values;")
            .AppendLine(BlockCloseBrace)
            .AppendLine()
            .AppendLine("        values.Add((memberAddress, writeAddress, value, id));")
            .AppendLine(ClassCloseBrace)
            .AppendLine()
            .AppendLine("    private bool TryWriteTwinCatRxStructure(string variable, System.Collections.Generic.IReadOnlyList<(string MemberAddress, string WriteAddress, object Value, string? Id)> values)")
            .AppendLine(ClassOpenBrace)
            .AppendLine("        if (!_twinCatRxStructures.TryGetValue(variable, out var structure))")
            .AppendLine(BlockOpenBrace)
            .AppendLine(IndentedReturnFalse)
            .AppendLine(BlockCloseBrace)
            .AppendLine()
            .AppendLine("        try")
            .AppendLine(BlockOpenBrace)
            .AppendLine("        using var clone = TwinCatRxApiExtensions.CreateClone(structure);")
            .AppendLine("        for (var i = 0; i < values.Count; i++)")
            .AppendLine(BlockOpenBrace)
            .AppendLine("            var value = values[i];")
            .AppendLine("            TwinCatRxHashTableExtensions.Value(clone, value.MemberAddress, value.Value);")
            .AppendLine(BlockCloseBrace)
            .AppendLine("        var structuredValue = clone.Structure;")
            .AppendLine("        if (structuredValue is null)")
            .AppendLine(BlockOpenBrace)
            .AppendLine(IndentedReturnFalse)
            .AppendLine(BlockCloseBrace)
            .AppendLine()
            .AppendLine("        RequireTwinCatRxClient().Write(variable, structuredValue);")
            .AppendLine("        return true;")
            .AppendLine(BlockCloseBrace)
            .AppendLine("        catch (Exception)")
            .AppendLine(BlockOpenBrace)
            .AppendLine(IndentedReturnFalse)
            .AppendLine(BlockCloseBrace)
            .AppendLine(ClassCloseBrace)
            .AppendLine();
    }

    /// <summary>Appends the generated trimming annotation used by structured HashTableRx writes.</summary>
    /// <param name="sb">The string builder.</param>
    private static void AppendRequiresUnreferencedCodeAttribute(StringBuilder sb)
    {
        _ = sb.AppendLine(Net5OrGreaterDirective)
            .AppendLine("    [RequiresUnreferencedCode(\"Structured writes use HashTableRx structure materialization.\")]")
            .AppendLine(EndIfDirective);
    }

    /// <summary>Appends a nullable string literal to generated source.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="value">The string value.</param>
    private static void AppendNullableStringLiteral(StringBuilder sb, string? value)
    {
        if (value is null)
        {
            _ = sb.Append("null");
            return;
        }

        _ = sb.Append('"').Append(Escape(value)).Append('"');
    }

    /// <summary>Gets unique notification registrations.</summary>
    /// <param name="properties">The PLC property specifications.</param>
    /// <returns>The unique notification registrations.</returns>
    private static List<NotificationRegistration> GetNotificationRegistrations(IReadOnlyList<PlcPropertySpec> properties)
    {
        var registrations = new List<NotificationRegistration>();
        foreach (var property in properties)
        {
            if (property.Kind == WriteOnlyKind || ContainsNotification(registrations, property.Address))
            {
                continue;
            }

            registrations.Add(new NotificationRegistration(property.Address, property.CycleTime, property.ArraySize));
        }

        return registrations;
    }

    /// <summary>Gets unique write variable registrations.</summary>
    /// <param name="properties">The PLC property specifications.</param>
    /// <returns>The unique write variable registrations.</returns>
    private static List<WriteRegistration> GetWriteRegistrations(IReadOnlyList<PlcPropertySpec> properties)
    {
        var registrations = new List<WriteRegistration>();
        var structuredVariables = GetStructuredVariables(properties);
        foreach (var property in properties)
        {
            if (!property.IsWritable)
            {
                continue;
            }

            var writeAddress = GetWriteAddress(property);
            var registrationAddress = GetWriteRegistrationAddress(property, structuredVariables);
            AddWriteRegistration(registrations, registrationAddress, property.ArraySize);
            if (!string.Equals(writeAddress, registrationAddress, StringComparison.OrdinalIgnoreCase))
            {
                AddWriteRegistration(registrations, writeAddress, property.ArraySize);
            }
        }

        return registrations;
    }

    /// <summary>Adds a write registration when it does not already exist.</summary>
    /// <param name="registrations">The write registrations.</param>
    /// <param name="writeAddress">The write address.</param>
    /// <param name="arraySize">The array size.</param>
    private static void AddWriteRegistration(List<WriteRegistration> registrations, string writeAddress, int arraySize)
    {
        if (ContainsWriteRegistration(registrations, writeAddress))
        {
            return;
        }

        registrations.Add(new WriteRegistration(writeAddress, arraySize));
    }

    /// <summary>Gets unique structured notification root variables.</summary>
    /// <param name="properties">The PLC property specifications.</param>
    /// <returns>The unique structured notification root variables.</returns>
    private static List<string> GetStructuredVariables(IReadOnlyList<PlcPropertySpec> properties)
    {
        var variables = new List<string>();
        foreach (var property in properties)
        {
            if (property.Kind != StructuredKind || string.IsNullOrWhiteSpace(property.MemberAddress) || ContainsString(variables, property.Address))
            {
                continue;
            }

            variables.Add(property.Address);
        }

        return variables;
    }

    /// <summary>Gets write-capable property specifications.</summary>
    /// <param name="properties">The PLC property specifications.</param>
    /// <returns>The write-capable properties.</returns>
    private static List<PlcPropertySpec> GetWriteProperties(IReadOnlyList<PlcPropertySpec> properties)
    {
        var writableProperties = new List<PlcPropertySpec>();
        foreach (var property in properties)
        {
            if (property.IsWritable)
            {
                writableProperties.Add(property);
            }
        }

        return writableProperties;
    }

    /// <summary>Gets write-capable structured property specifications.</summary>
    /// <param name="properties">The write-capable PLC property specifications.</param>
    /// <param name="structuredVariables">The structured root variables.</param>
    /// <returns>The structured write properties.</returns>
    private static List<StructuredWritePropertySpec> GetStructuredWriteProperties(IReadOnlyList<PlcPropertySpec> properties, IReadOnlyList<string> structuredVariables)
    {
        var structuredWriteProperties = new List<StructuredWritePropertySpec>();
        foreach (var property in properties)
        {
            var target = GetStructuredWriteTarget(property, structuredVariables);
            if (target is not null)
            {
                structuredWriteProperties.Add(new StructuredWritePropertySpec(property, target));
            }
        }

        return structuredWriteProperties;
    }

    /// <summary>Determines whether a notification registration already exists.</summary>
    /// <param name="registrations">The existing registrations.</param>
    /// <param name="variable">The notification variable.</param>
    /// <returns><c>true</c> when the registration exists.</returns>
    private static bool ContainsNotification(List<NotificationRegistration> registrations, string variable)
    {
        for (var i = 0; i < registrations.Count; i++)
        {
            if (string.Equals(registrations[i].Variable, variable, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a write registration already exists.</summary>
    /// <param name="registrations">The existing registrations.</param>
    /// <param name="variable">The write variable.</param>
    /// <returns><c>true</c> when the registration exists.</returns>
    private static bool ContainsWriteRegistration(List<WriteRegistration> registrations, string variable)
    {
        for (var i = 0; i < registrations.Count; i++)
        {
            if (string.Equals(registrations[i].Variable, variable, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a string already exists.</summary>
    /// <param name="values">The existing values.</param>
    /// <param name="value">The value to find.</param>
    /// <returns><c>true</c> when the value exists.</returns>
    private static bool ContainsString(List<string> values, string value)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (string.Equals(values[i], value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Gets the PLC write address for a property.</summary>
    /// <param name="property">The PLC property specification.</param>
    /// <returns>The PLC write address.</returns>
    private static string GetWriteAddress(PlcPropertySpec property) =>
        property switch
        {
            { WriteAddress: { } writeAddress } when !string.IsNullOrWhiteSpace(writeAddress) => writeAddress,
            { Kind: StructuredKind, MemberAddress: { } memberAddress } when !string.IsNullOrWhiteSpace(memberAddress) => CombineAddress(property.Address, memberAddress),
            _ => property.Address,
        };

    /// <summary>Gets the settings write registration address for a property.</summary>
    /// <param name="property">The PLC property specification.</param>
    /// <param name="structuredVariables">The structured root variables.</param>
    /// <returns>The write registration address.</returns>
    private static string GetWriteRegistrationAddress(PlcPropertySpec property, IReadOnlyList<string> structuredVariables)
    {
        var structuredTarget = GetStructuredWriteTarget(property, structuredVariables);
        return structuredTarget?.RootAddress ?? GetWriteAddress(property);
    }

    /// <summary>Gets the structured write target for a property.</summary>
    /// <param name="property">The PLC property specification.</param>
    /// <param name="structuredVariables">The structured root variables.</param>
    /// <returns>The structured write target, or <c>null</c> when the property is not structure-backed.</returns>
    private static StructuredWriteTarget? GetStructuredWriteTarget(PlcPropertySpec property, IReadOnlyList<string> structuredVariables)
    {
        if (property.Kind == StructuredKind && !string.IsNullOrWhiteSpace(property.MemberAddress))
        {
            return new StructuredWriteTarget(property.Address, property.MemberAddress!);
        }

        if (property.Kind != WriteOnlyKind)
        {
            return null;
        }

        for (var i = 0; i < structuredVariables.Count; i++)
        {
            var root = structuredVariables[i];
            var prefix = root.EndsWith(".", StringComparison.Ordinal) ? root : root + ".";
            if (property.Address.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return new StructuredWriteTarget(root, property.Address.Substring(prefix.Length));
            }
        }

        return null;
    }

    /// <summary>Combines a structured root and member address.</summary>
    /// <param name="root">The structured root address.</param>
    /// <param name="member">The member address.</param>
    /// <returns>The combined address.</returns>
    private static string CombineAddress(string root, string member) =>
        root.EndsWith(".", StringComparison.Ordinal) || member.StartsWith(".", StringComparison.Ordinal)
            ? root + member
            : root + "." + member;

    /// <summary>Gets whether properties include any notification tags.</summary>
    /// <param name="properties">The PLC property specifications.</param>
    /// <returns><c>true</c> when at least one notification property exists.</returns>
    private static bool HasNotificationProperties(IReadOnlyList<PlcPropertySpec> properties)
    {
        foreach (var property in properties)
        {
            if (property.Kind != WriteOnlyKind)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Appends a file-scoped namespace when present.</summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="ns">The namespace.</param>
    private static void AppendNamespace(StringBuilder sb, string? ns)
    {
        if (ns is null)
        {
            return;
        }

        _ = sb.Append("namespace ").Append(ns).AppendLine(";")
            .AppendLine();
    }

    /// <summary>Gets a source hint name.</summary>
    /// <param name="ns">The namespace.</param>
    /// <param name="className">The class name.</param>
    /// <param name="suffix">The generated file suffix.</param>
    /// <returns>The source hint name.</returns>
    private static string GetHintName(string? ns, string className, string suffix) =>
        string.IsNullOrWhiteSpace(ns)
            ? className + "." + suffix + ".g.cs"
            : ns + "." + className + "." + suffix + ".g.cs";

    /// <summary>Gets the namespace for a named type.</summary>
    /// <param name="symbol">The named type symbol.</param>
    /// <returns>The namespace, or <c>null</c> for the global namespace.</returns>
    private static string? GetNamespace(INamedTypeSymbol symbol) =>
        symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();

    /// <summary>Gets the API surface represented by an attribute.</summary>
    /// <param name="attribute">The attribute to inspect.</param>
    /// <returns>The selected API surface.</returns>
    private static ApiSurface GetApiSurface(AttributeData attribute) =>
        attribute.AttributeClass?.ToDisplayString().StartsWith(ReactiveLibraryNamespace + ".", StringComparison.Ordinal) == true
            ? ApiSurface.Reactive
            : ApiSurface.Lean;

    /// <summary>Gets the library namespace for an API surface.</summary>
    /// <param name="surface">The API surface.</param>
    /// <returns>The library namespace.</returns>
    private static string GetLibraryNamespace(ApiSurface surface) =>
        surface == ApiSurface.Reactive ? ReactiveLibraryNamespace : LeanLibraryNamespace;

    /// <summary>Gets the core namespace for an API surface.</summary>
    /// <param name="surface">The API surface.</param>
    /// <returns>The core namespace.</returns>
    private static string GetCoreNamespace(ApiSurface surface) =>
        surface == ApiSurface.Reactive ? ReactiveCoreNamespace : LeanCoreNamespace;

    /// <summary>Gets the collections namespace for an API surface.</summary>
    /// <param name="surface">The API surface.</param>
    /// <returns>The collections namespace.</returns>
    private static string GetCollectionsNamespace(ApiSurface surface) =>
        surface == ApiSurface.Reactive ? ReactiveCollectionsNamespace : LeanCollectionsNamespace;

    /// <summary>Gets a constructor string argument.</summary>
    /// <param name="attribute">The attribute to inspect.</param>
    /// <param name="index">The constructor argument index.</param>
    /// <returns>The constructor string value, or <c>null</c> when unavailable.</returns>
    private static string? GetConstructorString(AttributeData attribute, int index) =>
        attribute.ConstructorArguments.Length > index ? attribute.ConstructorArguments[index].Value as string : null;

    /// <summary>Gets a named string argument value from an attribute.</summary>
    /// <param name="attribute">The attribute to inspect.</param>
    /// <param name="name">The named argument name.</param>
    /// <returns>The named string value, or <c>null</c> when it is not present.</returns>
    private static string? GetNamedString(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name)
            {
                return argument.Value.Value as string;
            }
        }

        return null;
    }

    /// <summary>Gets a named integer argument value from an attribute.</summary>
    /// <param name="attribute">The attribute to inspect.</param>
    /// <param name="name">The named argument name.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>The named integer value, or the default value.</returns>
    private static int GetNamedInt(AttributeData attribute, string name, int defaultValue)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is int value)
            {
                return value;
            }
        }

        return defaultValue;
    }

    /// <summary>Gets a named Boolean argument value from an attribute.</summary>
    /// <param name="attribute">The attribute to inspect.</param>
    /// <param name="name">The named argument name.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>The named Boolean value, or the default value.</returns>
    private static bool GetNamedBool(AttributeData attribute, string name, bool defaultValue)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is bool value)
            {
                return value;
            }
        }

        return defaultValue;
    }

    /// <summary>Converts Roslyn accessibility to generated C# accessibility text.</summary>
    /// <param name="symbol">The target class symbol.</param>
    /// <returns>The generated accessibility text.</returns>
    private static string GetAccessibility(INamedTypeSymbol symbol) =>
        symbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            _ => "internal",
        };

    /// <summary>Creates a legal C# identifier from a PLC variable name.</summary>
    /// <param name="variable">The PLC variable name.</param>
    /// <returns>The sanitized identifier.</returns>
    private static string SanitizeIdentifier(string variable)
    {
        var builder = new StringBuilder(variable.Length);
        foreach (var character in variable)
        {
            if (char.IsLetterOrDigit(character))
            {
                _ = builder.Append(character);
            }
        }

        var text = builder.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Value";
        }

        return char.IsDigit(text[0]) ? "Value" + text : text;
    }

    /// <summary>Converts a PascalCase identifier to camelCase.</summary>
    /// <param name="value">The identifier to convert.</param>
    /// <returns>The camelCase identifier.</returns>
    private static string ToCamel(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "value";
        }

        var characters = value.ToCharArray();
        characters[0] = char.ToLowerInvariant(characters[0]);
        return new string(characters);
    }

    /// <summary>Gets a local variable name for a structured notification root.</summary>
    /// <param name="index">The structured notification index.</param>
    /// <returns>The local variable name.</returns>
    private static string GetStructureLocalName(int index) =>
        "structure" + index.ToString(CultureInfo.InvariantCulture);

    /// <summary>Escapes a string for inclusion in generated C# source.</summary>
    /// <param name="value">The string value to escape.</param>
    /// <returns>The escaped string.</returns>
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>Describes an attributed legacy stream class and its reactive properties.</summary>
    private sealed class LegacyStreamSpec
    {
        /// <summary>Initializes a new instance of the <see cref="LegacyStreamSpec"/> class.</summary>
        /// <param name="ns">The containing namespace.</param>
        /// <param name="className">The class name.</param>
        /// <param name="accessibility">The class accessibility.</param>
        /// <param name="surface">The generated API surface.</param>
        /// <param name="properties">The reactive properties.</param>
        public LegacyStreamSpec(string? ns, string className, string accessibility, ApiSurface surface, IReadOnlyList<LegacyReactivePropertySpec> properties)
        {
            Namespace = ns;
            ClassName = className;
            Accessibility = accessibility;
            Surface = surface;
            Properties = properties;
        }

        /// <summary>Gets the containing namespace.</summary>
        public string? Namespace { get; }

        /// <summary>Gets the class name.</summary>
        public string ClassName { get; }

        /// <summary>Gets the class accessibility.</summary>
        public string Accessibility { get; }

        /// <summary>Gets the generated API surface.</summary>
        public ApiSurface Surface { get; }

        /// <summary>Gets the reactive properties.</summary>
        public IReadOnlyList<LegacyReactivePropertySpec> Properties { get; }
    }

    /// <summary>Describes a generated legacy reactive property.</summary>
    private sealed class LegacyReactivePropertySpec
    {
        /// <summary>Initializes a new instance of the <see cref="LegacyReactivePropertySpec"/> class.</summary>
        /// <param name="variable">The PLC variable name.</param>
        /// <param name="typeName">The generated property type name.</param>
        /// <param name="id">The optional stream identifier.</param>
        /// <param name="propertyName">The generated property name.</param>
        /// <param name="observableName">The generated observable name.</param>
        public LegacyReactivePropertySpec(string variable, string typeName, string? id, string propertyName, string observableName)
        {
            Variable = variable;
            TypeName = typeName;
            Id = id;
            PropertyName = propertyName;
            ObservableName = observableName;
        }

        /// <summary>Gets the PLC variable name.</summary>
        public string Variable { get; }

        /// <summary>Gets the generated property type name.</summary>
        public string TypeName { get; }

        /// <summary>Gets the optional stream identifier.</summary>
        public string? Id { get; }

        /// <summary>Gets the generated property name.</summary>
        public string PropertyName { get; }

        /// <summary>Gets the generated observable name.</summary>
        public string ObservableName { get; }
    }

    /// <summary>Describes a generated PLC connection class.</summary>
    private sealed class ConnectionSpec
    {
        /// <summary>Initializes a new instance of the <see cref="ConnectionSpec"/> class.</summary>
        /// <param name="ns">The containing namespace.</param>
        /// <param name="className">The class name.</param>
        /// <param name="accessibility">The class accessibility.</param>
        /// <param name="adsAddress">The ADS address.</param>
        /// <param name="port">The ADS port.</param>
        /// <param name="settingsId">The settings identifier.</param>
        /// <param name="properties">The PLC properties.</param>
        public ConnectionSpec(string? ns, string className, string accessibility, string adsAddress, int port, string settingsId, IReadOnlyList<PlcPropertySpec> properties)
        {
            Namespace = ns;
            ClassName = className;
            Accessibility = accessibility;
            AdsAddress = adsAddress;
            Port = port;
            SettingsId = settingsId;
            Properties = properties;
        }

        /// <summary>Gets the containing namespace.</summary>
        public string? Namespace { get; }

        /// <summary>Gets the class name.</summary>
        public string ClassName { get; }

        /// <summary>Gets the class accessibility.</summary>
        public string Accessibility { get; }

        /// <summary>Gets the ADS address.</summary>
        public string AdsAddress { get; }

        /// <summary>Gets the ADS port.</summary>
        public int Port { get; }

        /// <summary>Gets the settings identifier.</summary>
        public string SettingsId { get; }

        /// <summary>Gets or sets the generated API surface.</summary>
        public ApiSurface Surface { get; set; }

        /// <summary>Gets the PLC properties.</summary>
        public IReadOnlyList<PlcPropertySpec> Properties { get; }
    }

    /// <summary>Groups the generated property identity.</summary>
    private sealed class PlcPropertyIdentity
    {
        /// <summary>Initializes a new instance of the <see cref="PlcPropertyIdentity"/> class.</summary>
        /// <param name="propertyName">The generated property name.</param>
        /// <param name="typeName">The fully qualified property type name.</param>
        /// <param name="observableName">The generated observable name.</param>
        public PlcPropertyIdentity(string propertyName, string typeName, string observableName)
        {
            PropertyName = propertyName;
            TypeName = typeName;
            ObservableName = observableName;
        }

        /// <summary>Gets the generated property name.</summary>
        public string PropertyName { get; }

        /// <summary>Gets the fully qualified property type name.</summary>
        public string TypeName { get; }

        /// <summary>Gets the generated observable name.</summary>
        public string ObservableName { get; }
    }

    /// <summary>Groups the PLC address metadata for a generated property.</summary>
    private sealed class PlcAddressSpec
    {
        /// <summary>Initializes a new instance of the <see cref="PlcAddressSpec"/> class.</summary>
        /// <param name="kind">The PLC tag kind.</param>
        /// <param name="address">The PLC address.</param>
        /// <param name="memberAddress">The optional structured member address.</param>
        /// <param name="writeAddress">The optional write address.</param>
        /// <param name="id">The optional identifier.</param>
        public PlcAddressSpec(string kind, string address, string? memberAddress, string? writeAddress, string? id)
        {
            Kind = kind;
            Address = address;
            MemberAddress = memberAddress;
            WriteAddress = writeAddress;
            Id = id;
        }

        /// <summary>Gets the PLC tag kind.</summary>
        public string Kind { get; }

        /// <summary>Gets the PLC address.</summary>
        public string Address { get; }

        /// <summary>Gets the optional structured member address.</summary>
        public string? MemberAddress { get; }

        /// <summary>Gets the optional write address.</summary>
        public string? WriteAddress { get; }

        /// <summary>Gets the optional identifier.</summary>
        public string? Id { get; }
    }

    /// <summary>Groups notification timing and array metadata.</summary>
    private sealed class PlcNotificationSpec
    {
        /// <summary>Initializes a new instance of the <see cref="PlcNotificationSpec"/> class.</summary>
        /// <param name="cycleTime">The notification cycle time.</param>
        /// <param name="arraySize">The optional array size.</param>
        public PlcNotificationSpec(int cycleTime, int arraySize)
        {
            CycleTime = cycleTime;
            ArraySize = arraySize;
        }

        /// <summary>Gets the notification cycle time.</summary>
        public int CycleTime { get; }

        /// <summary>Gets the optional array size.</summary>
        public int ArraySize { get; }
    }

    /// <summary>Describes an attributed PLC property.</summary>
    private sealed class PlcPropertySpec
    {
        /// <summary>Initializes a new instance of the <see cref="PlcPropertySpec"/> class.</summary>
        /// <param name="identity">The generated property identity.</param>
        /// <param name="address">The PLC address metadata.</param>
        /// <param name="notification">The PLC notification metadata.</param>
        /// <param name="canWrite">A value indicating whether writes should be generated.</param>
        public PlcPropertySpec(PlcPropertyIdentity identity, PlcAddressSpec address, PlcNotificationSpec notification, bool canWrite)
        {
            PropertyName = identity.PropertyName;
            TypeName = identity.TypeName;
            Kind = address.Kind;
            Address = address.Address;
            MemberAddress = address.MemberAddress;
            WriteAddress = address.WriteAddress;
            Id = address.Id;
            ObservableName = identity.ObservableName;
            CycleTime = notification.CycleTime;
            ArraySize = notification.ArraySize;
            IsWritable = address.Kind == WriteOnlyKind || canWrite;
            SubjectField = "_" + ToCamel(identity.PropertyName) + "Subject";
            SetterName = "Set" + identity.PropertyName;
            ReadMethodName = "Read" + identity.PropertyName;
            WriteMethodName = "Write" + identity.PropertyName;
        }

        /// <summary>Gets the property name.</summary>
        public string PropertyName { get; }

        /// <summary>Gets the property type name.</summary>
        public string TypeName { get; }

        /// <summary>Gets the PLC tag kind.</summary>
        public string Kind { get; }

        /// <summary>Gets the PLC address.</summary>
        public string Address { get; }

        /// <summary>Gets the structured member address.</summary>
        public string? MemberAddress { get; }

        /// <summary>Gets the optional write address.</summary>
        public string? WriteAddress { get; }

        /// <summary>Gets the optional identifier.</summary>
        public string? Id { get; }

        /// <summary>Gets the observable property name.</summary>
        public string ObservableName { get; }

        /// <summary>Gets the notification cycle time.</summary>
        public int CycleTime { get; }

        /// <summary>Gets the array size.</summary>
        public int ArraySize { get; }

        /// <summary>Gets a value indicating whether writes should be generated.</summary>
        public bool IsWritable { get; }

        /// <summary>Gets the generated signal field name.</summary>
        public string SubjectField { get; }

        /// <summary>Gets the generated setter method name.</summary>
        public string SetterName { get; }

        /// <summary>Gets the generated read method name.</summary>
        public string ReadMethodName { get; }

        /// <summary>Gets the generated write method name.</summary>
        public string WriteMethodName { get; }
    }

    /// <summary>Describes a notification registration.</summary>
    private sealed class NotificationRegistration
    {
        /// <summary>Initializes a new instance of the <see cref="NotificationRegistration"/> class.</summary>
        /// <param name="variable">The notification variable.</param>
        /// <param name="cycleTime">The notification cycle time.</param>
        /// <param name="arraySize">The array size.</param>
        public NotificationRegistration(string variable, int cycleTime, int arraySize)
        {
            Variable = variable;
            CycleTime = cycleTime;
            ArraySize = arraySize;
        }

        /// <summary>Gets the notification variable.</summary>
        public string Variable { get; }

        /// <summary>Gets the notification cycle time.</summary>
        public int CycleTime { get; }

        /// <summary>Gets the array size.</summary>
        public int ArraySize { get; }
    }

    /// <summary>Describes a write registration.</summary>
    private sealed class WriteRegistration
    {
        /// <summary>Initializes a new instance of the <see cref="WriteRegistration"/> class.</summary>
        /// <param name="variable">The write variable.</param>
        /// <param name="arraySize">The array size.</param>
        public WriteRegistration(string variable, int arraySize)
        {
            Variable = variable;
            ArraySize = arraySize;
        }

        /// <summary>Gets the write variable.</summary>
        public string Variable { get; }

        /// <summary>Gets the array size.</summary>
        public int ArraySize { get; }
    }

    /// <summary>Describes a write-capable property and its structured target.</summary>
    private sealed class StructuredWritePropertySpec
    {
        /// <summary>Initializes a new instance of the <see cref="StructuredWritePropertySpec"/> class.</summary>
        /// <param name="property">The write-capable property.</param>
        /// <param name="target">The structured write target.</param>
        public StructuredWritePropertySpec(PlcPropertySpec property, StructuredWriteTarget target)
        {
            Property = property;
            Target = target;
        }

        /// <summary>Gets the write-capable property.</summary>
        public PlcPropertySpec Property { get; }

        /// <summary>Gets the structured write target.</summary>
        public StructuredWriteTarget Target { get; }
    }

    /// <summary>Describes a structured root/member write target.</summary>
    private sealed class StructuredWriteTarget
    {
        /// <summary>Initializes a new instance of the <see cref="StructuredWriteTarget"/> class.</summary>
        /// <param name="rootAddress">The structured root address.</param>
        /// <param name="memberAddress">The structured member address.</param>
        public StructuredWriteTarget(string rootAddress, string memberAddress)
        {
            RootAddress = rootAddress;
            MemberAddress = memberAddress;
        }

        /// <summary>Gets the structured root address.</summary>
        public string RootAddress { get; }

        /// <summary>Gets the structured member address.</summary>
        public string MemberAddress { get; }
    }
}
