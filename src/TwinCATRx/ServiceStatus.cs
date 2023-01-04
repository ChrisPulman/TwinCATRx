// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CP.TwinCatRx
{
    /// <summary>
    /// Service Status.
    /// </summary>
    public enum ServiceStatus
    {
        /// <summary>
        /// The unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The running.
        /// </summary>
        Running,

        /// <summary>
        /// The stopped.
        /// </summary>
        Stopped,

        /// <summary>
        /// The paused.
        /// </summary>
        Paused,

        /// <summary>
        /// The stopping.
        /// </summary>
        Stopping,

        /// <summary>
        /// The starting.
        /// </summary>
        Starting,

        /// <summary>
        /// The status changing.
        /// </summary>
        StatusChanging,

        /// <summary>
        /// The faulted.
        /// </summary>
        Faulted
    }
}
