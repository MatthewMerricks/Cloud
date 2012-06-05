using System;
using System.Windows.Controls;

namespace Dialog.Abstractions.Wpf.Intefaces
{
  public interface IModalDialogService
  {
    void ShowDialog<TViewModel>(IModalWindow view, TViewModel viewModel, Grid gridContainer, Action<TViewModel> onDialogClose);

    void ShowDialog<TDialogViewModel>(IModalWindow view, TDialogViewModel viewModel, Grid gridContainer);
  }
}