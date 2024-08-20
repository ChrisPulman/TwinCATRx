// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CP.TwinCatRx.Core;

/// <summary>
/// Interface for Notification.
/// </summary>
public interface INotification
{
    /// <summary>
    /// Gets the update rate.
    /// </summary>
    /// <value>The update rate.</value>
    int UpdateRate { get; }

    /// <summary>
    /// Gets the variable.
    /// </summary>
    /// <value>The variable.</value>
    string? Variable { get; }

    /// <summary>
    /// Gets the size of the array.
    /// </summary>
    /// <value>
    /// The size of the array.
    /// </value>
    int ArraySize { get; }
}
