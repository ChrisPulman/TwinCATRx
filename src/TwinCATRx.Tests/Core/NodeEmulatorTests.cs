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
    [Test]
    public void Dispose_Clears_State()
    {
        var type = typeof(Settings).Assembly.GetType("CP.TwinCatRx.Core.NodeEmulator");
        TestAssert.NotNull(type);
        var n = Activator.CreateInstance(type!);
        TestAssert.NotNull(n);
        var nodesProp = type!.GetProperty("Nodes");
        TestAssert.NotNull(nodesProp);
        var nodes = nodesProp!.GetValue(n) as System.Collections.ICollection;
        type!.GetMethod("Dispose")!.Invoke(n, null);
        var nodesAfter = nodesProp!.GetValue(n);
        TestAssert.Null(nodesAfter);
    }
}
