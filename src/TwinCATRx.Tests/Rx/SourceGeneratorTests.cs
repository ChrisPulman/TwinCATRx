// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
namespace TwinCATRx.Tests.Rx;

public class SourceGeneratorTests
{
    [Test]
    public void Generated_Stream_Updates_Property_And_Observable()
    {
        var client = new RxFakeClient(new[]
        {
            (Variable: ".A", Data: (object?)123, Id: (string?)null),
        }.ToObservable());
        var generated = new GeneratedStreams();
        var observed = new List<int?>();
        using var observer = generated.AValueObservable.Subscribe(observed.Add);

        using var binding = generated.BindTwinCatRx(client);

        TestAssert.Equal(123, generated.AValue);
        TestAssert.Contains(123, observed);
    }
}
