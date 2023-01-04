// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace CP.TwinCATRx.Core
{
    /// <summary>
    /// WriteVariable for ISettings.
    /// </summary>
    [DataContract]
    [Serializable]
    public class WriteVariable : IWriteVariable
    {
        /// <summary>
        /// Gets or sets the variable.
        /// </summary>
        /// <value>The variable.</value>
        [DataMember]
        public string? Variable { get; set; }
    }
}
