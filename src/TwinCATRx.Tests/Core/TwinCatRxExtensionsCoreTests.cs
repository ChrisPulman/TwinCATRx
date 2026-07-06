// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using CP.TwinCatRx.Core;

namespace TwinCATRx.Tests.Core;

/// <summary>Tests for core TwinCatRx extensions and helpers.</summary>
public class TwinCatRxExtensionsCoreTests
{
    /// <summary>Verifies notification registration.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task AddNotification_Should_Add_To_List()
    {
        var s = new Settings();
        await TUnitAssert.That(s.Notifications).IsEmpty();
        s.AddNotification(".MyVar", cycleTime: 200, arraySize: 5);
        await TUnitAssert.That(s.Notifications.Count).IsEqualTo(1);
        await TUnitAssert.That(s.Notifications[0].Variable).IsEqualTo(".MyVar");
        await TUnitAssert.That(s.Notifications[0].UpdateRate).IsEqualTo(200);
        await TUnitAssert.That(s.Notifications[0].ArraySize).IsEqualTo(5);
    }

    /// <summary>Verifies write-variable registration.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task AddWriteVariable_Should_Add_To_List()
    {
        var s = new Settings();
        await TUnitAssert.That(s.WriteVariables).IsEmpty();
        s.AddWriteVariable(".MyWrite", arraySize: 10);
        await TUnitAssert.That(s.WriteVariables.Count).IsEqualTo(1);
        await TUnitAssert.That(s.WriteVariables[0].Variable).IsEqualTo(".MyWrite");
        await TUnitAssert.That(s.WriteVariables[0].ArraySize).IsEqualTo(10);
    }

    /// <summary>Verifies retrying until success.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task OnErrorRetry_Basic_Retry_Works()
    {
        var attempts = 0;
        var seq = Observable.Defer<int>(() =>
        {
            attempts++;
            return attempts < 3 ? Observable.Throw<int>(new InvalidOperationException()) : Observable.Return(42);
        });

        var result = 0;
        foreach (var value in CP.TwinCatRx.Core.TwinCatRxExtensions.OnErrorRetry<int, InvalidOperationException>(seq, _ => { }).ToEnumerable())
        {
            result = value;
        }

        await TUnitAssert.That(result).IsEqualTo(42);
        await TUnitAssert.That(attempts).IsEqualTo(3);
    }

    /// <summary>Verifies missing assembly load returns null.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task AssemblyLoad_And_GetType_Returns_Null_For_Missing_File()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");
        var asm = path.AssemblyLoad();
        await TUnitAssert.That(asm).IsNull();
        await TUnitAssert.That(path.GetType("Some.Type")).IsNull();
    }

    /// <summary>Verifies default settings are populated.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Settings_Defaults_Populates_Defaults()
    {
        var s = new Settings().Defaults<Settings>();
        await TUnitAssert.That(s.SettingsId).IsEqualTo("Defaults");
        await TUnitAssert.That(s.Notifications).IsNotNull();
        await TUnitAssert.That(s.WriteVariables).IsNotNull();
        await TUnitAssert.That(s.Notifications).IsNotEmpty();
        await TUnitAssert.That(s.WriteVariables).IsNotEmpty();
    }
}
