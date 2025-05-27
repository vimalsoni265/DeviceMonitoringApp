using DeviceMonitoring.Core.Devices;
using DeviceMonitoring.Services;
using DeviceMonitoring.UI.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DeviceMonitoring.UI
{
    /// <summary>
    /// Represents the view model for the main window of the application, providing data binding and interaction logic.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        #region Private Members
        private DeviceMonitoringService m_deviceMonitoringServiceInstance;
        private readonly IExportService m_exportService;
        private readonly INotificationManger m_notificationManger;
        private readonly IConfiguration m_configuration;
        private ObservableCollection<DeviceModel> m_devices = [];
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
            ExportToJsonCommand= new AsyncDelegateCommand(ExecuteExportDeviceDataCommand, CanExecuteExportDeviceDataCommand);
        }
        #endregion

        #region Private Methods


        public async Task RegisterAndLoadDevicesAsync()
        {
            IsBusy = true;
            try
            {
                await Task.Run(() => RegisterDevices());
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
        private void RegisterDevices()
        {
            var count = m_configuration.GetValue<int>("DeviceSettings:DeviceRegistrationCount");
            for (int i = 0; i < count; i++)
            {
                var id = Guid.NewGuid().ToString();
                m_deviceMonitoringServiceInstance.RegisterDevice(id, $"Temperature Sensor {id.Substring(0, 5).ToUpper()}", Core.DeviceType.TemperatureSensor);
                Task.Delay(10).Wait(); // Simulate a delay for device registration
            }

            IsBusy = false;
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
            // Asynchronously retrieve all devices from the device monitoring service
            // Run background logic
            var deviceModels = await Task.Run(() =>
            {
                var models = new List<DeviceModel>();
                foreach (var device in m_deviceMonitoringServiceInstance.GetAllDevices())
                {
                    models.Add(new DeviceModel(device));

                    device.DeviceDataChanged += OnDeviceDataChanged;
                    device.DeviceStateChanged += OnDeviceStateChanged;
                }

                return models;
            });

            // Check if devices are not null and contain any items
            if (deviceModels.Count != 0)
            {
                // Clear the current device list
                Devices.Clear();
                // Add the retrieved device models to the collection
                deviceModels.ForEach(Devices.Add);
            }
        }

        /// <summary>
        /// Handles the event triggered when a device's state changes.
        /// </summary>
        private void OnDeviceStateChanged(object? sender, DeviceMonitorStateChangedEventArgs e)
        {
            var device = Devices.FirstOrDefault(d => d.Device.Id.Equals(e.DeviceId));
            device?.TriggerStateChanged();
        }

        /// <summary>
        /// Handles the event triggered when a device's data changes.
        /// </summary>
        private void OnDeviceDataChanged(object? sender, DeviceDataChangedEventArgs e)
        {
            var device = Devices.FirstOrDefault(d => d.Device.Id.Equals(e.DeviceId));
            device?.TriggerDataChanged();

            // Trigger graph update.
            if (LiveGraph.IsVisible && LiveGraph.SelectedDevice != null && LiveGraph.SelectedDevice.Id.Equals(e.DeviceId))
                LiveGraph.AddDataPoint(e.NewValue);
        }

        /// <summary>
        /// Determines whether the specified device ID is valid and retrieves the corresponding device model if found.
        /// </summary>
        /// <returns><see langword="true"/> if the specified device ID is valid and a matching device model is found; otherwise,
        /// <see langword="false"/>.</returns>
        private bool IsValidDeviceId(string? deviceId, out DeviceModel? device)
        {
            device = null;

            if (!string.IsNullOrEmpty(deviceId.Trim()) && 
                Devices != null && 
                Devices.Any())
            {
                device = Devices?.FirstOrDefault(d => d.Device.Id.Equals(deviceId));
                return (device != null);
            }
            return false;
        }

        #region Command Implementation

        /// <summary>
        /// Determines whether the "Start Monitoring" command can be executed for the specified device.
        /// </summary>
        /// <param name="deviceId">The unique identifier of the device to evaluate.</param>
        private bool CanExecuteStartMonitoringCommand(string? deviceId)
        {
            if (Devices != null && !string.IsNullOrEmpty(deviceId))
            {
                var device = Devices.FirstOrDefault(d => d.Device.Id.Equals(deviceId));
                return device != null && device.Device.DeviceMonitorState != Core.DeviceMonitorState.Monitoring;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the "Stop Monitoring" command can be executed for the specified device.
        /// </summary>
        /// <param name="deviceId">The unique identifier of the device to evaluate.</param>
        private bool CanExecuteStopMonitoringCommand(string? deviceId)
        {
            if (Devices != null && !string.IsNullOrEmpty(deviceId))
            {
                var device = Devices.FirstOrDefault(d => d.Device.Id.Equals(deviceId));
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
            if (Devices != null && !string.IsNullOrEmpty(deviceId))
            {
                var device = Devices.FirstOrDefault(d => d.Device.Id.Equals(deviceId));
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
                if (!IsValidDeviceId(deviceId, out _))
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
                ReEvaluateCommands(this.GetType());
            }
        }

        /// <summary>
        /// Initiates the monitoring process for the specified device.
        /// </summary>
        private void ExecuteStopDeviceCommand(string? deviceId)
        {
            try
            {
                if (!IsValidDeviceId(deviceId, out _))
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
                ReEvaluateCommands(this.GetType());
            }
        }

        /// <summary>
        /// Displays a live graph for the specified device.
        /// </summary>
        private void ExecuteViewLiveGraphCommand(string? deviceId)
        {
            try
            {
                if (!IsValidDeviceId(deviceId, out DeviceModel? device))
                    return;

                // Set the graph visibility flag to True if it is not already set
                if (!LiveGraph.IsVisible)
                    LiveGraph.IsVisible = true;

                // Configure the live graph for the selected device
                if (device != null && (LiveGraph.SelectedDevice == null || LiveGraph.SelectedDevice.Id != device.Device.Id))
                    LiveGraph.ConfigureForDevice(device.Device);
            }
            catch (Exception ex)
            {
                m_notificationManger.ShowMessage($"Error stopping monitoring for device ID {deviceId}: {ex.Message}");
            }
            finally
            {
                // Re-evaluate command states after starting the device
                ReEvaluateCommands(this.GetType());
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
                ReEvaluateCommands(this.GetType());
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
                if (string.IsNullOrEmpty(fileName))
                    m_notificationManger.ShowMessage($"File exported! {fileName}");
            }
            catch (Exception ex)
            {
                m_notificationManger.ShowMessage($"Error exporting devices data: {ex.Message}");
            }
            finally
            {
                // Re-evaluate command states after exporting data
                ReEvaluateCommands(this.GetType());
            }
        }

        #endregion

        #endregion
    }
}

