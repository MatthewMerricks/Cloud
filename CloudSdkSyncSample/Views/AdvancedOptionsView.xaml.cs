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
using CloudSdkSyncSample.ViewModels;
using CloudSdkSyncSample.Static;
using System.ComponentModel;

namespace CloudSdkSyncSample.Views
{
    /// <summary>
    /// Interaction logic for AdvancedOptions.xaml
    /// </summary>
    public partial class AdvancedOptionsView : Window
    {
        public AdvancedOptionsView()
        {
            InitializeComponent();
            Loaded += AdvancedOptionsView_Loaded;
            Unloaded += AdvancedOptionsView_Unloaded;
            Closing += OnAdvancedOptionsView_Closing;
        }

        void OnAdvancedOptionsView_Closing(object sender, CancelEventArgs e)
        {
            AdvancedOptionsViewModel vm = (AdvancedOptionsViewModel)DataContext;
            if (vm != null)
            {
                bool shouldCancel = vm.OnWindowClosing();
                e.Cancel = shouldCancel;
            }
        }

        private void AdvancedOptionsView_Loaded(object sender, RoutedEventArgs e)
        {
            AdvancedOptionsViewModel vm = (AdvancedOptionsViewModel)DataContext;
            if (vm != null)
            {
                vm.NotifyBrowseTempDownloadFolder += OnNotifyBrowseTempDownloadFolder;
                vm.NotifyBrowseDatabaseFolder += OnNotifyBrowseDatabaseFolder;
                vm.NotifyBrowseTraceFolder += OnNotifyBrowseTraceFolder;
                vm.NotifyAdvancedSettingsChanged += OnNotifyAdvancedSettingsChanged;
            }
        }

        private void AdvancedOptionsView_Unloaded(object sender, RoutedEventArgs e)
        {
            AdvancedOptionsViewModel vm = (AdvancedOptionsViewModel)DataContext;
            if (vm != null)
            {
                vm.NotifyBrowseTempDownloadFolder += OnNotifyBrowseTempDownloadFolder;
                vm.NotifyBrowseDatabaseFolder += OnNotifyBrowseDatabaseFolder;
                vm.NotifyBrowseTraceFolder += OnNotifyBrowseTraceFolder;
            }
        }

        private void OnNotifyBrowseTempDownloadFolder(object sender, Support.NotificationEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowser.Description = "Choose a folder for your temporary downloaded files.";
            if (!String.IsNullOrWhiteSpace(tbTempDownloadFolder.Text))
            {
                folderBrowser.SelectedPath = tbTempDownloadFolder.Text;
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
                BindingExpression be = tbTempDownloadFolder.GetBindingExpression(TextBox.TextProperty);
                tbTempDownloadFolder.Text = folderBrowser.SelectedPath;
                be.UpdateSource();
            }
        }

        private void OnNotifyBrowseDatabaseFolder(object sender, Support.NotificationEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowser.Description = "Choose a folder for the database file IndexDB.sdf.";
            if (!String.IsNullOrWhiteSpace(tbDatabaseFolderFullPath.Text))
            {
                folderBrowser.SelectedPath = tbDatabaseFolderFullPath.Text;
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
                BindingExpression be = tbDatabaseFolderFullPath.GetBindingExpression(TextBox.TextProperty);
                tbDatabaseFolderFullPath.Text = folderBrowser.SelectedPath;
                be.UpdateSource();
            }
        }

        private void OnNotifyBrowseTraceFolder(object sender, Support.NotificationEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowser.Description = "Choose a folder for trace files.";
            if (!String.IsNullOrWhiteSpace(tbTraceFolder.Text))
            {
                folderBrowser.SelectedPath = tbTraceFolder.Text;
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
                BindingExpression be = tbTraceFolder.GetBindingExpression(TextBox.TextProperty);
                tbTraceFolder.Text = folderBrowser.SelectedPath;
                be.UpdateSource();
            }
        }

        private void OnNotifyAdvancedSettingsChanged(object sender, Support.NotificationEventArgs<string, bool> e)
        {
            MessageBoxResult result = MessageBox.Show("Some settings have changed.  Do you want to cancel the changes anyway?", "Cancel Anyway?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            e.Completed(result == MessageBoxResult.Yes);
        }
    }
}
