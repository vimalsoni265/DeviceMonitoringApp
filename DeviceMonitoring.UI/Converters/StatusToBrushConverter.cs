using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DeviceMonitoring.UI.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        // Static readonly frozen brushes for thread safety and performance
        private static readonly Brush m_monitoringBrush;
        private static readonly Brush m_stoppedBrush;
        private static readonly Brush m_defaultBrush;

        static StatusToBrushConverter()
        {
            // Initialize brushes
            m_monitoringBrush = Brushes.Green.Clone();
            m_stoppedBrush = Brushes.Red.Clone();
            m_defaultBrush = Brushes.Gray.Clone();

            // Freeze brushes for thread safety and performance
            m_monitoringBrush.Freeze();
            m_stoppedBrush.Freeze();
            m_defaultBrush.Freeze();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string;
            return status switch
            {
                "Monitoring" => m_monitoringBrush,
                "Stopped" => m_stoppedBrush,
                _ => m_defaultBrush
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
