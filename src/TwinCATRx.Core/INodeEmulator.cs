// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace CP.TwinCatRx.Core;

/// <summary>Interface for Node Emulator.</summary>
/// <seealso cref="IDisposable"/>
public interface INodeEmulator : IDisposable
{
    /// <summary>Gets the nodes.</summary>
    /// <value>The nodes.</value>
    HashSet<INodeEmulator>? Nodes { get; }

    /// <summary>Gets or sets the tag.</summary>
    /// <value>The tag.</value>
    object? Tag { get; set; }

    /// <summary>Gets or sets the text.</summary>
    /// <value>The text.</value>
    string Text { get; set; }
}
