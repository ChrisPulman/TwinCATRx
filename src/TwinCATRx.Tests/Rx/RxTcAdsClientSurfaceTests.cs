// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CP.TwinCatRx;

namespace TwinCATRx.Tests.Rx;

/// <summary>
/// Surface-level tests for RxTcAdsClient lifecycle.
/// </summary>
public class RxTcAdsClientSurfaceTests
{
    /// <summary>
    /// Verifies default state and dispose behavior of RxTcAdsClient.
    /// </summary>
    [Fact]
    public void RxTcAdsClient_Default_State_And_Dispose()
    {
        var c = new RxTcAdsClient();
        c.IsDisposed.Should().BeFalse();
        c.Connected.Should().BeFalse();

        // Dispose is safe to call when not connected
        c.Dispose();

        // IsDisposed may remain false if no cleanup was created; ensure no exception and state is still sane
        c.Connected.Should().BeFalse();
    }
}
