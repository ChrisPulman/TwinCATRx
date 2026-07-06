// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using CP.TwinCatRx;

namespace TwinCATRx.Tests.Rx;

/// <summary>Surface-level tests for RxTcAdsClient lifecycle.</summary>
public class RxTcAdsClientSurfaceTests
{
    /// <summary>Verifies default state and dispose behavior of RxTcAdsClient.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task RxTcAdsClient_Default_State_And_Dispose()
    {
        var c = new RxTcAdsClient();
        await TUnitAssert.That(c.IsDisposed).IsFalse();
        await TUnitAssert.That(c.Connected).IsFalse();

        c.Dispose();

        await TUnitAssert.That(c.Connected).IsFalse();
    }
}
