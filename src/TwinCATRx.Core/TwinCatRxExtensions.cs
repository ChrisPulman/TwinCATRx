// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TwinCAT.Ads;

#if REACTIVE_SHIM
namespace CP.TwinCatRx.Core.Reactive;
#else
namespace CP.TwinCatRx.Core;
#endif

/// <summary>Observable TwinCAT extensions.</summary>
public static class TwinCatRxExtensions
{
    /// <summary>Extends ADS clients with observable state helpers.</summary>
    /// <param name="client">The ADS client.</param>
    extension(AdsClient client)
    {
        /// <summary>Observes ADS state changed events.</summary>
        /// <returns>The ADS state changed observable sequence.</returns>
        public IObservable<AdsStateChangedEventArgs> AdsStateChangedObserver() =>
            Observable.FromEventPattern<EventHandler<AdsStateChangedEventArgs>, AdsStateChangedEventArgs>(
                handler => client.AdsStateChanged += handler,
                handler => client.AdsStateChanged -= handler).Select(pattern => pattern.EventArgs);

        /// <summary>Polls ADS state from the client.</summary>
        /// <returns>The ADS state observable sequence.</returns>
        public IObservable<StateInfo> AdsStateObserver() =>
            Observable.Create<StateInfo>(observer =>
            {
                var timer = new Timer(
                    _ =>
                    {
                        try
                        {
                            observer.OnNext(client.IsConnected ? client.ReadState() : new StateInfo { AdsState = AdsState.Invalid });
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    },
                    null,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1));

                return ReactiveUI.Primitives.Disposables.Scope.Create(timer.Dispose);
            });
    }

    /// <summary>Extends observable sequences with retry helpers.</summary>
    /// <typeparam name="TSource">The observable value type.</typeparam>
    /// <param name="source">The observable sequence.</param>
    extension<TSource>(IObservable<TSource?> source)
    {
        /// <summary>Repeats the observable sequence until it completes successfully.</summary>
        /// <returns>The retrying observable sequence.</returns>
        public IObservable<TSource?> OnErrorRetry()
        {
            var checkedSource = Require(source, nameof(source));
            return checkedSource.Retry(int.MaxValue);
        }
    }

    /// <summary>Extends observable sequences with typed error retry helpers.</summary>
    /// <typeparam name="TSource">The observable value type.</typeparam>
    /// <typeparam name="TException">The handled exception type.</typeparam>
    /// <param name="source">The observable sequence.</param>
    extension<TSource, TException>(IObservable<TSource?> source)
        where TException : Exception
    {
        /// <summary>Runs the error handler and repeats the observable sequence.</summary>
        /// <param name="onError">The error handler.</param>
        /// <returns>The retrying observable sequence.</returns>
        public IObservable<TSource?> OnErrorRetry(Action<TException> onError)
        {
            var checkedSource = Require(source, nameof(source));
            var checkedOnError = Require(onError, nameof(onError));
            return checkedSource.OnErrorRetry(checkedOnError, TimeSpan.Zero);
        }

        /// <summary>Runs the error handler and repeats the observable sequence after a delay.</summary>
        /// <param name="onError">The error handler.</param>
        /// <param name="delay">The retry delay.</param>
        /// <returns>The retrying observable sequence.</returns>
        public IObservable<TSource?> OnErrorRetry(Action<TException> onError, TimeSpan delay)
        {
            var checkedSource = Require(source, nameof(source));
            var checkedOnError = Require(onError, nameof(onError));
            return checkedSource.OnErrorRetry(checkedOnError, int.MaxValue, delay);
        }

        /// <summary>Runs the error handler and repeats the observable sequence for the retry count.</summary>
        /// <param name="onError">The error handler.</param>
        /// <param name="retryCount">The retry count.</param>
        /// <returns>The retrying observable sequence.</returns>
        public IObservable<TSource?> OnErrorRetry(Action<TException> onError, int retryCount)
        {
            var checkedSource = Require(source, nameof(source));
            var checkedOnError = Require(onError, nameof(onError));
            return checkedSource.OnErrorRetry(checkedOnError, retryCount, TimeSpan.Zero);
        }

        /// <summary>Runs the error handler and repeats the observable sequence after a delay for the retry count.</summary>
        /// <param name="onError">The error handler.</param>
        /// <param name="retryCount">The retry count.</param>
        /// <param name="delay">The retry delay.</param>
        /// <returns>The retrying observable sequence.</returns>
        public IObservable<TSource?> OnErrorRetry(Action<TException> onError, int retryCount, TimeSpan delay)
        {
            var checkedSource = Require(source, nameof(source));
            var checkedOnError = Require(onError, nameof(onError));
            return checkedSource.OnErrorRetry(checkedOnError, retryCount, delay, TaskPoolSequencer.Default);
        }

        /// <summary>Runs the error handler and repeats the observable sequence using the supplied delay sequencer.</summary>
        /// <param name="onError">The error handler.</param>
        /// <param name="retryCount">The retry count.</param>
        /// <param name="delay">The retry delay.</param>
        /// <param name="delaySequencer">The delay sequencer.</param>
        /// <returns>The retrying observable sequence.</returns>
        public IObservable<TSource?> OnErrorRetry(Action<TException> onError, int retryCount, TimeSpan delay, ISequencer delaySequencer)
        {
            var checkedSource = Require(source, nameof(source));
            var checkedOnError = Require(onError, nameof(onError));
            var checkedDelaySequencer = Require(delaySequencer, nameof(delaySequencer));

            return Observable.Defer(() =>
            {
                var dueTime = delay.Ticks < 0 ? TimeSpan.Zero : delay;
                var empty = Observable.Empty<TSource?>();
                var count = 0;
                IObservable<TSource?>? self = null;
                self = checkedSource.Catch((TException ex) =>
                {
                    checkedOnError(ex);

                    if (++count >= retryCount)
                    {
                        return Observable.Throw<TSource?>(ex);
                    }

                    return dueTime == TimeSpan.Zero
                        ? self!
                        : empty.Delay(dueTime, checkedDelaySequencer).Concat(self!);
                });
                return self;
            });
        }
    }

    /// <summary>Extends settings with TwinCAT variable registration helpers.</summary>
    /// <param name="settings">The TwinCAT settings.</param>
    extension(ISettings settings)
    {
        /// <summary>Adds a notification variable to the settings.</summary>
        /// <param name="variableName">The PLC variable name.</param>
        /// <param name="cycleTime">The polling cycle time.</param>
        /// <param name="arraySize">The array size.</param>
        public void AddNotification(string variableName, int cycleTime = 100, int arraySize = -1)
        {
            if (settings is null)
            {
                return;
            }

            settings.Notifications.Add(new Notification(cycleTime, variableName, arraySize));
        }

        /// <summary>Adds a write variable to the settings.</summary>
        /// <param name="variableName">The PLC variable name.</param>
        /// <param name="arraySize">The array size.</param>
        public void AddWriteVariable(string variableName, int arraySize = -1)
        {
            if (settings is null)
            {
                return;
            }

            settings.WriteVariables.Add(new WriteVariable(variableName, arraySize));
        }
    }

    /// <summary>Extends assembly path strings with dynamic loading helpers.</summary>
    /// <param name="dllFullName">The full DLL path.</param>
    extension(string dllFullName)
    {
        /// <summary>Loads an assembly from a DLL file path.</summary>
        /// <returns>The loaded assembly.</returns>
        [RequiresDynamicCode("Loads an assembly at runtime via Assembly.Load which requires dynamic code.")]
        [RequiresUnreferencedCode("Uses reflection-based assembly loading which may be trimmed.")]
        public Assembly? AssemblyLoad()
        {
            Assembly? assembly = null;
            if (File.Exists(dllFullName))
            {
                using var fs = File.Open(dllFullName, FileMode.Open, FileAccess.Read);
                using var ms = new MemoryStream();
                var buffer = new byte[1024];
                int read;
                while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                assembly = Assembly.Load(ms.ToArray());
            }

            return assembly;
        }

        /// <summary>Gets a type from an assembly file.</summary>
        /// <param name="engineType">The type name.</param>
        /// <returns>The resolved type.</returns>
        [RequiresDynamicCode("Accesses type by name using reflection which may require dynamic code.")]
        [RequiresUnreferencedCode("Uses reflection to access type by name which may be trimmed in AOT.")]
        public Type? GetType(string engineType) => dllFullName.AssemblyLoad()?.GetType(engineType);
    }

    /// <summary>Returns a value or throws when it is null.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="parameterName">The parameter name.</param>
    /// <returns>The non-null value.</returns>
    private static T Require<T>(T? value, string parameterName)
        where T : class =>
        value ?? throw new ArgumentNullException(parameterName);
}
