// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace CP.TwinCatRx.Core.Reactive;
#else
namespace CP.TwinCatRx.Core;
#endif

/// <summary>Represents a Node of a Node Collection.</summary>
[Serializable]
internal sealed class NodeEmulator : INodeEmulator
{
    /// <summary>Tracks whether this instance has been disposed.</summary>
    private bool _disposedValue;

    /// <summary>Gets contains Child Nodes of this node.</summary>
    public HashSet<INodeEmulator>? Nodes { get; private set; } = [];

    /// <summary>Gets or sets container for object Data.</summary>
    /// <returns>Object.</returns>
    public object? Tag { get; set; }

    /// <summary>Gets or sets node Name.</summary>
    /// <returns>String.</returns>
    public string Text { get; set; } = string.Empty;

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
    /// unmanaged resources.
    /// </param>
    private void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (Nodes is not null && disposing)
        {
            foreach (var item in Nodes)
            {
                item.Dispose();
            }

            Nodes.Clear();
            Nodes = null;
            Tag = null;
        }

        _disposedValue = true;
    }
}
