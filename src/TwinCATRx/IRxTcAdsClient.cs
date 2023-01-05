// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using CP.TwinCATRx.Core;

namespace CP.TwinCatRx
{
    /// <summary>
    /// interface for Rx Tc Ads Client.
    /// </summary>
    /// <seealso cref="IDisposable"/>
    public interface IRxTcAdsClient : ICancelable
    {
        /// <summary>
        /// Gets the code.
        /// </summary>
        /// <value>The code.</value>
        IObservable<string[]> Code { get; }

        /// <summary>
        /// Gets the data received.
        /// </summary>
        /// <value>The data received.</value>
        IObservable<(string Variable, object? Data)> DataReceived { get; }

        /// <summary>
        /// Gets the error received.
        /// </summary>
        /// <value>The error received.</value>
        IObservable<Exception> ErrorReceived { get; }

        /// <summary>
        /// Gets the on write.
        /// </summary>
        /// <value>The on write.</value>
        IObservable<string?> OnWrite { get; }

        /// <summary>
        /// Gets the read write handle information.
        /// </summary>
        /// <value>The read write handle information.</value>
        IDictionary<string, uint?> ReadWriteHandleInfo { get; }

        /// <summary>
        /// Gets the write handle information.
        /// </summary>
        /// <value>The write handle information.</value>
        IDictionary<string, uint?> WriteHandleInfo { get; }

        /// <summary>
        /// Connects the specified settings.
        /// </summary>
        /// <param name="settings">The settings.</param>
        void Connect(ISettings settings);

        /// <summary>
        /// Disconnects this instance.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Reads the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="parameters">The parameters.</param>
        void Read(string data, string parameters);

        /// <summary>
        /// Reads the array.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="length">The length.</param>
        void ReadArray(string data, int length);

        /// <summary>
        /// Writes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        void Write(string value);
    }
}
