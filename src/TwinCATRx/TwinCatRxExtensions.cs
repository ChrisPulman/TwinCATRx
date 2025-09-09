// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reflection;
using CP.Collections;

namespace CP.TwinCatRx;

/// <summary>
/// Observable TwinCAT Extensions.
/// </summary>
public static class TwinCatRxExtensions
{
    /// <summary>
    /// Observes the specified variable.
    /// </summary>
    /// <typeparam name="T">The Type of the data.</typeparam>
    /// <param name="this">The this.</param>
    /// <param name="variable">The variable.</param>
    /// <returns>An Observable of T.</returns>
#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Generic cast is driven by the user's T; trimming will not remove observed payload types in typical usage.")]
#endif
    public static IObservable<T> Observe<T>(this IRxTcAdsClient @this, string variable) =>
        @this?.DataReceived
            .Where(x => string.Equals(x.Variable, variable, StringComparison.OrdinalIgnoreCase) && x.Data != null)
            .Select(x => (T)x.Data!)!;

    /// <summary>
    /// Observes the specified variable.
    /// </summary>
    /// <typeparam name="T">The Type of the data.</typeparam>
    /// <param name="this">The this.</param>
    /// <param name="variable">The variable.</param>
    /// <param name="id">The identifier.</param>
    /// <returns>
    /// An Observable of T.
    /// </returns>
#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Generic cast is driven by the user's T; trimming will not remove observed payload types in typical usage.")]
#endif
    public static IObservable<T> Observe<T>(this IRxTcAdsClient @this, string variable, string id) =>
        @this?.DataReceived
            .Where(x => string.Equals(x.Id, id) && string.Equals(x.Variable, variable, StringComparison.OrdinalIgnoreCase) && x.Data != null)
            .Select(x => (T)x.Data!)!;

    /// <summary>
    /// Creates the structure.
    /// </summary>
    /// <param name="this">The this.</param>
    /// <param name="variable">The variable.</param>
    /// <returns>
    /// A HashTableRx with a link to the PLC.
    /// </returns>
#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "HashTableRx usage is explicit; no reflection-based access required.")]
#endif
    public static HashTableRx? CreateStruct(this IRxTcAdsClient @this, string variable)
    {
        if (@this == null)
        {
            return default;
        }

        var ht = new HashTableRx(@this.Settings?.Port < 851);
        ht.Tag?.Add(nameof(RxTcAdsClient), @this);
        ht.Tag?.Add("Variable", variable);
        @this?.DataReceived
            .Where(x => x.Variable.ToUpperInvariant().Equals(variable.ToUpperInvariant(), StringComparison.InvariantCulture) && x.Data != null)
            .Subscribe(x => ht[true] = x.Data);
        return ht;
    }

    /// <summary>
    /// Writes the values.
    /// </summary>
    /// <param name="this">The HashTableRx to write values into.</param>
    /// <param name="setValues">The set values.</param>
    /// <returns>True if successful.</returns>
#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "No reflection or dynamic code; strongly-typed write path.")]
#endif
    public static bool WriteValues(this HashTableRx @this, Action<HashTableRx> setValues)
    {
        if (@this == null || setValues == null)
        {
            return false;
        }

        if (@this.Tag?[nameof(RxTcAdsClient)] is RxTcAdsClient plc && @this.Tag?["Variable"] is string variable)
        {
            if (!plc.Connected)
            {
                return false;
            }

            using (var htClone = @this.CreateClone())
            {
                setValues(htClone);
                var structure = htClone.GetStructure();
                if (structure == null)
                {
                    return false;
                }

                plc.Write(variable, structure);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Writes the values asynchronous.
    /// </summary>
    /// <param name="this">The this.</param>
    /// <param name="setValues">The set values.</param>
    /// <param name="time">The time to delay between writes.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "No reflection or dynamic code; async path delegates to strongly-typed write.")]
#endif
    public static async Task<bool> WriteValuesAsync(this HashTableRx @this, Action<HashTableRx> setValues, TimeSpan time)
    {
        if (@this == null || setValues == null)
        {
            return false;
        }

        if (@this.Tag?[nameof(RxTcAdsClient)] is RxTcAdsClient plc && @this.Tag?["Variable"] is string variable)
        {
            if (!plc.Connected)
            {
                return false;
            }

            if (plc.IsPaused)
            {
                // If the PLC is paused, wait until it is resumed.
                var tcs = new TaskCompletionSource<bool>();
                var d = plc.IsPausedObservable.Subscribe(isPaused =>
                {
                    if (!isPaused)
                    {
                        tcs.TrySetResult(true);
                    }
                });
                _ = await tcs.Task;
                d.Dispose();
            }
            else
            {
                // If the PLC is not paused, pause it for the specified time.
                plc.Pause(time);
            }

            using (var htClone = @this.CreateClone())
            {
                setValues(htClone);
                var structure = htClone.GetStructure();
                if (structure == null)
                {
                    return false;
                }

                plc.Write(variable, structure);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Structures the ready.
    /// </summary>
    /// <param name="this">The this.</param>
    /// <returns>
    /// An Observable when values have been set.
    /// </returns>
    /// <exception cref="ArgumentNullException">The HashTableRx cannot be null.</exception>
#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Pure Rx composition; no reflection or dynamic code.")]
#endif
    public static IObservable<HashTableRx> StructureReady(this HashTableRx @this)
    {
        if (@this == null)
        {
            throw new ArgumentNullException(nameof(@this));
        }

        return @this.ObserveAll.Where(_ => @this.Count > 0).Take(1).Delay(TimeSpan.FromSeconds(2)).Select(_ => @this);
    }

    /// <summary>
    /// Clones the specified HashTableRx.
    /// </summary>
    /// <param name="this">The this.</param>
    /// <returns>
    /// A HashTableRx.
    /// </returns>
    /// <exception cref="ArgumentNullException">The HashTableRx cannot be null.</exception>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("May use reflection if structure contains fields/properties.")]
#endif
    public static HashTableRx CreateClone(this HashTableRx @this)
    {
        if (@this == null)
        {
            throw new ArgumentNullException(nameof(@this));
        }

        return new(@this!.UseUpperCase) { [true] = @this.GetStructure() };
    }
}
