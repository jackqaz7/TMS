using System;
using System.Windows.Input;

namespace TMS_WPF_UI.Helpers
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            // WPF asks this before enabling command-bound controls. Example: the dashboard
            // disables Refresh while IsLoading is true.
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            // Execute runs the action supplied by the view model, such as LoginAsync or
            // LoadPositionsAsync wrapped in a lambda.
            _execute(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            // CommandManager.RequerySuggested lets WPF re-check CanExecute during common UI
            // events, keeping button enabled/disabled state in sync with view-model state.
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
