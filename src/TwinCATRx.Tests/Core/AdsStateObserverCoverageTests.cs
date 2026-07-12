// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using TwinCAT.Ads;
using LeanCoreExtensions = CP.TwinCatRx.Core.TwinCatRxExtensions;
using ReactiveCoreExtensions = CP.TwinCatRx.Core.Reactive.TwinCatRxExtensions;

namespace TwinCATRx.Tests.Core;

/// <summary>Exercises ADS state observables without opening an ADS connection.</summary>
public class AdsStateObserverCoverageTests
{
    /// <summary>The maximum time allowed for the timer's immediate callback.</summary>
    private const int ObserverTimeoutSeconds = 2;

    /// <summary>Subscribes and unsubscribes the lean event wrapper on a disconnected client.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Lean_AdsStateChangedObserver_Supports_Disconnected_Subscription()
    {
        using var client = new AdsClient();
        using var subscription = System.ObservableExtensions.Subscribe(
            LeanCoreExtensions.AdsStateChangedObserver(client),
            _ => { });

        await TUnitAssert.That(subscription).IsNotNull();
    }

    /// <summary>Subscribes and unsubscribes the Reactive event wrapper on a disconnected client.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Reactive_AdsStateChangedObserver_Supports_Disconnected_Subscription()
    {
        using var client = new AdsClient();
        using var subscription = System.ObservableExtensions.Subscribe(
            ReactiveCoreExtensions.AdsStateChangedObserver(client),
            _ => { });

        await TUnitAssert.That(subscription).IsNotNull();
    }

    /// <summary>Verifies that the lean polling wrapper reports an invalid state while disconnected.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public Task Lean_AdsStateObserver_Reports_Invalid_While_Disconnected() =>
        AssertDisconnectedState(LeanCoreExtensions.AdsStateObserver);

    /// <summary>Verifies that the Reactive polling wrapper reports an invalid state while disconnected.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public Task Reactive_AdsStateObserver_Reports_Invalid_While_Disconnected() =>
        AssertDisconnectedState(ReactiveCoreExtensions.AdsStateObserver);

    /// <summary>Asserts the first state produced by a disconnected ADS client.</summary>
    /// <param name="createObservable">Creates the state observable for the surface under test.</param>
    /// <returns>The assertion task.</returns>
    private static async Task AssertDisconnectedState(Func<AdsClient, IObservable<StateInfo>> createObservable)
    {
        using var client = new AdsClient();
        var completion = new TaskCompletionSource<StateInfo>();
        void SetResult(StateInfo state) => _ = completion.TrySetResult(state);
        void SetException(Exception exception) => _ = completion.TrySetException(exception);
        var firstState = System.Reactive.Linq.Observable.Take(createObservable(client), 1);
        using var subscription = System.ObservableExtensions.Subscribe(
            firstState,
            SetResult,
            SetException);

        var timeout = Task.Delay(TimeSpan.FromSeconds(ObserverTimeoutSeconds));
        var completed = await Task.WhenAny(completion.Task, timeout);

        await TUnitAssert.That(completed).IsSameReferenceAs(completion.Task);
        await TUnitAssert.That((await completion.Task).AdsState).IsEqualTo(AdsState.Invalid);
    }
}
