// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

#if REACTIVE_SHIM
namespace CP.TwinCatRx.Core.Reactive;
#else
namespace CP.TwinCatRx.Core;
#endif

/// <summary>WriteVariable for ISettings.</summary>
/// <remarks>
/// Initializes a new instance of the <see cref="WriteVariable" /> class.
/// </remarks>
/// <param name="variable">The variable.</param>
/// <param name="arraySize">Size of the array.</param>
[DataContract]
[Serializable]
internal sealed class WriteVariable(string? variable, int arraySize = -1) : IWriteVariable
{
    /// <summary>Gets the variable.</summary>
    /// <value>The variable.</value>
    [DataMember]
    public string? Variable { get; } = variable;

    /// <summary>Gets the size of the array.</summary>
    /// <value>
    /// The size of the array.
    /// </value>
    [DataMember]
    public int ArraySize { get; } = arraySize;
}
