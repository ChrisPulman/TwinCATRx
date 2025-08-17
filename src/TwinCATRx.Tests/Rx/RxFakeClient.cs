// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using CP.TwinCatRx;
using CP.TwinCatRx.Core;

namespace TwinCATRx.Tests.Rx;

/// <summary>
/// Simple fake implementation of IRxTcAdsClient for testing extensions.
/// </summary>
internal sealed class RxFakeClient : IRxTcAdsClient
{
    private bool _canceled;

    public RxFakeClient(IObservable<(string Variable, object? Data, string? Id)> data)
    {
        DataReceived = data;
        Settings = new Settings { Port = 851, AdsAddress = string.Empty, SettingsId = "Default" };
    }

    public IObservable<string[]> Code => Observable.Empty<string[]>();

    public IObservable<System.Reactive.Unit> InitializeComplete => Observable.Return(System.Reactive.Unit.Default);

    public IObservable<(string Variable, object? Data, string? Id)> DataReceived { get; }

    public IObservable<Exception> ErrorReceived => Observable.Empty<Exception>();

    public IObservable<string?> OnWrite => Observable.Empty<string?>();

    public IDictionary<string, uint?> ReadWriteHandleInfo { get; } = new Dictionary<string, uint?>();

    public ISettings? Settings { get; private set; }

    public IDictionary<string, (uint? Handle, int ArrayLength)> WriteHandleInfo { get; } = new Dictionary<string, (uint? Handle, int ArrayLength)>();

    public bool IsPaused { get; private set; }

    public IObservable<bool> IsPausedObservable => Observable.Return(IsPaused);

    public bool IsDisposed => _canceled;

    public bool IsCancellationRequested => _canceled;

    public void Pause(TimeSpan time) => IsPaused = true;

    public void Connect(ISettings settings) => Settings = settings;

    public void Disconnect()
    {
    }

    public void Read(string variable, int? arrayLength = null, string? id = null)
    {
    }

    public void Write(string variable, object value, string? id = null)
    {
    }

    public void Dispose() => _canceled = true;

    public void Cancel() => _canceled = true;
}
