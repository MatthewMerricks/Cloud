//
//  MainWindow.xaml.cs
//  Cloud Windows
//
//  Created by DavidBruck.
//  Copyright (c) Cloud.com. All rights reserved.
//

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Cloud.SQLProxies;
using SQLiteHelpers.ViewModels;
using System.Windows.Interop;

namespace SQLiteHelpers
{
    internal delegate void SetString(string toset);

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private readonly OpenFileDialog filePathDialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "db",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Multiselect = false,
                Title = "Select SQLite index.db file"
            };
        private void FilePathButton_Click(object sender, RoutedEventArgs e)
        {
            SetButtonsState(enabled: false);

            string oldFilePathBoxText = FilePathBox.Text;

            FilePathBox.Text = "Selecting new file path via dialog...";

            if (filePathDialog.ShowDialog(this) == true)
            {
                FilePathBox.Text = filePathDialog.FileName;
            }
            else
            {
                FilePathBox.Text = oldFilePathBoxText;
            }

            SetButtonsState(enabled: true);
        }

        private void SetButtonsState(bool enabled)
        {
            FilePathButton.IsEnabled = enabled;
            FilePathBox.IsEnabled = enabled;
            StatusButton.IsEnabled = enabled;
            EncryptButton.IsEnabled = enabled;
            DecryptButton.IsEnabled = enabled;
            MakeDBAndTestButton.IsEnabled = enabled;
            DeleteDBButton.IsEnabled = enabled;
            ScriptsFolderButton.IsEnabled = enabled;
            ScriptsFolderBox.IsEnabled = enabled;
        }

        private void ProcessCheckStatus(object filePathText)
        {
            string filePath = filePathText.ToString();

            string statusBlockTextToSet = MainViewModel.CheckStatus(filePath);

            if (statusBlockTextToSet != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new SetString(SetStatusBlockText), statusBlockTextToSet);
            }
        }
        private void SetStatusBlockText(string toSet)
        {
            StatusBlock.Text = toSet;

            SetButtonsState(enabled: true);
        }

        private void StatusButton_Click(object sender, RoutedEventArgs e)
        {
            SetButtonsState(enabled: false);

            StatusBlock.Text = "Processing status of database...";

            (new Thread(new ParameterizedThreadStart(ProcessCheckStatus))).Start(FilePathBox.Text);
        }

        private void EncryptButton_Click(object sender, RoutedEventArgs e)
        {
            SetButtonsState(enabled: false);

            StatusBlock.Text = "Encrypting database...";

            (new Thread(new ParameterizedThreadStart(ProcessChangeDBEncryption))).Start(new KeyValuePair<bool, string>(true, FilePathBox.Text));
        }

        private void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            SetButtonsState(enabled: false);

            StatusBlock.Text = "Decrypting database...";

            (new Thread(new ParameterizedThreadStart(ProcessChangeDBEncryption))).Start(new KeyValuePair<bool, string>(false, FilePathBox.Text));
        }

        private void ProcessChangeDBEncryption(object whetherToEncryptAndFilePathText)
        {
            KeyValuePair<bool, string> encryptAndFilePath = (KeyValuePair<bool, string>)whetherToEncryptAndFilePathText;

            string statusBlockTextToSet = MainViewModel.ChangeDBEncryption(encrypt: encryptAndFilePath.Key, filePath: encryptAndFilePath.Value);

            if (statusBlockTextToSet != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new SetString(SetStatusBlockText), statusBlockTextToSet);
            }
        }

        private void MakeDBAndTestButton_Click(object sender, RoutedEventArgs e)
        {
            SetButtonsState(enabled: false);

            StatusBlock.Text = "Making database and testing...";

            (new Thread(new ParameterizedThreadStart(ProcessMakeDBAndTest))).Start(new KeyValuePair<string, string>(FilePathBox.Text, ScriptsFolderBox.Text));
        }

        private void ProcessMakeDBAndTest(object makeDBState)
        {
            KeyValuePair<string, string> dbFileLocationAndScriptsFolder = (KeyValuePair<string, string>)makeDBState;

            string statusBlockTextToSet = MainViewModel.MakeDBAndTest(dbFileLocationAndScriptsFolder.Key, dbFileLocationAndScriptsFolder.Value);

            if (statusBlockTextToSet != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new SetString(SetStatusBlockText), statusBlockTextToSet);
            }
        }

        private void DeleteDBButton_Click(object sender, RoutedEventArgs e)
        {
            SetButtonsState(enabled: false);

            StatusBlock.Text = "Deleting database file...";

            (new Thread(new ParameterizedThreadStart(ProcessDeleteDB))).Start(FilePathBox.Text);
        }

        private void ProcessDeleteDB(object deleteDBState)
        {
            string statusBlockTextToSet = MainViewModel.DeleteDB(deleteDBState.ToString());

            if (statusBlockTextToSet != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new SetString(SetStatusBlockText), statusBlockTextToSet);
            }
        }

        private readonly System.Windows.Forms.FolderBrowserDialog folderPathDialog = new System.Windows.Forms.FolderBrowserDialog()
        {
            Description = "Select the folder with scripts to generate the database",
            RootFolder = Environment.SpecialFolder.DesktopDirectory,
            ShowNewFolderButton = false
        };
        private void ScriptsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            SetButtonsState(enabled: false);

            string oldFolderBoxText = ScriptsFolderBox.Text;

            ScriptsFolderBox.Text = "Selecting new file path via dialog...";

            if (folderPathDialog.ShowDialog(GetIWin32Window(this)) == System.Windows.Forms.DialogResult.OK)
            {
                ScriptsFolderBox.Text = folderPathDialog.SelectedPath;
            }
            else
            {
                ScriptsFolderBox.Text = oldFolderBoxText;
            }

            SetButtonsState(enabled: true);
        }
        private static System.Windows.Forms.IWin32Window GetIWin32Window(Visual visual)
        {
            HwndSource source = PresentationSource.FromVisual(visual) as HwndSource;
            return new OldWindow(source.Handle);
        }
        private class OldWindow : System.Windows.Forms.IWin32Window
        {
            private readonly IntPtr _handle;
            public OldWindow(IntPtr handle)
            {
                _handle = handle;
            }

            #region IWin32Window Members
            IntPtr System.Windows.Forms.IWin32Window.Handle
            {
                get { return _handle; }
            }
            #endregion
        }
    }
}
