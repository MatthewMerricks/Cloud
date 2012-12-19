using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CloudApiPublicSamples.ViewModels;
using CloudApiPublicSamples.Static;

namespace CloudApiPublicSamples.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            Loaded += OnMainView_Loaded;
            Unloaded += OnMainView_Unloaded;
            Closing += OnMainView_Closing;
        }

        void OnMainView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MainViewModel vm = (MainViewModel)DataContext;
            if (vm != null)
            {
                bool shouldCancel = vm.OnWindowClosing();
                e.Cancel = shouldCancel;
            }
        }

        private void OnMainView_Loaded(object sender, RoutedEventArgs e)
        {
            MainViewModel vm = (MainViewModel)DataContext;
            if (vm != null)
            {
                vm.NotifyBrowseSyncBoxFolder += OnNotifyBrowseSyncBoxFolder;
                vm.NotifySettingsChanged += OnNotifySettingsChanged;
                vm.NotifyException += OnNotifyException;
            }
        }

        private void OnNotifyException(object sender, Support.NotificationEventArgs<CloudApiPublic.Model.CLError> e)
        {
            MessageBox.Show(String.Format("Error starting the synchronization process. Message: {0}.", e.Message), "Sync Start Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OnMainView_Unloaded(object sender, RoutedEventArgs e)
        {
            MainViewModel vm = (MainViewModel)DataContext;
            if (vm != null)
            {
                vm.NotifyBrowseSyncBoxFolder -= OnNotifyBrowseSyncBoxFolder;
                vm.NotifySettingsChanged -= OnNotifySettingsChanged;
                vm.NotifyException -= OnNotifyException;
            }
        }

        private void OnNotifyBrowseSyncBoxFolder(object sender, Support.NotificationEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowser.Description = "Choose a folder to synchronize with your other devices.";
            if (!String.IsNullOrWhiteSpace(tbSyncBoxFolder.Text))
            {
                folderBrowser.SelectedPath = tbSyncBoxFolder.Text;
            }
            else
            {
                folderBrowser.RootFolder = Environment.SpecialFolder.UserProfile;
            }
            folderBrowser.ShowNewFolderButton = true;
            System.Windows.Forms.DialogResult result = folderBrowser.ShowDialog(this.GetIWin32Window());
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // The user selected a folder.
                BindingExpression be = tbSyncBoxFolder.GetBindingExpression(TextBox.TextProperty);
                tbSyncBoxFolder.Text = folderBrowser.SelectedPath;
                be.UpdateSource();
            }
        }

        private void OnNotifySettingsChanged(object sender, Support.NotificationEventArgs<string, bool> e)
        {
            MessageBoxResult result = MessageBox.Show("Some settings have changed.  Do you want to cancel the changes anyway?", "Cancel Anyway?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            e.Completed(result == MessageBoxResult.Yes);
        }
    }
}
