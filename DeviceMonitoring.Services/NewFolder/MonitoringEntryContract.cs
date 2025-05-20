using DeviceMonitoring.Core.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceMonitoring.Services.NewFolder
{
    public class MonitoringEntryContract
    {
        /// <summary>
        /// Gets or sets the device associated with the current operation.  
        /// </summary>
        public IDevice Device { get; set; }

        /// <summary>
        /// Gets or sets the time interval for the operation.
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the last update.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        public MonitoringEntryContract(IDevice device, int interval, DateTime lastUpdated)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
            Interval = interval;
            LastUpdated = lastUpdated;
        }
    }
}
