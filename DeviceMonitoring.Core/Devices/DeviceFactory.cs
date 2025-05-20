namespace DeviceMonitoring.Core.Devices
{
    public static class DeviceFactory
    {
        public static IDevice CreateDevice(string id, string name, DeviceType type)
        {
            IDevice? device = null;

            switch (type)
            {
                case DeviceType.TemperatureSensor:
                    device = new TemperatureControlDevice(id, name)
                    {
                        DeviceMonitorState = DeviceMonitorState.Idle
                    };
                    break; // Add break to prevent fall-through
                           // Add other device types here as needed
                default:
                    throw new NotSupportedException($"Device type {type} is not supported.");
            }

            return device;
        }
    }
}
