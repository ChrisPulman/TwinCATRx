// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace CP.TwinCATRx.Core
{
    /// <summary>
    /// Notification for ISettings.
    /// </summary>
    [Serializable]
    public class Notification : INotification
    {
        /// <summary>
        /// Gets or sets the Notification update rate.
        /// </summary>
        /// <value>The update rate.</value>
        public int UpdateRate { get; set; }

        /// <summary>
        /// Gets or sets the Notification variable name.
        /// </summary>
        /// <value>The variable.</value>
        public string? Variable { get; set; }
    }
}
