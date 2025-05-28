using DeviceMonitoring.Core;
using DeviceMonitoring.Core.Devices;
using DeviceMonitoring.Core.Exceptions;
using DeviceMonitoring.Services.NewFolder;
using System.Collections.Concurrent;

namespace DeviceMonitoring.Services
{
    public class DeviceMonitoringService : IDisposable
    {
        #region Private Fields

        private readonly ConcurrentDictionary<string, DeviceEntry> m_monitoringMap;

        private readonly CancellationTokenSource m_cts;
        private Task m_deviceMonitor;
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
            m_deviceMonitor = Task.Run(() => MonitorDevices(m_cts.Token), m_cts.Token);
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
            if (string.IsNullOrEmpty(deviceId))
                throw new ArgumentNullException(nameof(deviceId), "Device ID cannot be null or empty.");

            // Create the device based on the specified type and subscribe to its events.
            var device = DeviceFactory.CreateDevice(deviceId, deviceName, deviceType);
            device.DeviceStateChanged += (s, e) => DeviceMonitorStateChanged?.Invoke(s, e);

            // Create a new monitoring entry for the device.
            var entry = new DeviceEntry(device, deviceInterval ?? 1000, DateTime.Now);
            entry.SubscribeToStateChanged((s, e) => DeviceMonitorStateChanged?.Invoke(s, e));

            // Add the device to the monitoring map. If the device ID already exists, throw an exception.
            if (!m_monitoringMap.TryAdd(deviceId, entry))
                throw new DeviceAlreadyExistsException($"Device with ID {deviceId} already exists.");
        }

        /// <summary>
        /// Deregisters a device from monitoring and unsubscribes from its state change events.
        /// </summary>
        /// <remarks>This method removes the specified device from the monitoring map and unsubscribes
        /// from its  <see cref="DeviceStateChanged"/> event. If the device is not found in the monitoring map, no
        /// action is taken.</remarks>
        /// <param name="deviceId">The unique identifier of the device to deregister. Cannot be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="deviceId"/> is null or empty.</exception>
        public void DeregisterDevice(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
                throw new ArgumentNullException(nameof(deviceId), "Device ID cannot be null or empty.");

            // Attempt to remove the device from the monitoring map.
            if (!m_monitoringMap.TryRemove(deviceId, out DeviceEntry? entry))
            {
                Console.WriteLine($"Device with ID {deviceId} not found in monitoring map.");
                return;
            }

            // Dispose the entry.
            entry.Dispose();
        }

        /// <summary>
        /// Retrieves a device of the specified type by its unique identifier.
        /// </summary>
        public IDevice? GetDevice(string id)
        {
            if (!string.IsNullOrEmpty(id) &&
                m_monitoringMap.TryGetValue(id, out DeviceEntry? entry) &&
                entry.Device is IDevice device)
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
        public IEnumerable<IDevice> GetAllDevices()
        {
            if (m_monitoringMap?.Values is null || !m_monitoringMap.Values.Any())
                return [];

            // Return a snapshot of the current devices
            return m_monitoringMap.Values
                .Select(entry => entry.Device)
                .Where(device => device != null);
        }

        /// <summary>
        /// Starts monitoring the specified device asynchronously.
        /// </summary>
        /// <remarks>This method retrieves the device associated with the specified <paramref name="id"/>
        /// and starts its monitoring process if the device is found and is of a compatible type.</remarks>
        /// <typeparam name="T">The type of the device to monitor. Must implement <see cref="IDevice"/>.</typeparam>
        /// <param name="id">The unique identifier of the device to monitor. Must not be <see langword="null"/> or empty.</param>
        /// <returns></returns>
        public void StartMonitoring(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            // TryGet the device from the monitoring map
            if (!m_monitoringMap.TryGetValue(id, out DeviceEntry? entry))
                return;

            entry.SubscribeToDataChanged((s, e) => DeviceDataChanged?.Invoke(s, e));
            entry.Device.UpdateDeviceState(DeviceMonitorState.Monitoring);
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
        public void StopMonitoring(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            // TryGet the device from the monitoring map
            if (!m_monitoringMap.TryGetValue(id, out DeviceEntry? entry))
                return;

            entry.UnsubscribeFromDataChanged((s, e) => DeviceDataChanged?.Invoke(s, e));
            entry.Device.UpdateDeviceState(DeviceMonitorState.Stopped);
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
                try
                {
                    var now = DateTime.Now;
                    double minWait = double.MaxValue;

                    foreach (DeviceEntry entry in m_monitoringMap.Values)
                    {
                        var device = entry.Device;
                        if (device is null || 
                            device.DeviceMonitorState != DeviceMonitorState.Monitoring)
                            continue;

                        // Check if the device is ready for an update.
                        var elapsed = (now - entry.LastUpdated).TotalMilliseconds;
                        var wait = entry.Interval - elapsed;

                        if (elapsed >= entry.Interval)
                        {
                            // Perform device operation.
                            await device.PerformDeviceOperation();
                            entry.LastUpdated = now;
                            wait = entry.Interval; // After update, next due in full interval
                        }

                        if (wait < minWait)
                            minWait = wait;
                    }

                    // Wait until the next device is due, but not less than 10ms
                    var delay = Math.Max(10, (int)minWait);
                    await Task.Delay(delay, token);
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Method:{nameof(MonitorDevices)}, Error:{ex}");
                }
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

            // Dispose of all device entries in the monitoring map.
            foreach (var entry in m_monitoringMap.Values)
            {
                entry.Dispose();
            }
            m_monitoringMap.Clear();
        }

        #endregion
    }
}
