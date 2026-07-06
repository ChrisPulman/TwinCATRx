// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using CP.TwinCatRx;
using CP.TwinCatRx.Core;

namespace TwinCATRx.Tests.Rx;

/// <summary>Simple fake implementation of IRxTcAdsClient for testing extensions.</summary>
internal sealed class RxFakeClient : IRxTcAdsClient
{
    /// <summary>Stores fake write notifications.</summary>
    private readonly Signal<string?> _onWrite = new();

    /// <summary>Tracks whether the fake client has been canceled or disposed.</summary>
    private bool _canceled;

    /// <summary>Initializes a new instance of the <see cref="RxFakeClient"/> class.</summary>
    /// <param name="data">The data stream.</param>
    public RxFakeClient(IObservable<(string Variable, object? Data, string? Id)> data)
    {
        DataReceived = data;
        Settings = new Settings { Port = 851, AdsAddress = string.Empty, SettingsId = "Default" };
    }

    /// <inheritdoc/>
    public IObservable<string[]> Code => Observable.Empty<string[]>();

    /// <inheritdoc/>
    public IObservable<Unit> InitializeComplete => Observable.Return(Unit.Default);

    /// <inheritdoc/>
    public IObservableAsync<Unit> InitializeCompleteAsync => InitializeComplete.ToAsyncObservable();

    /// <inheritdoc/>
    public IObservable<(string Variable, object? Data, string? Id)> DataReceived { get; }

    /// <inheritdoc/>
    public IObservableAsync<(string Variable, object? Data, string? Id)> DataReceivedAsync => DataReceived.ToAsyncObservable();

    /// <inheritdoc/>
    public IObservable<Exception> ErrorReceived => Observable.Empty<Exception>();

    /// <inheritdoc/>
    public IObservableAsync<Exception> ErrorReceivedAsync => ErrorReceived.ToAsyncObservable();

    /// <inheritdoc/>
    public IObservable<string?> OnWrite => _onWrite;

    /// <inheritdoc/>
    public IObservableAsync<string?> OnWriteAsync => OnWrite.ToAsyncObservable();

    /// <inheritdoc/>
    public IDictionary<string, uint?> ReadWriteHandleInfo { get; } = new Dictionary<string, uint?>();

    /// <inheritdoc/>
    public ISettings? Settings { get; private set; }

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
    public IObservableAsync<bool> IsPausedObservableAsync => IsPausedObservable.ToAsyncObservable();

    /// <inheritdoc/>
    public bool IsDisposed => _canceled;

    /// <summary>Gets a value indicating whether cancellation was requested.</summary>
    public bool IsCancellationRequested => _canceled;

    /// <inheritdoc/>
    public void Pause(TimeSpan time) => IsPaused = true;

    /// <inheritdoc/>
    public void Connect(ISettings settings) => Settings = settings;

    /// <inheritdoc/>
    public void Disconnect()
    {
    }

    /// <inheritdoc/>
    public void Read(string variable, int? arrayLength = null, string? id = null)
    {
        ReadCalls.Add((variable, arrayLength, id));
    }

    /// <inheritdoc/>
    public void Write(string variable, object value, string? id = null)
    {
        WriteCalls.Add((variable, value, id));
        _onWrite.OnNext(string.IsNullOrWhiteSpace(id) ? "Success" : "Success," + id);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _canceled = true;
        _onWrite.Dispose();
    }

    /// <summary>Cancels the fake client.</summary>
    public void Cancel() => _canceled = true;
}
