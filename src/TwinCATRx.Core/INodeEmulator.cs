// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CP.TwinCatRx.Core;

/// <summary>
/// interface for Node Emulator.
/// </summary>
/// <seealso cref="IDisposable"/>
public interface INodeEmulator : IDisposable
{
    /// <summary>
    /// Gets the nodes.
    /// </summary>
    /// <value>The nodes.</value>
    HashSet<INodeEmulator>? Nodes { get; }

    /// <summary>
    /// Gets or sets the tag.
    /// </summary>
    /// <value>The tag.</value>
    object? Tag { get; set; }

    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    /// <value>The text.</value>
    string Text { get; set; }
}
