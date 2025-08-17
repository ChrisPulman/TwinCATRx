// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.ServiceProcess;
using CP.TwinCatRx;

namespace TwinCATRx.Tests.Rx;

/// <summary>
/// ObservableServiceControllerTests.
/// </summary>
public class ObservableServiceControllerTests
{
    /// <summary>
    /// Gets the services enumerates.
    /// </summary>
    [Fact(Skip = "Depends on OS services; keep as sanity check only.")]
    public void GetServices_Enumerates()
    {
        var count = ObservableServiceController.GetServices().ToEnumerable().Count();
        count.Should().BeGreaterThan(0);
    }
}
