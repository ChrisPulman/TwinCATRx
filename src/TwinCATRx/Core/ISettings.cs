// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CP.TwinCATRx.Core
{
    /// <summary>
    /// Interface for engine settings.
    /// </summary>
    public interface ISettings
    {
        /// <summary>
        /// Gets or sets the ads address.
        /// </summary>
        /// <value>
        /// The ads address.
        /// </value>
        string AdsAddress { get; set; }

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>
        /// The port.
        /// </value>
        int Port { get; set; }

        /// <summary>
        /// Gets or sets Notifications of this Engine.
        /// </summary>
        List<INotification> Notifications { get; set; }

        /// <summary>
        /// Gets or sets System Identifier.
        /// </summary>
        string? SettingsId { get; set; }

        /// <summary>
        /// Gets or sets Write variables to this Engine.
        /// </summary>
        List<IWriteVariable> WriteVariables { get; set; }

        /// <summary>
        /// Gets or sets Default settings.
        /// </summary>
        /// <typeparam name="T">The settings type to use.</typeparam>
        /// <returns>Default values of type T.</returns>
        T Defaults<T>()
            where T : ISettings, new();
    }
}
