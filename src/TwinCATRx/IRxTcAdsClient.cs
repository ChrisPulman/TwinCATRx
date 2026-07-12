// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
#if REACTIVE_SHIM
using CP.TwinCatRx.Core.Reactive;
#else
using CP.TwinCatRx.Core;
#endif

#if REACTIVE_SHIM
namespace CP.TwinCatRx.Reactive;
#else
namespace CP.TwinCatRx;
#endif

/// <summary>Interface for Rx Tc Ads Client.</summary>
/// <seealso cref="IDisposable"/>
public interface IRxTcAdsClient : IDisposable
{
    /// <summary>Gets the code.</summary>
    /// <value>The code.</value>
    IObservable<string[]> Code { get; }

    /// <summary>Gets the initialize complete. PLC is ready to read and write.</summary>
    /// <value>
    /// The initialize complete.
    /// </value>
    IObservable<Unit> InitializeComplete { get; }

    /// <summary>Gets the async initialize complete stream.</summary>
    IObservableAsync<Unit> InitializeCompleteAsync { get; }

    /// <summary>Gets the data received.</summary>
    /// <value>The data received.</value>
    IObservable<(string Variable, object? Data, string? Id)> DataReceived { get; }

    /// <summary>Gets the async data received stream.</summary>
    IObservableAsync<(string Variable, object? Data, string? Id)> DataReceivedAsync { get; }

    /// <summary>Gets the error received.</summary>
    /// <value>The error received.</value>
    IObservable<Exception> ErrorReceived { get; }

    /// <summary>Gets the async error received stream.</summary>
    IObservableAsync<Exception> ErrorReceivedAsync { get; }

    /// <summary>Gets the on write.</summary>
    /// <value>The on write.</value>
    IObservable<string?> OnWrite { get; }

    /// <summary>Gets the async write result stream.</summary>
    IObservableAsync<string?> OnWriteAsync { get; }

    /// <summary>Gets the read write handle information.</summary>
    /// <value>The read write handle information.</value>
    IDictionary<string, uint?> ReadWriteHandleInfo { get; }

    /// <summary>Gets the settings.</summary>
    /// <value>
    /// The settings.
    /// </value>
    ISettings? Settings { get; }

    /// <summary>Gets the write handle information.</summary>
    /// <value>The write handle information.</value>
    IDictionary<string, (uint? Handle, int ArrayLength)> WriteHandleInfo { get; }

    /// <summary>Gets a value indicating whether this instance is paused within WriteValuesAsync.</summary>
    /// <value>
    ///   <c>true</c> if this instance is paused; otherwise, <c>false</c>.
    /// </value>
    bool IsPaused { get; }

    /// <summary>Gets the is paused observable.</summary>
    /// <value>
    /// The is paused observable.
    /// </value>
    IObservable<bool> IsPausedObservable { get; }

    /// <summary>Gets the async paused state stream.</summary>
    IObservableAsync<bool> IsPausedObservableAsync { get; }

    /// <summary>Gets a value indicating whether the instance is disposed.</summary>
    bool IsDisposed { get; }

    /// <summary>Pauses the specified time.</summary>
    /// <param name="time">The time.</param>
    void Pause(TimeSpan time);

    /// <summary>Connects the specified settings.</summary>
    /// <param name="settings">The settings.</param>
    [RequiresUnreferencedCode("Invokes dynamic code generation and reflection to materialize PLC types.")]
    [RequiresDynamicCode("Invokes dynamic code generation and reflection to materialize PLC types.")]
    void Connect(ISettings settings);

    /// <summary>Disconnects this instance.</summary>
    void Disconnect();

    /// <summary>Reads the specified data.</summary>
    /// <param name="variable">The data.</param>
    /// <param name="arrayLength">Length of the array.</param>
    /// <param name="id">The identifier.</param>
    void Read(string variable, int? arrayLength = null, string? id = null);

    /// <summary>Writes the specified value.</summary>
    /// <param name="variable">The variable.</param>
    /// <param name="value">The value.</param>
    /// <param name="id">The identifier.</param>
    void Write(string variable, object value, string? id = null);
}
