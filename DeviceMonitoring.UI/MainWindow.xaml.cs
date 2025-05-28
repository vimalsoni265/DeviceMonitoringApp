using DeviceMonitoring.UI.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DeviceMonitoring.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel m_viewModel;
        public MainWindow()
        {
            InitializeComponent();

            var notificationService = App.ServiceProvider.GetRequiredService<INotificationManger>();
            if (notificationService is NotificationManger manager)
            {
                manager.SetSnackbar(MainSnackbar);
            }

            // DI container to manage all dependencies and their lifetimes.
            m_viewModel = App.ServiceProvider.GetRequiredService<MainWindowViewModel>();
            DataContext = m_viewModel;

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Window.Closed"/> event for the main window.
        /// </summary>
        private void MainWindow_Closed(object? sender, EventArgs e) => m_viewModel?.Dispose();

        /// <summary>
        /// Handles the <see cref="FrameworkElement.Loaded"/> event for the main window.
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await m_viewModel.RegisterAndLoadDevicesAsync();
        }
    }
}