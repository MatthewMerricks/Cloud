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

#if _SILVERLIGHT
            Messenger.Default.Register<Uri>(this, "PageSelectStorageSize_NavigationRequest",
                (uri) => ((Frame)(Application.Current.RootVisual as MainPage).FindName("ContentFrame")).Navigate(uri));
#else
            Messenger.Default.Register<Uri>(this, "PageSelectStorageSize_NavigationRequest",
                (uri) => { this.NavigationService.Navigate(uri); });
#endif
            CLAppMessages.SelectStorageSize_PresentMessageDialog.Register(this, SelectStorageSize_PresentMessageDialog);
            
        }

        #region "Message Handlers"

        void PageSelectStorageSize_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
#if !_SILVERLIGHT
            NavigationService.Navigated += new NavigatedEventHandler(OnNavigatedTo);
#endif
            cmdContinue.Focus();
        }

        void PageSelectStorageSize_Unloaded(object sender, RoutedEventArgs e)
        {
#if !_SILVERLIGHT
            NavigationService.Navigated -= new NavigatedEventHandler(OnNavigatedTo);
#endif
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

#if _SILVERLIGHT
        protected override void OnNavigatedTo(NavigationEventArgs e)
#else
        protected void OnNavigatedTo(object sender, NavigationEventArgs e)
#endif
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
