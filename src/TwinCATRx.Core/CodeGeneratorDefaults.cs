// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace CP.TwinCatRx.Core.Reactive;
#else
namespace CP.TwinCatRx.Core;
#endif

/// <summary>Provides shared compile-time defaults for code generation.</summary>
internal static class CodeGeneratorDefaults
{
    /// <summary>The default namespace assigned to generated source.</summary>
    internal const string Namespace = "TwinCATRx";

    /// <summary>The default TwinCAT system service ADS port.</summary>
    internal const int AdsPort = 801;
}
