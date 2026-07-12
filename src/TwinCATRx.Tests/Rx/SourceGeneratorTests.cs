// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace TwinCATRx.Tests.Rx;

/// <summary>Tests source generator bindings.</summary>
public class SourceGeneratorTests
{
    /// <summary>The direct PLC variable emitted by the generator.</summary>
    private const string DirectVariable = ".DirectValue";

    /// <summary>The structured PLC root variable emitted by the generator.</summary>
    private const string StructureVariable = ".Struct";

    /// <summary>The direct notification value used by the tests.</summary>
    private const int DirectValue = 123;

    /// <summary>The structured notification value used by the tests.</summary>
    private const int StructuredValue = 321;

    /// <summary>The writable structured value used by the tests.</summary>
    private const int StructuredWritableValue = 654;

    /// <summary>The value dispatched through generated write methods.</summary>
    private const int WriteValue = 456;

    /// <summary>The value dispatched through generated write-only methods.</summary>
    private const int WriteOnlyValue = 789;

    /// <summary>The TwinCAT 3 ADS port emitted by the generator.</summary>
    private const int TwinCat3Port = 851;

    /// <summary>The expected generated notification count.</summary>
    private const int NotificationCount = 2;

    /// <summary>The expected generated write-variable count.</summary>
    private const int WriteVariableCount = 5;

    /// <summary>The direct notification cycle time emitted by the generator.</summary>
    private const int DirectCycleTime = 50;

    /// <summary>The structured notification cycle time emitted by the generator.</summary>
    private const int StructuredCycleTime = 200;

    /// <summary>The index of the structured writable variable.</summary>
    private const int StructuredWritableIndex = 2;

    /// <summary>The index of the direct write-only variable.</summary>
    private const int WriteOnlyIndex = 3;

    /// <summary>The index of the structured write-only variable.</summary>
    private const int StructuredWriteOnlyIndex = 4;

    /// <summary>The expected number of direct write calls.</summary>
    private const int DirectWriteCount = 2;

    /// <summary>Verifies generated property and observable updates.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Generated_Stream_Updates_Property_And_Observable()
    {
        var data = new[]
        {
            (Variable: ".A", Data: (object?)DirectValue, Id: (string?)null),
        };
        var client = new RxFakeClient(Observable.FromEnumerable(data));
        var generated = new GeneratedStreams();
        var observed = new List<int?>();
        using var observer = generated.AValueObservable.SubscribeTo(observed.Add);

        using var binding = generated.BindTwinCatRx(client);

        await TUnitAssert.That(generated.AValue).IsEqualTo(DirectValue);
        var hasObservedValue = false;
        foreach (var value in observed)
        {
            if (value == DirectValue)
            {
                hasObservedValue = true;
                break;
            }
        }

        await TUnitAssert.That(hasObservedValue).IsTrue();
    }

    /// <summary>Verifies generated PLC settings collate notification and write tags.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Generated_Plc_Settings_Collate_Tags()
    {
        var generated = new GeneratedPlcConnection();

        var settings = generated.CreateTwinCatRxSettings();

        await TUnitAssert.That(settings.AdsAddress).IsEqualTo("1.2.3.4.5.6");
        await TUnitAssert.That(settings.Port).IsEqualTo(TwinCat3Port);
        await TUnitAssert.That(settings.SettingsId).IsEqualTo("GeneratedSettings");
        await TUnitAssert.That(settings.Notifications.Count).IsEqualTo(NotificationCount);
        await TUnitAssert.That(settings.WriteVariables.Count).IsEqualTo(WriteVariableCount);
        await TUnitAssert.That(settings.Notifications[0].Variable).IsEqualTo(DirectVariable);
        await TUnitAssert.That(settings.Notifications[0].UpdateRate).IsEqualTo(DirectCycleTime);
        await TUnitAssert.That(settings.Notifications[1].Variable).IsEqualTo(StructureVariable);
        await TUnitAssert.That(settings.Notifications[1].UpdateRate).IsEqualTo(StructuredCycleTime);
        await TUnitAssert.That(settings.WriteVariables[0].Variable).IsEqualTo(DirectVariable);
        await TUnitAssert.That(settings.WriteVariables[1].Variable).IsEqualTo(StructureVariable);
        await TUnitAssert.That(settings.WriteVariables[StructuredWritableIndex].Variable).IsEqualTo(".Struct.Nested.Writable");
        await TUnitAssert.That(settings.WriteVariables[WriteOnlyIndex].Variable).IsEqualTo(".WriteOnly");
        await TUnitAssert.That(settings.WriteVariables[StructuredWriteOnlyIndex].Variable).IsEqualTo(".Struct.Nested.WriteOnly");
    }

    /// <summary>Verifies generated PLC binding updates direct and structured notification properties.</summary>
    /// <returns>The test task.</returns>
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("Generated structured bindings use HashTableRx structure materialization.")]
#endif
    [Test]
    public async Task Generated_Plc_Binding_Updates_Direct_And_Structured_Properties()
    {
        var data = new Signal<(string Variable, object? Data, string? Id)>();
        var client = new RxFakeClient(data);
        var generated = new GeneratedPlcConnection();
        var directValues = new List<int>();
        var structuredValues = new List<int>();
        using var directSubscription = generated.DirectValueObservable.SubscribeTo(directValues.Add);
        using var structuredSubscription = generated.StructuredValueObservable.SubscribeTo(structuredValues.Add);

        using var binding = generated.BindTwinCatRx(client);
        data.OnNext((DirectVariable, DirectValue, null));
        data.OnNext((StructureVariable, new TestStructure(StructuredValue, StructuredWritableValue, 0), null));

        await TUnitAssert.That(generated.DirectValue).IsEqualTo(DirectValue);
        await TUnitAssert.That(generated.StructuredValue).IsEqualTo(StructuredValue);
        await TUnitAssert.That(generated.StructuredWritableValue).IsEqualTo(StructuredWritableValue);
        await TUnitAssert.That(ContainsValue(directValues, DirectValue)).IsTrue();
        await TUnitAssert.That(ContainsValue(structuredValues, StructuredValue)).IsTrue();
    }

    /// <summary>Verifies generated read helpers are only emitted for direct notification tags.</summary>
    /// <returns>The test task.</returns>
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("Generated structured bindings use HashTableRx structure materialization.")]
#endif
    [Test]
    public async Task Generated_Plc_Reads_Are_Not_Emitted_For_Structured_Or_WriteOnly_Tags()
    {
        var client = new RxFakeClient(Observable.Empty<(string Variable, object? Data, string? Id)>());
        var generated = new GeneratedPlcConnection();

        using var binding = generated.BindTwinCatRx(client);
        generated.ReadDirectValue();

        await TUnitAssert.That(client.ReadCalls.Count).IsEqualTo(1);
        await TUnitAssert.That(client.ReadCalls[0].Variable).IsEqualTo(DirectVariable);
        await TUnitAssert.That(typeof(GeneratedPlcConnection).GetMethod("ReadStructuredValue")).IsNull();
        await TUnitAssert.That(typeof(GeneratedPlcConnection).GetMethod("ReadStructuredWritableValue")).IsNull();
        await TUnitAssert.That(typeof(GeneratedPlcConnection).GetMethod("ReadWriteOnlyValue")).IsNull();
        await TUnitAssert.That(typeof(GeneratedPlcConnection).GetMethod("ReadStructuredWriteOnlyValue")).IsNull();
        await TUnitAssert.That(typeof(GeneratedPlcConnection).GetProperty("WriteOnlyValueObservable")).IsNull();
        await TUnitAssert.That(typeof(GeneratedPlcConnection).GetProperty("StructuredWriteOnlyValueObservable")).IsNull();
    }

    /// <summary>Verifies generated batch writes group structure-backed tag values into one root write.</summary>
    /// <returns>The test task.</returns>
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("Generated structured bindings use HashTableRx structure materialization.")]
#endif
    [Test]
    public async Task Generated_Plc_Write_Function_Groups_Structure_Backed_Tag_Values()
    {
        var data = new Signal<(string Variable, object? Data, string? Id)>();
        var client = new RxFakeClient(data);
        var generated = new GeneratedPlcConnection();

        using var binding = generated.BindTwinCatRx(client);
        data.OnNext((StructureVariable, new TestStructure(StructuredValue, 0, 0), null));
        generated.WriteTwinCatRx(
            (nameof(GeneratedPlcConnection.StructuredWritableValue), WriteValue),
            (nameof(GeneratedPlcConnection.StructuredWriteOnlyValue), WriteOnlyValue));

        await TUnitAssert.That(client.WriteCalls.Count).IsEqualTo(1);
        await TUnitAssert.That(client.WriteCalls[0].Variable).IsEqualTo(StructureVariable);
        await TUnitAssert.That(generated.StructuredWriteOnlyValue).IsEqualTo(WriteOnlyValue);
    }

    /// <summary>Verifies generated batch writes dispatch direct tag values through direct writes.</summary>
    /// <returns>The test task.</returns>
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("Generated structured bindings use HashTableRx structure materialization.")]
#endif
    [Test]
    public async Task Generated_Plc_Write_Function_Dispatches_Direct_Tag_Values()
    {
        var client = new RxFakeClient(Observable.Empty<(string Variable, object? Data, string? Id)>());
        var generated = new GeneratedPlcConnection();

        using var binding = generated.BindTwinCatRx(client);
        generated.WriteTwinCatRx((nameof(GeneratedPlcConnection.DirectValue), WriteValue), (nameof(GeneratedPlcConnection.WriteOnlyValue), WriteOnlyValue));

        await TUnitAssert.That(client.WriteCalls.Count).IsEqualTo(DirectWriteCount);
        await TUnitAssert.That(client.WriteCalls[0].Variable).IsEqualTo(DirectVariable);
        await TUnitAssert.That(client.WriteCalls[0].Value).IsEqualTo(WriteValue);
        await TUnitAssert.That(client.WriteCalls[1].Variable).IsEqualTo(".WriteOnly");
        await TUnitAssert.That(client.WriteCalls[1].Value).IsEqualTo(WriteOnlyValue);
        await TUnitAssert.That(generated.WriteOnlyValue).IsEqualTo(WriteOnlyValue);
    }

    /// <summary>Gets whether the collection contains a value.</summary>
    /// <param name="values">The values to inspect.</param>
    /// <param name="expected">The expected value.</param>
    /// <returns><c>true</c> when the value is present.</returns>
    private static bool ContainsValue(List<int> values, int expected)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] == expected)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Structure payload used by generated structured binding tests.</summary>
    private sealed class TestStructure
    {
        /// <summary>Initializes a new instance of the <see cref="TestStructure"/> class.</summary>
        /// <param name="value">The observed structured value.</param>
        /// <param name="writable">The writable structured value.</param>
        /// <param name="writeOnly">The write-only structured value.</param>
        public TestStructure(int value, int writable, int writeOnly) =>
            Nested = new(value, writable, writeOnly);

        /// <summary>Gets the nested structure.</summary>
        public TestNestedStructure Nested { get; }
    }

    /// <summary>Nested structure payload used by generated structured binding tests.</summary>
    private sealed class TestNestedStructure
    {
        /// <summary>Initializes a new instance of the <see cref="TestNestedStructure"/> class.</summary>
        /// <param name="value">The observed structured value.</param>
        /// <param name="writable">The writable structured value.</param>
        /// <param name="writeOnly">The write-only structured value.</param>
        public TestNestedStructure(int value, int writable, int writeOnly)
        {
            Value = value;
            Writable = writable;
            WriteOnly = writeOnly;
        }

        /// <summary>Gets the observed structured value.</summary>
        public int Value { get; }

        /// <summary>Gets the writable structured value.</summary>
        public int Writable { get; }

        /// <summary>Gets the write-only structured value.</summary>
        public int WriteOnly { get; }
    }
}
