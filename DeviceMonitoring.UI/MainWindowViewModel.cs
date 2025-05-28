using DeviceMonitoring.Core.Devices;
using DeviceMonitoring.Services;
using DeviceMonitoring.UI.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;

namespace DeviceMonitoring.UI
{
    /// <summary>
    /// Represents the view model for the main window of the application, providing data binding and interaction logic.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        #region Private Members
        private DeviceMonitoringService m_deviceMonitoringServiceInstance;
        private readonly IExportService m_exportService;
        private readonly INotificationManger m_notificationManger;
        private readonly IConfiguration m_configuration;
        private ObservableCollection<DeviceModel> m_devices = [];
        private Dictionary<string, DeviceModel> m_deviceMap = new();
        private bool _isDisposed;
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the collection of devices managed by the application.
        /// </summary>
        public ObservableCollection<DeviceModel> Devices
        {
            get => m_devices;
            set => SetProperty(ref m_devices, value);
        }

        /// <summary>
        /// Gets the command that starts monitoring connected devices.
        /// </summary>
        public ICommand StartDeviceMonitoringCommand { get; }

        /// <summary>
        /// Gets the command used to stop monitoring a device.
        /// </summary>
        public ICommand StopDeviceMonitoringCommand { get; }

        /// <summary>
        /// Gets the command that, when executed, displays live data in a graphical format.
        /// </summary>
        public ICommand ViewLiveGraphCommand { get; }

        /// <summary>
        /// Gets the command that hides the live graph when executed.
        /// </summary>
        public ICommand HideLiveGraphCommand { get; }

        /// <summary>
        /// Gets the command that exports data to a CSV file.
        /// </summary>
        public ICommand ExportToCSVCommand { get; }

        /// <summary>
        /// Gets the command that exports data to a Json file.
        /// </summary>
        public ICommand ExportToJsonCommand { get; }

        /// <summary>
        /// Gets the manager responsible for handling live graph operations.
        /// </summary>
        public LiveGraphManager LiveGraph { get; }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindowViewModel(DeviceMonitoringService deviceMonitoringService,
            IExportService exportService,
            INotificationManger notificationManger,
            IConfiguration configuration)
        {
            // Initialize Services
            m_exportService = exportService ?? throw new ArgumentNullException(nameof(exportService), "Export service cannot be null.");
            m_notificationManger = notificationManger ?? throw new ArgumentNullException(nameof(notificationManger), "Notification service cannot be null.");
            m_deviceMonitoringServiceInstance = deviceMonitoringService ?? throw new ArgumentNullException(nameof(deviceMonitoringService), "Device monitoring service cannot be null.");
            m_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null.");
            LiveGraph = new LiveGraphManager();

            // Initialize Commands.
            StartDeviceMonitoringCommand = new DelegateCommand<string?>(ExecuteStartDeviceCommand, CanExecuteStartMonitoringCommand);
            StopDeviceMonitoringCommand = new DelegateCommand<string?>(ExecuteStopDeviceCommand, CanExecuteStopMonitoringCommand);
            ViewLiveGraphCommand = new DelegateCommand<string?>(ExecuteViewLiveGraphCommand, CanExecuteViewLiveGraphCommand);
            HideLiveGraphCommand = new DelegateCommand<string?>(ExecuteHideLiveGraphCommand, CanExecuteHideLiveGraphCommand);
            ExportToCSVCommand = new AsyncDelegateCommand(ExecuteExportDeviceDataCommand, CanExecuteExportDeviceDataCommand);
            ExportToJsonCommand = new AsyncDelegateCommand(ExecuteExportDeviceDataCommand, CanExecuteExportDeviceDataCommand);
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Caches all command properties of the current instance for efficient re-evaluation.
        /// </summary>
        /// <remarks>This method identifies all properties of the current instance that are of type <see
        /// cref="DelegateCommandBase"/> or its derived types, retrieves their values, and stores them in an internal
        /// cache. This allows for optimized access to commands without repeatedly reflecting over the
        /// properties.</remarks>
        private void CachedCommands(ref List<DelegateCommandBase> cachedCommands)
        {
            // Cache commands for efficient re-evaluation           
            var commandProperties = this.GetType().GetProperties()
                                          .Where(prop => typeof(DelegateCommandBase).IsAssignableFrom(prop.PropertyType));
            foreach (var property in commandProperties)
            {
                if (property.GetValue(this) is DelegateCommandBase command)
                {
                    cachedCommands.Add(command);
                }
            }
        }

        public async Task RegisterAndLoadDevicesAsync()
        {
            IsBusy = true;
            try
            {
                await RegisterDevicesAsync();
                await LoadDevicesAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Registers a predefined number of temperature sensor devices with the monitoring service.
        /// </summary>
        /// <remarks>This method generates unique identifiers for each device and assigns them a name in
        /// the format  "Temperature Sensor {prefix}", where {prefix} is the first five characters of the generated
        /// identifier.  The devices are registered as temperature sensors.</remarks>
        private async Task RegisterDevicesAsync()
        {
            await Task.Run(() =>
            {
                var count = m_configuration.GetValue<int>("DeviceSettings:DeviceRegistrationCount");
                for (int i = 0; i < count; i++)
                {
                    var id = Guid.NewGuid().ToString();
                    m_deviceMonitoringServiceInstance.RegisterDevice(id, $"Temperature Sensor {id.Substring(0, 5).ToUpper()}", Core.DeviceType.TemperatureSensor);
                }
            });
        }

        /// <summary>
        /// Loads the list of devices and subscribes to their state and data change events.
        /// </summary>
        /// <remarks>This method clears the current device list and asynchronously retrieves all devices 
        /// from the device monitoring service. For each retrieved device, it subscribes to  the <see
        /// cref="Device.DeviceStateChanged"/> and <see cref="Device.DeviceDataChanged"/> events.</remarks>
        /// <returns></returns>
        private async Task LoadDevicesAsync()
        {
            // Unsubscribe from previous device events to prevent memory leaks and ensure clean state.
            if (Devices.Any())
            {
                foreach (var deviceModel in Devices)
                {
                    if (deviceModel.Device != null)
                    {
                        deviceModel.Device.DeviceDataChanged -= OnDeviceDataChanged;
                        deviceModel.Device.DeviceStateChanged -= OnDeviceStateChanged;
                    }
                }
            }

            var deviceModels = await Task.Run(() =>
            {
                var models = new List<DeviceModel>();
                foreach (var device in m_deviceMonitoringServiceInstance.GetAllDevices())
                {
                    models.Add(new DeviceModel(device));
                }

                return models;
            });

            // Check if devices are not null and contain any items
            if (deviceModels.Count != 0)
            {
                // Reset the current device list that results in a single "reset" notification to the UI. 
                Devices = [];
                m_deviceMap = [];

                // Add the retrieved device models to the collection
                deviceModels.ForEach(dm =>
                {
                    Devices.Add(dm);
                    // Populate the dictionary
                    m_deviceMap[dm.Device.Id] = dm;

                    dm.Device.DeviceDataChanged += OnDeviceDataChanged;
                    dm.Device.DeviceStateChanged += OnDeviceStateChanged;
                });
            }
        }

        /// <summary>
        /// Handles the event triggered when a device's state changes.
        /// </summary>
        private void OnDeviceStateChanged(object? sender, DeviceMonitorStateChangedEventArgs e)
        {
            // Dispatch to UI thread if needed
            if (!Dispatcher.CurrentDispatcher.CheckAccess())
            {
                Dispatcher.CurrentDispatcher.Invoke(() => OnDeviceStateChanged(sender, e));
                return;
            }

            if (m_deviceMap.TryGetValue(e.DeviceId, out var deviceModel))
                deviceModel.TriggerStateChanged();
        }

        /// <summary>
        /// Handles the event triggered when a device's data changes.
        /// </summary>
        private void OnDeviceDataChanged(object? sender, DeviceDataChangedEventArgs e)
        {
            // Dispatch to UI thread if needed
            if(!Dispatcher.CurrentDispatcher.CheckAccess())
            {
                Dispatcher.CurrentDispatcher.Invoke(() => OnDeviceDataChanged(sender, e));
                return;
            }

            if (m_deviceMap.TryGetValue(e.DeviceId, out var deviceModel))
                deviceModel.TriggerDataChanged();

            // Trigger graph update.
            if (LiveGraph.IsVisible && LiveGraph.SelectedDevice != null && LiveGraph.SelectedDevice.Id.Equals(e.DeviceId))
                LiveGraph.AddDataPoint(e.NewValue);
        }

        #region Command Implementation

        /// <summary>
        /// Determines whether the "Start Monitoring" command can be executed for the specified device.
        /// </summary>
        /// <param name="deviceId">The unique identifier of the device to evaluate.</param>
        private bool CanExecuteStartMonitoringCommand(string? deviceId)
        {
            if (!string.IsNullOrEmpty(deviceId) && m_deviceMap.TryGetValue(deviceId, out var device))
            {
                return device != null && !device.Device.DeviceMonitorState.Equals(Core.DeviceMonitorState.Monitoring);
            }
            return false;
        }

        /// <summary>
        /// Determines whether the "Stop Monitoring" command can be executed for the specified device.
        /// </summary>
        /// <param name="deviceId">The unique identifier of the device to evaluate.</param>
        private bool CanExecuteStopMonitoringCommand(string? deviceId)
        {
            if (!string.IsNullOrEmpty(deviceId) && m_deviceMap.TryGetValue(deviceId, out var device))
            {
                return device != null && device.Device.DeviceMonitorState.Equals(Core.DeviceMonitorState.Monitoring);
            }
            return false;
        }

        /// <summary>
        /// Determines whether the "View Live Graph" action can be executed for the specified device.
        /// </summary>
        /// <param name="deviceId"> The unique identifier of the device to evaluate.</param>
        private bool CanExecuteViewLiveGraphCommand(string? deviceId)
        {
            if (!string.IsNullOrEmpty(deviceId) && m_deviceMap.TryGetValue(deviceId, out var device))
            {
                return device != null && device.Device.DeviceMonitorState.Equals(Core.DeviceMonitorState.Monitoring);
            }

            return false;
        }

        /// <summary>
        /// Determines whether the "Hide Live Graph" action can be executed for the specified device.
        /// </summary>
        /// <param name="deviceId">The identifier of the device. This parameter is currently not used in the evaluation.</param>
        private bool CanExecuteHideLiveGraphCommand(string? deviceId)
        {
            return LiveGraph.IsVisible;
        }

        /// <summary>
        /// Determines whether the "Export to CSV/JSON" command can be executed.
        /// </summary>
        private bool CanExecuteExportDeviceDataCommand()
        {
            return (Devices != null && Devices.Any());
        }

        /// <summary>
        /// Initiates the monitoring process for the specified device.
        /// </summary>
        private void ExecuteStartDeviceCommand(string? deviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceId) || !m_deviceMap.TryGetValue(deviceId, out _))
                    return;

                m_deviceMonitoringServiceInstance.StartMonitoring(deviceId);
                m_notificationManger.ShowMessage($"Monitoring started for device {deviceId}.");
            }
            catch (Exception ex)
            {
                m_notificationManger.ShowMessage($"Error starting monitoring for device ID {deviceId}: {ex.Message}");
            }
            finally
            {
                // Re-evaluate command states after starting the device
                (StopDeviceMonitoringCommand as DelegateCommandBase)?.RaiseCanExecuteChanged();
                (ViewLiveGraphCommand as DelegateCommandBase)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Initiates the monitoring process for the specified device.
        /// </summary>
        private void ExecuteStopDeviceCommand(string? deviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceId) || !m_deviceMap.TryGetValue(deviceId, out _))
                    return;

                m_deviceMonitoringServiceInstance.StopMonitoring(deviceId);
                m_notificationManger.ShowMessage($"Monitoring stopped for device {deviceId}.");
            }
            catch (Exception ex)
            {
                m_notificationManger.ShowMessage($"Error stopping monitoring for device ID {deviceId}: {ex.Message}");
            }
            finally
            {
                // Re-evaluate command states after starting the device
                (StartDeviceMonitoringCommand as DelegateCommandBase)?.RaiseCanExecuteChanged();
                (ViewLiveGraphCommand as DelegateCommandBase)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Displays a live graph for the specified device.
        /// </summary>
        private void ExecuteViewLiveGraphCommand(string? deviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceId) || !m_deviceMap.TryGetValue(deviceId, out DeviceModel? deviceModel))
                    return;

                // Set the graph visibility flag to True if it is not already set
                if (!LiveGraph.IsVisible)
                    LiveGraph.IsVisible = true;

                // Configure the live graph for the selected device
                if (deviceModel is not null && (LiveGraph.SelectedDevice == null || !LiveGraph.SelectedDevice.Id.Equals(deviceModel.Device.Id)))
                    LiveGraph.ConfigureForDevice(deviceModel.Device);
            }
            catch (Exception ex)
            {
                m_notificationManger.ShowMessage($"Error stopping monitoring for device ID {deviceId}: {ex.Message}");
            }
            finally
            {
                // Re-evaluate command states after starting the device
                (HideLiveGraphCommand as DelegateCommandBase)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Hides the live graph associated with the specified device.
        /// </summary>
        /// <param name="obj">The identifier of the device for which the live graph should be hidden.  This parameter can be <see
        /// langword="null"/> if no specific device is targeted.</param>
        private void ExecuteHideLiveGraphCommand(string? deviceId)
        {
            try
            {
                LiveGraph.IsVisible = false;
                LiveGraph.SelectedDevice = null;
                LiveGraph.Clear();
            }
            catch (Exception ex)
            {
                m_notificationManger.ShowMessage($"Error hiding live graph for device ID {deviceId}: {ex.Message}");
            }
            finally
            {
                // Re-evaluate command states after hiding the graph
                (ViewLiveGraphCommand as DelegateCommandBase)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Executes the command to export data to a CSV/Json file.
        /// </summary>
        private async Task ExecuteExportDeviceDataCommand()
        {
            try
            {
                var fileName = await m_exportService.ExportAsync(Devices);
                if (!string.IsNullOrEmpty(fileName))
                    m_notificationManger.ShowMessage($"File exported! {fileName}");
            }
            catch (Exception ex)
            {
                m_notificationManger.ShowMessage($"Error exporting devices data: {ex.Message}");
            }
            finally
            {
                // Re-evaluate command states after exporting data
                (ExportToJsonCommand as DelegateCommandBase)?.RaiseCanExecuteChanged();
                (ExportToCSVCommand as DelegateCommandBase)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Releases the resources used by the current instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        /// <remarks>This method unsubscribes from device events, clears internal collections, and
        /// disposes of any disposable resources. It should be called when the instance is no longer needed to free up
        /// resources.</remarks>
        public void Dispose()
        {
            if (_isDisposed || Devices is null)
                return;

            // Unsubscribe from device events
            foreach (var deviceModel in Devices)
            {
                if (deviceModel.Device is null)
                    continue;

                deviceModel.Device.DeviceDataChanged -= OnDeviceDataChanged;
                deviceModel.Device.DeviceStateChanged -= OnDeviceStateChanged;
            }

            // Clear the collection and map
            Devices?.Clear();
            m_deviceMap?.Clear();
            LiveGraph?.Clear();

            // Dispose of the LiveGraphManager if it implements IDisposable
            // (LiveGraph as IDisposable)?.Dispose();


            GC.SuppressFinalize(this);
            _isDisposed = true;
        }

        #endregion

        #endregion
    }
}

