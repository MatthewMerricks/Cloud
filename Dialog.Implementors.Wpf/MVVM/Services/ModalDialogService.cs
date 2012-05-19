using System;
using Dialog.Abstractions.Wpf.Intefaces;
using System.Windows.Controls;
using System.Diagnostics;

namespace Dialog.Implementors.Wpf.MVVM.Services
{
  public class ModalDialogService : IModalDialogService
  {
    public void ShowDialog<TDialogViewModel>(IModalWindow view, TDialogViewModel viewModel, Grid gridContainer, Action<TDialogViewModel> onDialogClose) 
    {
        view.DataContext = viewModel;
        if (onDialogClose != null)
        {
            EventHandler eh = null;
            eh = (sender, args) =>
            {
                Debug.WriteLine("ShowDialog: 'Closed' event handler. Call the ViewModel event handler.");
                onDialogClose(viewModel);
                Debug.WriteLine("ShowDialog: 'Closed' event handler. Return from the ViewModel event handler.");
                ((IModalWindow)sender).Closed -= eh;
            };
            view.Closed += eh;
        }
        view.Show(gridContainer);            
    }

    public void ShowDialog<TDialogViewModel>(IModalWindow view, TDialogViewModel viewModel, Grid gridContainer)
    {
      this.ShowDialog(view, viewModel, gridContainer);
    }
  }
}