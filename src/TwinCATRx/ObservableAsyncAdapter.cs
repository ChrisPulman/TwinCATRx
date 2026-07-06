// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Async;

namespace CP.TwinCatRx;

/// <summary>Adapts an observable sequence to the async observable contract.</summary>
/// <typeparam name="T">The value type.</typeparam>
internal sealed class ObservableAsyncAdapter<T> : IObservableAsync<T>
{
    /// <summary>Stores the source observable sequence.</summary>
    private readonly IObservable<T> _source;

    /// <summary>Initializes a new instance of the <see cref="ObservableAsyncAdapter{T}"/> class.</summary>
    /// <param name="source">The source observable sequence.</param>
    public ObservableAsyncAdapter(IObservable<T> source) => _source = source;

    /// <summary>Subscribes the async observer to the adapted observable sequence.</summary>
    /// <param name="observer">The async observer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The async subscription.</returns>
    public ValueTask<IAsyncDisposable> SubscribeAsync(IObserverAsync<T> observer, CancellationToken cancellationToken)
    {
        if (observer is null)
        {
            throw new ArgumentNullException(nameof(observer));
        }

        var subscription = new ObservableAsyncSubscription<T>(observer, cancellationToken);
        subscription.SetSourceSubscription(_source.Subscribe(subscription));
        return new ValueTask<IAsyncDisposable>(subscription);
    }
}
