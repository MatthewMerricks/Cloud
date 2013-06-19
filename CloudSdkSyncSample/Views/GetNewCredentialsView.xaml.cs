using SampleLiveSync.ViewModels;
using SampleLiveSync.Static;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SampleLiveSync.Views
{
    /// <summary>
    /// Interaction logic for GetNewCredentialsView.xaml
    /// </summary>
    public partial class GetNewCredentialsView : Window
    {
        #region Constructors

        public GetNewCredentialsView()
        {
            InitializeComponent();
            Loaded += OnGetNewCredentialsView_Loaded;
            Unloaded += OnGetNewCredentialsView_Unloaded;
            Closing += OnGetNewCredentialsView_Closing;
        }

        #endregion

        #region Event Handlers

        void OnGetNewCredentialsView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GetNewCredentialsViewModel vm = (GetNewCredentialsViewModel)DataContext;
            if (vm != null)
            {
                bool shouldCancel = vm.OnWindowClosing();
                e.Cancel = shouldCancel;
            }
        }

        private void OnGetNewCredentialsView_Loaded(object sender, RoutedEventArgs e)
        {
            GetNewCredentialsViewModel vm = (GetNewCredentialsViewModel)DataContext;
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

        private void OnGetNewCredentialsView_Unloaded(object sender, RoutedEventArgs e)
        {
            GetNewCredentialsViewModel vm = (GetNewCredentialsViewModel)DataContext;
            if (vm != null)
            {
                vm.NotifyException -= OnNotifyException;
                vm.NotifyDialogResult -= vm_NotifyDialogResult;
            }
        }

        #endregion

        #region Notification Handlers

        private void OnNotifyException(object sender, Support.NotificationEventArgs<Cloud.CLError> e)
        {
            MessageBox.Show(System.Windows.Application.Current.MainWindow, String.Format("{0}.", e.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}
