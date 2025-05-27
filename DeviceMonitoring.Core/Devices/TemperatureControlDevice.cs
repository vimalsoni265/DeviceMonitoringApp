
namespace DeviceMonitoring.Core.Devices
{
    /// <summary>
    /// Represents a temperature control device capable of monitoring and reporting temperature readings.
    /// </summary>
    /// <remarks>This class extends the <see cref="DeviceBase"/> base class and provides functionality for
    /// tracking temperature readings, including the current and previous temperature values, as well as the timestamp
    /// of the last update. The device can be started and stopped for monitoring using the <see cref="StartMonitoring"/>
    /// and <see cref="StopMonitoring"/> methods.</remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="TemperatureControlDevice"/> class with the specified identifier
    /// and name.
    /// </remarks>
    /// <param name="id">The unique identifier for the temperature control device. Cannot be null or empty.</param>
    /// <param name="name">The name of the temperature control device. Cannot be null or empty.</param>
    public class TemperatureControlDevice(string id, string name) : IDevice
    {
        #region Private Members

        private readonly Random m_randTemp = new();

        private double m_CurrentTemperature;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current temperature value as a formatted string.
        /// </summary>
        public string CurrentValue => m_CurrentTemperature.ToString("F2");
        
        /// <summary>
        /// Gets or sets the unique identifier for the device.
        /// </summary>
        public string Id { get; set; } = id;

        /// <summary>
        /// Gets or sets the name of the device.
        /// </summary>
        public string Name { get; set; } = name;

        /// <summary>
        /// Gets or sets the type of the device.
        /// </summary>
        public DeviceType Type { get; set; } = DeviceType.TemperatureSensor;

        /// <summary>
        /// Gets or sets the current monitoring state of the device.
        /// </summary>
        public DeviceMonitorState DeviceMonitorState { get; set; }

        /// <summary>
        /// Occurs when the state of a monitored device changes.
        /// </summary>
        /// <remarks>This event is raised whenever a device's state transitions, such as from connected to
        /// disconnected or from idle to active. Subscribers can handle this event to respond to changes in device
        /// state.</remarks>
        public event EventHandler<DeviceMonitorStateChangedEventArgs> DeviceStateChanged;

        /// <summary>
        /// Occurs when the data associated with a device changes.
        /// </summary>
        /// <remarks>This event is triggered whenever there is a change in the device's data.  Subscribers
        /// can handle this event to respond to updates or changes in the device's state or data.</remarks>
        public event EventHandler<DeviceDataChangedEventArgs> DeviceDataChanged;

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs a device operation to update the current temperature reading.
        /// </summary>
        /// <remarks>This method generates a new random temperature value and updates the device's current
        /// temperature. The temperature value is a double within the range of 0 to 100.0001.</remarks>
        public void PerformDeviceOperation()
        {
            if (DeviceMonitorState != DeviceMonitorState.Monitoring)
                return;

            // Update the current temperature and record the time of the update
            m_CurrentTemperature = GetNewTemperatureFromDevice();

            // Invoke the DataChanged event to notify subscribers of the temperature change
            DeviceDataChanged?.Invoke(this, new DeviceDataChangedEventArgs(Id, m_CurrentTemperature));  
        }

        /// <summary>
        /// Retrieves a new temperature reading from the device.
        /// </summary>
        /// <returns>A string representation of the temperature value retrieved from the device.</returns>
        private double GetNewTemperatureFromDevice()
        {
            // Simulate in this case for now.
            return m_randTemp.NextDouble() * 100.01;
        }

        /// <summary>
        /// Updates the current state of the device and notifies subscribers of the state change.
        /// </summary>
        /// <remarks>This method updates the <see cref="DeviceMonitorState"/> property with the provided
        /// state and raises the <see cref="DeviceStateChanged"/> event to notify subscribers of the change.</remarks>
        /// <param name="state">The new state of the device to be set. This value cannot be null.</param>
        public void UpdateDeviceState(DeviceMonitorState state)
        {
            DeviceMonitorState = state;

            // Notify subscribers of the state change
            DeviceStateChanged?.Invoke(this, new DeviceMonitorStateChangedEventArgs(Id, state));
        }

        #endregion
    }
}
