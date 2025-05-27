using DeviceMonitoring.UI.Models;
using Microsoft.Win32;
using System.IO;

namespace DeviceMonitoring.UI
{
    /// <summary>
    /// Defines a service for exporting device data to a file asynchronously .
    public interface IExportService
    {
        /// <summary>
        /// Exports the given devices to a file asynchronously.
        /// </summary>
        /// <param name="devices">The collection of devices to export.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<string?> ExportAsync(IEnumerable<DeviceModel> devices);
    }
    public class ExportService : IExportService
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public ExportService()
        {

        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Exports a collection of devices to a file in either JSON or CSV format.
        /// </summary>
        public async Task<string?> ExportAsync(IEnumerable<DeviceModel> devices)
        {
            var dlg = new SaveFileDialog { Filter = "JSON|*.json|CSV|*.csv", DefaultExt = ".json" };
            if (dlg.ShowDialog() != true) return null;

            var path = dlg.FileName;
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            path = Path.Combine(Path.GetDirectoryName(path),
                $"{Path.GetFileNameWithoutExtension(path)}_{timestamp}{Path.GetExtension(path)}");

            if (Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                await File.WriteAllTextAsync(path, devices.ToCsv());
            else
                await File.WriteAllTextAsync(path, devices.ToJson());

            return Path.GetFileName(path);
        }

        #endregion
    }
}
