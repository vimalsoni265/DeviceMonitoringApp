namespace DeviceMonitoring.Core.Devices
{
    /// <summary>
    /// Represents data associated with a specific device, including its identifier, data type, and value.
    /// </summary>
    /// <remarks>This class is typically used to encapsulate information about a device's data in a structured
    /// format. The <see cref="DeviceId"/> uniquely identifies the device, <see cref="DataType"/> specifies the type of
    /// data, and <see cref="Value"/> holds the actual data value.</remarks>
    public class DeviceData
    {
        public string DeviceId { get; }
        public string DataType { get; }
        public object Value { get; }

        public DeviceData(string id, string type, object value)
        {
            DeviceId = id;
            DataType = type;
            Value = value;
        }
    }
}
