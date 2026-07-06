// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using CP.TwinCatRx;

namespace TwinCATRx.Tests.Rx;

/// <summary>Generated PLC connection test fixture.</summary>
[TwinCatPlcConnection("1.2.3.4.5.6", 851, SettingsId = "GeneratedSettings")]
internal sealed partial class GeneratedPlcConnection
{
    /// <summary>Gets the direct notification value.</summary>
    [DirectNotification(".DirectValue", CycleTime = 50, CanWrite = true)]
    public int DirectValue { get; private set; }

    /// <summary>Gets the structured notification value.</summary>
    [StructuredNotification(".Struct", "Nested.Value", CycleTime = 200, CanWrite = false)]
    public int StructuredValue { get; private set; }

    /// <summary>Gets the writable structured notification value.</summary>
    [StructuredNotification(".Struct", "Nested.Writable", CycleTime = 200, CanWrite = true)]
    public int StructuredWritableValue { get; private set; }

    /// <summary>Gets the write-only value.</summary>
    [WriteOnly(".WriteOnly", Id = "write-only")]
    public int WriteOnlyValue { get; private set; }

    /// <summary>Gets the structure-backed write-only value.</summary>
    [WriteOnly(".Struct.Nested.WriteOnly")]
    public int StructuredWriteOnlyValue { get; private set; }
}
