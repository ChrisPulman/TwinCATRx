// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CP.TwinCatRx.Core
{
    /// <summary>
    /// Represents a Node of a Node Collection.
    /// </summary>
    [Serializable]
    public class NodeEmulator : INodeEmulator
    {
        private bool _disposedValue;

        /// <summary>
        /// Gets contains Child Nodes of this node.
        /// </summary>
        public HashSet<INodeEmulator>? Nodes { get; private set; } = new HashSet<INodeEmulator>();

        /// <summary>
        /// Gets or sets container for object Data.
        /// </summary>
        /// <returns>Object.</returns>
        public object? Tag { get; set; }

        /// <summary>
        /// Gets or sets node Name.
        /// </summary>
        /// <returns>String.</returns>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
        /// unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (Nodes != null && disposing)
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
    }
}
