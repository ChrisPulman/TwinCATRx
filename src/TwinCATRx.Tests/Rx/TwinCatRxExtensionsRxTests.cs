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
    [Test]
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
        Assert.That(vals, Does.Contain(123));
        Assert.That(vals, Does.Not.Contain(456));
    }

    [Test]
    public void Observe_With_Id_Filters_By_Id()
    {
        var stream = new (string Variable, object? Data, string? Id)[]
        {
            (".A", 100, "x"),
            (".A", 200, "y"),
        }.ToObservable();
        var client = new RxFakeClient(stream);

        var vals = client.Observe<int>(".A", "y").ToEnumerable().ToArray();
        Assert.That(vals, Is.EqualTo(new[] { 200 }));
    }

    [Test]
    public void CreateStruct_Returns_HashTableRx_With_Tag()
    {
        var stream = Observable.Empty<(string Variable, object? Data, string? Id)>();
        var client = new RxFakeClient(stream);
        client.Connect(new Settings { Port = 801 });

        var ht = client.CreateStruct(".Struct1");
        Assert.That(ht, Is.Not.Null);
        Assert.That(ht!.Tag, Is.Not.Null);
        Assert.That(ht.Tag!.ContainsKey(nameof(RxTcAdsClient)), Is.True);
        Assert.That(ht.Tag!.ContainsKey("Variable"), Is.True);
    }

    [Test]
    public async Task WriteValuesAsync_Returns_False_When_Not_Connected()
    {
        var client = new RxFakeClient(Observable.Empty<(string Variable, object? Data, string? Id)>());
        var ht = client.CreateStruct(".Any");
        var ok = await ht!.WriteValuesAsync(h => { }, TimeSpan.FromMilliseconds(1));
        Assert.That(ok, Is.False);
    }

    [Test]
    public void StructureReady_Throws_On_Null()
    {
        Assert.That(() => CP.TwinCatRx.TwinCatRxExtensions.StructureReady(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void CreateClone_Copies_Structure()
    {
        var client = new RxFakeClient(Observable.Empty<(string Variable, object? Data, string? Id)>());
        var ht = client.CreateStruct(".Any");
        ht![true] = new { A = 1 };

        var clone = ht.CreateClone();
        Assert.That(clone, Is.Not.SameAs(ht));
        Assert.That(clone.ToString(), Is.Not.Null);
    }
}
