// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ServiceProcess;

namespace CP.TwinCatRx
{
    /// <summary>
    /// interface for Observable Service Controller.
    /// </summary>
    /// <seealso cref="IDisposable"/>
    public interface IObservableServiceController : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether this instance can stop.
        /// </summary>
        /// <value><c>true</c> if this instance can stop; otherwise, <c>false</c>.</value>
        bool CanStop { get; }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        /// <value>The display name.</value>
        string DisplayName { get; }

        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        /// <value>The name of the service.</value>
        string ServiceName { get; }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <value>The status.</value>
        ServiceControllerStatus Status { get; }

        /// <summary>
        /// Gets the status observer.
        /// </summary>
        /// <value>The status observer.</value>
        IObservable<ServiceControllerStatus> StatusObserver { get; }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        void Restart();

        /// <summary>
        /// Starts this instance.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops this instance.
        /// </summary>
        void Stop();
    }
}
