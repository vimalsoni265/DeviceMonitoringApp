using System.ComponentModel;
using System.Windows.Input;

namespace DeviceMonitoring.UI
{
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        #region Properties

        /// <summary>
        /// Determine ViewModel is disposed or not.
        /// </summary>
        public bool IsDisposed
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        protected ICommand CreateCommand(Func<Task> execute, Func<bool>? canExecute = null) =>
        new RelayCommand(execute, canExecute);

        protected ICommand CreateCommand(Action execute, Func<bool>? canExecute = null) =>
            new RelayCommand(() => { execute(); return Task.CompletedTask; }, canExecute);

        /// <summary>
        /// Frees up resources allocated by the class.
        /// </summary>
        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
            IsDisposed = true;
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Event Handlers

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event to notify listeners that a property value has changed.
        /// </summary>
        /// <remarks>This method is typically called by property setters to notify bound controls or other
        /// listeners of changes to the property value. Subclasses can override this method to provide additional
        /// behavior when a property changes.</remarks>
        /// <param name="propertyName">The name of the property that changed. This value cannot be <see langword="null"/> or empty.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Nested RelayCommand Class 

        /// <summary>
        /// Represents a command that can be executed asynchronously and determines its ability to execute based on a
        /// specified condition.
        /// </summary>
        /// <remarks>This class implements the <see cref="ICommand"/> interface, allowing it to be used in
        /// data binding scenarios, The command's ability to execute is determined by the
        /// optional <see cref="Func{TResult}"/> provided during construction. If no condition is provided, the command
        /// is always executable.<br/><br/>
        /// This class implementation is kept nested so that it can only be accessible through <see cref="ViewModelBase"/>.</remarks>
        private class RelayCommand : ICommand
        {
            #region Private Members

            private readonly Func<bool>? m_canExecute;
            private readonly Func<Task> m_execute;

            #endregion

            #region Constructor

            /// <summary>
            /// Constructs a new instance of the <see cref="RelayCommand"/> class.
            /// </summary>
            /// <param name="execute"></param>
            /// <param name="canExecute"></param>
            /// <exception cref="ArgumentNullException"></exception>
            public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
            {
                m_execute = execute ?? throw new ArgumentNullException(nameof(execute));
                m_canExecute = canExecute;
            }

            #endregion

            #region Event Handlers

            /// <summary>
            /// Occurs when changes occur that affect whether the command should execute.
            /// </summary>
            public event EventHandler? CanExecuteChanged;

            #endregion

            #region Public Methods

            /// <summary>
            /// Determines whether the command can be executed.
            /// </summary>
            /// <param name="parameter"></param>
            /// <returns></returns>
            public bool CanExecute(object? parameter) => m_canExecute == null || m_canExecute();

            /// <summary>
            /// Executes the command Async.
            /// </summary>
            /// <param name="parameter"></param>
            public async void Execute(object? parameter)
            {
                await m_execute();
            }

            /// <summary>
            /// Executes the command.
            /// </summary>
            /// <param name="parameter"></param>
            public void Execute() => Execute(null);

            /// <summary>
            /// Raises the <see cref="CanExecuteChanged"/> event.
            /// </summary>
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            #endregion
        }
        #endregion
    }
}
