// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Disposables;
using CP.TwinCatRx.Core;

namespace CP.TwinCatRx;

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
    /// Gets the initialize complete. PLC is ready to read and write.
    /// </summary>
    /// <value>
    /// The initialize complete.
    /// </value>
    IObservable<Unit> InitializeComplete { get; }

    /// <summary>
    /// Gets the data received.
    /// </summary>
    /// <value>The data received.</value>
    IObservable<(string Variable, object? Data, string? Id)> DataReceived { get; }

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
    /// Gets the settings.
    /// </summary>
    /// <value>
    /// The settings.
    /// </value>
    ISettings? Settings { get; }

    /// <summary>
    /// Gets the write handle information.
    /// </summary>
    /// <value>The write handle information.</value>
    IDictionary<string, (uint? Handle, int ArrayLength)> WriteHandleInfo { get; }

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
    /// <param name="variable">The data.</param>
    /// <param name="arrayLength">Length of the array.</param>
    /// <param name="id">The identifier.</param>
    void Read(string variable, int? arrayLength = null, string? id = null);

    /// <summary>
    /// Writes the specified value.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <param name="value">The value.</param>
    /// <param name="id">The identifier.</param>
    void Write(string variable, object value, string? id = null);
}
