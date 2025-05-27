namespace DeviceMonitoring.Core.Devices
{
    /// <summary>
    /// Represents a device with properties, state, and events for monitoring and managing its data and behavior.
    /// </summary>
    /// <remarks>The <see cref="IDevice"/> interface provides a contract for interacting with devices,
    /// including retrieving and updating their state, accessing their data, and responding to changes through events.
    /// Implementations of this interface should ensure thread safety for concurrent access to properties and methods
    /// where applicable.</remarks>
    public interface IDevice
    {
        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Gets or sets the name associated with the object.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the device.
        /// </summary>
        DeviceType Type { get; set; }

        /// <summary>
        /// Gets or sets the current value associated with this instance.
        /// </summary>
        string CurrentValue { get; }

        /// <summary>
        /// Gets or sets the current state of the device monitor.
        /// </summary>
        DeviceMonitorState DeviceMonitorState { get; set; }

        /// <summary>
        /// Performs an operation on the monitoring device.
        /// </summary>
        /// <remarks>This method executes a predefined operation on the currently connected device. 
        /// Ensure that a device is properly connected and initialized before calling this method.</remarks>
        void PerformDeviceOperation();

        /// <summary>
        /// Updates the current state of the device monitor.
        /// </summary>
        /// <remarks>Use this method to change the operational state of the device monitor. Ensure that
        /// the provided state is valid and appropriate for the current context.</remarks>
        /// <param name="state">The new state to set for the device monitor. This value must be a valid <see cref="DeviceMonitorState"/>
        /// enumeration.</param>
        void UpdateDeviceState(DeviceMonitorState state);

        /// <summary>
        /// Occurs when the state of a monitored device changes.
        /// </summary>
        /// <remarks>Subscribe to this event to receive notifications when a device's state changes.  The
        /// event provides details about the state change through the <see cref="DeviceMonitorStateChangedEventArgs"/>
        /// parameter.</remarks>
        event EventHandler<DeviceMonitorStateChangedEventArgs> DeviceStateChanged;

        /// <summary>
        /// Occurs when the data associated with a device changes.
        /// </summary>
        /// <remarks>This event is triggered whenever there is a change in the device's data, such as
        /// updates to its properties or state. Subscribers can handle this event to respond to these changes.</remarks>
        event EventHandler<DeviceDataChangedEventArgs> DeviceDataChanged;
    }
}
