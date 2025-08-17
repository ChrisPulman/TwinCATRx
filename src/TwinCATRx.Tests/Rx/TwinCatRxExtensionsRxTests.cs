// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CP.TwinCatRx;
using CP.TwinCatRx.Core;

namespace TwinCATRx.Tests.Rx;

/// <summary>
/// Tests for TwinCatRx extensions in CP.TwinCatRx.
/// </summary>
public class TwinCatRxExtensionsRxTests
{
    /// <summary>
    /// Ensure Observe filters by variable and casts to T.
    /// </summary>
    [Fact]
    public void Observe_Filters_By_Variable_And_Casts()
    {
        var data = new (string Variable, object? Data, string? Id)[]
        {
            (".A", null, null),
            (".A", 123, null),
            (".B", 456, null),
        };
        var stream = data.ToObservable();
        var client = new RxFakeClient(stream);

        var vals = client.Observe<int>(".A").ToEnumerable().ToArray();
        vals.Should().Contain(123);
        vals.Should().NotContain(456);
    }

    /// <summary>
    /// Ensure Observe with id filters by id.
    /// </summary>
    [Fact]
    public void Observe_With_Id_Filters_By_Id()
    {
        var stream = new (string Variable, object? Data, string? Id)[]
        {
            (".A", 100, "x"),
            (".A", 200, "y"),
        }.ToObservable();
        var client = new RxFakeClient(stream);

        var vals = client.Observe<int>(".A", "y").ToEnumerable().ToArray();
        vals.Should().BeEquivalentTo(new[] { 200 });
    }

    /// <summary>
    /// CreateStruct should return a HashTableRx with Tag variables set.
    /// </summary>
    [Fact]
    public void CreateStruct_Returns_HashTableRx_With_Tag()
    {
        var stream = Observable.Empty<(string Variable, object? Data, string? Id)>();
        var client = new RxFakeClient(stream);
        client.Connect(new Settings { Port = 801 });

        var ht = client.CreateStruct(".Struct1");
        ht.Should().NotBeNull();
        ht!.Tag.Should().NotBeNull();
        ht.Tag!.ContainsKey(nameof(RxTcAdsClient)).Should().BeTrue();
        ht.Tag!.ContainsKey("Variable").Should().BeTrue();
    }

    /// <summary>
    /// WriteValuesAsync should return false when not connected.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WriteValuesAsync_Returns_False_When_Not_Connected()
    {
        var client = new RxFakeClient(Observable.Empty<(string Variable, object? Data, string? Id)>());
        var ht = client.CreateStruct(".Any");

        // not connected returns false
        var ok = await ht!.WriteValuesAsync(h => { }, TimeSpan.FromMilliseconds(1));
        ok.Should().BeFalse();
    }

    /// <summary>
    /// StructureReady should throw on null arg.
    /// </summary>
    [Fact]
    public void StructureReady_Throws_On_Null()
    {
        Action act = () => CP.TwinCatRx.TwinCatRxExtensions.StructureReady(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// CreateClone should copy structure.
    /// </summary>
    [Fact]
    public void CreateClone_Copies_Structure()
    {
        var client = new RxFakeClient(Observable.Empty<(string Variable, object? Data, string? Id)>());
        var ht = client.CreateStruct(".Any");
        ht![true] = new { A = 1 };

        var clone = ht.CreateClone();
        clone.Should().NotBeSameAs(ht);
        clone.ToString().Should().NotBeNull();
    }
}
