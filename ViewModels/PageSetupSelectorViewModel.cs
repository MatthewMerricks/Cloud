using GalaSoft.MvvmLight;
using win_client.Model;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using MVVMProductsDemo.ViewModels;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;
using win_client.Common;
using System.Reflection;
using System.Linq;
using win_client.DataModels.Settings;

namespace win_client.ViewModels
{
         
    /// <summary>
    /// Page to select the Cloud storage size desired by the user.
    /// </summary>
    public class PageSetupSelectorViewModel : ValidatingViewModelBase
    {
        #region Instance Variables
        private readonly IDataService _dataService;

        private RelayCommand _pageSetupSelector_5GbCommand;
        private RelayCommand _pageSetupSelector_50GbCommand;
        private RelayCommand _pageSetupSelector_500GbCommand;
        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
        /// </summary>
        public PageSetupSelectorViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _dataService.GetData(
                (item, error) =>
                {
                    if (error != null)
                    {
                        // Report error here
                        return;
                    }

                    //&&&&               WelcomeTitle = item.Title;
                });
        }
        #endregion
      
        #region Commands
         
        /// <summary>
        /// The user clicked the free 5 GB button on the PageSetupSelector page.
        /// </summary>
        public RelayCommand PageSetupSelector_5GbCommand
        {
            get
            {
                return _pageSetupSelector_5GbCommand
                    ?? (_pageSetupSelector_5GbCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Store current storage selection
                                                Settings.Instance.setCloudQuota(5);
        
                                                // begin setup
                                                Uri nextPage = new System.Uri("/PageSetupSelector", System.UriKind.Relative);
                                                SendNavigationRequestMessage(nextPage);
                                            }));
            }
        }
        
        /// <summary>
        /// The user clicked the 50 GB button on the PageSetupSelector page.
        /// </summary>
        public RelayCommand PageSetupSelector_50GbCommand
        {
            get
            {
                return _pageSetupSelector_50GbCommand
                    ?? (_pageSetupSelector_50GbCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Not implemented, put up a dialog.
                                                ToDoNeedCreditCardInfo();
                                            }));
            }
        }

        /// <summary>
        /// The user clicked the 500 GB button on the PageSetupSelector page.
        /// </summary>
        public RelayCommand PageSetupSelector_500GbCommand
        {
            get
            {
                return _pageSetupSelector_50GbCommand
                    ?? (_pageSetupSelector_500GbCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Not implemented, put up a dialog.
                                                ToDoNeedCreditCardInfo();
                                            }));
            }
        }

        #endregion

        #region "Callbacks"

        /// <summary>
        /// Callback from the View's dialog box.
        /// </summary>
        private void DialogMessageCallback(MessageBoxResult result)
        {
        }

        #endregion


        #region Supporting Functions

        /// <summary>
        /// Put up a dialog.  We need credit card info here.  TODO:
        /// </summary>
        private void ToDoNeedCreditCardInfo() 
        {
            var message = new DialogMessage("This storage selection is not available for beta users.", DialogMessageCallback)
            {
                Button = MessageBoxButton.OK,
                Caption = "Not Available"
            };

            Messenger.Default.Send(message);
        }


        /// <summary>
        /// Send a navigation request.
        /// </summary>
        protected void SendNavigationRequestMessage(Uri uri) 
        {
            Messenger.Default.Send<Uri>(uri, "PageSetupSelector_NavigationRequest");
        }

        #endregion
    }
}