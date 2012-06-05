using Dialog.Abstractions.Wpf.GenericMessageBox;

namespace Dialog.Abstractions.Wpf.Intefaces
{
  public interface IMessageBoxService
  {
    GenericMessageBoxResult Show(string message, string caption, GenericMessageBoxButton buttons);
    void Show(string message, string caption);
  }
}