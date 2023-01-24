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
        /// <param name="arraySize">Size of the array.</param>
        public WriteVariable(string? variable, int arraySize = -1)
        {
            Variable = variable;
            ArraySize = arraySize;
        }

        /// <summary>
        /// Gets the variable.
        /// </summary>
        /// <value>The variable.</value>
        [DataMember]
        public string? Variable { get; }

        /// <summary>
        /// Gets the size of the array.
        /// </summary>
        /// <value>
        /// The size of the array.
        /// </value>
        [DataMember]
        public int ArraySize { get; }
    }
}
