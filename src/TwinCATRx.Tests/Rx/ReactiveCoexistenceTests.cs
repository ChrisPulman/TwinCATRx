// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using LeanClient = CP.TwinCatRx.IRxTcAdsClient;
using LeanSettings = CP.TwinCatRx.Core.Settings;
using LeanUnit = ReactiveUI.Primitives.RxVoid;
using ReactiveClient = CP.TwinCatRx.Reactive.IRxTcAdsClient;
using ReactiveCoreExtensions = CP.TwinCatRx.Core.Reactive.TwinCatRxExtensions;
using ReactiveSettings = CP.TwinCatRx.Core.Reactive.Settings;
using ReactiveUnit = System.Reactive.Unit;

namespace TwinCATRx.Tests.Rx;

/// <summary>Tests that lean and System.Reactive TwinCATRx surfaces coexist.</summary>
public class ReactiveCoexistenceTests
{
    /// <summary>Stores the scheduler parameter position.</summary>
    private const int SchedulerParameterIndex = 4;

    /// <summary>Stores the scheduler overload parameter count.</summary>
    private const int SchedulerOverloadParameterCount = 5;

    /// <summary>Verifies both public API surfaces can be referenced without namespace collisions.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Lean_And_Reactive_Apis_Coexist()
    {
        var leanUnit = typeof(LeanClient)
            .GetProperty(nameof(LeanClient.InitializeComplete))!
            .PropertyType
            .GetGenericArguments()[0];
        var reactiveUnit = typeof(ReactiveClient)
            .GetProperty(nameof(ReactiveClient.InitializeComplete))!
            .PropertyType
            .GetGenericArguments()[0];

        await TUnitAssert.That(leanUnit).IsEqualTo(typeof(LeanUnit));
        await TUnitAssert.That(reactiveUnit).IsEqualTo(typeof(ReactiveUnit));
        await TUnitAssert.That(typeof(LeanClient).Assembly).IsNotEqualTo(typeof(ReactiveClient).Assembly);
        await TUnitAssert.That(typeof(LeanSettings).Assembly).IsNotEqualTo(typeof(ReactiveSettings).Assembly);
    }

    /// <summary>Verifies the Reactive core scheduler alias is System.Reactive's scheduler contract.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Reactive_Core_Uses_System_Reactive_Scheduler()
    {
        var schedulerParameter = typeof(ReactiveCoreExtensions)
            .GetMethods()
            .Single(method =>
                method.Name == nameof(ReactiveCoreExtensions.OnErrorRetry) &&
                method.GetParameters().Length == SchedulerOverloadParameterCount &&
                method.GetParameters()[SchedulerParameterIndex].ParameterType == typeof(IScheduler))
            .GetParameters()[SchedulerParameterIndex]
            .ParameterType;

        await TUnitAssert.That(schedulerParameter).IsEqualTo(typeof(IScheduler));
    }

    /// <summary>Verifies Reactive attributes generate members against Reactive namespaces.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Reactive_Source_Generator_Emits_Reactive_Surface()
    {
        var connection = new GeneratedReactivePlcConnection();
        var stream = new GeneratedReactiveStreams();
        var settings = connection.CreateTwinCatRxSettings();
        var bindParameter = typeof(GeneratedReactivePlcConnection)
            .GetMethod(nameof(GeneratedReactivePlcConnection.BindTwinCatRx))!
            .GetParameters()[0]
            .ParameterType;
        var streamBindParameter = stream.GetType()
            .GetMethod(nameof(GeneratedReactiveStreams.BindTwinCatRx))!
            .GetParameters()[0]
            .ParameterType;

        await TUnitAssert.That(settings.GetType()).IsEqualTo(typeof(ReactiveSettings));
        await TUnitAssert.That(bindParameter).IsEqualTo(typeof(ReactiveClient));
        await TUnitAssert.That(streamBindParameter).IsEqualTo(typeof(ReactiveClient));
        await TUnitAssert.That(settings.SettingsId).IsEqualTo("ReactiveGeneratedSettings");
    }
}
