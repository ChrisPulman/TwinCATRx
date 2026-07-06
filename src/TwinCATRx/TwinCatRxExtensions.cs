// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using CP.Collections;

namespace CP.TwinCatRx;

/// <summary>Observable TwinCAT extensions.</summary>
public static class TwinCatRxExtensions
{
    /// <summary>Extends HashTableRx instances with TwinCAT write helpers.</summary>
    /// <param name="hashTable">The HashTableRx instance.</param>
    extension(HashTableRx hashTable)
    {
        /// <summary>Writes the values.</summary>
        /// <param name="setValues">The set values.</param>
        /// <returns>True if successful.</returns>
        [RequiresUnreferencedCode("May use reflection if the structure contains fields or properties.")]
        public bool WriteValues(Action<HashTableRx> setValues)
        {
            if (hashTable is null || setValues is null)
            {
                return false;
            }

            if (hashTable.Tag?[nameof(RxTcAdsClient)] is not RxTcAdsClient plc || hashTable.Tag?["Variable"] is not string variable)
            {
                return false;
            }

            if (!plc.Connected)
            {
                return false;
            }

            using var clone = hashTable.CreateClone();
            setValues(clone);
            var structure =
                clone.Structure;
            if (structure is null)
            {
                return false;
            }

            plc.Write(variable, structure);
            return true;
        }

        /// <summary>Writes the values asynchronously.</summary>
        /// <param name="setValues">The set values.</param>
        /// <param name="time">The time to delay between writes.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation.</returns>
        [RequiresUnreferencedCode("May use reflection if the structure contains fields or properties.")]
        public async Task<bool> WriteValuesAsync(Action<HashTableRx> setValues, TimeSpan time)
        {
            if (hashTable is null || setValues is null)
            {
                return false;
            }

            if (hashTable.Tag?[nameof(RxTcAdsClient)] is not RxTcAdsClient plc || hashTable.Tag?["Variable"] is not string variable)
            {
                return false;
            }

            if (!plc.Connected)
            {
                return false;
            }

            if (plc.IsPaused)
            {
                var completion = new TaskCompletionSource<bool>();
                var subscription = plc.IsPausedObservable.SubscribeTo(isPaused =>
                {
                    if (isPaused)
                    {
                        return;
                    }

                    _ = completion.TrySetResult(true);
                });

                _ = await completion.Task.ConfigureAwait(false);
                subscription.Dispose();
            }
            else
            {
                plc.Pause(time);
            }

            using var clone = hashTable.CreateClone();
            setValues(clone);
            var structure =
                clone.Structure;

            if (structure is null)
            {
                return false;
            }

            plc.Write(variable, structure);
            return true;
        }

        /// <summary>Returns an observable that fires when the structure is ready.</summary>
        /// <returns>An observable when values have been set.</returns>
        /// <exception cref="ArgumentNullException">The HashTableRx cannot be null.</exception>
        public IObservable<HashTableRx> StructureReady()
        {
            if (hashTable is null)
            {
                throw new ArgumentNullException(nameof(hashTable));
            }

            return hashTable.ObserveAll.Where(_ => hashTable.Count > 0).Take(1).Delay(TimeSpan.FromSeconds(2)).Select(_ => hashTable);
        }

        /// <summary>Clones the specified HashTableRx.</summary>
        /// <returns>A HashTableRx.</returns>
        /// <exception cref="ArgumentNullException">The HashTableRx cannot be null.</exception>
        [RequiresUnreferencedCode("May use reflection if the structure contains fields or properties.")]
        public HashTableRx CreateClone()
        {
            if (hashTable is null)
            {
                throw new ArgumentNullException(nameof(hashTable));
            }

            var clone = new HashTableRx(hashTable.UseUpperCase);
            var structure = hashTable.Structure;
            if (structure is not null)
            {
                clone.SetStructure(structure);
            }

            return clone;
        }
    }

    /// <summary>Extends reactive TwinCAT clients with observation helpers.</summary>
    /// <param name="client">The reactive TwinCAT client.</param>
    extension(IRxTcAdsClient client)
    {
        /// <summary>Observes the specified variable.</summary>
        /// <typeparam name="T">The Type of the data.</typeparam>
        /// <param name="variable">The variable.</param>
        /// <returns>An observable of T.</returns>
        public IObservable<T> Observe<T>(string variable) =>
            client.DataReceived
                .Where(x => string.Equals(x.Variable, variable, StringComparison.OrdinalIgnoreCase) && x.Data is not null)
                .Select(x => (T)x.Data!)!;

        /// <summary>Observes the specified variable as an async observable.</summary>
        /// <typeparam name="T">The Type of the data.</typeparam>
        /// <param name="variable">The variable.</param>
        /// <returns>An async observable of T.</returns>
        public IObservableAsync<T> ObserveAsyncObservable<T>(string variable) =>
            client.Observe<T>(variable).ToAsyncObservable();

        /// <summary>Observes the specified variable.</summary>
        /// <typeparam name="T">The Type of the data.</typeparam>
        /// <param name="variable">The variable.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>An observable of T.</returns>
        public IObservable<T> Observe<T>(string variable, string id) =>
            client.DataReceived
                .Where(x => string.Equals(x.Id, id, StringComparison.Ordinal) && string.Equals(x.Variable, variable, StringComparison.OrdinalIgnoreCase) && x.Data is not null)
                .Select(x => (T)x.Data!)!;

        /// <summary>Observes the specified variable and identifier as an async observable.</summary>
        /// <typeparam name="T">The Type of the data.</typeparam>
        /// <param name="variable">The variable.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>An async observable of T.</returns>
        public IObservableAsync<T> ObserveAsyncObservable<T>(string variable, string id) =>
            client.Observe<T>(variable, id).ToAsyncObservable();

        /// <summary>Creates the structure.</summary>
        /// <param name="variable">The variable.</param>
        /// <returns>A HashTableRx with a link to the PLC.</returns>
        [RequiresUnreferencedCode("HashTableRx.SetStructure may use reflection over fields and properties.")]
        public HashTableRx? CreateStruct(string variable)
        {
            if (client is null)
            {
                return default;
            }

            var table = new HashTableRx(client.Settings?.Port < 851);
            table.Tag?.Add(nameof(RxTcAdsClient), client);
            table.Tag?.Add("Variable", variable);
            _ = client.DataReceived
                .Where(x => string.Equals(x.Variable, variable, StringComparison.OrdinalIgnoreCase) && x.Data is not null)
                .SubscribeTo(x => table.SetStructure(x.Data!));
            return table;
        }
    }
}
