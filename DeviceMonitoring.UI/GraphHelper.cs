using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace DeviceMonitoring.UI
{
    public static class GraphHelper
    {
        public static DateTimeAxis GetCustomXAxes()
        {
            return new DateTimeAxis(TimeSpan.FromSeconds(1), Formatter)
            {
                Name = "Time",
                CustomSeparators = GraphHelper.GetSeparators(),
                AnimationsSpeed = TimeSpan.FromMilliseconds(0),
                SeparatorsPaint = new SolidColorPaint(SKColors.Silver.WithAlpha(50))
            };
        }

        public static double[] GetSeparators()
        {
            var now = DateTime.Now;

            return
            [
                now.AddHours(-1).Ticks,
                now.AddMinutes(-30).Ticks,
                now.AddMinutes(-15).Ticks,
                now.AddMinutes(-5).Ticks,
                now.AddMinutes(-1).Ticks,
                now.AddSeconds(-30).Ticks,
                now.AddSeconds(-20).Ticks,
                now.AddSeconds(-10).Ticks,
                now.AddSeconds(-5).Ticks,
                now.Ticks
            ];
        }

        public static string Formatter(DateTime date)
        {
            var secsAgo = (DateTime.Now - date).TotalSeconds;
            return secsAgo < 1 ? "now" : $"{secsAgo:N0}s ago";
        }

        public static LineSeries<DateTimePoint> GetLineSeriesForRealTimeGraph()
        {
            return new LineSeries<DateTimePoint>
            {
                Name = "Real-time Device Data",
                Values = null,
                Fill = null,
                GeometrySize = 0,
                GeometryFill = null,
                GeometryStroke = null
            };
        }
    }
}
