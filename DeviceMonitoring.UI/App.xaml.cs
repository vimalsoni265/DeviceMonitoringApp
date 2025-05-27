using DeviceMonitoring.Services;
using DeviceMonitoring.UI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DeviceMonitoring.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Register your services
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton(DeviceMonitoringService.Instance);

            services.AddSingleton<IExportService, ExportService>();
            services.AddSingleton<INotificationManger, NotificationManger>();
            services.AddTransient<MainWindowViewModel>();

            // Ensure the required namespace is included for BuildServiceProvider
            ServiceProvider = services.BuildServiceProvider();
            base.OnStartup(e);
        }
    }

}
