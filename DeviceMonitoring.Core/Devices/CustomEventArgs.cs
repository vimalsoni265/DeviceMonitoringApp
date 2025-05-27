namespace DeviceMonitoring.Core.Devices
{
    /// <summary>
    /// Provides data for the event that is raised when a device's data changes.
    /// </summary>
    /// <remarks>This class contains information about the device whose data has changed, including the
    /// device's unique identifier and the new value of the changed data. It is typically used as the event argument for
    /// events that notify subscribers of changes to device data.</remarks>
    public class DeviceDataChangedEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public object NewValue { get; }
        public DeviceDataChangedEventArgs(string deviceId, object newValue)
        {
            DeviceId = deviceId;
            NewValue = newValue;
        }
    }


    /// <summary>
    /// Provides data for the event that is raised when the state of a device monitor changes.
    /// </summary>
    /// <remarks>This event argument contains information about the device whose state has changed,  including
    /// its unique identifier and the new state.</remarks>
    public class DeviceMonitorStateChangedEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public DeviceMonitorState NewState { get; }
        public DeviceMonitorStateChangedEventArgs(string deviceId, DeviceMonitorState newState)
        {
            DeviceId = deviceId;
            NewState = newState;
        }
    }
}
