// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using CP.TwinCatRx;
using ReactiveUI.Extensions.Async;

namespace TwinCATRx.Tests.Rx;

public class AsyncObservableTests
{
    [Test]
    public void ObserveAsync_Bridges_Classic_Stream()
    {
        var client = new RxFakeClient(new[]
        {
            (Variable: ".A", Data: (object?)123, Id: (string?)null),
            (Variable: ".B", Data: (object?)456, Id: (string?)null),
        }.ToObservable());

        var values = client.ObserveAsync<int>(".A").ToObservable().ToEnumerable().ToArray();

        TestAssert.SequenceEqual(new[] { 123 }, values);
    }
}
