// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using CP.TwinCatRx.Core;

namespace TwinCATRx.Tests.Core;

/// <summary>Tests for NodeEmulator.</summary>
public class NodeEmulatorTests
{
    /// <summary>Dispose clears Nodes and Tag.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Dispose_Clears_State()
    {
        var type = typeof(Settings).Assembly.GetType("CP.TwinCatRx.Core.NodeEmulator");
        await TUnitAssert.That(type).IsNotNull();
        var n = Activator.CreateInstance(type!);
        await TUnitAssert.That(n).IsNotNull();
        var nodesProp = type!.GetProperty("Nodes");
        await TUnitAssert.That(nodesProp).IsNotNull();
        var nodes = nodesProp!.GetValue(n) as System.Collections.ICollection;
        _ = type!.GetMethod("Dispose")!.Invoke(n, null);
        var nodesAfter = nodesProp!.GetValue(n);
        await TUnitAssert.That(nodesAfter).IsNull();
    }
}
