﻿// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using CP.Collections;
using TwinCAT.Ads;

namespace CP.TwinCatRx
{
    /// <summary>
    /// Observable TwinCAT Extensions.
    /// </summary>
    public static class ObservableTwinCATExtensions
    {
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
        public static Assembly? AssemblyLoad(string dllFullName)
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
        /// Gets the type.
        /// </summary>
        /// <param name="dllFullName">Full name of the DLL.</param>
        /// <param name="engineType">Type of the engine.</param>
        /// <returns>A type.</returns>
        public static Type? GetType(this string dllFullName, string engineType) => AssemblyLoad(dllFullName)?.GetType(engineType);

        /// <summary>
        /// The ADS state changed observer.
        /// </summary>
        /// <param name="this">The @.</param>
        /// <returns>A Value.</returns>
        public static IObservable<AdsStateChangedEventArgs> AdsStateChangedObserver(this AdsClient @this) => Observable.FromEventPattern<EventHandler<AdsStateChangedEventArgs>, AdsStateChangedEventArgs>(h => @this.AdsStateChanged += h, h => @this.AdsStateChanged -= h).Select(x => x.EventArgs);

        /// <summary>
        /// Adses the state observer.
        /// </summary>
        /// <param name="this">The this.</param>
        /// <returns>Observable State Info.</returns>
        public static IObservable<StateInfo> AdsStateObserver(this AdsClient @this) => Observable.Create<StateInfo>(obs =>
                                                                                                 {
                                                                                                     return new SingleAssignmentDisposable
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
                                                                                                     };
                                                                                                 });

        /// <summary>
        /// Observes the specified variable.
        /// </summary>
        /// <typeparam name="T">The Type of the data.</typeparam>
        /// <param name="this">The this.</param>
        /// <param name="variable">The variable.</param>
        /// <returns>An Observable of T.</returns>
        public static IObservable<T> Observe<T>(this RxTcAdsClient @this, string variable) =>
            @this?.DataReceived.Where(x => x.Variable == variable && x.Data != null).Select(x => (T)x.Data!)!;

        /// <summary>
        /// Observes the specified variable.
        /// </summary>
        /// <typeparam name="T">The Type of the data.</typeparam>
        /// <param name="this">The this.</param>
        /// <param name="variable">The variable.</param>
        /// <returns>An Observable of T.</returns>
        public static IObservable<T> Observe<T>(this HashTableRx @this, string variable) =>
            @this?.ObserveAll.Where(x => x.key == variable && x.value != null).Select(x => (T)x.value!)!;

        /// <summary>
        /// Creates the hash table rx.
        /// </summary>
        /// <param name="this">The this.</param>
        /// <param name="variable">The variable.</param>
        /// <returns>A HashTableRx with a link to the PLC.</returns>
        public static HashTableRx CreateHashTableRx(this RxTcAdsClient @this, string variable) =>
            new(@this?.DataReceived.Where(x => x.Variable == variable)!);

        /// <summary>
        /// Values the specified variable.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="this">The this.</param>
        /// <param name="variable">The variable.</param>
        /// <returns>The value of the Tag.</returns>
        public static T? Value<T>(this HashTableRx @this, string? variable)
        {
            if (@this == null || @this.Count == 0)
            {
                return default;
            }

            if (@this.UseUpperCase)
            {
                variable = variable?.ToUpper(CultureInfo.InvariantCulture);
            }

            return (T?)@this[variable!];
        }
    }
}
