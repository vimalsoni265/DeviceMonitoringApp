using DeviceMonitoring.Core.Devices;

namespace DeviceMonitoring.Services.NewFolder
{
    public class DeviceEntry(IDevice device, int interval, DateTime lastUpdated)
    {
        /// <summary>
        /// Gets or sets the device associated with the current operation.  
        /// </summary>
        public IDevice Device { get; set; } = device ?? throw new ArgumentNullException(nameof(device));

        /// <summary>
        /// Gets or sets the time interval for the operation.
        /// </summary>
        public int Interval { get; set; } = interval;

        /// <summary>
        /// Gets or sets the date and time of the last update.
        /// </summary>
        public DateTime LastUpdated { get; set; } = lastUpdated;
    }
}
