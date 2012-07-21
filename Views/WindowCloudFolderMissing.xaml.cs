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

namespace win_client.Views
{
    public partial class WindowCloudFolderMissing : Window
    {
        public WindowCloudFolderMissing()
        {
            CLAppMessages.Message_WindowCloudFolderMissingShoudClose.Register(this, OnMessage_WindowCloudFolderMissingShoudClose);
            Loaded += new RoutedEventHandler(WindowCloudFolderMissing_Loaded);
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
