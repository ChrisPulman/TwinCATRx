// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    [Test]
    public void RxTcAdsClient_Default_State_And_Dispose()
    {
        var c = new RxTcAdsClient();
        Assert.Multiple(() =>
        {
            Assert.That(c.IsDisposed, Is.False);
            Assert.That(c.Connected, Is.False);
        });

        c.Dispose();

        Assert.That(c.Connected, Is.False);
    }
}
