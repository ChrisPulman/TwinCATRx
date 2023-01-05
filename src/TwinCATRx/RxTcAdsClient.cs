// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.ServiceProcess;
using CP.TwinCatRx.Core;
using CP.TwinCATRx.Core;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;

namespace CP.TwinCatRx
{
    /// <summary>
    /// Observable TwinCAT.
    /// </summary>
    public class RxTcAdsClient : IRxTcAdsClient
    {
        private readonly ISubject<AdsState> _clientState = new Subject<AdsState>();
        private readonly IList<string> _code = new List<string>();
        private readonly ISubject<string[]> _codeSubject = new Subject<string[]>();
        private readonly ISubject<(string Variable, object? Data)> _dataReceived = new Subject<(string Variable, object? Data)>();
        private readonly ISubject<Exception> _errorReceived = new Subject<Exception>();
        private readonly ISubject<string?> _onWriteSubject = new Subject<string?>();
        private readonly ISubject<(uint? handle, Type type, int length)> _readPLC = new Subject<(uint? handle, Type type, int length)>();
        private readonly ISubject<ServiceStatus> _serviceStatus = new Subject<ServiceStatus>();
        private readonly IDictionary<string, Type> _typeInfo = new Dictionary<string, Type>();
        private readonly ISubject<(uint? handle, object value)> _writePLC = new Subject<(uint? handle, object value)>();
        private CompositeDisposable? _cleanup;
        private ICodeGenerator? _codeGenerator;
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
        /// Gets the data received.
        /// </summary>
        /// <value>The data received.</value>
        public IObservable<(string Variable, object? Data)> DataReceived => _dataReceived.Retry().Publish().RefCount();

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
        public IDictionary<string, uint?> WriteHandleInfo { get; } = new Dictionary<string, uint?>();

        /// <summary>
        /// Connects the specified settings.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <exception cref="Exception">An Exception.</exception>
        public void Connect(ISettings settings)
        {
            if (_cleanup?.IsDisposed == true)
            {
                throw new Exception("ObservableTwinCAT has been Disposed");
            }

            try
            {
                if (!Connected)
                {
                    _plcCleanup = InitPLC(settings).Subscribe();
                    Connected = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
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
        /// <param name="data">The data.</param>
        /// <param name="parameters">The parameters.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Parameters - Parameter 0 must be set to the size of the Array.</exception>
        public void Read(string data, string parameters)
        {
            if (!string.IsNullOrWhiteSpace(data) && WriteHandleInfo.TryGetValue(data!.ToUpper(), out var handle))
            {
                var value = _typeInfo[data.ToUpper()];
                if (value.IsArray)
                {
                    if (string.IsNullOrWhiteSpace(parameters))
                    {
                        throw new ArgumentOutOfRangeException(nameof(parameters), "Parameter 0 must be set to the size of the Array");
                    }

                    ReadArray(data, int.Parse(parameters));
                }
                else
                {
                    ReadHandle(handle, value);
                }
            }
        }

        /// <summary>
        /// Reads the array.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="length">The length.</param>
        public void ReadArray(string data, int length)
        {
            if (!string.IsNullOrWhiteSpace(data) && WriteHandleInfo.TryGetValue(data!.ToUpper(), out var handle))
            {
                var value = _typeInfo[data.ToUpper()];
                ReadArrayHandle(handle, value, length);
            }
        }

        /// <summary>
        /// Writes the specified variable.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(string value)
        {
            uint? handle;
            if (!string.IsNullOrWhiteSpace(value) && ReadWriteHandleInfo.ContainsKey(value!.ToUpper()))
            {
                handle = ReadWriteHandleInfo[value.ToUpper()];
                WriteHandle(handle, value);
                return;
            }

            if (!string.IsNullOrWhiteSpace(value) && WriteHandleInfo.ContainsKey(value!.ToUpper()))
            {
                handle = WriteHandleInfo[value.ToUpper()];
                WriteHandle(handle, value);
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
            }
        }

        /// <summary>
        /// Creates the notification variables.
        /// </summary>
        /// <param name="notifications">The notifications.</param>
        /// <param name="client">The client.</param>
        /// <returns>A Value.</returns>
        private Exception? CreateNotificationVariables(List<TwinCATRx.Core.INotification>? notifications, AdsClient client)
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

                    var identifier = DateTime.Now.ToBinary().ToString(CultureInfo.InvariantCulture);
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
                        type = Type.GetType(CodeGenerator.PLCToCSharpTypeConverter(notificationType));
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
        private Exception? CreateWriteVariables(List<IWriteVariable>? writeVariables, AdsClient client)
        {
            var isTC3 = client.Address?.Port >= 851;
            foreach (var variable in writeVariables!.Select(t => t.Variable.ToUpper()).Where(variable => !string.IsNullOrEmpty(variable)))
            {
                try
                {
                    WriteHandleInfo.Add(variable, client?.CreateVariableHandle(variable));

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
                        type = Type.GetType(CodeGenerator.PLCToCSharpTypeConverter(notificationType));
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

        private IObservable<Unit> InitPLC(ISettings settings) =>
            Observable.Create<Unit>(o =>
                {
                    _cleanup = new();

                    var client = new AdsClient();

                    _codeGenerator = new CodeGenerator();
                    _codeGenerator.DisposeWith(_cleanup);
                    _cleanup.Add(client);
                    var intialised = false;

                    // Reset values to default
                    _code.Clear();
                    ReadWriteHandleInfo.Clear();
                    _typeInfo.Clear();
                    WriteHandleInfo.Clear();

                    try
                    {
                        if (string.IsNullOrWhiteSpace(settings.AdsAddress))
                        {
                            client.Connect(settings.Port);
                        }
                        else
                        {
                            client.Connect(settings.AdsAddress, settings.Port);
                        }

                        _codeGenerator.LoadSymbols(settings.AdsAddress, settings.Port);
                    }
                    catch (Exception ex)
                    {
                        _errorReceived.OnNext(ex);
                        o.OnError(ex);
                    }

                    var serviceList = new Dictionary<string, ServiceControllerStatus>();
                    ObservableServiceController.GetServices().Retry()
                    .Where(s => s.DisplayName == "TwinCAT System Service" || s.DisplayName == "TcEventLogger" || s.DisplayName == "TwinCAT3 System Service")
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
                        if (serviceList.Count >= 2
                    && ((serviceList.TryGetValue("TwinCAT System Service", out var tc2) && tc2 == ServiceControllerStatus.Running)
                    || (serviceList.TryGetValue("TwinCAT3 System Service", out var tc3) && tc3 == ServiceControllerStatus.Running))
                    && serviceList.TryGetValue("TcEventLogger", out var tcel) && tcel == ServiceControllerStatus.Running)
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
                                var nv = CreateNotificationVariables(settings.Notifications, client);
                                if (nv != null)
                                {
                                    throw nv;
                                }

                                nv = CreateWriteVariables(settings.WriteVariables, client);
                                if (nv != null)
                                {
                                    throw nv;
                                }

                                Task.Run(() => _codeSubject.OnNext(_code.ToArray()));
                                _codeGenerator.Dispose();
                                intialised = true;
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
                                        _onWriteSubject.OnNext(null);
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

                           if (v.type.IsArray && v.length > 0)
                           {
                               int[] args = { v.length };
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
                                   key = WriteHandleInfo.First(k => k.Value == v.handle).Key;
                               }

                               if (!string.IsNullOrWhiteSpace(key))
                               {
                                   _dataReceived.OnNext((Variable: key, Data: plcValueRead));
                               }
                           }
                       }
                       catch (Exception ex)
                       {
                           _errorReceived.OnNext(ex);
                       }
                   }).DisposeWith(_cleanup);

                    if (settings != null)
                    {
                        foreach (var notification in settings.Notifications)
                        {
                            Observable.Interval(TimeSpan.FromMilliseconds(notification.UpdateRate)).Retry().Subscribe(_ =>
                            {
                                if (notification.Variable != null && client.IsConnected && _typeInfo.TryGetValue(notification.Variable.ToUpper(), out var type))
                                {
                                    var kvp = ReadWriteHandleInfo.FirstOrDefault(k => k.Key == notification.Variable.ToUpper());
                                    if (type != null)
                                    {
                                        ReadHandle(kvp.Value, type);
                                    }
                                }
                            }).DisposeWith(_cleanup);
                        }
                    }

                    return _cleanup;
                })
                .OnErrorRetry((Exception ex) => _errorReceived.OnNext(ex), TimeSpan.FromSeconds(5)).Publish().RefCount();

        private void ReadArrayHandle(uint? handle, Type type, int length) =>
            _readPLC.OnNext((handle, type, length));

        /// <summary>
        /// Reads the specified handle.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="type">The type.</param>
        private void ReadHandle(uint? handle, Type type) =>
            _readPLC.OnNext((handle, type, -1));

        /// <summary>
        /// Writes the specified value to the handle.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="value">The value.</param>
        private void WriteHandle(uint? handle, object value) =>
            _writePLC.OnNext((handle, value));
    }
}
