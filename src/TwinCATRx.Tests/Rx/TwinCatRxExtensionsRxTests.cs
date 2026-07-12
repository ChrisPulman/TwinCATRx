// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using CP.Collections;
using CP.TwinCatRx;
using CP.TwinCatRx.Core;

namespace TwinCATRx.Tests.Rx;

/// <summary>Tests for TwinCatRx extensions in CP.TwinCatRx.</summary>
public class TwinCatRxExtensionsRxTests
{
    /// <summary>The value attached to the matching test variable.</summary>
    private const int MatchingValue = 123;

    /// <summary>The value attached to the nonmatching test variable.</summary>
    private const int NonMatchingValue = 456;

    /// <summary>The value carrying the nonmatching identifier.</summary>
    private const int FirstIdentifiedValue = 100;

    /// <summary>The value carrying the expected identifier.</summary>
    private const int ExpectedIdentifiedValue = 200;

    /// <summary>The TwinCAT 2 ADS port used by the structure test.</summary>
    private const int TwinCat2Port = 801;

    /// <summary>Verifies variable filtering and casting.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Observe_Filters_By_Variable_And_Casts()
    {
        var data = new (string Variable, object? Data, string? Id)[]
        {
            (".A", null, null),
            (".A", MatchingValue, null),
            (".B", NonMatchingValue, null),
        };
        var stream = Observable.FromEnumerable(data);
        var client = new RxFakeClient(stream);

        var saw123 = false;
        var saw456 = false;
        foreach (var value in client.Observe<int>(".A").ToEnumerable())
        {
            if (value == MatchingValue)
            {
                saw123 = true;
            }

            if (value == NonMatchingValue)
            {
                saw456 = true;
            }
        }

        await TUnitAssert.That(saw123).IsTrue();
        await TUnitAssert.That(saw456).IsFalse();
    }

    /// <summary>Verifies identifier filtering.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Observe_With_Id_Filters_By_Id()
    {
        var data = new (string Variable, object? Data, string? Id)[]
        {
            (".A", FirstIdentifiedValue, "x"),
            (".A", ExpectedIdentifiedValue, "y"),
        };
        var stream = Observable.FromEnumerable(data);
        var client = new RxFakeClient(stream);

        var observedCount = 0;
        var observedValue = 0;
        foreach (var value in client.Observe<int>(".A", "y").ToEnumerable())
        {
            observedCount++;
            observedValue = value;
        }

        await TUnitAssert.That(observedCount).IsEqualTo(1);
        await TUnitAssert.That(observedValue).IsEqualTo(ExpectedIdentifiedValue);
    }

    /// <summary>Verifies structure creation tags the client and variable.</summary>
    /// <returns>The test task.</returns>
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("HashTableRx.SetStructure may use reflection over fields and properties.")]
#endif
    [Test]
    public async Task CreateStruct_Returns_HashTableRx_With_Tag()
    {
        var stream = Observable.Empty<(string Variable, object? Data, string? Id)>();
        var client = new RxFakeClient(stream);
        client.Connect(new Settings { Port = TwinCat2Port });

        var table = client.CreateStruct(".Struct1");
        await TUnitAssert.That(table).IsNotNull();
        await TUnitAssert.That(table!.Tag).IsNotNull();
        await TUnitAssert.That(table.Tag!.ContainsKey(nameof(RxTcAdsClient))).IsTrue();
        await TUnitAssert.That(table.Tag!.ContainsKey("Variable")).IsTrue();
    }

    /// <summary>Verifies writes fail when the fake client is not connected.</summary>
    /// <returns>The test task.</returns>
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("HashTableRx.SetStructure may use reflection over fields and properties.")]
#endif
    [Test]
    public async Task WriteValuesAsync_Returns_False_When_Not_Connected()
    {
        var client = new RxFakeClient(Observable.Empty<(string Variable, object? Data, string? Id)>());
        var table = client.CreateStruct(".Any");
        var ok = await table!.WriteValuesAsync(_ => { }, TimeSpan.FromMilliseconds(1));
        await TUnitAssert.That(ok).IsFalse();
    }

    /// <summary>Verifies StructureReady rejects null receivers.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task StructureReady_Throws_On_Null()
    {
        await TUnitAssert.That(() => ((HashTableRx)null!).StructureReady()).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies structure cloning creates a distinct table.</summary>
    /// <returns>The test task.</returns>
#if NET9_0_OR_GREATER
    [RequiresUnreferencedCode("HashTableRx.SetStructure may use reflection over fields and properties.")]
#endif
    [Test]
    public async Task CreateClone_Copies_Structure()
    {
        var client = new RxFakeClient(Observable.Empty<(string Variable, object? Data, string? Id)>());
        var table = client.CreateStruct(".Any");
#if NET9_0_OR_GREATER
        table!.SetStructure(new { A = 1 });
#else
        table![true] = new { A = 1 };
#endif

        var clone = table.CreateClone();
        await TUnitAssert.That(ReferenceEquals(table, clone)).IsFalse();
        await TUnitAssert.That(clone.ToString()).IsNotNull();
    }
}
