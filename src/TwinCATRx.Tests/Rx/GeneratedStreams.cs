// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CP.TwinCatRx;

namespace TwinCATRx.Tests.Rx;

[TwinCatReactiveStream(".A", typeof(int), PropertyName = "AValue", ObservableName = "AValueObservable")]
internal partial class GeneratedStreams
{
}
