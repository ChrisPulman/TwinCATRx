// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace CP.TwinCATRx.Core
{
    /// <summary>
    /// Interface for Write Variable.
    /// </summary>
    public interface IWriteVariable
    {
        /// <summary>
        /// Gets or sets the variable.
        /// </summary>
        /// <value>The variable.</value>
        [DataMember]
        string? Variable { get; set; }
    }
}
