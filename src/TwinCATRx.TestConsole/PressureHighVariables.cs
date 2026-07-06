// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace TwinCATRx.TestConsole;

/// <summary>Shared live PLC variable paths for the pressure high PV examples.</summary>
internal static class PressureHighVariables
{
    /// <summary>Stores the live PLC ADS address.</summary>
    public const string AdsAddress = "10.1.180.147.1.1";

    /// <summary>Stores the root TwinCAT structure variable.</summary>
    public const string RootVariable = "GlobalVariables.Rig";

    /// <summary>Stores the relative observed pressure process value variable.</summary>
    public const string RelativeObservedVariable = "Casing.Pressure.High.PV.Value";

    /// <summary>Stores the relative writable pressure simulation variable.</summary>
    public const string RelativeSimulationVariable = "Casing.Pressure.High.PV.SimulationVal";

    /// <summary>Stores the full observed pressure process value variable.</summary>
    public const string FullObservedVariable = RootVariable + "." + RelativeObservedVariable;

    /// <summary>Stores the full writable pressure simulation variable.</summary>
    public const string FullSimulationVariable = RootVariable + "." + RelativeSimulationVariable;

    /// <summary>Stores the live PLC ADS port.</summary>
    public const int AdsPort = 851;
}
