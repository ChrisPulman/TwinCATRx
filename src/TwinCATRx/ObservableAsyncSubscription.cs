// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Async;

#if REACTIVE_SHIM
namespace CP.TwinCatRx.Reactive;
#else
namespace CP.TwinCatRx;
#endif

/// <summary>Coordinates an observable subscription with async observer callbacks.</summary>
/// <typeparam name="T">The value type.</typeparam>
internal sealed class ObservableAsyncSubscription<T> : IObserver<T>, IAsyncDisposable
{
    /// <summary>Stores the cancellation token registration.</summary>
    private readonly CancellationTokenRegistration _registration;

    /// <summary>Serializes async observer calls.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Stores the async observer.</summary>
    private readonly IObserverAsync<T> _observer;

    /// <summary>Signals disposal to pending observer calls.</summary>
    private readonly CancellationTokenSource _source = new();

    /// <summary>Stores whether disposal has already happened.</summary>
    private int _disposed;

    /// <summary>Stores the upstream observable subscription.</summary>
    private IDisposable? _sourceSubscription;

    /// <summary>Initializes a new instance of the <see cref="ObservableAsyncSubscription{T}"/> class.</summary>
    /// <param name="observer">The async observer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public ObservableAsyncSubscription(IObserverAsync<T> observer, CancellationToken cancellationToken)
    {
        _observer = observer;
        if (!cancellationToken.CanBeCanceled)
        {
            return;
        }

        _registration = cancellationToken.Register(Dispose);
    }

    /// <summary>Sets the upstream observable subscription.</summary>
    /// <param name="sourceSubscription">The upstream observable subscription.</param>
    public void SetSourceSubscription(IDisposable sourceSubscription)
    {
        _sourceSubscription = sourceSubscription;
        if (Volatile.Read(ref _disposed) == 0)
        {
            return;
        }

        sourceSubscription.Dispose();
    }

    /// <summary>Notifies the async observer that the sequence completed.</summary>
    public void OnCompleted() => InvokeObserver(() => _observer.OnCompletedAsync(Result.Success));

    /// <summary>Notifies the async observer that the sequence failed.</summary>
    /// <param name="error">The observable error.</param>
    public void OnError(Exception error) => InvokeObserver(() => _observer.OnErrorResumeAsync(error, _source.Token));

    /// <summary>Notifies the async observer that the sequence produced a value.</summary>
    /// <param name="value">The observable value.</param>
    public void OnNext(T value) => InvokeObserver(() => _observer.OnNextAsync(value, _source.Token));

    /// <summary>Disposes the async subscription.</summary>
    /// <returns>The completed disposal operation.</returns>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }

    /// <summary>Disposes the subscription resources.</summary>
    private void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _source.Cancel();
        _sourceSubscription?.Dispose();
        _registration.Dispose();
        _gate.Dispose();
        _source.Dispose();
    }

    /// <summary>Invokes the async observer callback synchronously under the subscription gate.</summary>
    /// <param name="callback">The observer callback.</param>
    private void InvokeObserver(Func<ValueTask> callback)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        try
        {
            _gate.Wait(_source.Token);
            try
            {
                if (!_source.IsCancellationRequested)
                {
                    callback().AsTask().GetAwaiter().GetResult();
                }
            }
            finally
            {
                _ = _gate.Release();
            }
        }
        catch (OperationCanceledException) when (_source.IsCancellationRequested)
        {
        }
    }
}
