using DeviceMonitoring.Core;
using DeviceMonitoring.Core.Devices;
using DeviceMonitoring.Core.Exceptions;
using DeviceMonitoring.Services.NewFolder;

namespace DeviceMonitoring.Services
{
    public class DeviceMonitoringService : IDisposable
    {
        #region Private Fields

        private readonly Dictionary<string, MonitoringEntryContract> m_monitoringMap;

        private readonly CancellationTokenSource m_cts;
        private Task m_deviceMonitor;
        private readonly object _lock = new();
        #endregion

        #region Events/Actions

        public event EventHandler<DeviceDataChangedEventArgs> DeviceDataChanged;
        public event EventHandler<DeviceMonitorStateChangedEventArgs>? DeviceMonitorStateChanged;

        #endregion

        #region Singleton Constructor

        /// <summary>
        /// Gets the singleton instance of the <see cref="DeviceManager"/> class.
        /// </summary>
        /// <remarks>This property provides a thread-safe, lazily initialized singleton instance of the 
        /// <see cref="DeviceManager"/> class. Use this instance to access shared functionality  without creating
        /// multiple instances.</remarks>
        private static readonly Lazy<DeviceMonitoringService> m_instance = new(() => new DeviceMonitoringService());

        /// <summary>
        /// Gets the singleton instance of the <see cref="DeviceManager"/> class.
        /// </summary>
        /// <remarks>This property provides access to the globally shared instance of the <see
        /// cref="DeviceManager"/> class. It ensures that only one instance of the repository is created and used
        /// throughout the application.</remarks>
        public static DeviceMonitoringService Instance => m_instance.Value;

        /// <summary>
        /// Constructor.
        /// </summary>
        private DeviceMonitoringService()
        {
            // Initialize the devices collection.
            m_monitoringMap = new();

            // Used TaskFactory + LongRunning because
            // we want dedicated threads and or need separation from the ThreadPool.
            m_cts = new CancellationTokenSource();
            m_deviceMonitor = Task.Factory.StartNew(() =>
                                    MonitorDevices(m_cts.Token),
                                    m_cts.Token,
                                    TaskCreationOptions.LongRunning,
                                    TaskScheduler.Default);
        }

        #endregion

        /// <summary>
        /// Adds a new device to the system with the specified ID, name, and type.
        /// </summary>
        /// <remarks>This method creates and registers a new device in the system based on the specified
        /// type.  Currently, only certain device types are supported. Additional device types can be added as
        /// needed.</remarks>
        /// <param name="deviceId">The unique identifier for the device. Cannot be null or empty.</param>
        /// <param name="deviceName">The name of the device to be added.</param>
        /// <param name="deviceType">The type of the device to be added. Must be a supported <see cref="DeviceType"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="deviceId"/> is null or empty.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="deviceType"/> is not a supported device type.</exception>
        public void RegisterDevice(string deviceId, string deviceName, DeviceType deviceType, int? deviceInterval = null)
        {
            if(string.IsNullOrEmpty(deviceId))
                throw new ArgumentNullException(nameof(deviceId), "Device ID cannot be null or empty.");

            if(m_monitoringMap.ContainsKey(deviceId))
                throw new DeviceAlreadyExistsException($"Device with ID {deviceId} already exists.");

            lock (_lock)
            {
                // Create the device based on the specified type and subscribe to its events.
                var device = DeviceFactory.CreateDevice(deviceId, deviceName, deviceType);
                device.DeviceStateChanged += (s, e) => DeviceMonitorStateChanged?.Invoke(s, e);
                device.DeviceDataChanged += (s, e) => DeviceDataChanged?.Invoke(s, e);


                // Create a new monitoring entry for the device.
                var entry = new MonitoringEntryContract(device, deviceInterval ?? 1000, DateTime.MinValue);

                // Add the device to the monitoring map.
                m_monitoringMap.Add(deviceId, entry);
            }
        }

        /// <summary>
        /// Retrieves the current data for the specified device.
        /// </summary>
        /// <param name="deviceId">The unique identifier of the device whose data is to be retrieved. Cannot be null or empty.</param>
        /// <returns>A <see cref="DeviceData"/> object containing the current data of the specified device,  or <see
        /// langword="null"/> if the device is not found.</returns>
        public DeviceData? GetDeviceData(string deviceId)
        {
            lock (_lock)
            {

                return m_monitoringMap.TryGetValue(deviceId, out MonitoringEntryContract? entry) && entry.Device is IDevice device
                    ? device.GetCurrentData()
                    : null;
            }
        }

        /// <summary>
        /// Retrieves a device of the specified type by its unique identifier.
        /// </summary>
        /// <remarks>If the device with the specified identifier exists but cannot be cast to the
        /// specified type <typeparamref name="T"/>, this method will return <see langword="null"/>.</remarks>
        /// <typeparam name="T">The type of the device to retrieve. Must be a class that implements <see cref="IDevice"/>.</typeparam>
        /// <param name="id">The unique identifier of the device to retrieve. Cannot be <see langword="null"/> or empty.</param>
        /// <returns>The device of type <typeparamref name="T"/> if found and successfully cast; otherwise, <see
        /// langword="null"/>.</returns>
        public T? GetDevice<T>(string id) where T : class, IDevice
        {
            if (m_monitoringMap.TryGetValue(id, out MonitoringEntryContract? entry) &&
                entry.Device is T device)
            {
                return device;
            }

            return null;
        }

        /// <summary>
        /// Retrieves a collection of all devices currently managed by the system.
        /// </summary>
        /// <remarks>The returned collection represents a snapshot of the current state of devices at the
        /// time of the call. Any subsequent changes to the underlying device collection will not be reflected in the
        /// returned result.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="IEnumerable{T}"/>
        /// of <see cref="IDevice"/> objects representing the current devices. If no devices are available, the
        /// collection will be empty.</returns>
        public async Task<IEnumerable<IDevice>> GetAllDevices()
        {
            if (m_monitoringMap?.Values is null || !m_monitoringMap.Values.Any())
                return Enumerable.Empty<IDevice>();

            var devices = m_monitoringMap.Values
                .Select(entry => entry.Device)
                .Where(device => device != null)
                .ToList();

            // Return a snapshot of the current devices
            return await Task.FromResult(devices);
        }

        /// <summary>
        /// Starts monitoring the specified device asynchronously.
        /// </summary>
        /// <remarks>This method retrieves the device associated with the specified <paramref name="id"/>
        /// and starts its monitoring process if the device is found and is of a compatible type.</remarks>
        /// <typeparam name="T">The type of the device to monitor. Must implement <see cref="IDevice"/>.</typeparam>
        /// <param name="id">The unique identifier of the device to monitor. Must not be <see langword="null"/> or empty.</param>
        /// <returns></returns>
        public void StartMonitoring<T>(string id) where T : class, IDevice
        {
            if (string.IsNullOrEmpty(id))
                return;

            // Get the device from the monitoring map
            var device = GetDevice<T>(id);
            if (device is null)
                return;

            //Update its state to Monitoring.
            device.UpdateDeviceState(DeviceMonitorState.Monitoring);
        }

        /// <summary>
        /// Stops monitoring the specified device.
        /// </summary>
        /// <remarks>This method attempts to stop monitoring the device associated with the specified
        /// <paramref name="id"/>. If the device is not found or is not of a compatible type, no action is
        /// taken.</remarks>
        /// <typeparam name="T">The type of the device, which must implement <see cref="IDevice"/>.</typeparam>
        /// <param name="id">The unique identifier of the device to stop monitoring.</param>
        /// <returns></returns>
        public void StopMonitoring<T>(string id) where T : class, IDevice
        {
            if (string.IsNullOrEmpty(id))
                return;

            // Get the device from the monitoring map
            var device = GetDevice<T>(id);
            if (device is null)
                return;

            //Update its state to Monitoring.
            device.UpdateDeviceState(DeviceMonitorState.Stopped);
        }

        #region Private methods

        /// <summary>
        /// Monitors the devices and updates their current temperature at regular intervals.
        /// </summary>
        /// <remarks>This method continuously iterates through the monitored devices, simulates
        /// temperature updates,  and raises the <see cref="TemperatureUpdated"/> event for each device with the updated
        /// temperature  and the timestamp of the update. The monitoring process runs until the provided  <paramref
        /// name="token"/> signals cancellation.  The method uses a delay between iterations to control the monitoring
        /// frequency. Adjust the delay  as needed to balance performance and responsiveness.</remarks>
        /// <param name="token">A <see cref="CancellationToken"/> used to signal the cancellation of the monitoring process.</param>
        /// <exception cref="NotImplementedException">Thrown when the method is terminated without proper implementation of the cancellation logic.</exception>
        private async Task MonitorDevices(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                foreach (MonitoringEntryContract entry in m_monitoringMap.Values)
                {
                    var device = entry.Device;
                    if (device is null)
                        continue;

                    if (!entry.Device.DeviceMonitorState.Equals(DeviceMonitorState.Monitoring))
                        continue;

                    // Check if the device is ready for an update.
                    var elapsed = (now - entry.LastUpdated).TotalMilliseconds;
                    if (elapsed >= entry.Interval)
                    {
                        // Perform device operation.
                        device.PerformDeviceOperation();
                    }
                }

                // Add Delay to adjust CPU load.
                await Task.Delay(100, token);
            }
        } 

        #endregion

        #region Implement IDisposable

        /// <summary>
        /// Releases the resources used by the current instance of the class.
        /// </summary>
        /// <remarks>This method cancels any ongoing operations, waits for the device monitor to complete,
        /// and disposes of internal resources. After calling this method, the instance should  no longer be
        /// used.</remarks>
        public void Dispose()
        {
            m_cts.Cancel();
            m_deviceMonitor.Wait();
            m_cts.Dispose();
        }

        #endregion
    }
}
