// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using CP.TwinCatRx;

namespace TwinCATRx.Tests.Rx;

/// <summary>Deterministic coverage tests for the lean observable bridge.</summary>
public class LeanObservableBridgeCoverageTests
{
    /// <summary>The value forwarded through the synchronous bridge.</summary>
    private const int ForwardedValue = 42;

    /// <summary>The value forwarded through the async bridge.</summary>
    private const int AsyncValue = 7;

    /// <summary>The value ignored after async disposal.</summary>
    private const int IgnoredValue = 8;

    /// <summary>The value ignored after cancellation.</summary>
    private const int CanceledValue = 3;

    /// <summary>The expected test error message.</summary>
    private const string ExpectedErrorMessage = "expected";

    /// <summary>Verifies all synchronous observer callbacks are forwarded.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task SubscribeTo_Forwards_All_Notifications()
    {
        var source = new ManualObservable<int>();
        var ignoredSource = new ManualObservable<int>();
        var values = new List<int>();
        Exception? observedError = null;
        var completed = false;
        var expectedError = new InvalidOperationException(ExpectedErrorMessage);

        using var subscription = source.SubscribeTo(values.Add, error => observedError = error, () => completed = true);
        using var ignoredSubscription = ignoredSource.SubscribeTo();
        source.OnNext(ForwardedValue);
        ignoredSource.OnNext(IgnoredValue);
        source.OnError(expectedError);
        source.OnCompleted();

        await TUnitAssert.That(values).IsEquivalentTo([ForwardedValue]);
        await TUnitAssert.That(ReferenceEquals(observedError, expectedError)).IsTrue();
        await TUnitAssert.That(completed).IsTrue();
    }

    /// <summary>Verifies bridge overloads reject null arguments.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Bridge_Overloads_Reject_Null_Arguments()
    {
        IObservable<int> source = new ManualObservable<int>();

        await TUnitAssert.That(() => ((IObservable<int>)null!).ToAsyncObservable()).Throws<ArgumentNullException>();
        await TUnitAssert.That(() => ((IObservable<int>)null!).SubscribeTo(_ => { }, _ => { }, () => { })).Throws<ArgumentNullException>();
        await TUnitAssert.That(() => source.SubscribeTo(null!, _ => { }, () => { })).Throws<ArgumentNullException>();
        await TUnitAssert.That(() => source.SubscribeTo(_ => { }, null!, () => { })).Throws<ArgumentNullException>();
        await TUnitAssert.That(() => source.SubscribeTo(_ => { }, _ => { }, null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies the value-only overload preserves source errors.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task SubscribeTo_ValueOnly_Rethrows_Source_Error()
    {
        var source = new ManualObservable<int>();
        var expected = new InvalidOperationException(ExpectedErrorMessage);
        using var subscription = source.SubscribeTo(_ => { });

        await TUnitAssert.That(() => source.OnError(expected)).Throws<InvalidOperationException>();
    }

    /// <summary>Verifies async bridge notifications and idempotent disposal.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Async_Bridge_Forwards_Notifications_And_Disposes_Upstream()
    {
        var source = new ManualObservable<int>();
        var observer = new RecordingAsyncObserver<int>();
        var expectedError = new InvalidOperationException(ExpectedErrorMessage);
        var subscription = await source.ToAsyncObservable().SubscribeAsync(observer, CancellationToken.None);

        source.OnNext(AsyncValue);
        source.OnError(expectedError);
        source.OnCompleted();

        await TUnitAssert.That(observer.Values).IsEquivalentTo([AsyncValue]);
        await TUnitAssert.That(ReferenceEquals(observer.Error, expectedError)).IsTrue();
        await TUnitAssert.That(observer.CompletionCount).IsEqualTo(1);

        await subscription.DisposeAsync();
        await subscription.DisposeAsync();
        source.OnNext(IgnoredValue);

        await TUnitAssert.That(source.SubscriptionDisposed).IsTrue();
        await TUnitAssert.That(observer.Values).IsEquivalentTo([AsyncValue]);
    }

    /// <summary>Verifies cancellation disposes the upstream subscription and suppresses callbacks.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Async_Bridge_Cancellation_Disposes_And_Suppresses_Callbacks()
    {
        var source = new ManualObservable<int>();
        var observer = new RecordingAsyncObserver<int>();
        using var cancellation = new CancellationTokenSource();
        var subscription = await source.ToAsyncObservable().SubscribeAsync(observer, cancellation.Token);

        await CancelAsync(cancellation);
        source.OnNext(CanceledValue);
        await subscription.DisposeAsync();

        await TUnitAssert.That(source.SubscriptionDisposed).IsTrue();
        await TUnitAssert.That(observer.Values).IsEmpty();
    }

    /// <summary>Verifies a token canceled before subscription immediately disposes the upstream subscription.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Async_Bridge_PreCanceled_Token_Disposes_New_Upstream_Subscription()
    {
        var source = new ManualObservable<int>();
        var observer = new RecordingAsyncObserver<int>();
        using var cancellation = new CancellationTokenSource();
        await CancelAsync(cancellation);

        var subscription = await source.ToAsyncObservable().SubscribeAsync(observer, cancellation.Token);
        await subscription.DisposeAsync();

        await TUnitAssert.That(source.SubscriptionDisposed).IsTrue();
    }

    /// <summary>Verifies async subscription rejects a null observer.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Async_Bridge_Rejects_Null_Observer()
    {
        var source = new ManualObservable<int>();

        await TUnitAssert.That(async () => await source.ToAsyncObservable().SubscribeAsync(null!, CancellationToken.None)).Throws<ArgumentNullException>();
    }

    /// <summary>Cancels a token source without blocking on supported target frameworks.</summary>
    /// <param name="source">The token source.</param>
    /// <returns>The cancellation task.</returns>
    private static async Task CancelAsync(CancellationTokenSource source)
    {
#if NET9_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
        await Task.CompletedTask;
#endif
    }

    /// <summary>Manually controlled observable used to avoid external dependencies.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    private sealed class ManualObservable<T> : IObservable<T>
    {
        /// <summary>Stores the current observer.</summary>
        private IObserver<T>? _observer;

        /// <summary>Gets a value indicating whether the source subscription was disposed.</summary>
        public bool SubscriptionDisposed { get; private set; }

        /// <summary>Sends a completion notification.</summary>
        public void OnCompleted() => _observer?.OnCompleted();

        /// <summary>Sends an error notification.</summary>
        /// <param name="error">The error.</param>
        public void OnError(Exception error) => _observer?.OnError(error);

        /// <summary>Sends a value notification.</summary>
        /// <param name="value">The value.</param>
        public void OnNext(T value) => _observer?.OnNext(value);

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            _observer = observer;
            return new CallbackDisposable(() => SubscriptionDisposed = true);
        }
    }

    /// <summary>Records async observer callbacks.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    private sealed class RecordingAsyncObserver<T> : IObserverAsync<T>
    {
        /// <summary>Gets the completion callback count.</summary>
        public int CompletionCount { get; private set; }

        /// <summary>Gets the last observed error.</summary>
        public Exception? Error { get; private set; }

        /// <summary>Gets observed values.</summary>
        public List<T> Values { get; } = [];

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => default;

        /// <inheritdoc/>
        public ValueTask OnCompletedAsync(Result result)
        {
            CompletionCount++;
            return default;
        }

        /// <inheritdoc/>
        public ValueTask OnErrorResumeAsync(Exception error, CancellationToken cancellationToken)
        {
            Error = error;
            return default;
        }

        /// <inheritdoc/>
        public ValueTask OnNextAsync(T value, CancellationToken cancellationToken)
        {
            Values.Add(value);
            return default;
        }
    }

    /// <summary>Disposable that invokes a callback once.</summary>
    /// <param name="dispose">The disposal callback.</param>
    private sealed class CallbackDisposable(Action dispose) : IDisposable
    {
        /// <summary>Tracks disposal.</summary>
        private int _disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            dispose();
        }
    }
}
