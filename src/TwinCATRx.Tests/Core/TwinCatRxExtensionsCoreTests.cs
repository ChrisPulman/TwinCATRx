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
    [Test]
    public void AddNotification_Should_Add_To_List()
    {
        var s = new Settings();
        Assert.That(s.Notifications, Is.Empty);
        s.AddNotification(".MyVar", cycleTime: 200, arraySize: 5);
        Assert.That(s.Notifications, Has.Count.EqualTo(1));
        Assert.That(s.Notifications[0].Variable, Is.EqualTo(".MyVar"));
        Assert.That(s.Notifications[0].UpdateRate, Is.EqualTo(200));
        Assert.That(s.Notifications[0].ArraySize, Is.EqualTo(5));
    }

    [Test]
    public void AddWriteVariable_Should_Add_To_List()
    {
        var s = new Settings();
        Assert.That(s.WriteVariables, Is.Empty);
        s.AddWriteVariable(".MyWrite", arraySize: 10);
        Assert.That(s.WriteVariables, Has.Count.EqualTo(1));
        Assert.That(s.WriteVariables[0].Variable, Is.EqualTo(".MyWrite"));
        Assert.That(s.WriteVariables[0].ArraySize, Is.EqualTo(10));
    }

    [Test]
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
        Assert.That(result, Is.EqualTo(42));
        Assert.That(attempts, Is.EqualTo(3));
    }

    [Test]
    public void AssemblyLoad_And_GetType_Returns_Null_For_Missing_File()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");
        var asm = path.AssemblyLoad();
        Assert.That(asm, Is.Null);
        Assert.That(path.GetType("Some.Type"), Is.Null);
    }

    [Test]
    public void Settings_Defaults_Populates_Defaults()
    {
        var s = new Settings().Defaults<Settings>();
        Assert.That(s.SettingsId, Is.EqualTo("Defaults"));
        Assert.That(s.Notifications, Is.Not.Null);
        Assert.That(s.WriteVariables, Is.Not.Null);
        Assert.That(s.Notifications, Is.Not.Empty);
        Assert.That(s.WriteVariables, Is.Not.Empty);
    }
}
