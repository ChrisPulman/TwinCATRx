// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace CP.TwinCatRx;

/// <summary>Adapts notification actions to the observable observer contract.</summary>
/// <typeparam name="T">The value type.</typeparam>
internal sealed class ActionObserver<T> : IObserver<T>
{
    /// <summary>Stores the completion handler.</summary>
    private readonly Action _onCompleted;

    /// <summary>Stores the error handler.</summary>
    private readonly Action<Exception> _onError;

    /// <summary>Stores the value handler.</summary>
    private readonly Action<T> _onNext;

    /// <summary>Initializes a new instance of the <see cref="ActionObserver{T}"/> class.</summary>
    /// <param name="onNext">The value handler.</param>
    /// <param name="onError">The error handler.</param>
    /// <param name="onCompleted">The completion handler.</param>
    public ActionObserver(Action<T> onNext, Action<Exception> onError, Action onCompleted)
    {
        _onNext = onNext;
        _onError = onError;
        _onCompleted = onCompleted;
    }

    /// <summary>Notifies that the observable sequence completed.</summary>
    public void OnCompleted() => _onCompleted();

    /// <summary>Notifies that the observable sequence failed.</summary>
    /// <param name="error">The observable error.</param>
    public void OnError(Exception error) => _onError(error);

    /// <summary>Notifies that the observable sequence produced a value.</summary>
    /// <param name="value">The observable value.</param>
    public void OnNext(T value) => _onNext(value);
}
