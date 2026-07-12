// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Reflection;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Disposables;
using ReactiveBridge = CP.TwinCatRx.Reactive.ObservableBridgeExtensions;
using ReactiveExtensions = CP.TwinCatRx.Reactive.TwinCatRxExtensions;
using ReactiveRxClient = CP.TwinCatRx.Reactive.RxTcAdsClient;

namespace TwinCATRx.Tests.Rx;

/// <summary>Exercises the System.Reactive runtime implementation without a live ADS endpoint.</summary>
public class ReactiveRuntimeCoverageTests
{
    /// <summary>The expected observed value.</summary>
    private const int ExpectedValue = 42;

    /// <summary>The value emitted after cancellation to prove it is ignored.</summary>
    private const int IgnoredValue = 99;

    /// <summary>The identifier selected by the test.</summary>
    private const string TargetId = "target";

    /// <summary>The variable selected by the test.</summary>
    private const string Variable = ".Value";

    /// <summary>The expected exception message.</summary>
    private const string ExpectedErrorMessage = "expected";

    /// <summary>A representative local ADS handle.</summary>
    private const uint Handle = 17;

    /// <summary>A representative PLC variable.</summary>
    private const string ScalarVariable = ".Scalar";

    /// <summary>Verifies Reactive observations filter variable names and identifiers.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Observe_Filters_Variable_Id_And_Null_Data()
    {
        var data = new[]
        {
            (Variable: ".Other", Data: (object?)1, Id: (string?)TargetId),
            (Variable, Data: (object?)null, Id: (string?)TargetId),
            (Variable: ".value", Data: (object?)ExpectedValue, Id: (string?)TargetId),
            (Variable, Data: (object?)IgnoredValue, Id: (string?)"other"),
        };
        var client = new ReactiveRxFakeClient(System.Reactive.Linq.Observable.ToObservable(data));

        var values = new List<int>();
        using var subscription = ReactiveExtensions.Observe<int>(client, ".VALUE", TargetId).Subscribe(new RecordingObserver<int>(values));

        await TUnitAssert.That(values.Count).IsEqualTo(1);
        await TUnitAssert.That(values[0]).IsEqualTo(ExpectedValue);
    }

    /// <summary>Verifies both Reactive observation overloads bridge to async observables.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task ObserveAsyncObservable_Bridges_Both_Overloads()
    {
        var data = System.Reactive.Linq.Observable.Return((Variable, Data: (object?)ExpectedValue, Id: (string?)"id"));
        var client = new ReactiveRxFakeClient(data);
        var directValues = new List<int>();
        var identifiedValues = new List<int>();

        await using var direct = await ReactiveExtensions.ObserveAsyncObservable<int>(client, Variable)
            .SubscribeAsync(new RecordingAsyncObserver<int>(directValues), CancellationToken.None);
        await using var identified = await ReactiveExtensions.ObserveAsyncObservable<int>(client, Variable, "id")
            .SubscribeAsync(new RecordingAsyncObserver<int>(identifiedValues), CancellationToken.None);

        await TUnitAssert.That(directValues).IsEquivalentTo([ExpectedValue]);
        await TUnitAssert.That(identifiedValues).IsEquivalentTo([ExpectedValue]);
    }

    /// <summary>Verifies bridge validation and all notification callbacks.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task ObservableBridge_Validates_And_Forwards_Notifications()
    {
        var values = new List<int>();
        var completed = false;
        Exception? receivedError = null;
        using var completedSubscription = ReactiveBridge.SubscribeTo(
            System.Reactive.Linq.Observable.Return(ExpectedValue),
            values.Add,
            error => receivedError = error,
            () => completed = true);
        var expectedError = new InvalidOperationException(ExpectedErrorMessage);
        using var errorSubscription = ReactiveBridge.SubscribeTo(
            System.Reactive.Linq.Observable.Throw<int>(expectedError),
            values.Add,
            error => receivedError = error,
            () => completed = false);
        using var ignoredSubscription = ReactiveBridge.SubscribeTo(System.Reactive.Linq.Observable.Return(ExpectedValue));

        await TUnitAssert.That(values).IsEquivalentTo([ExpectedValue]);
        await TUnitAssert.That(completed).IsTrue();
        await TUnitAssert.That(receivedError).IsSameReferenceAs(expectedError);
        await TUnitAssert.That(() => ReactiveBridge.ToAsyncObservable<int>(null!)).Throws<ArgumentNullException>();
        await TUnitAssert.That(() => ReactiveBridge.SubscribeTo<int>(null!, _ => { }, _ => { }, () => { })).Throws<ArgumentNullException>();
        await TUnitAssert.That(() => ReactiveBridge.SubscribeTo(System.Reactive.Linq.Observable.Empty<int>(), null!, _ => { }, () => { })).Throws<ArgumentNullException>();
        await TUnitAssert.That(() => ReactiveBridge.SubscribeTo(System.Reactive.Linq.Observable.Empty<int>(), _ => { }, null!, () => { })).Throws<ArgumentNullException>();
        await TUnitAssert.That(() => ReactiveBridge.SubscribeTo(System.Reactive.Linq.Observable.Empty<int>(), _ => { }, _ => { }, null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies the default bridge error handler rethrows the original exception.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task ObservableBridge_Default_Error_Handler_Rethrows()
    {
        var expected = new InvalidOperationException(ExpectedErrorMessage);

        var exception = await TUnitAssert.That(() => ReactiveBridge.SubscribeTo(System.Reactive.Linq.Observable.Throw<int>(expected), _ => { })).Throws<InvalidOperationException>();

        await TUnitAssert.That(exception).IsSameReferenceAs(expected);
    }

    /// <summary>Verifies async bridge next, error, completion, cancellation, and idempotent disposal paths.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task AsyncBridge_Forwards_Notifications_And_Honors_Cancellation()
    {
        using var source = new Subject<int>();
        using var cancellation = new CancellationTokenSource();
        var observer = new RecordingAsyncObserver<int>([]);
        var asyncSource = ReactiveBridge.ToAsyncObservable(source);
        await using var subscription = await asyncSource.SubscribeAsync(observer, cancellation.Token);

        source.OnNext(ExpectedValue);
        source.OnError(new InvalidOperationException(ExpectedErrorMessage));
#if NET9_0_OR_GREATER
        await cancellation.CancelAsync();
#else
        cancellation.Cancel();
#endif
        source.OnNext(IgnoredValue);
        await subscription.DisposeAsync();

        await TUnitAssert.That(observer.Values).IsEquivalentTo([ExpectedValue]);
        await TUnitAssert.That(observer.Errors.Count).IsEqualTo(1);
        await TUnitAssert.That(observer.CompletionCount).IsEqualTo(0);
        var nullObserverRejected = false;
        try
        {
            _ = await asyncSource.SubscribeAsync(null!, CancellationToken.None);
        }
        catch (ArgumentNullException)
        {
            nullObserverRejected = true;
        }

        await TUnitAssert.That(nullObserverRejected).IsTrue();
    }

    /// <summary>Verifies async completion is forwarded.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task AsyncBridge_Forwards_Completion()
    {
        var observer = new RecordingAsyncObserver<int>([]);

        await using var subscription = await ReactiveBridge.ToAsyncObservable(System.Reactive.Linq.Observable.Return(ExpectedValue))
            .SubscribeAsync(observer, CancellationToken.None);

        await TUnitAssert.That(observer.Values).IsEquivalentTo([ExpectedValue]);
        await TUnitAssert.That(observer.CompletionCount).IsEqualTo(1);
    }

    /// <summary>Verifies the Reactive client's disconnected public surface and handle maps.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Reactive_Client_Exposes_Disconnected_Surface()
    {
        using var client = new ReactiveRxClient();
        client.ReadWriteHandleInfo[ScalarVariable] = Handle;
        client.WriteHandleInfo[ScalarVariable] = (Handle, 1);

        await TUnitAssert.That(client.Code).IsNotNull();
        await TUnitAssert.That(client.InitializeComplete).IsNotNull();
        await TUnitAssert.That(client.InitializeCompleteAsync).IsNotNull();
        await TUnitAssert.That(client.DataReceived).IsNotNull();
        await TUnitAssert.That(client.DataReceivedAsync).IsNotNull();
        await TUnitAssert.That(client.ErrorReceived).IsNotNull();
        await TUnitAssert.That(client.ErrorReceivedAsync).IsNotNull();
        await TUnitAssert.That(client.OnWrite).IsNotNull();
        await TUnitAssert.That(client.OnWriteAsync).IsNotNull();
        await TUnitAssert.That(client.IsPausedObservable).IsNotNull();
        await TUnitAssert.That(client.IsPausedObservableAsync).IsNotNull();
        await TUnitAssert.That(client.ReadWriteHandleInfo.ContainsKey(ScalarVariable.ToUpperInvariant())).IsTrue();
        await TUnitAssert.That(client.WriteHandleInfo.ContainsKey(ScalarVariable.ToLowerInvariant())).IsTrue();
        await TUnitAssert.That(client.Settings).IsNull();
        await TUnitAssert.That(client.Connected).IsFalse();
    }

    /// <summary>Verifies disconnected pause, read, write, disconnect, and disposal paths.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task Reactive_Client_Local_Lifecycle_Is_Deterministic()
    {
        var client = new ReactiveRxClient();
        var errors = new List<Exception>();
        using var errorSubscription = client.ErrorReceived.Subscribe(new RecordingObserver<Exception>(errors));

        client.Pause(TimeSpan.Zero);
        client.Read(string.Empty);
        client.Write(string.Empty, ExpectedValue);
        await TUnitAssert.That(errors.Count).IsEqualTo(1);
        await TUnitAssert.That(errors[0]).IsTypeOf<ObjectDisposedException>();

        InitializeCleanup(client);
        client.ReadWriteHandleInfo[ScalarVariable] = Handle;
        GetPrivateField<Dictionary<string, Type>>(client, "_typeInfo")[ScalarVariable] = typeof(int);
        client.Read(ScalarVariable, id: TargetId);
        client.Write(ScalarVariable, ExpectedValue, TargetId);
        client.Pause(TimeSpan.Zero);
        client.Disconnect();
        client.Dispose();
        client.Dispose();

        await TUnitAssert.That(client.IsDisposed).IsTrue();
        await TUnitAssert.That(client.Connected).IsFalse();
        await TUnitAssert.That(client.ReadWriteHandleInfo).IsEmpty();
        await TUnitAssert.That(client.WriteHandleInfo).IsEmpty();
    }

    /// <summary>Gets a private instance field.</summary>
    /// <typeparam name="T">The field type.</typeparam>
    /// <param name="instance">The containing instance.</param>
    /// <param name="name">The field name.</param>
    /// <returns>The field value.</returns>
    private static T GetPrivateField<T>(object instance, string name) =>
        (T)(instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance)
            ?? throw new InvalidOperationException("Private field was not found: " + name));

    /// <summary>Initializes local disposable state without connecting to ADS.</summary>
    /// <param name="client">The client.</param>
    private static void InitializeCleanup(ReactiveRxClient client) =>
        typeof(ReactiveRxClient).GetField("_cleanup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(client, new MultipleDisposable());

    /// <summary>Records async observer calls synchronously for deterministic assertions.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    /// <param name="values">The value collection.</param>
    private sealed class RecordingAsyncObserver<T>(List<T> values) : IObserverAsync<T>
    {
        /// <summary>Gets the observed values.</summary>
        public List<T> Values { get; } = values;

        /// <summary>Gets observed errors.</summary>
        public List<Exception> Errors { get; } = [];

        /// <summary>Gets the completion count.</summary>
        public int CompletionCount { get; private set; }

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => default;

        /// <inheritdoc/>
        public ValueTask OnCompletedAsync(Result result)
        {
            CompletionCount++;
            return default;
        }

        /// <inheritdoc/>
        public ValueTask OnErrorResumeAsync(Exception error, CancellationToken cancellationToken)
        {
            Errors.Add(error);
            return default;
        }

        /// <inheritdoc/>
        public ValueTask OnNextAsync(T value, CancellationToken cancellationToken)
        {
            Values.Add(value);
            return default;
        }
    }

    /// <summary>Records synchronous observable values in a supplied list.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    /// <param name="values">The destination list.</param>
    private sealed class RecordingObserver<T>(List<T> values) : IObserver<T>
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
