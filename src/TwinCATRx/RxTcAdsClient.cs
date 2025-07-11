// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.ServiceProcess;
using CP.TwinCatRx.Core;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;

namespace CP.TwinCatRx;

/// <summary>
/// Observable TwinCAT ADS Client.
/// </summary>
public partial class RxTcAdsClient : IRxTcAdsClient
{
    private readonly Subject<AdsState> _clientState = new();
    private readonly List<string> _code = [];
    private readonly Subject<string[]> _codeSubject = new();
    private readonly Subject<(string Variable, object? Data, string? Id)> _dataReceived = new();
    private readonly Subject<Exception> _errorReceived = new();
    private readonly Subject<string?> _onWriteSubject = new();
    private readonly Subject<(uint? handle, Type type, int length, string? id)> _readPLC = new();
    private readonly Subject<ServiceStatus> _serviceStatus = new();
    private readonly Dictionary<string, Type> _typeInfo = [];
    private readonly Subject<(uint? handle, object value, int length, string? id)> _writePLC = new();
    private readonly ReplaySubject<Unit> _initCompleteSubject = new(1);
    private readonly ReplaySubject<bool> _isPausedSubject = new(1);
    private CompositeDisposable? _cleanup;
    private CodeGenerator? _codeGenerator;
    private IDisposable? _plcCleanup;

    /// <summary>
    /// Gets codes this instance.
    /// </summary>
    /// <returns>A Value.</returns>
    public IObservable<string[]> Code => _codeSubject.Retry().Publish().RefCount();

    /// <summary>
    /// Gets a value indicating whether this <see cref="RxTcAdsClient"/> is connected.
    /// </summary>
    /// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
    public bool Connected { get; internal set; }

    /// <summary>
    /// Gets the initialize complete. PLC is ready to read and write.
    /// </summary>
    /// <value>
    /// The initialize complete.
    /// </value>
    public IObservable<Unit> InitializeComplete => _initCompleteSubject.Retry().Publish().RefCount();

    /// <summary>
    /// Gets the data received.
    /// </summary>
    /// <value>The data received.</value>
    public IObservable<(string Variable, object? Data, string? Id)> DataReceived => _dataReceived.Retry().Publish().RefCount();

    /// <summary>
    /// Gets error received.
    /// </summary>
    /// <returns>A Value.</returns>
    public IObservable<Exception> ErrorReceived => _errorReceived.Retry().Publish().RefCount();

    /// <summary>
    /// Gets a value indicating whether gets a value that indicates whether the object is disposed.
    /// </summary>
    public bool IsDisposed => _cleanup?.IsDisposed ?? false;

    /// <summary>
    /// Gets the on write.
    /// </summary>
    /// <value>The on write.</value>
    public IObservable<string?> OnWrite => _onWriteSubject.Retry().Publish().RefCount();

    /// <summary>
    /// Gets the read write handle information.
    /// </summary>
    /// <value>The read write handle information.</value>
    public IDictionary<string, uint?> ReadWriteHandleInfo { get; } = new Dictionary<string, uint?>();

    /// <summary>
    /// Gets the write handle information.
    /// </summary>
    /// <value>The write handle information.</value>
    public IDictionary<string, (uint? Handle, int ArrayLength)> WriteHandleInfo { get; } = new Dictionary<string, (uint? Handle, int ArrayLength)>();

    /// <summary>
    /// Gets the settings.
    /// </summary>
    /// <value>
    /// The settings.
    /// </value>
    public ISettings? Settings { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this instance is paused.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is paused; otherwise, <c>false</c>.
    /// </value>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// Gets the is paused observable.
    /// </summary>
    /// <value>
    /// The is paused observable.
    /// </value>
    public IObservable<bool> IsPausedObservable => _isPausedSubject.Retry().Publish().RefCount();

    /// <summary>
    /// Connects the specified settings.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <exception cref="Exception">An Exception.</exception>
    public void Connect(ISettings settings)
    {
        if (_cleanup?.IsDisposed == true)
        {
            _errorReceived.OnNext(new Exception("RxTcAdsClient has been Disposed"));
            return;
        }

        try
        {
            if (!Connected)
            {
                Settings = settings;
                _plcCleanup = InitPLC().Subscribe();
                Connected = true;
            }
        }
        catch (Exception ex)
        {
            _errorReceived.OnNext(ex);
        }
    }

    /// <summary>
    /// Disconnects this instance.
    /// </summary>
    public void Disconnect() => _plcCleanup?.Dispose();

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Reads the specified variable.
    /// </summary>
    /// <param name="variable">The data.</param>
    /// <param name="arrayLength">Length of the array.</param>
    /// <param name="id">The identifier.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Parameters - Parameter 0 must be set to the size of the Array.</exception>
    public void Read(string variable, int? arrayLength = null, string? id = null)
    {
        if (!string.IsNullOrWhiteSpace(variable) && WriteHandleInfo.TryGetValue(variable!.ToUpper(), out var item))
        {
            var type = _typeInfo[variable.ToUpper()];
            if (type.IsArray || type == typeof(string))
            {
                if (item.ArrayLength > 0)
                {
                    ReadArrayHandle(item.Handle, type, item.ArrayLength, id);
                    return;
                }

                if (!arrayLength.HasValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(arrayLength), "arrayLength must be set to the size of the Array");
                }

                ReadArrayHandle(item.Handle, type, arrayLength.Value, id);
            }
            else
            {
                ReadHandle(item.Handle, type, id);
            }
        }
    }

    /// <summary>
    /// Writes the specified variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <param name="value">The value.</param>
    /// <param name="id">The identifier.</param>
    public void Write(string variable, object value, string? id = null)
    {
        uint? handle;
        if (!string.IsNullOrWhiteSpace(variable) && ReadWriteHandleInfo.ContainsKey(variable!.ToUpper()))
        {
            handle = ReadWriteHandleInfo[variable.ToUpper()];
            WriteHandle(handle, value, id: id);
            return;
        }

        if (!string.IsNullOrWhiteSpace(variable) && WriteHandleInfo.TryGetValue(variable!.ToUpper(), out var item))
        {
            WriteHandle(item.Handle, value, item.ArrayLength, id: id);
        }
    }

    /// <summary>
    /// Pauses the specified time.
    /// </summary>
    /// <param name="time">The time.</param>
    public void Pause(TimeSpan time)
    {
        if (_cleanup == null || _cleanup.IsDisposed)
        {
            _errorReceived.OnNext(new Exception("RxTcAdsClient has been Disposed"));
            return;
        }

        if (time.TotalMilliseconds > 0)
        {
            var cleanup = new CompositeDisposable();
            cleanup!.DisposeWith(_cleanup!);
            IsPaused = true;
            _isPausedSubject.OnNext(IsPaused);
            Observable.Timer(time).Subscribe(_ =>
            {
                IsPaused = false;
                _isPausedSubject.OnNext(IsPaused);
                cleanup?.Dispose();
            }).DisposeWith(cleanup!);
        }
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
    /// unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_cleanup?.IsDisposed == false && disposing)
        {
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
    }

    /// <summary>
    /// Creates the notification variables.
    /// </summary>
    /// <param name="notifications">The notifications.</param>
    /// <param name="client">The client.</param>
    /// <returns>A Value.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1866:Use char overload", Justification = "Not valid for all TFM's")]
    private Exception? CreateNotificationVariables(List<Core.INotification>? notifications, AdsClient client)
    {
        var isTC3 = client?.Address?.Port >= 851;
        for (var i = 0; i < notifications?.Count; i++)
        {
            try
            {
                var notificationVariable = notifications[i].Variable;
                if (string.IsNullOrEmpty(notificationVariable) && i == 0)
                {
                    continue;
                }

                var identifier = DateTime.UtcNow.ToBinary().ToString(CultureInfo.InvariantCulture);
                var dataTypesFileName = notificationVariable?.StartsWith(".") == true
                    ? "PLC_" + notificationVariable.Remove(0, 1)
                    : "PLC_" + notificationVariable;
                foreach (var file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetFilesWhere(x => x.Name.Contains(dataTypesFileName)).Select(x => x.Name).ToList())
                {
                    var f = file;
                    File.Delete(f);
                }

                dataTypesFileName += identifier + ".dll";
                Type? type;
                var nodeEmulator = _codeGenerator?.SearchSymbols(notificationVariable);
                var symbol = (ISymbol?)nodeEmulator?.Tag;
                var notificationType = symbol?.TypeName;
                if (_codeGenerator?.CreateDll(nodeEmulator, dataTypesFileName, isTwinCat3: isTC3) == true)
                {
                    var s = notificationVariable?.StartsWith(".") == true
                        ? "PLC_" + notificationVariable.Remove(0, 1)
                        : "PLC_" + notificationVariable;
                    s += $"{identifier}.dll${_codeGenerator.CreateCSharpCodeString(nodeEmulator, isTwinCat3: isTC3)}";
                    _code.Add(s);
                    type = dataTypesFileName.GetType("TwinCATRx." + notificationType);
                }
                else
                {
                    var types = CodeGenerator.PLCToCSharpTypeConverter(notificationType).Split(',');
                    type = Type.GetType(types[0]);
                }

                if (type != null && notificationVariable != null && !string.IsNullOrWhiteSpace(notificationVariable))
                {
                    var handle = client?.CreateVariableHandle(notificationVariable.ToUpper());
                    if (handle.HasValue)
                    {
                        ReadWriteHandleInfo.Add(notificationVariable.ToUpper(), handle.Value);
                        _typeInfo.Add(notificationVariable.ToUpper(), type);
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        return null;
    }

    /// <summary>
    /// Creates the write variables.
    /// </summary>
    /// <param name="writeVariables">The write variables.</param>
    /// <param name="client">The client.</param>
    /// <returns>A Value.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1866:Use char overload", Justification = "Not valid for all TFM's")]
    private Exception? CreateWriteVariables(List<IWriteVariable>? writeVariables, AdsClient client)
    {
        var isTC3 = client.Address?.Port >= 851;
        foreach (var (variable, arraySize) in writeVariables!.Select(t => (Variable: t.Variable!.ToUpper(), t.ArraySize)).Where(x => !string.IsNullOrEmpty(x.Variable)))
        {
            try
            {
                WriteHandleInfo.Add(variable, (client?.CreateVariableHandle(variable), arraySize));

                var nodeEmulator = _codeGenerator?.SearchSymbols(variable);
                if (nodeEmulator == null)
                {
                    continue;
                }

                var symbol = (ISymbol?)nodeEmulator.Tag;
                var notificationType = symbol?.TypeName;
                Type? type = null;
                try
                {
                    var types = CodeGenerator.PLCToCSharpTypeConverter(notificationType).Split(',');
                    type = Type.GetType(types[0]);
                }
                catch
                {
                }

                if (type != null)
                {
                    _typeInfo.Add(variable, type);
                }
                else
                {
                    var s = variable.StartsWith(".")
                      ? "PLC_" + variable.Remove(0, 1)
                      : "PLC_" + variable;
                    s += ".dll$" + _codeGenerator?.CreateCSharpCodeString(nodeEmulator, isTwinCat3: isTC3);
                    _code.Add(s);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        return null;
    }

    private IObservable<Unit> InitPLC() =>
        Observable.Create<Unit>(o =>
            {
                _cleanup = [];

                var client = new AdsClient();
                client.DisposeWith(_cleanup);
                _codeGenerator = new CodeGenerator();
                _codeGenerator.DisposeWith(_cleanup);
                var intialised = false;

                // Reset values to default
                _code.Clear();
                ReadWriteHandleInfo.Clear();
                _typeInfo.Clear();
                WriteHandleInfo.Clear();

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

                    _codeGenerator.LoadSymbols(Settings!.AdsAddress, Settings!.Port);
                }
                catch (Exception ex)
                {
                    _errorReceived.OnNext(ex);
                    o.OnError(ex);
                }

                var serviceList = new Dictionary<string, ServiceControllerStatus>();
                ObservableServiceController.GetServices()
                .Where(s => s.DisplayName == "TwinCAT System Service" || s.DisplayName == "TwinCAT3 System Service")
                .Retry()
                .Subscribe(s =>
                {
                    serviceList.Add(s.DisplayName, s.Status);
#pragma warning disable RCS1198 // Avoid unnecessary boxing of value type.
                    Console.WriteLine($"ServiceName: {s.DisplayName} is {s.Status}");
#pragma warning restore RCS1198 // Avoid unnecessary boxing of value type.
                    if (s.Status != ServiceControllerStatus.Running)
                    {
                        s.Start();
                        var ex = new Exception("Service Fault");

                        _errorReceived.OnNext(ex);
                        o.OnError(ex);
                    }

                    s.StatusObserver.Retry().Subscribe(status =>
                    {
#pragma warning disable RCS1198 // Avoid unnecessary boxing of value type.
                        Console.WriteLine($"ServiceName: {s.DisplayName} is {status}");
#pragma warning restore RCS1198 // Avoid unnecessary boxing of value type.
                        serviceList[s.DisplayName] = status;
                        if (status != ServiceControllerStatus.Running)
                        {
                            s.Start();
                            var ex = new Exception("Service Fault");
                            _errorReceived.OnNext(ex);
                            o.OnError(ex);
                        }
                    }).DisposeWith(_cleanup);
                }).DisposeWith(_cleanup);

                Observable.Interval(TimeSpan.FromSeconds(1))
                .Retry()
                .Subscribe(_ =>
                {
                    if (serviceList.Count >= 1
                        && ((serviceList.TryGetValue("TwinCAT System Service", out var tc2) && tc2 == ServiceControllerStatus.Running)
                        || (serviceList.TryGetValue("TwinCAT3 System Service", out var tc3) && tc3 == ServiceControllerStatus.Running)))
                    {
                        _serviceStatus.OnNext(ServiceStatus.Running);
                    }
                    else
                    {
                        _serviceStatus.OnNext(ServiceStatus.Faulted);
                    }

                    try
                    {
                        _clientState.OnNext(client?.IsConnected == true ? client.ReadState().AdsState : AdsState.Invalid);
                    }
                    catch (Exception innerex)
                    {
                        _clientState.OnNext(AdsState.Invalid);
                        var ex = new Exception("Ads Fault", innerex);
                        _errorReceived.OnNext(ex);
                        o.OnError(ex);
                    }
                }).DisposeWith(_cleanup);

                var services = _clientState.DistinctUntilChanged()
                .CombineLatest(_serviceStatus.DistinctUntilChanged(), (c, s) => new { client = c, service = s });

                services.Retry().Subscribe(s =>
                {
                    if (!intialised && s.service == ServiceStatus.Running && s.client == AdsState.Run)
                    {
                        try
                        {
                            var nv = CreateNotificationVariables(Settings!.Notifications, client);
                            if (nv != null)
                            {
                                throw nv;
                            }

                            nv = CreateWriteVariables(Settings!.WriteVariables, client);
                            if (nv != null)
                            {
                                throw nv;
                            }

                            Task.Run(() => _codeSubject.OnNext([.. _code]));
                            _codeGenerator.Dispose();
                            intialised = true;
                            _initCompleteSubject.OnNext(Unit.Default);
                        }
                        catch (Exception ex)
                        {
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
                            o.OnError(ex);
                        }
                    }
                }).DisposeWith(_cleanup);

                _writePLC.Subscribe(v =>
                {
                    if (intialised && client?.IsConnected == true)
                    {
                        try
                        {
                            if (v.handle != null)
                            {
                                var data = v.value;
                                if (data != null)
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
                    }
                }).DisposeWith(_cleanup);

                _readPLC.Retry().Subscribe(v =>
               {
                   try
                   {
                       object? plcValueRead = null;

                       if ((v.type.IsArray || v.type == typeof(string)) && v.length > 0)
                       {
                           int[] args = [v.length];
                           if (v.handle != null)
                           {
                               plcValueRead = client.ReadAny(v.handle.Value, v.type, args);
                           }
                       }
                       else if (v.handle != null)
                       {
                           plcValueRead = client.ReadAny(v.handle.Value, v.type);
                       }

                       if (plcValueRead != null)
                       {
                           var key = ReadWriteHandleInfo.FirstOrDefault(k => k.Value == v.handle).Key;
                           if (string.IsNullOrWhiteSpace(key))
                           {
                               key = WriteHandleInfo.First(k => k.Value.Handle == v.handle).Key;
                           }

                           if (!string.IsNullOrWhiteSpace(key))
                           {
                               _dataReceived.OnNext((Variable: key, Data: plcValueRead, Id: v.id));
                           }
                       }
                   }
                   catch (Exception ex)
                   {
                       _errorReceived.OnNext(ex);
                   }
               }).DisposeWith(_cleanup);

                if (Settings! != null)
                {
                    foreach (var notification in Settings!.Notifications)
                    {
                        Observable.Interval(TimeSpan.FromMilliseconds(notification.UpdateRate)).Retry().Subscribe(_ =>
                        {
                            if (notification.Variable != null && client.IsConnected && _typeInfo.TryGetValue(notification.Variable.ToUpper(), out var type))
                            {
                                var kvp = ReadWriteHandleInfo.FirstOrDefault(k => k.Key.Equals(notification.Variable, StringComparison.CurrentCultureIgnoreCase));
                                if (type != null)
                                {
                                    if (type.IsArray || type == typeof(string))
                                    {
                                        if (notification.ArraySize > 0)
                                        {
                                            ReadArrayHandle(kvp.Value, type, notification.ArraySize, null);
                                        }
                                        else
                                        {
                                            _errorReceived.OnNext(new Exception($"Please set Notification ArraySize to the {(type == typeof(string) ? "String" : "Array")} length."));
                                        }
                                    }
                                    else
                                    {
                                        ReadHandle(kvp.Value, type, null);
                                    }
                                }
                            }
                        }).DisposeWith(_cleanup);
                    }
                }

                return _cleanup;
            })
            .OnErrorRetry((Exception ex) => _errorReceived.OnNext(ex), TimeSpan.FromSeconds(5)).Publish().RefCount();

    private void ReadArrayHandle(uint? handle, Type type, int length, string? id) =>
        _readPLC.OnNext((handle, type, length, id));

    private void ReadHandle(uint? handle, Type type, string? id) =>
        _readPLC.OnNext((handle, type, -1, id));

    private void WriteHandle(uint? handle, object value, int length = -1, string? id = null) =>
        _writePLC.OnNext((handle, value, length, id));
}
