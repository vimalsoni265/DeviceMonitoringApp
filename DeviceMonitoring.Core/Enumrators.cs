namespace DeviceMonitoring.Core
{
    /// <summary>
    /// Represents the state of a device monitor.
    /// </summary>
    /// <remarks>This enumeration defines the possible states of a device monitor, such as whether it is
    /// actively monitoring, stopped/Idle, or in an unknown state.</remarks>
    public enum DeviceMonitorState
    {
        Unknown = -1,
        Idle,
        Monitoring,
        Stopped,
    }

    /// <summary>
    /// Represents the type of a device in a monitoring or control system.  
    /// </summary>
    /// <remarks>This enumeration is used to categorize devices by their functionality.  Currently, it
    /// includes a temperature sensor, but additional device types  can be added as needed to support other
    /// functionalities, such as motion  detection or environmental monitoring.</remarks>
    public enum DeviceType
    {
        Unknown = -1,
        TemperatureSensor,

        /* Could add more device types here, such as:
        HumiditySensor,
        PressureSensor,
        MotionSensor,
        LightSensor,
        SmokeDetector,
        GasDetector,
        WaterLeakDetector,
        DoorWindowSensor,
        Camera
        */
    }
}
