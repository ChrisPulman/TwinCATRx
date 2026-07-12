// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using System.ServiceProcess;
using ReactiveServiceController = CP.TwinCatRx.Reactive.ObservableServiceController;

namespace TwinCATRx.Tests.Rx;

/// <summary>Non-live lifecycle parity tests for the Reactive service wrapper.</summary>
public class ReactiveServiceControllerParityCoverageTests
{
    /// <summary>Verifies null-state getters and disposed commands never query Windows services.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Null_State_And_Disposed_Commands_Do_Not_Query_Service_Manager()
    {
        var service = new ServiceController();
        var controller = new TestObservableServiceController(service, TimeSpan.FromHours(1));
        SetWrappedService(controller, null);

        await TUnitAssert.That(controller.CanStop).IsFalse();
        await TUnitAssert.That(controller.DisplayName).IsEmpty();
        await TUnitAssert.That(controller.ServiceName).IsEmpty();
        await TUnitAssert.That(controller.Status).IsEqualTo(ServiceControllerStatus.Stopped);
        await TUnitAssert.That(controller.StatusObserver).IsNotNull();
        await TUnitAssert.That(controller.IsDisposed).IsFalse();

        controller.ExposeDispose(false);
        await TUnitAssert.That(controller.IsDisposed).IsFalse();

        controller.Dispose();
        controller.Start();
        controller.Stop();
        controller.Restart();
        controller.Dispose();

        await TUnitAssert.That(controller.IsDisposed).IsTrue();
    }

    /// <summary>Replaces the wrapped controller for null-state branch validation.</summary>
    /// <param name="controller">The observable wrapper.</param>
    /// <param name="service">The replacement service.</param>
    private static void SetWrappedService(ReactiveServiceController controller, ServiceController? service) =>
        typeof(ReactiveServiceController)
            .GetField("_serviceController", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(controller, service);

    /// <summary>Test wrapper exposing the protected disposal overload.</summary>
    private sealed class TestObservableServiceController : ReactiveServiceController
    {
        /// <summary>Initializes a new instance of the <see cref="TestObservableServiceController"/> class.</summary>
        /// <param name="service">The unconnected service-controller object.</param>
        /// <param name="interval">The polling interval.</param>
        public TestObservableServiceController(ServiceController service, TimeSpan interval)
            : base(service, interval)
        {
        }

        /// <summary>Invokes the protected disposal path.</summary>
        /// <param name="disposing">Whether managed resources should be disposed.</param>
        public void ExposeDispose(bool disposing) => Dispose(disposing);
    }
}
