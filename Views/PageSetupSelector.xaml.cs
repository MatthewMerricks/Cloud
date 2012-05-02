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

namespace win_client.Views
{
    public partial class PageSetupSelector : Page
    {
        public PageSetupSelector()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(Page_Loaded);

            Messenger.Default.Register<Uri>(this, "PageSetupSelector_NavigationRequest",
                (uri) => ((Frame)(Application.Current.RootVisual as MainPage).FindName("ContentFrame")).Navigate(uri));

            Messenger.Default.Register<DialogMessage>(
                this,
                msg =>
                {
                    var result = MessageBox.Show(
                        msg.Content,
                        msg.Caption,
                        msg.Button);

                    // Send callback
                    msg.ProcessCallback(result);
                });
        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        #region "Page Loaded"
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            cmdContinue.Focus();
        }
        #endregion

    }
}
