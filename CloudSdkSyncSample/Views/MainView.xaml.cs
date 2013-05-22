using SampleLiveSync.ViewModels;
using SampleLiveSync.Static;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SampleLiveSync.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        #region Constructors

        public MainView()
        {
            InitializeComponent();
            Loaded += OnMainView_Loaded;
            Unloaded += OnMainView_Unloaded;
            Closing += OnMainView_Closing;
        }

        #endregion

        #region Event Handlers

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
                vm.NotifyBrowseSyncboxFolder += OnNotifyBrowseSyncboxFolder;
                vm.NotifySettingsChanged += OnNotifySettingsChanged;
                vm.NotifyException += OnNotifyException;
            }
        }

        private void OnMainView_Unloaded(object sender, RoutedEventArgs e)
        {
            MainViewModel vm = (MainViewModel)DataContext;
            if (vm != null)
            {
                vm.NotifyBrowseSyncboxFolder -= OnNotifyBrowseSyncboxFolder;
                vm.NotifySettingsChanged -= OnNotifySettingsChanged;
                vm.NotifyException -= OnNotifyException;
            }
        }

        #endregion

        #region Notification Handlers

        private void OnNotifyBrowseSyncboxFolder(object sender, Support.NotificationEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowser.Description = "Choose a folder to synchronize with your other devices.";
            if (!String.IsNullOrWhiteSpace(tbSyncboxFolder.Text))
            {
                folderBrowser.SelectedPath = tbSyncboxFolder.Text;
            }
            folderBrowser.RootFolder = Environment.SpecialFolder.DesktopDirectory;
            folderBrowser.ShowNewFolderButton = true;
            System.Windows.Forms.DialogResult result = folderBrowser.ShowDialog(this.GetIWin32Window());
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // The user selected a folder.
                BindingExpression be = tbSyncboxFolder.GetBindingExpression(TextBox.TextProperty);
                tbSyncboxFolder.Text = folderBrowser.SelectedPath;
                be.UpdateSource();
            }
        }

        private void OnNotifySettingsChanged(object sender, Support.NotificationEventArgs<string, bool> e)
        {
            MessageBoxResult result = MessageBox.Show(System.Windows.Application.Current.MainWindow, "Some settings have changed.  Do you want to cancel the changes and exit anyway?", "Cancel and Exit Anyway?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            e.Completed(result == MessageBoxResult.Yes);
        }

        private void OnNotifyException(object sender, Support.NotificationEventArgs<Cloud.Model.CLError> e)
        {
            MessageBox.Show(System.Windows.Application.Current.MainWindow, String.Format("{0}.", e.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}
