using GalaSoft.MvvmLight;
using win_client.Model;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;

namespace win_client.ViewModels
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm/getstarted
    /// </para>
    /// </summary>
    public class PageCreateNewAccountViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        private RelayCommand _pageHome_CreateNewAccountCommand;
        private RelayCommand _pageCreateNewAccount_BackCommand;

        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
        /// </summary>
        public PageCreateNewAccountViewModel(IDataService dataService)
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
        /// Back command from the PageCreateNewAccount page.
        /// </summary>
        public RelayCommand PageCreateNewAccount_BackCommand
        {
            get
            {
                return _pageCreateNewAccount_BackCommand
                    ?? (_pageCreateNewAccount_BackCommand = new RelayCommand(
                                            () =>
                                            {
                                                Uri nextPage = new System.Uri("/PageHome", System.UriKind.Relative);
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