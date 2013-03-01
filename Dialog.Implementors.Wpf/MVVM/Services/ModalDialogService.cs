using System;
using Dialog.Abstractions.Wpf.Intefaces;
using System.Windows.Controls;
using System.Diagnostics;
using Xceed.Wpf.Toolkit;
using System.Windows;
using System.Windows.Threading;
using CloudApiPrivate.Static;
using Cloud.Static;

namespace Dialog.Implementors.Wpf.MVVM.Services
{
    public class ModalDialogService : IModalDialogService
    {
        public void ShowDialog<TDialogViewModel>(IModalWindow view, TDialogViewModel viewModel, Grid gridContainer, Action<TDialogViewModel> onDialogClose)
        {
            // Set the ViewModel as the DataContext of the view.
            view.DataContext = viewModel;

            // This code was removed from the original ChildWindow code.  This has implications to Silverlight compatibility.
            //if (onDialogClose != null)
            //{
            //    EventHandler eh = null;
            //    eh = (sender, args) =>
            //    {
            //        Debug.WriteLine("ShowDialog: 'Closed' event handler. Call the ViewModel event handler.");
            //        onDialogClose(viewModel);
            //        Debug.WriteLine("ShowDialog: 'Closed' event handler. Return from the ViewModel event handler.");
            //        ((IModalWindow)sender).Closed -= eh;
            //    };
            //    view.Closed += eh;
            //}

            // Changes below to use a real modal dialog instead of the simulated ChildWindow "modal" dialog that wasn't really
            // modal at all.  This has implications to Silverlight compatibility.
            Window viewWindow = (Window)view;
            viewWindow.Owner = Window.GetWindow(gridContainer);
            viewWindow.ShowInTaskbar = false;
            viewWindow.Topmost = true;
            viewWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            viewWindow.ResizeMode = ResizeMode.NoResize;

            // Show the dialog on the main thread and drive the action when done.
            Dispatcher dispatcher =  Application.Current.Dispatcher;
            dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () =>
            {
                bool? dialogResult = ((Window)view).ShowDialog();
                if (onDialogClose != null)
                {
                    onDialogClose(viewModel);
                }
            });

            // Original ChildWindow code:
            // view.Show(gridContainer);
        }

        public void ShowDialog<TDialogViewModel>(IModalWindow view, TDialogViewModel viewModel, Grid gridContainer)
        {
            this.ShowDialog(view, viewModel, gridContainer);
        }
    }
}