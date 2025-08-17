// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using CP.TwinCatRx.Core;

namespace TwinCATRx.Tests.Core;

/// <summary>
/// Tests for core TwinCatRx extensions and helpers.
/// </summary>
public class TwinCatRxExtensionsCoreTests
{
    /// <summary>
    /// Verifies AddNotification adds an item to settings.
    /// </summary>
    [Fact]
    public void AddNotification_Should_Add_To_List()
    {
        var s = new Settings();
        s.Notifications.Should().BeEmpty();
        s.AddNotification(".MyVar", cycleTime: 200, arraySize: 5);
        s.Notifications.Should().HaveCount(1);
        s.Notifications[0].Variable.Should().Be(".MyVar");
        s.Notifications[0].UpdateRate.Should().Be(200);
        s.Notifications[0].ArraySize.Should().Be(5);
    }

    /// <summary>
    /// Verifies AddWriteVariable adds an item to settings.
    /// </summary>
    [Fact]
    public void AddWriteVariable_Should_Add_To_List()
    {
        var s = new Settings();
        s.WriteVariables.Should().BeEmpty();
        s.AddWriteVariable(".MyWrite", arraySize: 10);
        s.WriteVariables.Should().HaveCount(1);
        s.WriteVariables[0].Variable.Should().Be(".MyWrite");
        s.WriteVariables[0].ArraySize.Should().Be(10);
    }

    /// <summary>
    /// Ensures OnErrorRetry retries until success.
    /// </summary>
    [Fact]
    public void OnErrorRetry_Basic_Retry_Works()
    {
        var attempts = 0;
        var seq = Observable.Defer(() =>
        {
            attempts++;
            if (attempts < 3)
            {
                return Observable.Throw<int>(new InvalidOperationException());
            }

            return Observable.Return(42);
        });

        var result = seq.OnErrorRetry<int, InvalidOperationException>(_ => { }).ToEnumerable().Last();
        result.Should().Be(42);
        attempts.Should().Be(3);
    }

    /// <summary>
    /// AssemblyLoad/GetType return null for non-existent files.
    /// </summary>
    [Fact]
    public void AssemblyLoad_And_GetType_Returns_Null_For_Missing_File()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");
        var asm = path.AssemblyLoad();
        asm.Should().BeNull();
        path.GetType("Some.Type").Should().BeNull();
    }

    /// <summary>
    /// Defaults populate a Settings instance.
    /// </summary>
    [Fact]
    public void Settings_Defaults_Populates_Defaults()
    {
        var s = new Settings().Defaults<Settings>();
        s.SettingsId.Should().Be("Defaults");
        s.Notifications.Should().NotBeNull();
        s.WriteVariables.Should().NotBeNull();
        s.Notifications.Should().NotBeEmpty();
        s.WriteVariables.Should().NotBeEmpty();
    }
}
