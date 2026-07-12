// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Reflection;
using CP.Collections;
using CP.TwinCatRx;
using CP.TwinCatRx.Core;
using ReactiveUI.Primitives.Disposables;
using LeanTwinCatRxExtensions = CP.TwinCatRx.TwinCatRxExtensions;

namespace TwinCATRx.Tests.Rx;

/// <summary>Deterministic coverage tests for disconnected lean client behavior.</summary>
public class LeanRxClientCoverageTests
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

    /// <summary>Verifies observable surfaces and case-insensitive handle maps without connecting.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Default_Client_Exposes_Disconnected_Observable_Surface()
    {
        using var client = new RxTcAdsClient();
        client.ReadWriteHandleInfo[ScalarVariable] = Handle;
        client.WriteHandleInfo[WriteVariable] = (Handle, ArraySize);

        await TUnitAssert.That(client.Code).IsNotNull();
        await TUnitAssert.That(client.InitializeComplete).IsNotNull();
        await TUnitAssert.That(client.InitializeCompleteAsync).IsNotNull();
        await TUnitAssert.That(client.DataReceived).IsNotNull();
        await TUnitAssert.That(client.DataReceivedAsync).IsNotNull();
        await TUnitAssert.That(client.ErrorReceived).IsNotNull();
        await TUnitAssert.That(client.ErrorReceivedAsync).IsNotNull();
        await TUnitAssert.That(client.OnWrite).IsNotNull();
        await TUnitAssert.That(client.OnWriteAsync).IsNotNull();
        await TUnitAssert.That(client.IsPausedObservable).IsNotNull();
        await TUnitAssert.That(client.IsPausedObservableAsync).IsNotNull();
        await TUnitAssert.That(client.ReadWriteHandleInfo.ContainsKey(ScalarVariable.ToLowerInvariant())).IsTrue();
        await TUnitAssert.That(client.WriteHandleInfo.ContainsKey(WriteVariable.ToUpperInvariant())).IsTrue();
        await TUnitAssert.That(client.Settings).IsNull();
        await TUnitAssert.That(client.IsPaused).IsFalse();
    }

    /// <summary>Verifies pause guards and state transitions without starting ADS.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Pause_Uses_Only_Local_State_And_Disconnect_Resets_It()
    {
        using var client = new RxTcAdsClient();
        var errors = new List<Exception>();
        using var errorSubscription = client.ErrorReceived.SubscribeTo(errors.Add);

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

    /// <summary>Verifies read and write guards and local handle selection without an ADS subscription.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Read_And_Write_Select_Local_Handle_Metadata()
    {
        using var client = new RxTcAdsClient();
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

    /// <summary>Verifies private deterministic type and filename helpers by reflection.</summary>
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
        using var client = new RxTcAdsClient();
        var settings = new Settings();
        settings.AddNotification(ArrayVariable, arraySize: ArraySize);
        SetProperty(client, nameof(RxTcAdsClient.Settings), settings);
        GetPrivateField<Dictionary<string, Type>>(client, TypeInfoFieldName)[ArrayVariable] = typeof(int[]);
        client.ReadWriteHandleInfo[ArrayVariable] = Handle;

        client.Read(ArrayVariable);

        await TUnitAssert.That(client.Settings).IsSameReferenceAs(settings);
    }

    /// <summary>Verifies successful HashTableRx write paths only queue local writes.</summary>
    /// <returns>The test task.</returns>
    [Test]
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("HashTableRx structure APIs reflect over test data.")]
#endif
    public async Task HashTable_Write_Helpers_Exercise_Local_Connected_Paths()
    {
        using var client = new RxTcAdsClient();
        InitializeCleanup(client);
        SetProperty(client, nameof(RxTcAdsClient.Connected), true);
        using var table = CreateTaggedTable(client, ScalarVariable);

        var syncResult = table.WriteValues(clone => clone.SetStructure(new TestStructure { Value = FirstUpdatedValue }));
        var asyncResult = await table.WriteValuesAsync(clone => clone.SetStructure(new TestStructure { Value = SecondUpdatedValue }), TimeSpan.FromHours(1));

        await TUnitAssert.That(syncResult).IsTrue();
        await TUnitAssert.That(asyncResult).IsTrue();
        await TUnitAssert.That(client.IsPaused).IsTrue();
    }

    /// <summary>Verifies the paused HashTableRx write path resumes on a local disconnect signal.</summary>
    /// <returns>The test task.</returns>
    [Test]
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("HashTableRx structure APIs reflect over test data.")]
#endif
    public async Task HashTable_Async_Write_Waits_For_Local_Pause_Reset()
    {
        using var client = new RxTcAdsClient();
        InitializeCleanup(client);
        SetProperty(client, nameof(RxTcAdsClient.Connected), true);
        client.Pause(TimeSpan.FromHours(1));
        using var table = CreateTaggedTable(client, ScalarVariable);

        var writeTask = table.WriteValuesAsync(_ => { }, TimeSpan.Zero);
        await Task.Yield();
        client.Disconnect();
        var result = await writeTask;

        await TUnitAssert.That(result).IsTrue();
        await TUnitAssert.That(client.IsPaused).IsFalse();
    }

    /// <summary>Verifies HashTableRx guards and synchronous data population.</summary>
    /// <returns>The test task.</returns>
    [Test]
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("HashTableRx structure APIs reflect over test data.")]
#endif
    public async Task HashTable_Helpers_Cover_Guards_And_Immediate_Data()
    {
        var table = new HashTableRx(false);
        var fake = new RxFakeClient(Observable.Return((ScalarVariable, (object?)new TestStructure { Value = ObservedValue }, (string?)null)));

        var populated = fake.CreateStruct(ScalarVariable);
        var nullClientResult = LeanTwinCatRxExtensions.CreateStruct((IRxTcAdsClient)null!, ScalarVariable);

        await TUnitAssert.That(((HashTableRx)null!).WriteValues(_ => { })).IsFalse();
        await TUnitAssert.That(table.WriteValues(null!)).IsFalse();
        await TUnitAssert.That(table.WriteValues(_ => { })).IsFalse();
        await TUnitAssert.That(await table.WriteValuesAsync(null!, TimeSpan.Zero)).IsFalse();
        await TUnitAssert.That(await table.WriteValuesAsync(_ => { }, TimeSpan.Zero)).IsFalse();
        await TUnitAssert.That(nullClientResult).IsNull();
        await TUnitAssert.That(populated).IsNotNull();
        await TUnitAssert.That(populated!.Count).IsGreaterThan(0);

        populated.Dispose();
        fake.Dispose();
        table.Dispose();
    }

    /// <summary>Verifies initialized disposal clears local client state and is idempotent.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Dispose_Clears_Local_State_And_Is_Idempotent()
    {
        var client = new RxTcAdsClient();
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

    /// <summary>Creates a populated table with the tags expected by the write extensions.</summary>
    /// <param name="client">The disconnected local client.</param>
    /// <param name="variable">The variable name.</param>
    /// <returns>The tagged table.</returns>
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("HashTableRx structure APIs reflect over test data.")]
#endif
    private static HashTableRx CreateTaggedTable(RxTcAdsClient client, string variable)
    {
        var table = new HashTableRx(false);
        table.SetStructure(new TestStructure { Value = 1 });
        table.Tag![nameof(RxTcAdsClient)] = client;
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
        typeof(RxTcAdsClient).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Private method was not found: " + name);

    /// <summary>Initializes the client's local disposable collection without creating an ADS client.</summary>
    /// <param name="client">The client.</param>
    private static void InitializeCleanup(RxTcAdsClient client) =>
        typeof(RxTcAdsClient).GetField("_cleanup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(client, new MultipleDisposable());

    /// <summary>Sets a property, including non-public setters, for deterministic state setup.</summary>
    /// <param name="instance">The containing instance.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The value.</param>
    private static void SetProperty(object instance, string propertyName, object value) =>
        instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!.SetValue(instance, value);

    /// <summary>Simple reflected structure used by HashTableRx.</summary>
    private sealed class TestStructure
    {
        /// <summary>Gets or sets the test value.</summary>
        public int Value { get; set; }
    }
}
