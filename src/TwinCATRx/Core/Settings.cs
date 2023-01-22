// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Serialization;

namespace CP.TwinCATRx.Core
{
    /// <summary>
    /// Base settings for Engine Settings file.
    /// </summary>
    [Serializable]
    [XmlInclude(typeof(WriteVariable))]
    [XmlInclude(typeof(Notification))]
    public class Settings : ISettings
    {
        /// <summary>
        /// Gets or sets the Ads Address.
        /// </summary>
        public string AdsAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Port of the PLC to connect to.
        /// </summary>
        public int Port { get; set; } = 801;

        /// <summary>
        /// Gets or sets Notifications of this Engine.
        /// </summary>
        public List<INotification> Notifications { get; set; } = new();

        /// <summary>
        /// Gets or sets System Identifier.
        /// </summary>
        public string? SettingsId { get; set; }

        /// <summary>
        /// Gets or sets Write variables to this Engine.
        /// </summary>
        public List<IWriteVariable> WriteVariables { get; set; } = new();

        /// <summary>
        /// Default Settings used - called when no file exists.
        /// </summary>
        /// <typeparam name="T">The settings type to use.</typeparam>
        /// <returns>A ISettings.</returns>
        public virtual T Defaults<T>()
            where T : ISettings, new()
        {
            var s = new T
            {
                SettingsId = "Defaults"
            };
            s.Notifications.Add(new Notification(100, ".UIStructure"));
            s.WriteVariables.Add(new WriteVariable(".FailSafeCounter"));
            return s;
        }
    }
}
