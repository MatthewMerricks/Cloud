using SampleLiveSync.ViewModels;
using SampleLiveSync.Static;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SampleLiveSync.Views
{
    /// <summary>
    /// Interaction logic for GetNewCredentialView.xaml
    /// </summary>
    public partial class GetNewCredentialView : Window
    {
        #region Constructors

        public GetNewCredentialView()
        {
            InitializeComponent();
            Loaded += OnGetNewCredentialView_Loaded;
            Unloaded += OnGetNewCredentialView_Unloaded;
            Closing += OnGetNewCredentialView_Closing;
        }

        #endregion

        #region Event Handlers

        void OnGetNewCredentialView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GetNewCredentialViewModel vm = (GetNewCredentialViewModel)DataContext;
            if (vm != null)
            {
                bool shouldCancel = vm.OnWindowClosing();
                e.Cancel = shouldCancel;
            }
        }

        private void OnGetNewCredentialView_Loaded(object sender, RoutedEventArgs e)
        {
            GetNewCredentialViewModel vm = (GetNewCredentialViewModel)DataContext;
            if (vm != null)
            {
                vm.NotifyException += OnNotifyException;
                vm.NotifyDialogResult += vm_NotifyDialogResult;
            }
        }

        void vm_NotifyDialogResult(object sender, Support.NotificationEventArgs<Cloud.Model.GenericHolder<bool>> e)
        {
            this.DialogResult = e.Data.Value;
        }

        private void OnGetNewCredentialView_Unloaded(object sender, RoutedEventArgs e)
        {
            GetNewCredentialViewModel vm = (GetNewCredentialViewModel)DataContext;
            if (vm != null)
            {
                vm.NotifyException -= OnNotifyException;
                vm.NotifyDialogResult -= vm_NotifyDialogResult;
            }
        }

        #endregion

        #region Notification Handlers

        private void OnNotifyException(object sender, Support.NotificationEventArgs<Cloud.Model.CLError> e)
        {
            MessageBox.Show(String.Format("{0}.", e.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}
