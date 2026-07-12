// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using CP.Collections;
using CP.TwinCatRx;
using CP.TwinCatRx.Core;

namespace TwinCATRx.TestConsole;

/// <summary>The main entry point for the application.</summary>
internal static class Program
{
    /// <summary>Stores the HashTableRx test option.</summary>
    private const string HashTableOption = "1";

    /// <summary>Stores the source-generated test option.</summary>
    private const string SourceGeneratedOption = "2";

    /// <summary>Stores the PLC notification cycle in milliseconds.</summary>
    private const int NotificationCycleMilliseconds = 100;

    /// <summary>Stores the initialization polling delay in milliseconds.</summary>
    private const int InitializationPollMilliseconds = 100;

    /// <summary>Stores the minimum simulated pressure.</summary>
    private const float MinimumSimulatedPressure = 101F;

    /// <summary>Stores the maximum simulated pressure.</summary>
    private const float MaximumSimulatedPressure = 160F;

    /// <summary>Stores the simulated pressure increment.</summary>
    private const float SimulatedPressureIncrement = 5F;

    /// <summary>Provides a replaceable output abstraction for the console sample.</summary>
    private static readonly TextWriter Output = Console.Out;

    /// <summary>Provides a replaceable input abstraction for the console sample.</summary>
    private static readonly TextReader Input = Console.In;

    /// <summary>Stores the simulation write interval.</summary>
    private static readonly TimeSpan SimulationWriteInterval = TimeSpan.FromSeconds(5);

    /// <summary>Runs the test console.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The application task.</returns>
    [RequiresDynamicCode("RxTcAdsClient generates PLC structure types at runtime.")]
    [RequiresUnreferencedCode("RxTcAdsClient uses reflection to materialize PLC types.")]
    public static async Task Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        try
        {
            await RunSelectedOptionAsync(GetSelectedOption(args), cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            await Output.WriteLineAsync("Stopping pressure simulation example.").ConfigureAwait(false);
        }
    }

    /// <summary>Gets the selected test option.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The selected option.</returns>
    private static string GetSelectedOption(string[] args)
    {
        if (args.Length > 0 && IsKnownOption(args[0]))
        {
            return args[0];
        }

        Output.WriteLine("Select a live TwinCATRx test:");
        Output.WriteLine("1. HashTableRx structure observation and write");
        Output.WriteLine("2. Source-generated structured observation and write");
        Output.Write("Option: ");
        var selected = Input.ReadLine();
        return IsKnownOption(selected) ? selected! : HashTableOption;
    }

    /// <summary>Gets whether a console option is known.</summary>
    /// <param name="option">The option text.</param>
    /// <returns><c>true</c> when the option is known.</returns>
    private static bool IsKnownOption(string? option) =>
        string.Equals(option, HashTableOption, StringComparison.Ordinal)
        || string.Equals(option, SourceGeneratedOption, StringComparison.Ordinal);

    /// <summary>Runs the selected live test option.</summary>
    /// <param name="option">The selected option.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The run task.</returns>
    [RequiresDynamicCode("RxTcAdsClient generates PLC structure types at runtime.")]
    [RequiresUnreferencedCode("RxTcAdsClient uses reflection to materialize PLC types.")]
    private static Task RunSelectedOptionAsync(string option, CancellationToken cancellationToken) =>
        option == SourceGeneratedOption
            ? RunSourceGeneratedTestAsync(cancellationToken)
            : RunHashTableTestAsync(cancellationToken);

    /// <summary>Runs the HashTableRx structure-based live test.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The run task.</returns>
    [RequiresDynamicCode("RxTcAdsClient generates PLC structure types at runtime.")]
    [RequiresUnreferencedCode("RxTcAdsClient uses reflection to materialize PLC types.")]
    private static Task RunHashTableTestAsync(CancellationToken cancellationToken)
    {
        PrintTestHeader("HashTableRx structure observation and write");
        return RunClientAsync(
            CreateHashTableSettings(),
            async (client, token) =>
            {
                using var values = client.CreateStruct(PressureHighVariables.RootVariable)
                    ?? throw new InvalidOperationException("The PLC structure could not be created.");
                await WaitForStructureReadyAsync(values, token).ConfigureAwait(false);
                using var observedSubscription = values.Observe<float>(PressureHighVariables.RelativeObservedVariable).SubscribeTo(PrintObservedPressure);
                await WriteSimulationValuesAsync(value => WriteHashTableSimulationValueAsync(values, value), token).ConfigureAwait(false);
            },
            cancellationToken);
    }

    /// <summary>Runs the source-generated live test.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The run task.</returns>
    [RequiresDynamicCode("RxTcAdsClient generates PLC structure types at runtime.")]
    [RequiresUnreferencedCode("RxTcAdsClient uses reflection to materialize PLC types.")]
    private static Task RunSourceGeneratedTestAsync(CancellationToken cancellationToken)
    {
        PrintTestHeader("Source-generated structured observation and write");
        var generated = new PressureHighSourceGeneratedStreams();
        return RunClientAsync(
            generated.CreateTwinCatRxSettings(),
            async (client, token) =>
            {
                using var binding = generated.BindTwinCatRx(client);
                using var observedSubscription = generated.PressureHighValueObservable.SubscribeTo(PrintGeneratedObservedPressure);
                await WriteSimulationValuesAsync(value => WriteGeneratedSimulationValueAsync(generated, value), token).ConfigureAwait(false);
            },
            cancellationToken);
    }

    /// <summary>Runs a live test with a configured TwinCATRx client.</summary>
    /// <param name="settings">The client settings.</param>
    /// <param name="runAsync">The option-specific run function.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The run task.</returns>
    [RequiresDynamicCode("RxTcAdsClient generates PLC structure types at runtime.")]
    [RequiresUnreferencedCode("RxTcAdsClient uses reflection to materialize PLC types.")]
    private static async Task RunClientAsync(Settings settings, Func<RxTcAdsClient, CancellationToken, Task> runAsync, CancellationToken cancellationToken)
    {
        using var client = new RxTcAdsClient();
        using var initializeSubscription = client.InitializeComplete.SubscribeTo(_ => Output.WriteLine("TwinCATRx client initialized."));
        using var errorSubscription = client.ErrorReceived.SubscribeTo(error => Output.WriteLine("ADS error: " + error));
        try
        {
            client.Connect(settings);
            await WaitForInitializationAsync(client, cancellationToken).ConfigureAwait(false);
            await runAsync(client, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            client.Disconnect();
        }
    }

    /// <summary>Prints a selected test header.</summary>
    /// <param name="testName">The test name.</param>
    private static void PrintTestHeader(string testName)
    {
        Output.WriteLine("Running " + testName + ".");
        Output.WriteLine("Connecting to ADS " + PressureHighVariables.AdsAddress + ":" + PressureHighVariables.AdsPort + ".");
        Output.WriteLine("Observing " + PressureHighVariables.FullObservedVariable + ".");
        Output.WriteLine("Writing " + PressureHighVariables.FullSimulationVariable + " with values above 100. Press Ctrl+C to stop.");
    }

    /// <summary>Creates the HashTableRx structure test settings.</summary>
    /// <returns>The live ADS settings.</returns>
    private static Settings CreateHashTableSettings()
    {
        var settings = CreateBaseSettings("PressureHighHashTableExample");
        settings.AddNotification(PressureHighVariables.RootVariable, cycleTime: NotificationCycleMilliseconds);
        return settings;
    }

    /// <summary>Creates base live ADS settings.</summary>
    /// <param name="settingsId">The settings identifier.</param>
    /// <returns>The live ADS settings.</returns>
    private static Settings CreateBaseSettings(string settingsId)
    {
        const string adsAddress = PressureHighVariables.AdsAddress;
        return new Settings
        {
            AdsAddress = adsAddress,
            Port = PressureHighVariables.AdsPort,
            SettingsId = settingsId
        };
    }

    /// <summary>Prints an observed pressure value.</summary>
    /// <param name="value">The observed value.</param>
    private static void PrintObservedPressure(float value) =>
        Output.WriteLine(DateTimeOffset.Now.ToString("HH:mm:ss.fff") + " " + PressureHighVariables.FullObservedVariable + " = " + value.ToString("F3"));

    /// <summary>Prints a source-generated observed pressure value.</summary>
    /// <param name="value">The observed value.</param>
    private static void PrintGeneratedObservedPressure(float value) =>
        PrintObservedPressure(value);

    /// <summary>Waits until the ADS client has completed initialization.</summary>
    /// <param name="client">The ADS client.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The wait task.</returns>
    private static async Task WaitForInitializationAsync(RxTcAdsClient client, CancellationToken cancellationToken)
    {
        while (!client.Connected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(InitializationPollMilliseconds, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Waits until the HashTableRx structure has been populated from the PLC.</summary>
    /// <param name="values">The HashTableRx values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The wait task.</returns>
    private static async Task WaitForStructureReadyAsync(HashTableRx values, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void SetException(Exception error) => _ = completion.TrySetException(error);

        await using var cancellationRegistration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        using var subscription = values.StructureReady().SubscribeTo(
            _ => completion.TrySetResult(true),
            SetException,
            static () => { });
        await completion.Task.ConfigureAwait(false);
    }

    /// <summary>Writes continuously changing simulation values above 100.</summary>
    /// <param name="writeValueAsync">The write function.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The write loop task.</returns>
    private static async Task WriteSimulationValuesAsync(Func<float, Task> writeValueAsync, CancellationToken cancellationToken)
    {
        var value = MinimumSimulatedPressure;
        while (!cancellationToken.IsCancellationRequested)
        {
            await writeValueAsync(value).ConfigureAwait(false);
            await Output.WriteLineAsync(
                ("Wrote " + PressureHighVariables.FullSimulationVariable + " = " + value.ToString("F1")).AsMemory(),
                cancellationToken).ConfigureAwait(false);
            value = GetNextSimulationValue(value);
            await Task.Delay(SimulationWriteInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Writes a HashTableRx simulation value.</summary>
    /// <param name="values">The HashTableRx values.</param>
    /// <param name="value">The simulation value.</param>
    /// <returns>The write task.</returns>
    private static async Task WriteHashTableSimulationValueAsync(HashTableRx values, float value)
    {
        var written = await values.WriteValuesAsync(
            hashTable => hashTable.Value(PressureHighVariables.RelativeSimulationVariable, value),
            SimulationWriteInterval).ConfigureAwait(false);
        if (written)
        {
            return;
        }

        throw new InvalidOperationException("The HashTableRx simulation value write was not queued.");
    }

    /// <summary>Writes a source-generated simulation value.</summary>
    /// <param name="generated">The source-generated PLC binding.</param>
    /// <param name="value">The simulation value.</param>
    /// <returns>The write task.</returns>
    private static Task WriteGeneratedSimulationValueAsync(PressureHighSourceGeneratedStreams generated, float value)
    {
        generated.WriteTwinCatRx((nameof(PressureHighSourceGeneratedStreams.PressureHighSimulationValue), value));
        return Task.CompletedTask;
    }

    /// <summary>Gets the next pressure simulation value.</summary>
    /// <param name="value">The current value.</param>
    /// <returns>The next value.</returns>
    private static float GetNextSimulationValue(float value) =>
        value >= MaximumSimulatedPressure ? MinimumSimulatedPressure : value + SimulatedPressureIncrement;
}
