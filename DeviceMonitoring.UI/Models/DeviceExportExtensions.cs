using System.Text;
using System.Text.Json;

namespace DeviceMonitoring.UI.Models
{
    public static class DeviceExportExtensions
    {
        public static string ToCsv(this IEnumerable<DeviceModel> devices)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Id,Name,Temperature,Status,LastUpdated");

            foreach (var d in devices)
            {
                sb.AppendLine(d.ToString());
            }

            return sb.ToString();
        }

        public static string ToJson(this IEnumerable<DeviceModel> devices)
        {
            var exportList = devices.Select(d => new
            {
                Id = d.Device.Id,
                Name = d.Device.Name,
                Temperature = d.Device.CurrentValue,
                Status = d.Device.DeviceMonitorState.ToString(),
                LastUpdated = d.LastUpdated
            });

            return JsonSerializer.Serialize(exportList, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
