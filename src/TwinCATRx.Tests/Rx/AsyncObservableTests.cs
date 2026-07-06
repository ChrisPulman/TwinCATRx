// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using CP.TwinCatRx;

namespace TwinCATRx.Tests.Rx;

/// <summary>Tests async observable bridge behavior.</summary>
public class AsyncObservableTests
{
    /// <summary>Verifies async observable observation over a classic stream.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task ObserveAsync_Bridges_Classic_Stream()
    {
        var data = new[]
        {
            (Variable: ".A", Data: (object?)123, Id: (string?)null),
            (Variable: ".B", Data: (object?)456, Id: (string?)null),
        };
        var client = new RxFakeClient(Observable.FromEnumerable(data));

        var values = new List<int>();
        await using var subscription = await client.ObserveAsyncObservable<int>(".A").SubscribeAsync(
            new TestObserverAsync<int>(values.Add),
            CancellationToken.None);

        await TUnitAssert.That(values.Count).IsEqualTo(1);
        await TUnitAssert.That(values[0]).IsEqualTo(123);
    }

    /// <summary>Async observer used by bridge tests.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="onNext">The value handler.</param>
    private sealed class TestObserverAsync<T>(Action<T> onNext) : IObserverAsync<T>
    {
        /// <inheritdoc/>
        public ValueTask DisposeAsync() => default;

        /// <inheritdoc/>
        public ValueTask OnCompletedAsync(Result result) => default;

        /// <inheritdoc/>
        public ValueTask OnErrorResumeAsync(Exception error, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Async observer received an error.", error);

        /// <inheritdoc/>
        public ValueTask OnNextAsync(T value, CancellationToken cancellationToken)
        {
            onNext(value);
            return default;
        }
    }
}
