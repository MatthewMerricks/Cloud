using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using win_client.Model;
using System;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Controls;

namespace win_client.ViewModels
{  

    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm/getstarted
    /// </para>
    /// </summary>
    public class PageHomeViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        private RelayCommand _pageHome_CreateNewAccountCommand;
        private RelayCommand _pageCreateNewAccount_BackCommand;

        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
        /// </summary>
        public PageHomeViewModel(IDataService dataService)
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
        
        /// <summary>
        /// Create new account from the PageHome page.
        /// </summary>
        public RelayCommand PageHome_CreateNewAccountCommand
        {
            get
            {
                return _pageHome_CreateNewAccountCommand 
                    ?? (_pageHome_CreateNewAccountCommand = new RelayCommand(
                                          () =>
                                          {
                                              Uri nextPage = new System.Uri("/PageCreateNewAccount", System.UriKind.Relative);
                                              SendNavigationRequestMessage(nextPage);
                                          }));
            }
        }

       /// <summary>
        /// Send a navigation request.
        /// </summary>
        protected void SendNavigationRequestMessage(Uri uri) 
        {
            Messenger.Default.Send<Uri>(uri, "NavigationRequest");
        }

    }
}