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
        TestAssert.Empty(s.Notifications);
        s.AddNotification(".MyVar", cycleTime: 200, arraySize: 5);
        TestAssert.Count(1, s.Notifications);
        TestAssert.Equal(".MyVar", s.Notifications[0].Variable);
        TestAssert.Equal(200, s.Notifications[0].UpdateRate);
        TestAssert.Equal(5, s.Notifications[0].ArraySize);
    }

    [Test]
    public void AddWriteVariable_Should_Add_To_List()
    {
        var s = new Settings();
        TestAssert.Empty(s.WriteVariables);
        s.AddWriteVariable(".MyWrite", arraySize: 10);
        TestAssert.Count(1, s.WriteVariables);
        TestAssert.Equal(".MyWrite", s.WriteVariables[0].Variable);
        TestAssert.Equal(10, s.WriteVariables[0].ArraySize);
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
        TestAssert.Equal(42, result);
        TestAssert.Equal(3, attempts);
    }

    [Test]
    public void AssemblyLoad_And_GetType_Returns_Null_For_Missing_File()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");
        var asm = path.AssemblyLoad();
        TestAssert.Null(asm);
        TestAssert.Null(path.GetType("Some.Type"));
    }

    [Test]
    public void Settings_Defaults_Populates_Defaults()
    {
        var s = new Settings().Defaults<Settings>();
        TestAssert.Equal("Defaults", s.SettingsId);
        TestAssert.NotNull(s.Notifications);
        TestAssert.NotNull(s.WriteVariables);
        TestAssert.NotEmpty(s.Notifications);
        TestAssert.NotEmpty(s.WriteVariables);
    }
}
