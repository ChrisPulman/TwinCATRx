// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
#if WINDOWS
using System.ServiceProcess;
#endif
using System.Runtime.InteropServices;
using CP.TwinCatRx.Core;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;
using RxNotification = CP.TwinCatRx.Core.INotification;

namespace CP.TwinCatRx;

/// <summary>Observable TwinCAT ADS Client.</summary>
public class RxTcAdsClient : IRxTcAdsClient
{
    /// <summary>Publishes ADS client state changes.</summary>
    private readonly Signal<AdsState> _clientState = new();

    /// <summary>Publishes generated code snapshots.</summary>
    private readonly Signal<string[]> _codeSubject = new();

    /// <summary>Publishes data read from PLC variables.</summary>
    private readonly Signal<(string Variable, object? Data, string? Id)> _dataReceived = new();

    /// <summary>Publishes client errors.</summary>
    private readonly Signal<Exception> _errorReceived = new();

    /// <summary>Publishes write results.</summary>
    private readonly Signal<string?> _onWriteSubject = new();

    /// <summary>Queues PLC read requests.</summary>
    private readonly Signal<(uint? handle, Type type, int length, string? id)> _readPLC = new();

    /// <summary>Publishes TwinCAT service status updates.</summary>
    private readonly ReplaySignal<ServiceStatus> _serviceStatus = new(1);

    /// <summary>Queues PLC write requests.</summary>
    private readonly Signal<(uint? handle, object value, int length, string? id)> _writePLC = new();

    /// <summary>Publishes PLC initialization completion.</summary>
    private readonly ReplaySignal<Unit> _initCompleteSubject = new(1);

    /// <summary>Publishes pause state changes.</summary>
    private readonly ReplaySignal<bool> _isPausedSubject = new(1);

    /// <summary>Stores generated code payloads.</summary>
    private readonly List<string> _code = [];

    /// <summary>Stores resolved PLC variable types by variable name.</summary>
    private readonly Dictionary<string, Type> _typeInfo = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Maps read-write ADS handles to variable names.</summary>
    private readonly Dictionary<uint, string> _readWriteVariablesByHandle = [];

    /// <summary>Maps write ADS handles to variable names.</summary>
    private readonly Dictionary<uint, string> _writeVariablesByHandle = [];

    /// <summary>Stores disposable resources owned by this client.</summary>
    private CompositeDisposable? _cleanup;

    /// <summary>Stores the dynamic PLC code generator.</summary>
    private CodeGenerator? _codeGenerator;

    /// <summary>Stores the active PLC initialization subscription.</summary>
    private IDisposable? _plcCleanup;

    /// <summary>Gets codes this instance.</summary>
    /// <returns>A Value.</returns>
    public IObservable<string[]> Code => _codeSubject.Retry(int.MaxValue).Publish().RefCount();

    /// <summary>Gets a value indicating whether this <see cref="RxTcAdsClient"/> is connected.</summary>
    /// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
    public bool Connected { get; internal set; }

    /// <summary>Gets the initialize complete. PLC is ready to read and write.</summary>
    /// <value>
    /// The initialize complete.
    /// </value>
    public IObservable<Unit> InitializeComplete => _initCompleteSubject.Retry(int.MaxValue).Publish().RefCount();

    /// <inheritdoc/>
    public IObservableAsync<Unit> InitializeCompleteAsync => InitializeComplete.ToAsyncObservable();

    /// <summary>Gets the data received.</summary>
    /// <value>The data received.</value>
    public IObservable<(string Variable, object? Data, string? Id)> DataReceived => _dataReceived.Retry(int.MaxValue).Publish().RefCount();

    /// <inheritdoc/>
    public IObservableAsync<(string Variable, object? Data, string? Id)> DataReceivedAsync => DataReceived.ToAsyncObservable();

    /// <summary>Gets error received.</summary>
    /// <returns>A Value.</returns>
    public IObservable<Exception> ErrorReceived => _errorReceived.Retry(int.MaxValue).Publish().RefCount();

    /// <inheritdoc/>
    public IObservableAsync<Exception> ErrorReceivedAsync => ErrorReceived.ToAsyncObservable();

    /// <summary>Gets a value indicating whether gets a value that indicates whether the object is disposed.</summary>
    public bool IsDisposed => _cleanup?.IsDisposed ?? false;

    /// <summary>Gets the on write.</summary>
    /// <value>The on write.</value>
    public IObservable<string?> OnWrite => _onWriteSubject.Retry(int.MaxValue).Publish().RefCount();

    /// <inheritdoc/>
    public IObservableAsync<string?> OnWriteAsync => OnWrite.ToAsyncObservable();

    /// <summary>Gets the read write handle information.</summary>
    /// <value>The read write handle information.</value>
    public IDictionary<string, uint?> ReadWriteHandleInfo { get; } = new Dictionary<string, uint?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the write handle information.</summary>
    /// <value>The write handle information.</value>
    public IDictionary<string, (uint? Handle, int ArrayLength)> WriteHandleInfo { get; } = new Dictionary<string, (uint? Handle, int ArrayLength)>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the settings.</summary>
    /// <value>
    /// The settings.
    /// </value>
    public ISettings? Settings { get; private set; }

    /// <summary>Gets a value indicating whether this instance is paused.</summary>
    /// <value>
    ///   <c>true</c> if this instance is paused; otherwise, <c>false</c>.
    /// </value>
    public bool IsPaused { get; private set; }

    /// <summary>Gets the is paused observable.</summary>
    /// <value>
    /// The is paused observable.
    /// </value>
    public IObservable<bool> IsPausedObservable => _isPausedSubject.Retry(int.MaxValue).Publish().RefCount();

    /// <inheritdoc/>
    public IObservableAsync<bool> IsPausedObservableAsync => IsPausedObservable.ToAsyncObservable();

    /// <summary>Connects the specified settings.</summary>
    /// <param name="settings">The settings.</param>
    /// <exception cref="Exception">An Exception.</exception>
    [RequiresUnreferencedCode("Invokes dynamic code generation and reflection to materialize PLC types.")]
    [RequiresDynamicCode("Invokes dynamic code generation and reflection to materialize PLC types.")]
    public void Connect(ISettings settings)
    {
        if (_cleanup?.IsDisposed == true)
        {
            _errorReceived.OnNext(new ObjectDisposedException(nameof(RxTcAdsClient)));
            return;
        }

        try
        {
            if (_plcCleanup is null)
            {
                Settings = settings;
                Connected = false;
                _plcCleanup = InitPLC().SubscribeTo();
            }
        }
        catch (Exception ex)
        {
            _errorReceived.OnNext(ex);
        }
    }

    /// <summary>Disconnects this instance.</summary>
    public void Disconnect()
    {
        _plcCleanup?.Dispose();
        _plcCleanup = null;
        Connected = false;
        if (!IsPaused)
        {
            return;
        }

        IsPaused = false;
        _isPausedSubject.OnNext(IsPaused);
    }

    /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Reads the specified variable.</summary>
    /// <param name="variable">The data.</param>
    /// <param name="arrayLength">Length of the array.</param>
    /// <param name="id">The identifier.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Parameters - Parameter 0 must be set to the size of the Array.</exception>
    public void Read(string variable, int? arrayLength = null, string? id = null)
    {
        if (!TryGetReadTarget(variable, arrayLength, out var handle, out var type, out var readLength) || type is null)
        {
            return;
        }

        if (type.IsArray || type == typeof(string))
        {
            ReadArrayHandle(handle, type, readLength, id);
        }
        else
        {
            ReadHandle(handle, type, id);
        }
    }

    /// <summary>Writes the specified variable.</summary>
    /// <param name="variable">The variable.</param>
    /// <param name="value">The value.</param>
    /// <param name="id">The identifier.</param>
    public void Write(string variable, object value, string? id = null)
    {
        if (string.IsNullOrWhiteSpace(variable))
        {
            return;
        }

        if (ReadWriteHandleInfo.TryGetValue(variable, out var readWritehandle))
        {
            WriteHandle(readWritehandle, value, id: id);
            return;
        }

        if (!WriteHandleInfo.TryGetValue(variable, out var item))
        {
            return;
        }

        WriteHandle(item.Handle, value, item.ArrayLength, id: id);
    }

    /// <summary>Pauses the specified time.</summary>
    /// <param name="time">The time.</param>
    public void Pause(TimeSpan time)
    {
        if (_cleanup?.IsDisposed != false)
        {
            _errorReceived.OnNext(new ObjectDisposedException(nameof(RxTcAdsClient)));
            return;
        }

        if (time.TotalMilliseconds <= 0)
        {
            return;
        }

        var cleanup = new CompositeDisposable();
        _ = cleanup!.DisposeWith(_cleanup!);
        IsPaused = true;
        _isPausedSubject.OnNext(IsPaused);
        _ = Observable.Timer(time).SubscribeTo(_ =>
        {
            IsPaused = false;
            _isPausedSubject.OnNext(IsPaused);
            cleanup?.Dispose();
        }).DisposeWith(cleanup!);
    }

    /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
    /// unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_cleanup?.IsDisposed != false || !disposing)
        {
            return;
        }

        _plcCleanup?.Dispose();
        _cleanup?.Dispose();
        _code.Clear();
        ReadWriteHandleInfo.Clear();
        _typeInfo.Clear();
        WriteHandleInfo.Clear();
        _clientState.Dispose();
        _codeSubject.Dispose();
        _errorReceived.Dispose();
        _onWriteSubject.Dispose();
        _serviceStatus.Dispose();
        _readPLC.Dispose();
        _writePLC.Dispose();
        _dataReceived.Dispose();
        _initCompleteSubject.Dispose();
        _isPausedSubject.Dispose();
    }

    /// <summary>Builds the generated data type file prefix.</summary>
    /// <param name="variable">The PLC variable name.</param>
    /// <returns>The generated data type file prefix.</returns>
    private static string BuildDataTypesFileName(string variable) =>
        variable.StartsWith(".")
            ? "PLC_" + variable.Remove(0, 1)
            : "PLC_" + variable;

    /// <summary>Deletes stale generated data type files.</summary>
    /// <param name="dataTypesBaseName">The generated data type file prefix.</param>
    private static void DeleteGeneratedDataTypeFiles(string dataTypesBaseName)
    {
        foreach (var file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetFilesWhere(file => file.Name.Contains(dataTypesBaseName)))
        {
            File.Delete(file.FullName);
        }
    }

    /// <summary>Tries to resolve a primitive PLC type to a CLR type.</summary>
    /// <param name="plcType">The PLC type name.</param>
    /// <param name="type">The resolved CLR type.</param>
    /// <returns><c>true</c> when the PLC type was resolved.</returns>
    [RequiresUnreferencedCode("Uses type name lookup for PLC primitive mappings.")]
    private static bool TryResolvePlcType(string? plcType, out Type? type)
    {
        type = null;
        try
        {
            var types = CodeGenerator.PLCToCSharpTypeConverter(plcType).Split(',');
            type = Type.GetType(types[0]);
            return type is not null;
        }
        catch (UnsuportedTypeException)
        {
            return false;
        }
    }

    /// <summary>Finds the configured notification array length for a variable.</summary>
    /// <param name="variable">The PLC variable.</param>
    /// <returns>The configured array length, or <c>-1</c> when none is configured.</returns>
    private int FindNotificationArrayLength(string variable)
    {
        var notifications = Settings?.Notifications;
        if (notifications is null)
        {
            return -1;
        }

        for (var i = 0; i < notifications.Count; i++)
        {
            var notification = notifications[i];
            if (string.Equals(notification.Variable, variable, StringComparison.OrdinalIgnoreCase))
            {
                return notification.ArraySize;
            }
        }

        return -1;
    }

    /// <summary>Resolves a read handle, type, and array length for a PLC variable.</summary>
    /// <param name="variable">The PLC variable.</param>
    /// <param name="arrayLength">The requested array length.</param>
    /// <param name="handle">The resolved ADS handle.</param>
    /// <param name="type">The resolved value type.</param>
    /// <param name="readLength">The resolved read length.</param>
    /// <returns><c>true</c> when a read target was resolved.</returns>
    private bool TryGetReadTarget(string variable, int? arrayLength, out uint? handle, out Type? type, out int readLength)
    {
        handle = null;
        type = null;
        readLength = -1;
        if (string.IsNullOrWhiteSpace(variable) || !_typeInfo.TryGetValue(variable, out type))
        {
            return false;
        }

        if (!TryGetReadHandle(variable, out handle, out readLength))
        {
            return false;
        }

        if (!type.IsArray && type != typeof(string))
        {
            return true;
        }

        if (readLength > 0)
        {
            return true;
        }

        if (arrayLength.HasValue)
        {
            readLength = arrayLength.Value;
            return true;
        }

        throw new ArgumentOutOfRangeException(nameof(arrayLength), "arrayLength must be set to the size of the Array");
    }

    /// <summary>Resolves a read handle for a PLC variable.</summary>
    /// <param name="variable">The PLC variable.</param>
    /// <param name="handle">The resolved ADS handle.</param>
    /// <param name="arrayLength">The registered array length.</param>
    /// <returns><c>true</c> when a handle was resolved.</returns>
    private bool TryGetReadHandle(string variable, out uint? handle, out int arrayLength)
    {
        if (ReadWriteHandleInfo.TryGetValue(variable, out handle))
        {
            arrayLength = FindNotificationArrayLength(variable);
            return true;
        }

        if (WriteHandleInfo.TryGetValue(variable, out var writeHandle))
        {
            handle = writeHandle.Handle;
            arrayLength = writeHandle.ArrayLength;
            return true;
        }

        handle = null;
        arrayLength = -1;
        return false;
    }

    /// <summary>Creates the notification variables.</summary>
    /// <param name="notifications">The notifications.</param>
    /// <param name="client">The client.</param>
    /// <returns>A Value.</returns>
    [RequiresUnreferencedCode("Invokes dynamic code generation and reflection to materialize PLC types.")]
    [RequiresDynamicCode("Invokes dynamic code generation and reflection to materialize PLC types.")]
    private Exception? CreateNotificationVariables(List<RxNotification>? notifications, AdsClient client)
    {
        if (notifications is null)
        {
            return null;
        }

        var isTwinCat3 = client.Address?.Port >= 851;
        for (var i = 0; i < notifications.Count; i++)
        {
            var notification = notifications[i];
            if (i == 0 && string.IsNullOrEmpty(notification.Variable))
            {
                continue;
            }

            try
            {
                CreateNotificationVariable(notification, client, isTwinCat3);
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        return null;
    }

    /// <summary>Creates a notification variable registration.</summary>
    /// <param name="notification">The notification to register.</param>
    /// <param name="client">The ADS client.</param>
    /// <param name="isTwinCat3">Whether TwinCAT 3 packing should be used.</param>
    [RequiresUnreferencedCode("Invokes dynamic code generation and reflection to materialize PLC types.")]
    [RequiresDynamicCode("Invokes dynamic code generation and reflection to materialize PLC types.")]
    private void CreateNotificationVariable(RxNotification notification, AdsClient client, bool isTwinCat3)
    {
        var notificationVariable = notification.Variable ?? string.Empty;
        if (string.IsNullOrWhiteSpace(notificationVariable))
        {
            return;
        }

        var identifier = DateTime.UtcNow.ToBinary().ToString(CultureInfo.InvariantCulture);
        var dataTypesBaseName = BuildDataTypesFileName(notificationVariable);
        DeleteGeneratedDataTypeFiles(dataTypesBaseName);

        var dataTypesFileName = dataTypesBaseName + identifier + ".dll";
        var type = ResolveNotificationType(notificationVariable, dataTypesFileName, identifier, isTwinCat3);
        if (type is null)
        {
            return;
        }

        var handle = client.CreateVariableHandle(notificationVariable);
        ReadWriteHandleInfo[notificationVariable] = handle;
        _readWriteVariablesByHandle[handle] = notificationVariable;
        _typeInfo[notificationVariable] = type;
    }

    /// <summary>Resolves the CLR type used by a notification variable.</summary>
    /// <param name="notificationVariable">The notification variable name.</param>
    /// <param name="dataTypesFileName">The generated data type file name.</param>
    /// <param name="identifier">The generated code identifier.</param>
    /// <param name="isTwinCat3">Whether TwinCAT 3 packing should be used.</param>
    /// <returns>The resolved CLR type.</returns>
    [RequiresUnreferencedCode("Invokes dynamic code generation and reflection to materialize PLC types.")]
    [RequiresDynamicCode("Invokes dynamic code generation and reflection to materialize PLC types.")]
    private Type? ResolveNotificationType(string notificationVariable, string dataTypesFileName, string identifier, bool isTwinCat3)
    {
        var nodeEmulator = _codeGenerator?.SearchSymbols(notificationVariable);
        var symbol = (ISymbol?)nodeEmulator?.Tag;
        var notificationType = symbol?.TypeName;
        if (_codeGenerator?.CreateDll(nodeEmulator, dataTypesFileName, isTwinCat3: isTwinCat3) == true)
        {
            var generatedCode = BuildDataTypesFileName(notificationVariable);
            generatedCode += $"{identifier}.dll${_codeGenerator.CreateCSharpCodeString(nodeEmulator, isTwinCat3: isTwinCat3)}";
            _code.Add(generatedCode);
            return dataTypesFileName.GetType("TwinCATRx." + notificationType);
        }

        return TryResolvePlcType(notificationType, out var type) ? type : null;
    }

    /// <summary>Creates the write variables.</summary>
    /// <param name="writeVariables">The write variables.</param>
    /// <param name="client">The client.</param>
    /// <returns>A Value.</returns>
    [RequiresUnreferencedCode("May rely on dynamic type generation depending on PLC type definitions.")]
    [RequiresDynamicCode("May rely on dynamic type generation depending on PLC type definitions.")]
    private Exception? CreateWriteVariables(List<IWriteVariable>? writeVariables, AdsClient client)
    {
        if (writeVariables is null)
        {
            return null;
        }

        var isTC3 = client.Address?.Port >= 851;
        foreach (var writeVariable in writeVariables)
        {
            try
            {
                CreateWriteVariable(writeVariable, client, isTC3);
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        return null;
    }

    /// <summary>Creates a write variable registration.</summary>
    /// <param name="writeVariable">The write variable.</param>
    /// <param name="client">The ADS client.</param>
    /// <param name="isTwinCat3">Whether TwinCAT 3 packing should be used.</param>
    [RequiresUnreferencedCode("May rely on dynamic type generation depending on PLC type definitions.")]
    [RequiresDynamicCode("May rely on dynamic type generation depending on PLC type definitions.")]
    private void CreateWriteVariable(IWriteVariable writeVariable, AdsClient client, bool isTwinCat3)
    {
        var variable = writeVariable.Variable ?? string.Empty;
        if (string.IsNullOrEmpty(variable))
        {
            return;
        }

        var handle = client.CreateVariableHandle(variable);
        WriteHandleInfo[variable] = (handle, writeVariable.ArraySize);
        _writeVariablesByHandle[handle] = variable;

        var nodeEmulator = _codeGenerator?.SearchSymbols(variable);
        if (nodeEmulator is null)
        {
            return;
        }

        var symbol = (ISymbol?)nodeEmulator.Tag;
        var notificationType = symbol?.TypeName;
        if (TryResolvePlcType(notificationType, out var type) && type is not null)
        {
            _typeInfo[variable] = type;
            return;
        }

        var generatedCode = BuildDataTypesFileName(variable);
        generatedCode += ".dll$" + _codeGenerator?.CreateCSharpCodeString(nodeEmulator, isTwinCat3: isTwinCat3);
        _code.Add(generatedCode);
    }

    /// <summary>Initializes the PLC connection and reactive read/write loops.</summary>
    /// <returns>The PLC initialization observable sequence.</returns>
    [RequiresUnreferencedCode("Invokes dynamic code generation and reflection to materialize PLC types.")]
    [RequiresDynamicCode("Invokes dynamic code generation and reflection to materialize PLC types.")]
    private IObservable<Unit> InitPLC() =>
        CP.TwinCatRx.Core.TwinCatRxExtensions.OnErrorRetry<Unit, Exception>(
            Observable.Create<Unit>(o =>
            {
                _cleanup = [];

                var client = new AdsClient();
                _ = client.DisposeWith(_cleanup);
                _codeGenerator = new();
                _ = _codeGenerator.DisposeWith(_cleanup);
                var intialised = false;

                // Reset values to default
                _code.Clear();
                ReadWriteHandleInfo.Clear();
                _typeInfo.Clear();
                WriteHandleInfo.Clear();
                _readWriteVariablesByHandle.Clear();
                _writeVariablesByHandle.Clear();

                try
                {
                    if (string.IsNullOrWhiteSpace(Settings!.AdsAddress))
                    {
                        client.Connect(Settings!.Port);
                    }
                    else
                    {
                        client.Connect(Settings!.AdsAddress, Settings!.Port);
                    }

                    _ = _codeGenerator.LoadSymbols(Settings!.AdsAddress, Settings!.Port);
                }
                catch (Exception ex)
                {
                    Connected = false;
                    _errorReceived.OnNext(ex);
                    o.OnError(ex);
                }

#if WINDOWS
                // If running on non-Windows (e.g., Windows TFM deployed on Linux), skip ServiceController usage.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var serviceList = new Dictionary<string, ServiceControllerStatus>(StringComparer.OrdinalIgnoreCase);
                    _ = ObservableServiceController.GetServices()

                    // Use non-localized ServiceName instead of DisplayName
                    .Where(s => string.Equals(s.ServiceName, "TcSysSrv", StringComparison.OrdinalIgnoreCase))
                    .Retry(int.MaxValue)
                    .SubscribeTo(s =>
                    {
                        // Idempotent update (avoid duplicate-key exceptions on resubscribe)
                        serviceList[s.ServiceName] = s.Status;
                        Console.WriteLine($"ServiceName: {s.ServiceName} is {s.Status}");
                        if (s.Status != ServiceControllerStatus.Running)
                        {
                            s.Start();
                            var ex = new InvalidOperationException("Service Fault");

                            _errorReceived.OnNext(ex);
                            o.OnError(ex);
                        }

                        _ = s.StatusObserver.Retry(int.MaxValue).SubscribeTo(status =>
                        {
                            Console.WriteLine($"ServiceName: {s.ServiceName} is {status}");
                            serviceList[s.ServiceName] = status;
                            if (status == ServiceControllerStatus.Running)
                            {
                                return;
                            }

                            s.Start();
                            var ex = new InvalidOperationException("Service Fault");
                            _errorReceived.OnNext(ex);
                            o.OnError(ex);
                        }).DisposeWith(_cleanup);
                    }).DisposeWith(_cleanup);

                    // Periodically update service status
                    _ = Observable.Interval(TimeSpan.FromSeconds(1))
                    .Retry(int.MaxValue)
                    .SubscribeTo(_ =>
                    {
                        if (!serviceList.TryGetValue("TcSysSrv", out var tc))
                        {
                            // SCM likely unavailable; treat as running to align with non-Windows behavior
                            _serviceStatus.OnNext(ServiceStatus.Running);
                        }
                        else if (tc == ServiceControllerStatus.Running)
                        {
                            _serviceStatus.OnNext(ServiceStatus.Running);
                        }
                        else
                        {
                            _serviceStatus.OnNext(ServiceStatus.Faulted);
                        }
                    }).DisposeWith(_cleanup);
                }
                else
                {
                    // On non-Windows runtime, assume service is available and running
                    _serviceStatus.OnNext(ServiceStatus.Running);
                }
#else
                // On non-Windows, assume service is available and running
                _serviceStatus.OnNext(ServiceStatus.Running);
#endif

                // Periodically update ADS client state
                _ = Observable.Interval(TimeSpan.FromSeconds(1))
                .Retry(int.MaxValue)
                .SubscribeTo(_ =>
                {
                    try
                    {
                        _clientState.OnNext(client?.IsConnected == true ? client.ReadState().AdsState : AdsState.Invalid);
                    }
                    catch (Exception innerex)
                    {
                        _clientState.OnNext(AdsState.Invalid);
                        var ex = new InvalidOperationException("Ads Fault", innerex);
                        _errorReceived.OnNext(ex);
                        o.OnError(ex);
                    }
                }).DisposeWith(_cleanup);

                var services = _clientState.DistinctUntilChanged()
                .CombineLatest(_serviceStatus.DistinctUntilChanged(), (c, s) => (client: c, service: s));

                _ = services.Retry(int.MaxValue).SubscribeTo(s =>
                {
                    if (!intialised && s.service == ServiceStatus.Running && s.client == AdsState.Run)
                    {
                        try
                        {
                            var nv = CreateNotificationVariables(Settings!.Notifications, client);
                            if (nv is not null)
                            {
                                throw nv;
                            }

                            nv = CreateWriteVariables(Settings!.WriteVariables, client);
                            if (nv is not null)
                            {
                                throw nv;
                            }

                            _ = Task.Run(() => _codeSubject.OnNext([.. _code]));
                            _codeGenerator.Dispose();
                            intialised = true;
                            Connected = true;
                            _initCompleteSubject.OnNext(Unit.Default);
                        }
                        catch (Exception ex)
                        {
                            Connected = false;
                            o.OnError(ex);
                        }
                    }
                    else if (s.client != AdsState.Invalid && s.client != AdsState.Run)
                    {
                        try
                        {
                            // PLC program is not Running
                            client?.WriteControl(new StateInfo(AdsState.Run, client.ReadState().DeviceState));
                        }
                        catch (Exception ex)
                        {
                            Connected = false;
                            o.OnError(ex);
                        }
                    }
                }).DisposeWith(_cleanup);

                _ = _writePLC.SubscribeTo(v =>
                {
                    if (!intialised || client?.IsConnected != true)
                    {
                        return;
                    }

                    try
                    {
                        if (v.handle is not null)
                        {
                            var data = v.value;
                            if (data is not null)
                            {
                                client.WriteAny(v.handle.Value, data);
                                _onWriteSubject.OnNext(string.IsNullOrWhiteSpace(v.id) ? "Success" : $"Success,{v.id}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _onWriteSubject.OnNext(ex.ToString());
                        _errorReceived.OnNext(ex);
                    }
                }).DisposeWith(_cleanup);

                _ = _readPLC.Retry(int.MaxValue).SubscribeTo(v =>
                {
                    try
                    {
                        object? plcValueRead = null;

                        if ((v.type.IsArray || v.type == typeof(string)) && v.length > 0)
                        {
                            int[] args = [v.length];
                            if (v.handle is not null)
                            {
                                plcValueRead = client.ReadAny(v.handle.Value, v.type, args);
                            }
                        }
                        else if (v.handle is not null)
                        {
                            plcValueRead = client.ReadAny(v.handle.Value, v.type);
                        }

                        if (plcValueRead is not null && v.handle.HasValue)
                        {
                            if (!_readWriteVariablesByHandle.TryGetValue(v.handle.Value, out var key))
                            {
                                _ = _writeVariablesByHandle.TryGetValue(v.handle.Value, out key);
                            }

                            if (!string.IsNullOrWhiteSpace(key))
                            {
                                _dataReceived.OnNext((Variable: key, Data: plcValueRead, Id: v.id));
                            }
                        }
                    }
                    catch (SystemException engex)
                    {
                        _errorReceived.OnNext(engex);
                    }
                    catch (Exception ex)
                    {
                        _errorReceived.OnNext(ex);
                    }
                }).DisposeWith(_cleanup);

                if ((Settings!) is not null)
                {
                    foreach (var notification in Settings!.Notifications)
                    {
                        _ = Observable.Interval(TimeSpan.FromMilliseconds(notification.UpdateRate)).Retry(int.MaxValue).SubscribeTo(_ =>
                        {
                            if (notification.Variable is null || !client.IsConnected || !_typeInfo.TryGetValue(notification.Variable, out var type) || !ReadWriteHandleInfo.TryGetValue(notification.Variable, out var handle))
                            {
                                return;
                            }

                            if (type.IsArray || type == typeof(string))
                            {
                                if (notification.ArraySize > 0)
                                {
                                    ReadArrayHandle(handle, type, notification.ArraySize, null);
                                    return;
                                }

                                _errorReceived.OnNext(new InvalidOperationException($"Please set Notification ArraySize to the {(type == typeof(string) ? "String" : "Array")} length."));
                                return;
                            }

                            ReadHandle(handle, type, null);
                        }).DisposeWith(_cleanup);
                    }
                }

                return _cleanup;
            }),
            _errorReceived.OnNext,
            TimeSpan.FromSeconds(5)).Publish().RefCount();

    /// <summary>Queues a PLC array read request.</summary>
    /// <param name="handle">The ADS variable handle.</param>
    /// <param name="type">The value type.</param>
    /// <param name="length">The array length.</param>
    /// <param name="id">The correlation identifier.</param>
    private void ReadArrayHandle(uint? handle, Type type, int length, string? id) =>
        _readPLC.OnNext((handle, type, length, id));

    /// <summary>Queues a PLC scalar read request.</summary>
    /// <param name="handle">The ADS variable handle.</param>
    /// <param name="type">The value type.</param>
    /// <param name="id">The correlation identifier.</param>
    private void ReadHandle(uint? handle, Type type, string? id) =>
        _readPLC.OnNext((handle, type, -1, id));

    /// <summary>Queues a PLC write request.</summary>
    /// <param name="handle">The ADS variable handle.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="length">The array length.</param>
    /// <param name="id">The correlation identifier.</param>
    private void WriteHandle(uint? handle, object value, int length = -1, string? id = null) =>
        _writePLC.OnNext((handle, value, length, id));
}
