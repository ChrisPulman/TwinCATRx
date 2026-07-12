// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.ExceptionServices;
using ReactiveUI.Primitives.Async;

#if REACTIVE_SHIM
namespace CP.TwinCatRx.Reactive;
#else
namespace CP.TwinCatRx;
#endif

/// <summary>Observable bridge helpers.</summary>
public static class ObservableBridgeExtensions
{
    /// <summary>Extends observable sequences with bridge and subscription helpers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="source">The source observable.</param>
    extension<T>(IObservable<T> source)
    {
        /// <summary>Converts an observable sequence to a ReactiveUI.Primitives async observable sequence.</summary>
        /// <returns>An async observable that subscribes to the source observable.</returns>
        public IObservableAsync<T> ToAsyncObservable()
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ObservableAsyncAdapter<T>(source);
        }

        /// <summary>Subscribes to an observable sequence without handling values.</summary>
        /// <returns>The subscription.</returns>
        public IDisposable SubscribeTo() => source.SubscribeTo(static _ => { });

        /// <summary>Subscribes to an observable sequence using an action for values.</summary>
        /// <param name="onNext">The value handler.</param>
        /// <returns>The subscription.</returns>
        public IDisposable SubscribeTo(Action<T> onNext) => source.SubscribeTo(onNext, ThrowObservableError, static () => { });

        /// <summary>Subscribes to an observable sequence using actions for all notifications.</summary>
        /// <param name="onNext">The value handler.</param>
        /// <param name="onError">The error handler.</param>
        /// <param name="onCompleted">The completion handler.</param>
        /// <returns>The subscription.</returns>
        public IDisposable SubscribeTo(Action<T> onNext, Action<Exception> onError, Action onCompleted)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (onNext is null)
            {
                throw new ArgumentNullException(nameof(onNext));
            }

            if (onError is null)
            {
                throw new ArgumentNullException(nameof(onError));
            }

            if (onCompleted is null)
            {
                throw new ArgumentNullException(nameof(onCompleted));
            }

            return source.Subscribe(new ActionObserver<T>(onNext, onError, onCompleted));
        }
    }

    /// <summary>Throws an observable error while preserving the original stack trace.</summary>
    /// <param name="error">The observable error.</param>
    private static void ThrowObservableError(Exception error) => ExceptionDispatchInfo.Capture(error).Throw();
}
