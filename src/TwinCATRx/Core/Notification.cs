// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CP.TwinCATRx.Core
{
    /// <summary>
    /// Notification for ISettings.
    /// </summary>
    [Serializable]
    public class Notification : INotification
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Notification" /> class.
        /// </summary>
        /// <param name="updateRate">The update rate.</param>
        /// <param name="variable">The variable.</param>
        /// <param name="arraySize">Size of the array.</param>
        public Notification(int updateRate, string? variable, int arraySize = -1)
        {
            UpdateRate = updateRate;
            Variable = variable;
            ArraySize = arraySize;
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

        /// <summary>
        /// Gets the size of the array.
        /// </summary>
        /// <value>
        /// The size of the array.
        /// </value>
        public int ArraySize { get; }
    }
}
