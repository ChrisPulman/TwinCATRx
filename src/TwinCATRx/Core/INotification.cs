// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CP.TwinCATRx.Core
{
    /// <summary>
    /// Interface for Notification.
    /// </summary>
    public interface INotification
    {
        /// <summary>
        /// Gets or sets the update rate.
        /// </summary>
        /// <value>The update rate.</value>
        int UpdateRate { get; set; }

        /// <summary>
        /// Gets or sets the variable.
        /// </summary>
        /// <value>The variable.</value>
        string? Variable { get; set; }
    }
}
