// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using CP.TwinCatRx;

namespace TwinCATRx.Tests.Rx;

/// <summary>Generated stream test fixture.</summary>
[TwinCatReactiveStream(".A", typeof(int), PropertyName = "AValue", ObservableName = "AValueObservable")]
internal sealed partial class GeneratedStreams
{
    /// <summary>Gets the generated type marker.</summary>
    internal string GeneratedTypeMarker => nameof(GeneratedStreams);
}
