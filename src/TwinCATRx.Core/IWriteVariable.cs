// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace CP.TwinCatRx.Core;

/// <summary>Interface for Write Variable.</summary>
public interface IWriteVariable
{
    /// <summary>Gets the variable.</summary>
    /// <value>The variable.</value>
    [DataMember]
    string? Variable { get; }

    /// <summary>Gets the size of the array.</summary>
    /// <value>
    /// The size of the array.
    /// </value>
    [DataMember]
    int ArraySize { get; }
}
