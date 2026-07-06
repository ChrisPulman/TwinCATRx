// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace CP.TwinCatRx.Core;

/// <summary>Notification for ISettings.</summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Notification" /> class.
/// </remarks>
/// <param name="updateRate">The update rate.</param>
/// <param name="variable">The variable.</param>
/// <param name="arraySize">Size of the array.</param>
[Serializable]
internal sealed class Notification(int updateRate, string? variable, int arraySize = -1) : INotification
{
    /// <summary>Gets the Notification update rate.</summary>
    /// <value>The update rate.</value>
    public int UpdateRate { get; } = updateRate;

    /// <summary>Gets the Notification variable name.</summary>
    /// <value>The variable.</value>
    public string? Variable { get; } = variable;

    /// <summary>Gets the size of the array.</summary>
    /// <value>
    /// The size of the array.
    /// </value>
    public int ArraySize { get; } = arraySize;
}
