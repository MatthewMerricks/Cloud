using System;
using System.Windows;
using System.Windows.Controls;

namespace Dialog.Abstractions.Wpf.Intefaces
{
  public interface IModalWindow
  {
    bool? DialogResult { get; set; }
    event EventHandler Closed;
    void Show(Grid gridContainer);
    object DataContext { get; set; }
    void Close();
  }
}