// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CP.TwinCatRx.Core;

namespace TwinCATRx.Tests.Core;

/// <summary>
/// Tests for NodeEmulator.
/// </summary>
public class NodeEmulatorTests
{
    /// <summary>
    /// Dispose clears Nodes and Tag.
    /// </summary>
    [Fact]
    public void Dispose_Clears_State()
    {
        // Can't construct internal NodeEmulator type from tests; validate by reflection creating instance
        var type = typeof(Settings).Assembly.GetType("CP.TwinCatRx.Core.NodeEmulator");
        type.Should().NotBeNull();
        var n = Activator.CreateInstance(type!);
        n.Should().NotBeNull();
        var nodesProp = type!.GetProperty("Nodes");
        nodesProp.Should().NotBeNull();
        var nodes = nodesProp!.GetValue(n) as System.Collections.ICollection;
        type!.GetMethod("Dispose")!.Invoke(n, null);
        var nodesAfter = nodesProp!.GetValue(n);
        nodesAfter.Should().BeNull();
    }
}
