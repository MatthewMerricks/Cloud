//
//  PageSelectStorageSize.xaml.cs
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
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Data;
using win_client.Common;
using System.Globalization;
using win_client.ViewModels;

namespace win_client.Views
{
    public partial class PageSelectStorageSize : Page
    {
        #region "Instance Variables"

        private bool _isLoaded = false;

        #endregion

        public PageSelectStorageSize()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(PageSelectStorageSize_Loaded);
            Unloaded += new RoutedEventHandler(PageSelectStorageSize_Unloaded);

            Messenger.Default.Register<Uri>(this, "PageSelectStorageSize_NavigationRequest",
                (uri) => ((Frame)(Application.Current.RootVisual as MainPage).FindName("ContentFrame")).Navigate(uri));
            AppMessages.SelectStorageSize_PresentMessageDialog.Register(this, SelectStorageSize_PresentMessageDialog);

        }

        #region "Message Handlers"

        void PageSelectStorageSize_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            cmdContinue.Focus();
        }

        void PageSelectStorageSize_Unloaded(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Unregister(this);
        }

        private void SelectStorageSize_PresentMessageDialog(DialogMessage msg)
        {
            var result = MessageBox.Show(
                msg.Content,
                msg.Caption,
                msg.Button);

            msg.ProcessCallback(result);     // Send callback
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_isLoaded)
            {
                cmdContinue.Focus();
            }

            var vm = DataContext as PageSelectStorageSizeViewModel;
            vm.PageSelectStorageSize_NavigatedToCommand.Execute(null);
        }
 
        #endregion


    }

}
