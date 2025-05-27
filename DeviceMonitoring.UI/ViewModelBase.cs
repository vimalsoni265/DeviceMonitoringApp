using System.Windows.Input;

namespace DeviceMonitoring.UI
{
    /// <summary>
    /// Serves as the base class for view models in an MVVM (Model-View-ViewModel) architecture.
    /// </summary>
    public abstract class ViewModelBase : BindableBase
    {
        protected virtual void ReEvaluateCommands(Type type)
        {
            var commandProperties = type
                .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .Where(p => typeof(ICommand).IsAssignableFrom(p.PropertyType));

            foreach (var commandProperty in commandProperties)
            {
                if (commandProperty.GetValue(this) is DelegateCommandBase command)
                {
                    command.RaiseCanExecuteChanged();
                }
            }
        }

    }
}
