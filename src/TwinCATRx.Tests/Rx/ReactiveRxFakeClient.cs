// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI.Primitives.Async;
using ReactiveBridge = CP.TwinCatRx.Reactive.ObservableBridgeExtensions;
using ReactiveClient = CP.TwinCatRx.Reactive.IRxTcAdsClient;
using ReactiveSettings = CP.TwinCatRx.Core.Reactive.ISettings;
using ReactiveUnit = System.Reactive.Unit;
using Settings = CP.TwinCatRx.Core.Reactive.Settings;

namespace TwinCATRx.Tests.Rx;

/// <summary>Deterministic System.Reactive fake used to exercise the Reactive package.</summary>
internal sealed class ReactiveRxFakeClient : ReactiveClient
{
    /// <summary>The default TwinCAT 3 ADS port.</summary>
    private const int TwinCat3Port = 851;

    /// <summary>Stores write notifications.</summary>
    private readonly Subject<string?> _onWrite = new();

    /// <summary>Stores whether the client has been disposed.</summary>
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="ReactiveRxFakeClient"/> class.</summary>
    /// <param name="data">The deterministic data stream.</param>
    public ReactiveRxFakeClient(IObservable<(string Variable, object? Data, string? Id)> data)
    {
        DataReceived = data;
        Settings = new Settings { Port = TwinCat3Port, AdsAddress = string.Empty, SettingsId = "ReactiveFake" };
    }

    /// <inheritdoc/>
    public IObservable<string[]> Code => Observable.Empty<string[]>();

    /// <inheritdoc/>
    public IObservable<ReactiveUnit> InitializeComplete => System.Reactive.Linq.Observable.Return(ReactiveUnit.Default);

    /// <inheritdoc/>
    public IObservableAsync<ReactiveUnit> InitializeCompleteAsync => ReactiveBridge.ToAsyncObservable(InitializeComplete);

    /// <inheritdoc/>
    public IObservable<(string Variable, object? Data, string? Id)> DataReceived { get; }

    /// <inheritdoc/>
    public IObservableAsync<(string Variable, object? Data, string? Id)> DataReceivedAsync => ReactiveBridge.ToAsyncObservable(DataReceived);

    /// <inheritdoc/>
    public IObservable<Exception> ErrorReceived => Observable.Empty<Exception>();

    /// <inheritdoc/>
    public IObservableAsync<Exception> ErrorReceivedAsync => ReactiveBridge.ToAsyncObservable(ErrorReceived);

    /// <inheritdoc/>
    public IObservable<string?> OnWrite => _onWrite;

    /// <inheritdoc/>
    public IObservableAsync<string?> OnWriteAsync => ReactiveBridge.ToAsyncObservable(OnWrite);

    /// <inheritdoc/>
    public IDictionary<string, uint?> ReadWriteHandleInfo { get; } = new Dictionary<string, uint?>();

    /// <inheritdoc/>
    public ReactiveSettings? Settings { get; private set; }

    /// <inheritdoc/>
    public IDictionary<string, (uint? Handle, int ArrayLength)> WriteHandleInfo { get; } = new Dictionary<string, (uint? Handle, int ArrayLength)>();

    /// <summary>Gets recorded read calls.</summary>
    public List<(string Variable, int? ArrayLength, string? Id)> ReadCalls { get; } = [];

    /// <summary>Gets recorded write calls.</summary>
    public List<(string Variable, object Value, string? Id)> WriteCalls { get; } = [];

    /// <inheritdoc/>
    public bool IsPaused { get; private set; }

    /// <inheritdoc/>
    public IObservable<bool> IsPausedObservable => Observable.Return(IsPaused);

    /// <inheritdoc/>
    public IObservableAsync<bool> IsPausedObservableAsync => ReactiveBridge.ToAsyncObservable(IsPausedObservable);

    /// <inheritdoc/>
    public bool IsDisposed => _disposed;

    /// <inheritdoc/>
    public void Pause(TimeSpan time) => IsPaused = true;

    /// <inheritdoc/>
    public void Connect(ReactiveSettings settings) => Settings = settings;

    /// <inheritdoc/>
    public void Disconnect()
    {
    }

    /// <inheritdoc/>
    public void Read(string variable, int? arrayLength = null, string? id = null) => ReadCalls.Add((variable, arrayLength, id));

    /// <inheritdoc/>
    public void Write(string variable, object value, string? id = null)
    {
        WriteCalls.Add((variable, value, id));
        _onWrite.OnNext(string.IsNullOrWhiteSpace(id) ? "Success" : "Success," + id);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposed = true;
        _onWrite.Dispose();
    }
}
