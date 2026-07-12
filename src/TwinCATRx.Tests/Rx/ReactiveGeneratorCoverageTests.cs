// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using ReactiveBridge = CP.TwinCatRx.Reactive.ObservableBridgeExtensions;

namespace TwinCATRx.Tests.Rx;

/// <summary>Exercises generated Reactive bindings with an in-memory client.</summary>
public class ReactiveGeneratorCoverageTests
{
    /// <summary>The generated direct variable address.</summary>
    private const string DirectVariable = ".ReactiveDirect";

    /// <summary>The generated value.</summary>
    private const int ExpectedValue = 73;

    /// <summary>The expected number of generated direct writes.</summary>
    private const int ExpectedWriteCount = 2;

    /// <summary>Verifies the generated Reactive legacy stream binds and validates clients.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Generated_Reactive_Stream_Binds_And_Validates_Client()
    {
        var client = new ReactiveRxFakeClient(System.Reactive.Linq.Observable.Return((Variable: ".ReactiveA", Data: (object?)ExpectedValue, Id: (string?)null)));
        var generated = new GeneratedReactiveStreams();
        var observed = new List<int?>();
        using var observation = generated.ReactiveAValueObservable.Subscribe(new ListObserver<int?>(observed));

        using var binding = generated.BindTwinCatRx(client);

        await TUnitAssert.That(generated.ReactiveAValue).IsEqualTo(ExpectedValue);
        await TUnitAssert.That(observed).Contains(ExpectedValue);
        await TUnitAssert.That(() => generated.BindTwinCatRx(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies generated Reactive connection settings, binding, reads, and writes.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Generated_Reactive_Connection_Executes_Direct_Binding_Read_And_Write()
    {
        using var data = new Subject<(string Variable, object? Data, string? Id)>();
        var client = new ReactiveRxFakeClient(data);
        var generated = new GeneratedReactivePlcConnection();
        var observed = new List<int>();
        using var observation = ReactiveBridge.SubscribeTo(generated.ReactiveDirectValueObservable, observed.Add);
        var settings = generated.CreateTwinCatRxSettings();

        using var binding = generated.BindTwinCatRx(client);
        data.OnNext((DirectVariable, ExpectedValue, null));
        generated.ReadReactiveDirectValue();
        generated.WriteReactiveDirectValue(ExpectedValue + 1);
        generated.WriteTwinCatRx((nameof(GeneratedReactivePlcConnection.ReactiveDirectValue), ExpectedValue + ExpectedWriteCount));

        await TUnitAssert.That(settings.AdsAddress).IsEqualTo("6.5.4.3.2.1");
        await TUnitAssert.That(settings.Notifications.Count).IsEqualTo(1);
        await TUnitAssert.That(settings.WriteVariables.Count).IsEqualTo(1);
        await TUnitAssert.That(generated.ReactiveDirectValue).IsEqualTo(ExpectedValue);
        await TUnitAssert.That(observed).Contains(ExpectedValue);
        await TUnitAssert.That(client.ReadCalls.Count).IsEqualTo(1);
        await TUnitAssert.That(client.ReadCalls[0].Variable).IsEqualTo(DirectVariable);
        await TUnitAssert.That(client.WriteCalls.Count).IsEqualTo(ExpectedWriteCount);
        await TUnitAssert.That(client.WriteCalls[0].Variable).IsEqualTo(DirectVariable);
        await TUnitAssert.That(generated.TwinCatRxClient).IsSameReferenceAs(client);
        await TUnitAssert.That(() => new GeneratedReactivePlcConnection().ReadReactiveDirectValue()).Throws<InvalidOperationException>();
        await TUnitAssert.That(() => generated.BindTwinCatRx(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Records observable values in a supplied list.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    /// <param name="values">The destination list.</param>
    private sealed class ListObserver<T>(List<T> values) : IObserver<T>
    {
        /// <inheritdoc/>
        public void OnCompleted()
        {
        }

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
        }

        /// <inheritdoc/>
        public void OnNext(T value) => values.Add(value);
    }
}
