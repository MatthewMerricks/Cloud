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
    public partial class PageCreateNewAccount : Page
    {
        public PageCreateNewAccount()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(Page_Loaded);

            Messenger.Default.Register<Uri>(this, "PageCreateNewAccount_NavigationRequest",
                (uri) => ((Frame)(Application.Current.RootVisual as MainPage).FindName("ContentFrame")).Navigate(uri));
            AppMessages.CreateNewAccount_FocusToError.Register(this, OnCreateNewAccount_FocusToError_Message);

        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        #region "Page Loaded"
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            tbEMail.Focus();
        }
        #endregion

        #region "Message Handlers"

        private void OnCreateNewAccount_FocusToError_Message(string notUsed)
        {
            if (Validation.GetHasError(tbEMail) == true )  {
                tbEMail.Focus();
                return;
            }
            if (Validation.GetHasError(tbFullName) == true )  {
                tbFullName.Focus();
                return;
            }
            if(Validation.GetHasError(tbPassword) == true)
            {
                tbPassword.Focus();
                return;
            }
            if(Validation.GetHasError(tbConfirmPassword) == true)
            {
                tbConfirmPassword.Focus();
                return;
            }
            if(Validation.GetHasError(tbComputerName) == true)
            {
                tbComputerName.Focus();
                return;
            }
        }

        #endregion "ChangeScreenMessage"


    }
}
