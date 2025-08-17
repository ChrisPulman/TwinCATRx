// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using TwinCAT.Ads;

namespace CP.TwinCatRx.Core;

/// <summary>
/// Observable TwinCAT Extensions.
/// </summary>
public static class TwinCatRxExtensions
{
    /// <summary>
    /// Adds the notification.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="variableName">Name of the variable.</param>
    /// <param name="cycleTime">The cycle time.</param>
    /// <param name="arraySize">Size of the array.</param>
    public static void AddNotification(this ISettings settings, string variableName, int cycleTime = 100, int arraySize = -1)
    {
        if (settings == null)
        {
            return;
        }

        settings.Notifications.Add(new Notification(cycleTime, variableName, arraySize));
    }

    /// <summary>
    /// Adds the write variable.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="variableName">Name of the variable.</param>
    /// <param name="arraySize">Size of the array.</param>
    public static void AddWriteVariable(this ISettings settings, string variableName, int arraySize = -1)
    {
        if (settings == null)
        {
            return;
        }

        settings.WriteVariables.Add(new WriteVariable(variableName, arraySize));
    }

    /// <summary>
    /// <para>Repeats the source observable sequence until it successfully terminates.</para>
    /// <para>This is same as Retry().</para>
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <param name="source">The source.</param>
    /// <returns>A Value.</returns>
    public static IObservable<TSource?> OnErrorRetry<TSource>(this IObservable<TSource?> source) => source.Retry();

    /// <summary>
    /// When caught exception, do onError action and repeat observable sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TException">The type of the exception.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="onError">The on error.</param>
    /// <returns>A Value.</returns>
    public static IObservable<TSource?> OnErrorRetry<TSource, TException>(this IObservable<TSource?> source, Action<TException> onError)
        where TException : Exception => source.OnErrorRetry(onError, TimeSpan.Zero);

    /// <summary>
    /// When caught exception, do onError action and repeat observable sequence after delay time.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TException">The type of the exception.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="onError">The on error.</param>
    /// <param name="delay">The delay.</param>
    /// <returns>A Value.</returns>
#pragma warning disable RCS1231 // Make parameter ref read-only.
    public static IObservable<TSource?> OnErrorRetry<TSource, TException>(this IObservable<TSource?> source, Action<TException> onError, TimeSpan delay)
        where TException : Exception => source.OnErrorRetry(onError, int.MaxValue, delay);

    /// <summary>
    /// When caught exception, do onError action and repeat observable sequence during within retryCount.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TException">The type of the exception.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="onError">The on error.</param>
    /// <param name="retryCount">The retry count.</param>
    /// <returns>A Value.</returns>
    public static IObservable<TSource?> OnErrorRetry<TSource, TException>(this IObservable<TSource?> source, Action<TException> onError, int retryCount)
        where TException : Exception => source.OnErrorRetry(onError, retryCount, TimeSpan.Zero);

    /// <summary>
    /// When caught exception, do onError action and repeat observable sequence after delay time
    /// during within retryCount.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TException">The type of the exception.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="onError">The on error.</param>
    /// <param name="retryCount">The retry count.</param>
    /// <param name="delay">The delay.</param>
    /// <returns>A Value.</returns>
    public static IObservable<TSource?> OnErrorRetry<TSource, TException>(this IObservable<TSource?> source, Action<TException> onError, int retryCount, TimeSpan delay)
#pragma warning restore RCS1231 // Make parameter ref read-only.
        where TException : Exception => source.OnErrorRetry(onError, retryCount, delay, Scheduler.Default);

    /// <summary>
    /// When caught exception, do onError action and repeat observable sequence after delay
    /// time(work on delayScheduler) during within retryCount.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TException">The type of the exception.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="onError">The on error.</param>
    /// <param name="retryCount">The retry count.</param>
    /// <param name="delay">The delay.</param>
    /// <param name="delayScheduler">The delay scheduler.</param>
    /// <returns>A Value.</returns>
    public static IObservable<TSource?> OnErrorRetry<TSource, TException>(this IObservable<TSource?> source, Action<TException> onError, int retryCount, TimeSpan delay, IScheduler delayScheduler)
        where TException : Exception => Observable.Defer(() =>
        {
            var dueTime = (delay.Ticks < 0) ? TimeSpan.Zero : delay;
            var empty = Observable.Empty<TSource?>();
            var count = 0;
            IObservable<TSource?>? self = null;
            self = source.Catch((TException ex) =>
            {
                onError(ex);

                return (++count < retryCount)
                    ? (dueTime == TimeSpan.Zero)
                        ? self!.SubscribeOn(Scheduler.CurrentThread)
                        : empty.Delay(dueTime, delayScheduler).Concat(self!).SubscribeOn(Scheduler.CurrentThread)
                    : Observable.Throw<TSource?>(ex);
            });
            return self;
        });

    /// <summary>
    /// Loads the Assembly.
    /// </summary>
    /// <param name="dllFullName">Full name of the DLL.</param>
    /// <returns>assembly loaded.</returns>
    [RequiresDynamicCode("Loads an assembly at runtime via Assembly.Load which requires dynamic code.")]
    [RequiresUnreferencedCode("Uses reflection-based assembly loading which may be trimmed.")]
    public static Assembly? AssemblyLoad(this string dllFullName)
    {
        Assembly? assembly = null;
        if (File.Exists(dllFullName))
        {
            using (var fs = File.Open(dllFullName, FileMode.Open, FileAccess.Read))
            using (var ms = new MemoryStream())
            {
                var buffer = new byte[1024];
                int read;
                while ((read = fs.Read(buffer, 0, 1024)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                assembly = Assembly.Load(ms.ToArray());
            }

            // Force clean up
            GC.Collect();
        }

        return assembly;
    }

    /// <summary>
    /// Gets the type from the assembly file.
    /// </summary>
    /// <param name="dllFullName">Full name of the DLL.</param>
    /// <param name="engineType">Type of the engine.</param>
    /// <returns>A type.</returns>
    [RequiresDynamicCode("Accesses type by name using reflection which may require dynamic code.")]
    [RequiresUnreferencedCode("Uses reflection to access type by name which may be trimmed in AOT.")]
    public static Type? GetType(this string dllFullName, string engineType) => dllFullName.AssemblyLoad()?.GetType(engineType);

    /// <summary>
    /// The ADS state changed observer.
    /// </summary>
    /// <param name="this">The @.</param>
    /// <returns>A Value.</returns>
    public static IObservable<AdsStateChangedEventArgs> AdsStateChangedObserver(this AdsClient @this) => Observable.FromEventPattern<EventHandler<AdsStateChangedEventArgs>, AdsStateChangedEventArgs>(h => @this.AdsStateChanged += h, h => @this.AdsStateChanged -= h).Select(x => x.EventArgs);

    /// <summary>
    /// ADS state observer.
    /// </summary>
    /// <param name="this">The this.</param>
    /// <returns>Observable State Info.</returns>
    public static IObservable<StateInfo> AdsStateObserver(this AdsClient @this) =>
        Observable.Create<StateInfo>(obs => new SingleAssignmentDisposable
        {
            Disposable = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ =>
            {
                try
                {
                    if (!@this.IsConnected)
                    {
                        obs.OnNext(new StateInfo { AdsState = AdsState.Invalid });
                    }
                    else
                    {
                        obs.OnNext(@this.ReadState());
                    }
                }
                catch (Exception ex)
                {
                    obs.OnError(ex);
                }
            })
        });
}
