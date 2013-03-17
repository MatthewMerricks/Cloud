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
            string oldFilePathBoxText = filePathDialog.FileName;

            FilePathBox.Text = "Selecting new file path via dialog...";

            if (filePathDialog.ShowDialog(this) == true)
            {
                FilePathBox.Text = filePathDialog.FileName;
            }
            else
            {
                FilePathBox.Text = oldFilePathBoxText;
            }
        }

        private void SetButtonsState(bool enabled)
        {
            FilePathButton.IsEnabled = enabled;
            StatusButton.IsEnabled = enabled;
            EncryptButton.IsEnabled = enabled;
            DecryptButton.IsEnabled = enabled;
        }

        private void ProcessCheckStatus(object filePathText)
        {
            string filePath = filePathText.ToString();

            string statusBlockTextToSet = SQLiteProxy.Processors.MainViewModel.CheckStatus(filePath);

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

            string statusBlockTextToSet = SQLiteProxy.Processors.MainViewModel.ChangeDBEncryption(encrypt: encryptAndFilePath.Key, filePath: encryptAndFilePath.Value);

            if (statusBlockTextToSet != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new SetString(SetStatusBlockText), statusBlockTextToSet);
            }
        }
    }
}
