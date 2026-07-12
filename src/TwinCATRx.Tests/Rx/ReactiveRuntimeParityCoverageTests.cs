// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Reflection;
using CP.Collections.Reactive;
using CP.TwinCatRx.Reactive;
using ReactiveUI.Primitives.Disposables;
using ReactiveClient = CP.TwinCatRx.Reactive.RxTcAdsClient;
using ReactiveCoreExtensions = CP.TwinCatRx.Core.Reactive.TwinCatRxExtensions;
using ReactiveExtensions = CP.TwinCatRx.Reactive.TwinCatRxExtensions;
using ReactiveInterface = CP.TwinCatRx.Reactive.IRxTcAdsClient;
using ReactiveSettings = CP.TwinCatRx.Core.Reactive.Settings;

namespace TwinCATRx.Tests.Rx;

/// <summary>Deterministic parity coverage for the System.Reactive runtime surface.</summary>
public class ReactiveRuntimeParityCoverageTests
{
    /// <summary>A representative ADS handle.</summary>
    private const uint Handle = 17;

    /// <summary>A second representative ADS handle.</summary>
    private const uint SecondHandle = 18;

    /// <summary>A third representative ADS handle.</summary>
    private const uint ThirdHandle = 19;

    /// <summary>A representative array size.</summary>
    private const int ArraySize = 4;

    /// <summary>The expected number of registered read-write handles.</summary>
    private const int ExpectedReadWriteHandleCount = 2;

    /// <summary>The first updated test structure value.</summary>
    private const int FirstUpdatedValue = 2;

    /// <summary>The second updated test structure value.</summary>
    private const int SecondUpdatedValue = 3;

    /// <summary>The immediately observed test structure value.</summary>
    private const int ObservedValue = 5;

    /// <summary>A representative PLC scalar variable.</summary>
    private const string ScalarVariable = ".Scalar";

    /// <summary>A representative PLC array variable.</summary>
    private const string ArrayVariable = ".Array";

    /// <summary>A representative PLC write-only variable.</summary>
    private const string WriteVariable = ".Write";

    /// <summary>The private type information field name.</summary>
    private const string TypeInfoFieldName = "_typeInfo";

    /// <summary>Verifies pause guards and local state transitions without starting ADS.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Pause_Uses_Only_Local_State_And_Disconnect_Resets_It()
    {
        using var client = new ReactiveClient();
        var errors = new List<Exception>();
        using var errorSubscription = client.ErrorReceived.Subscribe(new RecordingObserver<Exception>(errors));

        client.Pause(TimeSpan.FromMilliseconds(1));
        await TUnitAssert.That(errors.Count).IsEqualTo(1);
        await TUnitAssert.That(errors[0]).IsTypeOf<ObjectDisposedException>();

        InitializeCleanup(client);
        client.Pause(TimeSpan.Zero);
        await TUnitAssert.That(client.IsPaused).IsFalse();

        client.Pause(TimeSpan.FromHours(1));
        await TUnitAssert.That(client.IsPaused).IsTrue();
        client.Disconnect();

        await TUnitAssert.That(client.IsPaused).IsFalse();
        await TUnitAssert.That(client.Connected).IsFalse();
    }

    /// <summary>Verifies read and write guards and local handle selection without ADS.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Read_And_Write_Select_Local_Handle_Metadata()
    {
        using var client = new ReactiveClient();
        var typeInfo = GetPrivateField<Dictionary<string, Type>>(client, TypeInfoFieldName);

        client.Read(string.Empty);
        client.Read(ScalarVariable);
        client.Write(string.Empty, 1);
        client.Write(ScalarVariable, 1);

        typeInfo[ScalarVariable] = typeof(int);
        client.Read(ScalarVariable);
        client.ReadWriteHandleInfo[ScalarVariable] = Handle;
        client.Read(ScalarVariable, id: "scalar");
        client.Write(ScalarVariable, 1, "scalar");

        typeInfo[ArrayVariable] = typeof(int[]);
        client.ReadWriteHandleInfo[ArrayVariable] = SecondHandle;
        await TUnitAssert.That(() => client.Read(ArrayVariable)).Throws<ArgumentOutOfRangeException>();
        client.Read(ArrayVariable, ArraySize, "array");

        typeInfo[WriteVariable] = typeof(string);
        client.WriteHandleInfo[WriteVariable] = (ThirdHandle, ArraySize);
        client.Read(WriteVariable, id: "write");
        client.Write(WriteVariable, "value", "write");

        await TUnitAssert.That(client.ReadWriteHandleInfo.Count).IsEqualTo(ExpectedReadWriteHandleCount);
        await TUnitAssert.That(client.WriteHandleInfo.Count).IsEqualTo(1);
    }

    /// <summary>Verifies deterministic private type and filename helpers by reflection.</summary>
    /// <returns>The test task.</returns>
    [Test]
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("Exercises the client's private PLC type-resolution helper by reflection.")]
#endif
    public async Task Static_Helper_Methods_Handle_Supported_And_Unsupported_Types()
    {
        var buildName = GetPrivateStaticMethod("BuildDataTypesFileName");
        var resolveType = GetPrivateStaticMethod("TryResolvePlcType");

        var dottedName = (string?)buildName.Invoke(null, [ScalarVariable]);
        var plainName = (string?)buildName.Invoke(null, ["Scalar"]);
        object?[] supportedArguments = ["DINT", null];
        object?[] unsupportedArguments = ["NOT_A_PLC_TYPE", null];
        var supported = (bool)resolveType.Invoke(null, supportedArguments)!;
        var unsupported = (bool)resolveType.Invoke(null, unsupportedArguments)!;

        await TUnitAssert.That(dottedName).IsEqualTo("PLC_Scalar");
        await TUnitAssert.That(plainName).IsEqualTo("PLC_Scalar");
        await TUnitAssert.That(supported).IsTrue();
        await TUnitAssert.That(supportedArguments[1]).IsEqualTo(typeof(int));
        await TUnitAssert.That(unsupported).IsFalse();
        await TUnitAssert.That(unsupportedArguments[1]).IsNull();
    }

    /// <summary>Verifies configured notification lengths are used for array reads.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Read_Uses_Configured_Notification_Array_Length()
    {
        using var client = new ReactiveClient();
        var settings = new ReactiveSettings();
        ReactiveCoreExtensions.AddNotification(settings, ArrayVariable, arraySize: ArraySize);
        SetProperty(client, nameof(ReactiveClient.Settings), settings);
        GetPrivateField<Dictionary<string, Type>>(client, TypeInfoFieldName)[ArrayVariable] = typeof(int[]);
        client.ReadWriteHandleInfo[ArrayVariable] = Handle;

        client.Read(ArrayVariable);

        await TUnitAssert.That(client.Settings).IsSameReferenceAs(settings);
    }

    /// <summary>Verifies successful Reactive HashTable writes only queue local values.</summary>
    /// <returns>The test task.</returns>
    [Test]
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("HashTableRx structure APIs reflect over test data.")]
#endif
    public async Task HashTable_Write_Helpers_Exercise_Local_Connected_Paths()
    {
        using var client = new ReactiveClient();
        InitializeCleanup(client);
        SetProperty(client, nameof(ReactiveClient.Connected), true);
        var table = CreateTaggedTable(client, ScalarVariable);

        var syncResult = ReactiveExtensions.WriteValues(table, clone => clone.SetStructure(new TestStructure { Value = FirstUpdatedValue }));
        var asyncResult = await ReactiveExtensions.WriteValuesAsync(table, clone => clone.SetStructure(new TestStructure { Value = SecondUpdatedValue }), TimeSpan.FromHours(1));
        table.Dispose();

        await TUnitAssert.That(syncResult).IsTrue();
        await TUnitAssert.That(asyncResult).IsTrue();
        await TUnitAssert.That(client.IsPaused).IsTrue();
    }

    /// <summary>Verifies a paused async write resumes on a local disconnect signal.</summary>
    /// <returns>The test task.</returns>
    [Test]
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("HashTableRx structure APIs reflect over test data.")]
#endif
    public async Task HashTable_Async_Write_Waits_For_Local_Pause_Reset()
    {
        using var client = new ReactiveClient();
        InitializeCleanup(client);
        SetProperty(client, nameof(ReactiveClient.Connected), true);
        client.Pause(TimeSpan.FromHours(1));
        var table = CreateTaggedTable(client, ScalarVariable);

        var writeTask = ReactiveExtensions.WriteValuesAsync(table, _ => { }, TimeSpan.Zero);
        await Task.Yield();
        client.Disconnect();
        var result = await writeTask;
        table.Dispose();

        await TUnitAssert.That(result).IsTrue();
        await TUnitAssert.That(client.IsPaused).IsFalse();
    }

    /// <summary>Verifies Reactive HashTable guards, cloning, and immediate population.</summary>
    /// <returns>The test task.</returns>
    [Test]
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("HashTableRx structure APIs reflect over test data.")]
#endif
    public async Task HashTable_Helpers_Cover_Guards_Clone_And_Immediate_Data()
    {
        using var table = new HashTableRx(false);
        using var fake = new ReactiveRxFakeClient(System.Reactive.Linq.Observable.Return((ScalarVariable, (object?)new TestStructure { Value = ObservedValue }, (string?)null)));

        var populated = ReactiveExtensions.CreateStruct(fake, ScalarVariable);
        var nullClientResult = ReactiveExtensions.CreateStruct((ReactiveInterface)null!, ScalarVariable);
        using var emptyClone = ReactiveExtensions.CreateClone(table);

        await TUnitAssert.That(ReactiveExtensions.WriteValues((HashTableRx)null!, _ => { })).IsFalse();
        await TUnitAssert.That(ReactiveExtensions.WriteValues(table, null!)).IsFalse();
        await TUnitAssert.That(ReactiveExtensions.WriteValues(table, _ => { })).IsFalse();
        await TUnitAssert.That(await ReactiveExtensions.WriteValuesAsync((HashTableRx)null!, _ => { }, TimeSpan.Zero)).IsFalse();
        await TUnitAssert.That(await ReactiveExtensions.WriteValuesAsync(table, null!, TimeSpan.Zero)).IsFalse();
        await TUnitAssert.That(await ReactiveExtensions.WriteValuesAsync(table, _ => { }, TimeSpan.Zero)).IsFalse();
        await TUnitAssert.That(() => ReactiveExtensions.CreateClone((HashTableRx)null!)).Throws<ArgumentNullException>();
        await TUnitAssert.That(() => ReactiveExtensions.StructureReady((HashTableRx)null!)).Throws<ArgumentNullException>();
        await TUnitAssert.That(emptyClone.Count).IsEqualTo(0);
        await TUnitAssert.That(nullClientResult).IsNull();
        await TUnitAssert.That(populated).IsNotNull();
        await TUnitAssert.That(populated!.Count).IsGreaterThan(0);

        using var populatedClone = ReactiveExtensions.CreateClone(populated);
        await TUnitAssert.That(populatedClone.Count).IsEqualTo(populated.Count);
        populated.Dispose();
    }

    /// <summary>Verifies initialized disposal clears local Reactive client state.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Dispose_Clears_Local_State_And_Is_Idempotent()
    {
        var client = new ReactiveClient();
        InitializeCleanup(client);
        client.ReadWriteHandleInfo[ScalarVariable] = Handle;
        client.WriteHandleInfo[WriteVariable] = (Handle, ArraySize);
        GetPrivateField<Dictionary<string, Type>>(client, TypeInfoFieldName)[ScalarVariable] = typeof(int);

        client.Dispose();
        client.Dispose();

        await TUnitAssert.That(client.IsDisposed).IsTrue();
        await TUnitAssert.That(client.ReadWriteHandleInfo).IsEmpty();
        await TUnitAssert.That(client.WriteHandleInfo).IsEmpty();
        await TUnitAssert.That(GetPrivateField<Dictionary<string, Type>>(client, TypeInfoFieldName)).IsEmpty();
    }

    /// <summary>Creates a populated Reactive table with tags expected by write helpers.</summary>
    /// <param name="client">The disconnected local client.</param>
    /// <param name="variable">The variable name.</param>
    /// <returns>The tagged table.</returns>
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("HashTableRx structure APIs reflect over test data.")]
#endif
    private static HashTableRx CreateTaggedTable(ReactiveClient client, string variable)
    {
        var table = new HashTableRx(false);
        table.SetStructure(new TestStructure { Value = 1 });
        table.Tag![nameof(CP.TwinCatRx.Reactive.RxTcAdsClient)] = client;
        table.Tag["Variable"] = variable;
        return table;
    }

    /// <summary>Gets a private instance field.</summary>
    /// <typeparam name="T">The field type.</typeparam>
    /// <param name="instance">The containing instance.</param>
    /// <param name="name">The field name.</param>
    /// <returns>The field value.</returns>
    private static T GetPrivateField<T>(object instance, string name) =>
        (T)(instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance)
            ?? throw new InvalidOperationException("Private field was not found: " + name));

    /// <summary>Gets a private static method.</summary>
    /// <param name="name">The method name.</param>
    /// <returns>The method.</returns>
    private static MethodInfo GetPrivateStaticMethod(string name) =>
        typeof(ReactiveClient).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Private method was not found: " + name);

    /// <summary>Initializes local disposable state without creating an ADS client.</summary>
    /// <param name="client">The client.</param>
    private static void InitializeCleanup(ReactiveClient client) =>
        typeof(ReactiveClient).GetField("_cleanup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(client, new MultipleDisposable());

    /// <summary>Sets a property, including non-public setters, for local state setup.</summary>
    /// <param name="instance">The containing instance.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The value.</param>
    private static void SetProperty(object instance, string propertyName, object value) =>
        instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!.SetValue(instance, value);

    /// <summary>Simple reflected structure used by Reactive HashTableRx.</summary>
    private sealed class TestStructure
    {
        /// <summary>Gets or sets the test value.</summary>
        public int Value { get; set; }
    }

    /// <summary>Records synchronous observable values.</summary>
    /// <typeparam name="T">The observed type.</typeparam>
    /// <param name="values">The destination list.</param>
    private sealed class RecordingObserver<T>(List<T> values) : IObserver<T>
    {
        /// <inheritdoc/>
        public void OnCompleted()
        {
        }

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
        }

        /// <inheritdoc/>
        public void OnNext(T value) => values.Add(value);
    }
}
