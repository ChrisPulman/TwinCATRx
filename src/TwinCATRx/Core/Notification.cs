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
        /// Initializes a new instance of the <see cref="Notification"/> class.
        /// </summary>
        /// <param name="updateRate">The update rate.</param>
        /// <param name="variable">The variable.</param>
        public Notification(int updateRate, string? variable)
        {
            UpdateRate = updateRate;
            Variable = variable;
        }

        /// <summary>
        /// Gets the Notification update rate.
        /// </summary>
        /// <value>The update rate.</value>
        public int UpdateRate { get; }

        /// <summary>
        /// Gets the Notification variable name.
        /// </summary>
        /// <value>The variable.</value>
        public string? Variable { get; }
    }
}
