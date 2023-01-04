// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using TwinCAT.Ads;

namespace CP.TwinCatRx
{
    /// <summary>
    /// Observable TwinCAT Extensions.
    /// </summary>
    public static class ObservableTwinCATExtensions
    {
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
    }
}
