// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using CP.Collections;

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
        /// <returns>
        /// A HashTableRx with a link to the PLC.
        /// </returns>
        public static HashTableRx? CreateStruct(this IRxTcAdsClient @this, string variable)
        {
            if (@this == null)
            {
                return default;
            }

            var ht = new HashTableRx(@this.Settings?.Port < 851);
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
        public static HashTableRx CreateClone(this HashTableRx @this)
        {
            if (@this == null)
            {
                return default!;
            }

            return new(@this!.UseUpperCase) { [true] = @this.GetStucture() };
        }
    }
}
