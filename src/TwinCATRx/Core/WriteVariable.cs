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
        /// Initializes a new instance of the <see cref="WriteVariable" /> class.
        /// </summary>
        /// <param name="variable">The variable.</param>
        public WriteVariable(string? variable) => Variable = variable;

        /// <summary>
        /// Gets the variable.
        /// </summary>
        /// <value>The variable.</value>
        [DataMember]
        public string? Variable { get; }
    }
}
