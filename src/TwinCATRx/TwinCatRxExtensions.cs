// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using CP.Collections;
using CP.TwinCatRx.Core;

namespace CP.TwinCatRx
{
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
        public static IObservable<T> Observe<T>(this IRxTcAdsClient @this, string variable) =>
            @this?.DataReceived.Where(x => x.Variable.ToUpperInvariant().Equals(variable.ToUpperInvariant(), StringComparison.InvariantCulture) && x.Data != null).Select(x => (T)x.Data!)!;

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
        public static IObservable<T> Observe<T>(this IRxTcAdsClient @this, string variable, string id) =>
            @this?.DataReceived.Where(x => string.Equals(x.Id, id) && x.Variable.ToUpperInvariant().Equals(variable.ToUpperInvariant(), StringComparison.InvariantCulture) && x.Data != null).Select(x => (T)x.Data!)!;

        /// <summary>
        /// Creates the structure.
        /// </summary>
        /// <param name="this">The this.</param>
        /// <param name="variable">The variable.</param>
        /// <param name="useUpperCase">if set to <c>true</c> [use upper case].</param>
        /// <returns>
        /// A HashTableRx with a link to the PLC.
        /// </returns>
        public static HashTableRx CreateStruct(this IRxTcAdsClient @this, string variable, bool useUpperCase)
        {
            var ht = new HashTableRx(useUpperCase);
            ht.Tag?.Add(nameof(RxTcAdsClient), @this);
            ht.Tag?.Add("Variable", variable);
            @this?.DataReceived.Where(x => x.Variable.ToUpperInvariant().Equals(variable.ToUpperInvariant(), StringComparison.InvariantCulture) && x.Data != null).Subscribe(x => ht[true] = x.Data);
            return ht;
        }

        /// <summary>
        /// Writes the values.
        /// </summary>
        /// <param name="this">The this.</param>
        /// <param name="setValues">The set values.</param>
        public static void WriteValues(this HashTableRx @this, Action<HashTableRx> setValues)
        {
            if (@this == null || setValues == null)
            {
                return;
            }

            if (@this.Tag?[nameof(RxTcAdsClient)] is RxTcAdsClient plc && @this.Tag?["Variable"] is string variable)
            {
                using (var htClone = @this.CreateClone())
                {
                    setValues(htClone);
                    plc.Write(variable, htClone.GetStucture());
                }
            }
        }

        /// <summary>
        /// Structures the ready.
        /// </summary>
        /// <param name="this">The this.</param>
        /// <returns>An Observable when values have been set.</returns>
        public static IObservable<HashTableRx> StructureReady(this HashTableRx @this)
        {
            if (@this == null)
            {
                return default!;
            }

            return @this.ObserveAll.Where(_ => @this.Count > 0).Take(1).Delay(TimeSpan.FromSeconds(2)).Select(_ => @this);
        }

        /// <summary>
        /// Clones the specified HashTableRx.
        /// </summary>
        /// <param name="this">The this.</param>
        /// <returns>A HashTableRx.</returns>
        public static HashTableRx CreateClone(this HashTableRx @this) => new(true) { [true] = @this.GetStucture() };

        /// <summary>
        /// Values the specified variable.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="this">The this.</param>
        /// <param name="variable">The variable.</param>
        /// <returns>The value of the Tag.</returns>
        public static T? Value<T>(this HashTableRx @this, string? variable)
        {
            if (@this == null || @this.Count == 0)
            {
                return default;
            }

            if (@this.UseUpperCase)
            {
                variable = variable?.ToUpperInvariant();
            }

            return (T?)@this[variable!];
        }

        /// <summary>
        /// Values the specified variable.
        /// </summary>
        /// <typeparam name="T">The Type.</typeparam>
        /// <param name="this">The this.</param>
        /// <param name="variable">The variable.</param>
        /// <param name="value">The value.</param>
        /// <returns>True if value was set.</returns>
        public static bool Value<T>(this HashTableRx @this, string? variable, T? value)
        {
            if (@this == null || @this.Count == 0)
            {
                return false;
            }

            if (@this?.UseUpperCase == true)
            {
                variable = variable?.ToUpperInvariant();
            }

            if (@this!.Value<T>(variable) == null)
            {
                throw new InvalidVariableException(variable);
            }

            if (@this!.Value<T>(variable)?.GetType() != value?.GetType())
            {
                throw new InvalidCastException($"Failed To Set Value, unable to cast from {typeof(T)}");
            }

            @this![variable!] = value;
            return true;
        }

        /// <summary>
        /// Gets the stucture.
        /// </summary>
        /// <param name="this">The this.</param>
        /// <returns>
        /// An object of the current values.
        /// </returns>
        public static object GetStucture(this HashTableRx @this)
        {
            if (@this == null)
            {
                return default!;
            }

            return @this[true]!;
        }
    }
}
