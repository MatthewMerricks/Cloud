//
//  WindowCloudFolderMissing.xaml.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Data;
using win_client.Common;
using win_client.ViewModels;
using Ookii.Dialogs.Wpf;
using win_client.AppDelegate;

namespace win_client.Views
{
    public partial class WindowCloudFolderMissing : Window
    {
        public WindowCloudFolderMissing()
        {
            CLAppMessages.Message_WindowCloudFolderMissingShoudClose.Register(this, OnMessage_WindowCloudFolderMissingShoudClose);
            CLAppMessages.Message_WindowCloudFolderMissingShouldChooseCloudFolder.Register(this, OnMessage_WindowCloudFolderMissingShouldChooseCloudFolder);
            
            Loaded += new RoutedEventHandler(WindowCloudFolderMissing_Loaded);
        }

        private void OnMessage_WindowCloudFolderMissingShouldChooseCloudFolder(string obj)
        {
            VistaFolderBrowserDialog folderBrowser = new VistaFolderBrowserDialog();
            folderBrowser.Description = CLAppDelegate.Instance.ResourceManager.GetString("windowCloudFolderMissingFolderBrowserDescription");
            folderBrowser.RootFolder = Environment.SpecialFolder.MyDocuments;  // no way to get to the user's home directory.  RootFolder is a SpecialFolder.
            folderBrowser.ShowNewFolderButton = true;
            bool? wasOkButtonClicked = folderBrowser.ShowDialog(this);
            if (wasOkButtonClicked.HasValue && wasOkButtonClicked.Value)
            {
                // The user selected a folder.  Deliver the path to the ViewModel to process.
                WindowCloudFolderMissingViewModel vm = (WindowCloudFolderMissingViewModel)DataContext;
                if (vm.WindowCloudFolderMissingViewModel_CreateCloudFolderCommand.CanExecute(folderBrowser.SelectedPath))
                {
                    vm.WindowCloudFolderMissingViewModel_CreateCloudFolderCommand.Execute(folderBrowser.SelectedPath);
                }
            }

        }

        private void OnMessage_WindowCloudFolderMissingShoudClose(string obj)
        {
            this.Close();
        }

        void WindowCloudFolderMissing_Loaded(object sender, RoutedEventArgs e)
        {
            WindowCloudFolderMissingViewModel vm = (WindowCloudFolderMissingViewModel)DataContext;
            vm.ViewGridContainer = LayoutRoot;
        }
    }
}
