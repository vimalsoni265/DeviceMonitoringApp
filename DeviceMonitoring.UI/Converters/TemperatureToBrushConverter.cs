using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DeviceMonitoring.UI.Converters
{
    public class TemperatureToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush m_lowTempBrush;
        private static readonly SolidColorBrush m_moderateTempBrush;
        private static readonly SolidColorBrush m_highTempBrush;
        private static readonly SolidColorBrush m_fallbackBrush;

        static TemperatureToBrushConverter()
        {
            // Initialize brushes
            m_lowTempBrush = FromHex("#FF4CAF50"); // Green for low temperatures
            m_moderateTempBrush = FromHex("#FFFFEB3B"); // Yellow for moderate temperatures 
            m_highTempBrush = FromHex("#FFF44336"); // Red for high temperatures
            m_fallbackBrush = new SolidColorBrush(Colors.Black);

            // Freeze brushes for thread safety and performance
            m_lowTempBrush.Freeze();
            m_moderateTempBrush.Freeze();
            m_highTempBrush.Freeze();
            m_fallbackBrush.Freeze();
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (double.TryParse(value?.ToString(), out double  temp))
            {
                if (temp < 40)
                    return m_lowTempBrush; // Green for low temperatures
                else if (temp < 70)
                    return m_moderateTempBrush; // Yellow for moderate temperatures
                else
                    return m_highTempBrush; // Red for high temperatures
            }

            return m_fallbackBrush; // fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private static SolidColorBrush FromHex(string hex)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
        }
    }
}
