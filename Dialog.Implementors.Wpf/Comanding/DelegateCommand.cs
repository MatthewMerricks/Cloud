using System;
using System.Windows.Input;

namespace Dialog.Implementors.Wpf.Comanding
{
  public class DelegateCommand<T> : ICommand
  {
    private readonly Action<T> executeAction;
    private readonly Func<T, bool> canExecuteAction;

    public DelegateCommand(Action<T> executeAction, Func<T, bool> canExecuteAction)
    {
      this.executeAction = executeAction;
      this.canExecuteAction = canExecuteAction;
    }

    public DelegateCommand(Action<T> executeAction) : this(executeAction, null)
    {
    }


    /// <summary>
    /// Defines the method that determines whether the command can execute in its current state.
    /// </summary>
    /// <returns>
    /// true if this command can be executed; otherwise, false.
    /// </returns>
    /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null. </param>
    public bool CanExecute(object parameter)
    {
      if (canExecuteAction != null)
      {
        return canExecuteAction((T) parameter);
      }
      return true;
    }

    /// <summary>
    /// Defines the method to be called when the command is invoked.
    /// </summary>
    /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null. </param>
    public void Execute(object parameter)
    {
      if (CanExecute(parameter))
      {
        executeAction((T) parameter);
      }
    }

    public event EventHandler CanExecuteChanged;
  }
}