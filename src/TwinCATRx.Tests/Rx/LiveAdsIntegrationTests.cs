// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if LIVE_ADS_TESTS
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CP.Collections;
using CP.TwinCatRx.Core;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;

namespace TwinCATRx.Tests.Rx;

/// <summary>Live ADS integration tests for a TwinCAT 3 PLC.</summary>
[Category("LiveAds")]
public class LiveAdsIntegrationTests
{
    /// <summary>Maps CLR primitive types to reversible live ADS value mutations.</summary>
    private static readonly Dictionary<Type, Func<object, object>> PrimitiveValueMutators =
        new Dictionary<Type, Func<object, object>>
        {
            [typeof(bool)] = CreateChangedBooleanValue,
            [typeof(byte)] = CreateChangedByteValue,
            [typeof(sbyte)] = CreateChangedSignedByteValue,
            [typeof(short)] = CreateChangedInt16Value,
            [typeof(ushort)] = CreateChangedUInt16Value,
            [typeof(int)] = CreateChangedInt32Value,
            [typeof(uint)] = CreateChangedUInt32Value,
            [typeof(long)] = CreateChangedInt64Value,
            [typeof(ulong)] = CreateChangedUInt64Value,
            [typeof(float)] = value => CreateChangedSingleValue(value),
            [typeof(double)] = value => CreateChangedDoubleValue(value)
        };

    /// <summary>Verifies symbol loading, read operations, direct writes, and HashTableRx writes against the live PLC.</summary>
    /// <returns>The test task.</returns>
    [Test]
#if NET9_0_OR_GREATER
    [RequiresDynamicCode("RxTcAdsClient generates PLC structure types at runtime.")]
    [RequiresUnreferencedCode("RxTcAdsClient uses reflection to materialize PLC structure types.")]
#endif
    public async Task LiveAds_Read_And_Write_Operations_RoundTrip()
    {
        var options = LiveAdsOptions.Create();

        VerifyAdsEndpoint(options);
        VerifySymbolLoading(options);
        await VerifyNotificationOnlyReadAsync(options).ConfigureAwait(false);
        await VerifyPrimitiveLeafRoundTripAsync(options).ConfigureAwait(false);

        using var client = new RxTcAdsClient();
        var capture = new LiveAdsCapture(client, options.Variable);
        using var subscriptions = capture.Subscribe();

        client.Connect(options.CreateSettings());
        _ = await capture.WaitForInitializeAsync(options.OperationTimeout).ConfigureAwait(false);
        await TUnitAssert.That(client.Connected).IsTrue();

        var table = client.CreateStruct(options.Variable);
        await TUnitAssert.That(table).IsNotNull();
        var liveTable = table!;

        var beforeTask = capture.WaitForDataAsync("live-read-before", options.OperationTimeout);
        client.Read(options.Variable, id: "live-read-before");
        var before = await beforeTask.ConfigureAwait(false);
        await TUnitAssert.That(before.Data).IsNotNull();
        await WaitForStructureAsync(liveTable, options.OperationTimeout).ConfigureAwait(false);

        var directWriteTask = capture.WaitForWriteAsync("live-write-same", options.OperationTimeout);
        client.Write(options.Variable, before.Data!, id: "live-write-same");
        var directWrite = await directWriteTask.ConfigureAwait(false);
        await TUnitAssert.That(directWrite).IsEqualTo("Success,live-write-same");

        var afterTask = capture.WaitForDataAsync("live-read-after", options.OperationTimeout);
        client.Read(options.Variable, id: "live-read-after");
        var after = await afterTask.ConfigureAwait(false);
        await TUnitAssert.That(after.Data).IsNotNull();

        var hashTableWriteTask = capture.WaitForWriteAsync(null, options.OperationTimeout);
        var hashTableWrite = liveTable.WriteValues(_ => { });
        await TUnitAssert.That(hashTableWrite).IsTrue();
        var hashTableWriteResult = await hashTableWriteTask.ConfigureAwait(false);
        await TUnitAssert.That(hashTableWriteResult).IsEqualTo("Success");

        var asyncHashTableWriteTask = capture.WaitForWriteAsync(null, options.OperationTimeout);
        var asyncHashTableWrite = await liveTable.WriteValuesAsync(_ => { }, TimeSpan.FromMilliseconds(25)).ConfigureAwait(false);
        await TUnitAssert.That(asyncHashTableWrite).IsTrue();
        var asyncHashTableWriteResult = await asyncHashTableWriteTask.ConfigureAwait(false);
        await TUnitAssert.That(asyncHashTableWriteResult).IsEqualTo("Success");

        client.Disconnect();
        await TUnitAssert.That(client.Connected).IsFalse();
    }

    /// <summary>Verifies that the ADS endpoint is reachable and running.</summary>
    /// <param name="options">The live ADS options.</param>
    private static void VerifyAdsEndpoint(LiveAdsOptions options)
    {
        using var client = new AdsClient();
        client.Connect(options.AdsAddress, options.Port);
        var state = client.ReadState();
        if (state.AdsState != AdsState.Run)
        {
            throw new InvalidOperationException("The live ADS endpoint is not in Run state.");
        }

        var handle = client.CreateVariableHandle(options.Variable);
        try
        {
            if (handle == 0)
            {
                throw new InvalidOperationException("The live ADS variable handle was not created.");
            }
        }
        finally
        {
            client.DeleteVariableHandle(handle);
        }
    }

    /// <summary>Verifies that the configured PLC symbol can be loaded by the TwinCATRx code generator.</summary>
    /// <param name="options">The live ADS options.</param>
#if NET9_0_OR_GREATER
    [RequiresDynamicCode("CodeGenerator can emit PLC structure assemblies.")]
    [RequiresUnreferencedCode("CodeGenerator resolves generated PLC structure types by reflection.")]
#endif
    private static void VerifySymbolLoading(LiveAdsOptions options)
    {
        using var codeGenerator = new CodeGenerator();
        var symbols = codeGenerator.LoadSymbols(options.AdsAddress, options.Port);
        if (symbols.Count == 0)
        {
            throw new InvalidOperationException("No ADS symbols were loaded from the live PLC.");
        }

        var node = codeGenerator.SearchSymbols(options.Variable);
        if (node.Tag is not null)
        {
            return;
        }

        throw new InvalidOperationException("The configured live ADS variable was not found in the PLC symbols.");
    }

    /// <summary>Verifies that notification-only handles can be used for explicit reads.</summary>
    /// <param name="options">The live ADS options.</param>
    /// <returns>The verification task.</returns>
#if NET9_0_OR_GREATER
    [RequiresDynamicCode("RxTcAdsClient generates PLC structure types at runtime.")]
    [RequiresUnreferencedCode("RxTcAdsClient uses reflection to materialize PLC structure types.")]
#endif
    private static async Task VerifyNotificationOnlyReadAsync(LiveAdsOptions options)
    {
        using var client = new RxTcAdsClient();
        var capture = new LiveAdsCapture(client, options.Variable);
        using var subscriptions = capture.Subscribe();

        client.Connect(options.CreateNotificationOnlySettings());
        _ = await capture.WaitForInitializeAsync(options.OperationTimeout).ConfigureAwait(false);
        await TUnitAssert.That(client.Connected).IsTrue();

        var readTask = capture.WaitForDataAsync("notification-only-read", options.OperationTimeout);
        client.Read(options.Variable, id: "notification-only-read");
        var data = await readTask.ConfigureAwait(false);
        await TUnitAssert.That(data.Data).IsNotNull();

        client.Disconnect();
        await TUnitAssert.That(client.Connected).IsFalse();
    }

    /// <summary>Verifies a reversible primitive leaf read/write round trip against the live PLC.</summary>
    /// <param name="options">The live ADS options.</param>
    /// <returns>The verification task.</returns>
#if NET9_0_OR_GREATER
    [RequiresDynamicCode("RxTcAdsClient generates PLC structure types at runtime.")]
    [RequiresUnreferencedCode("RxTcAdsClient uses reflection to materialize PLC structure types.")]
#endif
    private static async Task VerifyPrimitiveLeafRoundTripAsync(LiveAdsOptions options)
    {
        var leaf = ResolveMutationLeaf(options);
        using var client = new RxTcAdsClient();
        var capture = new LiveAdsCapture(client, leaf.Variable);
        using var subscriptions = capture.Subscribe();

        object? original = null;
        var restoreRequired = false;
        try
        {
            client.Connect(options.CreateLeafSettings(leaf.Variable));
            _ = await capture.WaitForInitializeAsync(options.OperationTimeout).ConfigureAwait(false);
            await TUnitAssert.That(client.Connected).IsTrue();

            var originalTask = capture.WaitForDataAsync("leaf-read-original", options.OperationTimeout);
            client.Read(leaf.Variable, id: "leaf-read-original");
            var originalRead = await originalTask.ConfigureAwait(false);
            await TUnitAssert.That(originalRead.Data).IsNotNull();

            original = originalRead.Data!;
            var changed = CreateChangedValue(original, leaf);
            await TUnitAssert.That(ValuesEqual(changed, original)).IsFalse();

            var writeTask = capture.WaitForWriteAsync("leaf-write-changed", options.OperationTimeout);
            client.Write(leaf.Variable, changed, id: "leaf-write-changed");
            var writeResult = await writeTask.ConfigureAwait(false);
            await TUnitAssert.That(writeResult).IsEqualTo("Success,leaf-write-changed");
            restoreRequired = true;

            var changedTask = capture.WaitForDataAsync("leaf-read-changed", options.OperationTimeout);
            client.Read(leaf.Variable, id: "leaf-read-changed");
            var changedRead = await changedTask.ConfigureAwait(false);
            await TUnitAssert.That(ValuesEqual(changedRead.Data, changed)).IsTrue();
        }
        finally
        {
            if (restoreRequired && original is not null)
            {
                var restoreTask = capture.WaitForWriteAsync("leaf-write-restore", options.OperationTimeout);
                client.Write(leaf.Variable, original, id: "leaf-write-restore");
                var restoreResult = await restoreTask.ConfigureAwait(false);
                await TUnitAssert.That(restoreResult).IsEqualTo("Success,leaf-write-restore");

                var restoredTask = capture.WaitForDataAsync("leaf-read-restore", options.OperationTimeout);
                client.Read(leaf.Variable, id: "leaf-read-restore");
                var restoredRead = await restoredTask.ConfigureAwait(false);
                await TUnitAssert.That(ValuesEqual(restoredRead.Data, original)).IsTrue();
            }

            client.Disconnect();
            await TUnitAssert.That(client.Connected).IsFalse();
        }
    }

    /// <summary>Resolves the PLC primitive leaf used by the reversible live write test.</summary>
    /// <param name="options">The live ADS options.</param>
    /// <returns>The mutation leaf.</returns>
    private static LiveAdsPrimitiveLeaf ResolveMutationLeaf(LiveAdsOptions options)
    {
        using var codeGenerator = new CodeGenerator();
        _ = codeGenerator.LoadSymbols(options.AdsAddress, options.Port);
        using var adsClient = new AdsClient();
        adsClient.Connect(options.AdsAddress, options.Port);
        if (!string.IsNullOrWhiteSpace(options.MutationVariable))
        {
            var configuredNode = codeGenerator.SearchSymbols(options.MutationVariable);
            var configuredLeaf = CreatePrimitiveLeaf(configuredNode, allowString: true)
                ?? throw new InvalidOperationException("The configured live ADS mutation variable is not a writable primitive or string symbol.");
            return CanRoundTripPrimitiveLeaf(adsClient, configuredLeaf)
                ? configuredLeaf
                : throw new InvalidOperationException("The configured live ADS mutation variable did not retain a reversible test value.");
        }

        var root = codeGenerator.SearchSymbols(options.Variable);
        return FindPrimitiveLeaf(root, allowString: false, adsClient)
            ?? FindPrimitiveLeaf(root, allowString: true, adsClient)
            ?? throw new InvalidOperationException("No writable primitive leaf was found under the configured live ADS variable.");
    }

    /// <summary>Finds a primitive leaf below a node.</summary>
    /// <param name="node">The node to search.</param>
    /// <param name="allowString">Whether string leaves may be selected.</param>
    /// <param name="adsClient">The ADS client used to verify candidate leaves.</param>
    /// <returns>The primitive leaf, or <c>null</c> when none was found.</returns>
    private static LiveAdsPrimitiveLeaf? FindPrimitiveLeaf(INodeEmulator? node, bool allowString, AdsClient adsClient)
    {
        var candidate = CreatePrimitiveLeaf(node, allowString);
        if (candidate is not null && CanRoundTripPrimitiveLeaf(adsClient, candidate))
        {
            return candidate;
        }

        var children = node?.Nodes;
        if (children is null)
        {
            return null;
        }

        foreach (var child in children)
        {
            candidate = FindPrimitiveLeaf(child, allowString, adsClient);
            if (candidate is not null)
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>Checks whether a primitive leaf can retain and restore a changed value.</summary>
    /// <param name="adsClient">The direct ADS client.</param>
    /// <param name="leaf">The candidate leaf.</param>
    /// <returns><c>true</c> when the leaf can be round-tripped.</returns>
    private static bool CanRoundTripPrimitiveLeaf(AdsClient adsClient, LiveAdsPrimitiveLeaf leaf)
    {
        object? original = null;
        var restoreRequired = false;
        try
        {
            original = adsClient.ReadValue(leaf.Variable, leaf.ClrType);
            if (original is null)
            {
                return false;
            }

            var changed = CreateChangedValue(original, leaf);
            adsClient.WriteValue(leaf.Variable, changed);
            restoreRequired = true;
            return PrimitiveLeafMatches(adsClient, leaf, changed);
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            if (restoreRequired && original is not null)
            {
                adsClient.WriteValue(leaf.Variable, original);
            }
        }
    }

    /// <summary>Checks whether a primitive leaf currently reads as an expected value.</summary>
    /// <param name="adsClient">The direct ADS client.</param>
    /// <param name="leaf">The primitive leaf.</param>
    /// <param name="expected">The expected value.</param>
    /// <returns><c>true</c> when the value matches immediately and after one PLC cycle window.</returns>
    private static bool PrimitiveLeafMatches(AdsClient adsClient, LiveAdsPrimitiveLeaf leaf, object expected)
    {
        var immediate = adsClient.ReadValue(leaf.Variable, leaf.ClrType);
        if (!ValuesEqual(immediate, expected))
        {
            return false;
        }

        Thread.Sleep(100);
        var settled = adsClient.ReadValue(leaf.Variable, leaf.ClrType);
        return ValuesEqual(settled, expected);
    }

    /// <summary>Creates a primitive leaf descriptor when a node can be safely mutated.</summary>
    /// <param name="node">The symbol node.</param>
    /// <param name="allowString">Whether string leaves may be selected.</param>
    /// <returns>The primitive leaf, or <c>null</c> when the node is not suitable.</returns>
    private static LiveAdsPrimitiveLeaf? CreatePrimitiveLeaf(INodeEmulator? node, bool allowString)
    {
        return TryGetWritableSymbol(node, out var symbol)
            && IsAllowedLeafCategory(symbol.Category, allowString)
            && TryResolveMutableLiveAdsType(symbol.TypeName, allowString, out var type)
            && TryGetVariablePath(symbol, out var variable)
            ? new LiveAdsPrimitiveLeaf(variable, symbol.TypeName, type)
            : null;
    }

    /// <summary>Gets a writable ADS symbol from a node.</summary>
    /// <param name="node">The symbol node.</param>
    /// <param name="symbol">The writable symbol.</param>
    /// <returns><c>true</c> when a writable symbol was found.</returns>
    private static bool TryGetWritableSymbol(INodeEmulator? node, [NotNullWhen(true)] out ISymbol? symbol)
    {
        if (node?.Tag is ISymbol candidate && !candidate.IsReadOnly)
        {
            symbol = candidate;
            return true;
        }

        symbol = null;
        return false;
    }

    /// <summary>Gets whether a symbol category can be used by the live ADS mutation test.</summary>
    /// <param name="category">The symbol category.</param>
    /// <param name="allowString">Whether string leaves may be selected.</param>
    /// <returns><c>true</c> when the category is allowed.</returns>
    private static bool IsAllowedLeafCategory(DataTypeCategory category, bool allowString) =>
        category == DataTypeCategory.Primitive || (allowString && category == DataTypeCategory.String);

    /// <summary>Gets a symbol variable path.</summary>
    /// <param name="symbol">The ADS symbol.</param>
    /// <param name="variable">The variable path.</param>
    /// <returns><c>true</c> when a variable path was resolved.</returns>
    private static bool TryGetVariablePath(ISymbol symbol, [NotNullWhen(true)] out string? variable)
    {
        variable = string.IsNullOrWhiteSpace(symbol.InstancePath) ? symbol.InstanceName : symbol.InstancePath;
        if (!string.IsNullOrWhiteSpace(variable))
        {
            return true;
        }

        variable = null;
        return false;
    }

    /// <summary>Resolves a PLC primitive type to a CLR type.</summary>
    /// <param name="plcType">The PLC type name.</param>
    /// <param name="type">The resolved CLR type.</param>
    /// <returns><c>true</c> when the type was resolved.</returns>
    private static bool TryResolveLiveAdsType(string? plcType, out Type? type)
    {
        type = null;
        try
        {
            var typeName = CodeGenerator.PLCToCSharpTypeConverter(plcType).Split(',')[0];
            type = typeName switch
            {
                "sbyte" => typeof(sbyte),
                "long" => typeof(long),
                "ulong" => typeof(ulong),
                _ => Type.GetType(typeName)
            };
            return type is not null;
        }
        catch (UnsuportedTypeException)
        {
            return false;
        }
    }

    /// <summary>Resolves a mutable PLC primitive type to a CLR type.</summary>
    /// <param name="plcType">The PLC type name.</param>
    /// <param name="allowString">Whether string leaves may be selected.</param>
    /// <param name="type">The resolved CLR type.</param>
    /// <returns><c>true</c> when the type can be mutated.</returns>
    private static bool TryResolveMutableLiveAdsType(string? plcType, bool allowString, [NotNullWhen(true)] out Type? type) =>
        TryResolveLiveAdsType(plcType, out type) && type is not null && CanMutateType(type, allowString);

    /// <summary>Gets whether a CLR type can be reversibly mutated by the live ADS test.</summary>
    /// <param name="type">The CLR type.</param>
    /// <param name="allowString">Whether string leaves may be selected.</param>
    /// <returns><c>true</c> when the type can be mutated.</returns>
    private static bool CanMutateType(Type type, bool allowString) =>
        PrimitiveValueMutators.ContainsKey(type) || (allowString && type == typeof(string));

    /// <summary>Creates a changed value for the supplied primitive leaf.</summary>
    /// <param name="original">The original value.</param>
    /// <param name="leaf">The live ADS primitive leaf.</param>
    /// <returns>The changed value.</returns>
    private static object CreateChangedValue(object original, LiveAdsPrimitiveLeaf leaf)
    {
        if (PrimitiveValueMutators.TryGetValue(leaf.ClrType, out var mutator))
        {
            return mutator(original);
        }

        if (leaf.ClrType == typeof(string))
        {
            return CreateChangedStringValue(original, leaf.TypeName);
        }

        throw new InvalidOperationException("The live ADS mutation variable type is not supported.");
    }

    /// <summary>Creates a changed boolean value.</summary>
    /// <param name="original">The original value.</param>
    /// <returns>The changed value.</returns>
    private static object CreateChangedBooleanValue(object original) =>
        !Convert.ToBoolean(original, CultureInfo.InvariantCulture);

    /// <summary>Creates a changed byte value.</summary>
    /// <param name="original">The original value.</param>
    /// <returns>The changed value.</returns>
    private static object CreateChangedByteValue(object original)
    {
        var value = Convert.ToByte(original, CultureInfo.InvariantCulture);
        return value == byte.MaxValue ? (byte)(value - 1) : (byte)(value + 1);
    }

    /// <summary>Creates a changed signed byte value.</summary>
    /// <param name="original">The original value.</param>
    /// <returns>The changed value.</returns>
    private static object CreateChangedSignedByteValue(object original)
    {
        var value = Convert.ToSByte(original, CultureInfo.InvariantCulture);
        return value == sbyte.MaxValue ? (sbyte)(value - 1) : (sbyte)(value + 1);
    }

    /// <summary>Creates a changed signed 16-bit value.</summary>
    /// <param name="original">The original value.</param>
    /// <returns>The changed value.</returns>
    private static object CreateChangedInt16Value(object original)
    {
        var value = Convert.ToInt16(original, CultureInfo.InvariantCulture);
        return value == short.MaxValue ? (short)(value - 1) : (short)(value + 1);
    }

    /// <summary>Creates a changed unsigned 16-bit value.</summary>
    /// <param name="original">The original value.</param>
    /// <returns>The changed value.</returns>
    private static object CreateChangedUInt16Value(object original)
    {
        var value = Convert.ToUInt16(original, CultureInfo.InvariantCulture);
        return value == ushort.MaxValue ? (ushort)(value - 1) : (ushort)(value + 1);
    }

    /// <summary>Creates a changed signed 32-bit value.</summary>
    /// <param name="original">The original value.</param>
    /// <returns>The changed value.</returns>
    private static object CreateChangedInt32Value(object original)
    {
        var value = Convert.ToInt32(original, CultureInfo.InvariantCulture);
        return value == int.MaxValue ? value - 1 : value + 1;
    }

    /// <summary>Creates a changed unsigned 32-bit value.</summary>
    /// <param name="original">The original value.</param>
    /// <returns>The changed value.</returns>
    private static object CreateChangedUInt32Value(object original)
    {
        var value = Convert.ToUInt32(original, CultureInfo.InvariantCulture);
        return value == uint.MaxValue ? value - 1 : value + 1;
    }

    /// <summary>Creates a changed signed 64-bit value.</summary>
    /// <param name="original">The original value.</param>
    /// <returns>The changed value.</returns>
    private static object CreateChangedInt64Value(object original)
    {
        var value = Convert.ToInt64(original, CultureInfo.InvariantCulture);
        return value == long.MaxValue ? value - 1 : value + 1;
    }

    /// <summary>Creates a changed unsigned 64-bit value.</summary>
    /// <param name="original">The original value.</param>
    /// <returns>The changed value.</returns>
    private static object CreateChangedUInt64Value(object original)
    {
        var value = Convert.ToUInt64(original, CultureInfo.InvariantCulture);
        return value == ulong.MaxValue ? value - 1 : value + 1;
    }

    /// <summary>Creates a changed single-precision value.</summary>
    /// <param name="original">The original value.</param>
    /// <returns>The changed value.</returns>
    private static float CreateChangedSingleValue(object original)
    {
        var value = Convert.ToSingle(original, CultureInfo.InvariantCulture);
        return !float.IsFinite(value) || value == 0F ? 1.25F : -value;
    }

    /// <summary>Creates a changed double-precision value.</summary>
    /// <param name="original">The original value.</param>
    /// <returns>The changed value.</returns>
    private static double CreateChangedDoubleValue(object original)
    {
        var value = Convert.ToDouble(original, CultureInfo.InvariantCulture);
        return !double.IsFinite(value) || value == 0D ? 1.25D : -value;
    }

    /// <summary>Creates a changed value for a PLC string.</summary>
    /// <param name="original">The original string value.</param>
    /// <param name="typeName">The PLC type name.</param>
    /// <returns>The changed string.</returns>
    private static string CreateChangedStringValue(object original, string typeName)
    {
        var current = Convert.ToString(original, CultureInfo.InvariantCulture) ?? string.Empty;
        var maxLength = GetStringLength(typeName);
        var changed = current.EndsWith("_rx", StringComparison.Ordinal) ? current[..^3] : current + "_rx";
        if (changed.Length > maxLength)
        {
            changed = current == "rx" ? "rX" : "rx";
        }

        if (changed.Length > maxLength)
        {
            changed = changed[..maxLength];
        }

        if (string.Equals(changed, current, StringComparison.Ordinal))
        {
            changed = current == "x" ? "y" : "x";
        }

        return changed;
    }

    /// <summary>Gets the maximum string length from a PLC string type.</summary>
    /// <param name="typeName">The PLC type name.</param>
    /// <returns>The maximum string length.</returns>
    private static int GetStringLength(string typeName)
    {
        var openIndex = typeName.IndexOf('(');
        var closeIndex = typeName.IndexOf(')', openIndex + 1);
        return openIndex >= 0
            && closeIndex > openIndex
            && int.TryParse(typeName[(openIndex + 1)..closeIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out var length)
            ? length
            : 80;
    }

    /// <summary>Compares two primitive values after normalizing to the expected value type.</summary>
    /// <param name="actual">The actual value.</param>
    /// <param name="expected">The expected value.</param>
    /// <returns><c>true</c> when values are equal.</returns>
    private static bool ValuesEqual(object? actual, object expected)
    {
        if (actual is null)
        {
            return false;
        }

        try
        {
            var converted = Convert.ChangeType(actual, expected.GetType(), CultureInfo.InvariantCulture);
            return Equals(converted, expected);
        }
        catch (InvalidCastException)
        {
            return Equals(actual, expected);
        }
        catch (FormatException)
        {
            return Equals(actual, expected);
        }
    }

    /// <summary>Waits for a populated HashTableRx structure.</summary>
    /// <param name="table">The HashTableRx table.</param>
    /// <param name="timeout">The timeout.</param>
    /// <returns>The wait task.</returns>
    private static async Task WaitForStructureAsync(HashTableRx table, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        while (!cancellation.IsCancellationRequested)
        {
            if (table.Count > 0)
            {
                return;
            }

            try
            {
                await Task.Delay(50, cancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        throw new TimeoutException("Timed out waiting for the live PLC structure to populate.");
    }

    /// <summary>Live ADS test options.</summary>
    private sealed class LiveAdsOptions
    {
        /// <summary>Stores the default ADS address.</summary>
        private const string DefaultAdsAddress = "10.1.180.147.1.1";

        /// <summary>Stores the default PLC variable.</summary>
        private const string DefaultVariable = "GlobalVariables.Rig";

        /// <summary>Stores the default ADS port.</summary>
        private const int DefaultPort = 851;

        /// <summary>Gets the ADS address.</summary>
        public string AdsAddress { get; private init; } = DefaultAdsAddress;

        /// <summary>Gets the operation timeout.</summary>
        public TimeSpan OperationTimeout { get; private init; } = TimeSpan.FromSeconds(60);

        /// <summary>Gets the PLC primitive leaf used for reversible mutation.</summary>
        public string MutationVariable { get; private init; } = string.Empty;

        /// <summary>Gets the ADS port.</summary>
        public int Port { get; private init; } = DefaultPort;

        /// <summary>Gets the PLC variable.</summary>
        public string Variable { get; private init; } = DefaultVariable;

        /// <summary>Creates options from environment variables.</summary>
        /// <returns>The live ADS options.</returns>
        public static LiveAdsOptions Create()
        {
            var adsAddress = Environment.GetEnvironmentVariable("TWINCATRX_LIVE_ADS_ADDRESS");
            var mutationVariable = Environment.GetEnvironmentVariable("TWINCATRX_LIVE_ADS_MUTATION_VARIABLE");
            var port = Environment.GetEnvironmentVariable("TWINCATRX_LIVE_ADS_PORT");
            var variable = Environment.GetEnvironmentVariable("TWINCATRX_LIVE_ADS_VARIABLE");

            return new LiveAdsOptions
            {
                AdsAddress = string.IsNullOrWhiteSpace(adsAddress) ? DefaultAdsAddress : adsAddress!,
                MutationVariable = string.IsNullOrWhiteSpace(mutationVariable) ? string.Empty : mutationVariable!,
                Port = int.TryParse(port, out var parsedPort) ? parsedPort : DefaultPort,
                Variable = string.IsNullOrWhiteSpace(variable) ? DefaultVariable : variable!,
                OperationTimeout = TimeSpan.FromSeconds(60)
            };
        }

        /// <summary>Creates settings for the live ADS client.</summary>
        /// <returns>The live ADS settings.</returns>
        public Settings CreateSettings()
        {
            var adsAddress = AdsAddress;
            var port = Port;
            var variable = Variable;
            var settings = new Settings
            {
                AdsAddress = adsAddress,
                Port = port,
                SettingsId = "LiveAds"
            };
            settings.AddNotification(variable, cycleTime: 250);
            settings.AddWriteVariable(variable);
            return settings;
        }

        /// <summary>Creates notification-only settings for explicit read validation.</summary>
        /// <returns>The live ADS settings.</returns>
        public Settings CreateNotificationOnlySettings()
        {
            var adsAddress = AdsAddress;
            var port = Port;
            var variable = Variable;
            var settings = new Settings
            {
                AdsAddress = adsAddress,
                Port = port,
                SettingsId = "LiveAdsNotificationOnly"
            };
            settings.AddNotification(variable, cycleTime: 250);
            return settings;
        }

        /// <summary>Creates settings for a primitive leaf round trip.</summary>
        /// <param name="variable">The primitive leaf variable.</param>
        /// <returns>The live ADS settings.</returns>
        public Settings CreateLeafSettings(string variable)
        {
            var adsAddress = AdsAddress;
            var port = Port;
            var settings = new Settings
            {
                AdsAddress = adsAddress,
                Port = port,
                SettingsId = "LiveAdsPrimitiveLeaf"
            };
            settings.AddNotification(variable, cycleTime: 250);
            settings.AddWriteVariable(variable);
            return settings;
        }
    }

    /// <summary>Describes a writable primitive leaf in the live PLC symbol tree.</summary>
    /// <param name="variable">The PLC variable path.</param>
    /// <param name="typeName">The PLC type name.</param>
    /// <param name="clrType">The CLR type.</param>
    private sealed class LiveAdsPrimitiveLeaf(string variable, string typeName, Type clrType)
    {
        /// <summary>Gets the CLR type.</summary>
        public Type ClrType { get; } = clrType;

        /// <summary>Gets the PLC type name.</summary>
        public string TypeName { get; } = typeName;

        /// <summary>Gets the PLC variable path.</summary>
        public string Variable { get; } = variable;
    }

    /// <summary>Captures live ADS client notifications for assertions.</summary>
    private sealed class LiveAdsCapture
    {
        /// <summary>Stores the client.</summary>
        private readonly RxTcAdsClient _client;

        /// <summary>Stores the synchronization gate.</summary>
        private readonly Lock _gate = new();

        /// <summary>Stores the expected variable.</summary>
        private readonly string _variable;

        /// <summary>Stores the latest error task completion source.</summary>
        private readonly TaskCompletionSource<Exception> _error = CreateTaskCompletionSource<Exception>();

        /// <summary>Stores the initialize task completion source.</summary>
        private readonly TaskCompletionSource<bool> _initialize = CreateTaskCompletionSource<bool>();

        /// <summary>Stores the current data wait identifier.</summary>
        private string? _dataId;

        /// <summary>Stores the current data wait task completion source.</summary>
        private TaskCompletionSource<(string Variable, object? Data, string? Id)> _data = CreateTaskCompletionSource<(string Variable, object? Data, string? Id)>();

        /// <summary>Stores the current write wait identifier.</summary>
        private string? _writeId;

        /// <summary>Stores the current write wait task completion source.</summary>
        private TaskCompletionSource<string?> _write = CreateTaskCompletionSource<string?>();

        /// <summary>Initializes a new instance of the <see cref="LiveAdsCapture"/> class.</summary>
        /// <param name="client">The client.</param>
        /// <param name="variable">The variable.</param>
        public LiveAdsCapture(RxTcAdsClient client, string variable)
        {
            _client = client;
            _variable = variable;
        }

        /// <summary>Subscribes to client notifications.</summary>
        /// <returns>The subscriptions.</returns>
        public IDisposable Subscribe()
        {
            var cleanup = new SubscriptionSet();
            cleanup.Add(_client.InitializeComplete.SubscribeTo(_ => _initialize.TrySetResult(true)));
            cleanup.Add(_client.ErrorReceived.SubscribeTo(error => _error.TrySetResult(error)));
            cleanup.Add(_client.DataReceived.SubscribeTo(OnDataReceived));
            cleanup.Add(_client.OnWrite.SubscribeTo(OnWrite));
            return cleanup;
        }

        /// <summary>Waits for client initialization.</summary>
        /// <param name="timeout">The timeout.</param>
        /// <returns>The initialize result.</returns>
        public Task<bool> WaitForInitializeAsync(TimeSpan timeout) =>
            WaitForSignalAsync(_initialize.Task, timeout, "live ADS initialization");

        /// <summary>Waits for data with the supplied identifier.</summary>
        /// <param name="id">The read identifier.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>The data notification.</returns>
        public Task<(string Variable, object? Data, string? Id)> WaitForDataAsync(string id, TimeSpan timeout)
        {
            lock (_gate)
            {
                _dataId = id;
                _data = CreateTaskCompletionSource<(string Variable, object? Data, string? Id)>();
                return WaitForSignalAsync(_data.Task, timeout, "live ADS read " + id);
            }
        }

        /// <summary>Waits for a write with the supplied identifier.</summary>
        /// <param name="id">The write identifier.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>The write notification.</returns>
        public Task<string?> WaitForWriteAsync(string? id, TimeSpan timeout)
        {
            lock (_gate)
            {
                _writeId = id;
                _write = CreateTaskCompletionSource<string?>();
                return WaitForSignalAsync(_write.Task, timeout, "live ADS write");
            }
        }

        /// <summary>Creates a task completion source with asynchronous continuations.</summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <returns>The task completion source.</returns>
        private static TaskCompletionSource<T> CreateTaskCompletionSource<T>() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Checks whether a write result matches the expected identifier.</summary>
        /// <param name="result">The write result.</param>
        /// <param name="id">The write identifier.</param>
        /// <returns><c>true</c> when the write result matches.</returns>
        private static bool IsExpectedWrite(string? result, string? id) =>
            id is null
                ? string.Equals(result, "Success", StringComparison.Ordinal)
                : string.Equals(result, "Success," + id, StringComparison.Ordinal);

        /// <summary>Handles data received notifications.</summary>
        /// <param name="data">The data notification.</param>
        private void OnDataReceived((string Variable, object? Data, string? Id) data)
        {
            lock (_gate)
            {
                if (string.Equals(data.Variable, _variable, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(data.Id, _dataId, StringComparison.Ordinal))
                {
                    _ = _data.TrySetResult(data);
                }
            }
        }

        /// <summary>Handles write notifications.</summary>
        /// <param name="result">The write result.</param>
        private void OnWrite(string? result)
        {
            lock (_gate)
            {
                if (IsExpectedWrite(result, _writeId))
                {
                    _ = _write.TrySetResult(result);
                }
            }
        }

        /// <summary>Waits for a signal or error.</summary>
        /// <typeparam name="T">The signal type.</typeparam>
        /// <param name="signal">The signal task.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="operation">The operation name.</param>
        /// <returns>The signal result.</returns>
        private async Task<T> WaitForSignalAsync<T>(Task<T> signal, TimeSpan timeout, string operation)
        {
            var delay = Task.Delay(timeout);
            var completed = await Task.WhenAny(signal, _error.Task, delay).ConfigureAwait(false);
            if (ReferenceEquals(completed, signal))
            {
                return await signal.ConfigureAwait(false);
            }

            if (ReferenceEquals(completed, _error.Task))
            {
                throw new InvalidOperationException(operation + " failed.", await _error.Task.ConfigureAwait(false));
            }

            throw new TimeoutException("Timed out during " + operation + ".");
        }

        /// <summary>Stores live ADS subscriptions.</summary>
        private sealed class SubscriptionSet : IDisposable
        {
            /// <summary>Stores subscriptions.</summary>
            private readonly List<IDisposable> _subscriptions = [];

            /// <summary>Adds a subscription.</summary>
            /// <param name="subscription">The subscription.</param>
            public void Add(IDisposable subscription) => _subscriptions.Add(subscription);

            /// <summary>Disposes all subscriptions.</summary>
            public void Dispose()
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.Dispose();
                }

                _subscriptions.Clear();
            }
        }
    }
}
#endif
