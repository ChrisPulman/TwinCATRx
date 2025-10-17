// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#if WINDOWS
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.ServiceProcess;

namespace CP.TwinCatRx;

/// <summary>
/// Observable Service Controller.
/// </summary>
public class ObservableServiceController : IObservableServiceController
{
    private readonly CompositeDisposable _cleanup = [];
    private readonly Subject<ServiceControllerStatus> _statusChanged = new();
    private ServiceController? _serviceController;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableServiceController"/> class.
    /// </summary>
    /// <param name="oService">The o service.</param>
    public ObservableServiceController(ServiceController oService) => CreateObject(oService, TimeSpan.FromSeconds(.5));

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableServiceController"/> class.
    /// </summary>
    /// <param name="oService">The o service.</param>
    /// <param name="oInterval">The o interval.</param>
    public ObservableServiceController(ServiceController oService, TimeSpan oInterval) => CreateObject(oService, oInterval);

    /// <summary>
    /// Gets a value indicating whether the is disposed.
    /// </summary>
    public bool IsDisposed => _cleanup.IsDisposed;

    /// <summary>
    /// Gets a value indicating whether this instance can stop.
    /// </summary>
    /// <value><c>true</c> if this instance can stop; otherwise, <c>false</c>.</value>
    public bool CanStop => _serviceController?.CanStop == true;

    /// <summary>
    /// Gets the display name.
    /// </summary>
    /// <value>The display name.</value>
    public string DisplayName => _serviceController == null ? string.Empty : _serviceController.DisplayName;

    /// <summary>
    /// Gets the name of the service.
    /// </summary>
    /// <value>The name of the service.</value>
    public string ServiceName => _serviceController == null ? string.Empty : _serviceController.ServiceName;

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <value>The status.</value>
    public ServiceControllerStatus Status => _serviceController == null ? ServiceControllerStatus.Stopped : _serviceController.Status;

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <value>The status.</value>
    public IObservable<ServiceControllerStatus> StatusObserver => _statusChanged;

    /// <summary>
    /// Gets the services.
    /// </summary>
    /// <returns>A Value.</returns>
    public static IObservable<ObservableServiceController> GetServices() =>
        Observable.Create<ObservableServiceController>(o =>
            {
                var d = new CompositeDisposable();
                try
                {
                    foreach (var sc in ServiceController.GetServices())
                    {
                        var service = new ObservableServiceController(sc);
                        service.DisposeWith(d);
                        o.OnNext(service);
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    // ServiceController may not be supported on certain Windows environments (e.g., Nano/containers).
                    // Treat as no services available and complete the sequence gracefully.
                    o.OnCompleted();
                }

                return d;
            });

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting
    /// unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Restarts this instance.
    /// </summary>
    public void Restart()
    {
        try
        {
            if (!IsDisposed)
            {
                if (_serviceController?.CanStop == true && (_serviceController.Status == ServiceControllerStatus.Running || _serviceController.Status == ServiceControllerStatus.Paused))
                {
                    Stop();
                    _serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                }

                if (_serviceController?.Status == ServiceControllerStatus.Stopped)
                {
                    Start();
                    _serviceController.WaitForStatus(ServiceControllerStatus.Running);
                }
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Starts this instance.
    /// </summary>
    public void Start()
    {
        try
        {
            if (!IsDisposed)
            {
                _serviceController?.Start();
                _serviceController?.WaitForStatus(ServiceControllerStatus.Running);
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Stops this instance.
    /// </summary>
    public void Stop()
    {
        try
        {
            if (!IsDisposed)
            {
                _serviceController?.Stop();
                _serviceController?.WaitForStatus(ServiceControllerStatus.Stopped);
            }
        }
        catch
        {
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
        if (!_cleanup.IsDisposed && disposing)
        {
            _serviceController?.Dispose();
            _cleanup.Dispose();
            _statusChanged.Dispose();
        }
    }

    /// <summary>
    /// Creates the object.
    /// </summary>
    /// <param name="service">The service.</param>
    /// <param name="interval">The interval.</param>
    private void CreateObject(ServiceController service, TimeSpan interval)
    {
        _serviceController = service;
        _serviceController.DisposeWith(_cleanup);
        var serviceControllerIsDisposed = false;
        _serviceController.Disposed += (e, o) => serviceControllerIsDisposed = true;

        Observable.Interval(interval).Retry().Subscribe(_ =>
        {
            try
            {
                if (!serviceControllerIsDisposed)
                {
                    var oCurrentStatus = _serviceController?.Status;
                    _serviceController?.Refresh();

                    if (_serviceController != null && oCurrentStatus != null && oCurrentStatus.HasValue && oCurrentStatus != _serviceController?.Status)
                    {
                        _statusChanged.OnNext(_serviceController!.Status);
                    }
                }
            }
            catch
            {
            }
        }).DisposeWith(_cleanup);
    }
}
#endif
