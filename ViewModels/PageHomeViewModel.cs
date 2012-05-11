//
//  PageHomeViewModel.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using win_client.Model;
using System;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Controls;
using MVVMProductsDemo.ViewModels;
using win_client.Common;
using win_client.DataModels.Settings;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;

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
    public class PageHomeViewModel : ValidatingViewModelBase
    {
        private readonly IDataService _dataService;

        private RelayCommand _pageHome_CreateNewAccountCommand;
        private RelayCommand _pageHome_SignInCommand;
        private RelayCommand _pageHome_NavigatedToCommand;        


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
        /// The <see cref="EMail" /> property's name.
        /// </summary>
        public const string EMailPropertyName = "EMail";

        private string _eMail = Settings.Instance.UserName;

        /// <summary>
        /// Sets and gets the EMail property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string EMail
        {
            get
            {
                return _eMail;
            }

            set
            {
                ValidateEMail(value);
                if(_eMail == value)
                {
                    return;
                }

                _eMail = value;
                Settings.Instance.UserName = _eMail;
                RaisePropertyChanged(EMailPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="Password" /> property's name.
        /// </summary>
        public const string PasswordPropertyName = "Password";

        private string _password = "";

        /// <summary>
        /// Sets and gets the Password property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Password
        {
            get
            {
                return _password;
            }

            set
            {
                ValidatePassword(value);
                if(_password == value)
                {
                    return;
                }

                _password = value;
                RaisePropertyChanged(PasswordPropertyName);
            }
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
        /// Sign in to an existing account from the PageHome page.
        /// </summary>
        public RelayCommand PageHome_SignInCommand
        {
            get
            {
                return _pageHome_SignInCommand
                    ?? (_pageHome_SignInCommand = new RelayCommand(
                                          () =>
                                          {
                                              CLExtensionMethods.ForceValidation(((MainPage)App.Current.RootVisual).LayoutRoot);
                                              if(!HasErrors)
                                              {
                                                  Uri nextPage = new System.Uri("/PageHome", System.UriKind.Relative);   //&&&& TODO: Begin the sign-in process.
                                                  SendNavigationRequestMessage(nextPage);
                                              }
                                              else
                                              {
                                                  CLAppMessages.Home_FocusToError.Send("");
                                              }
                                          }));
            }
        }

        /// <summary>
        /// The page was navigated to.
        /// </summary>
        public RelayCommand PageHome_NavigatedToCommand
        {
            get
            {
                return _pageHome_NavigatedToCommand
                    ?? (_pageHome_NavigatedToCommand = new RelayCommand(
                                            () =>
                                            {
                                                EMail = Settings.Instance.UserName;
                                            }));
            }
        }
        #region Validation
        /// <summary>
        /// Validate the EMail property.
        /// </summary>
        private void ValidateEMail(string eMail)
        {
            RemoveAllErrorsForPropertyName("EMail");
            if(!CLRegexValidation.IsEMail(eMail))
            {
                AddError("EMail", "Please enter your EMail address in the format 'yourname@yourmaildomain.com'.");
            }
        }

        /// <summary>
        /// Validate the Password property.
        /// </summary>
        private void ValidatePassword(string password)
        {
            RemoveAllErrorsForPropertyName("Password");
            if(!CLRegexValidation.IsXOK(password))
            {
                AddError("Password", "Please enter a password with at least 8 characters, including a least one number, one lower case character and one upper case character.");
            }
        }

        #endregion

        /// <summary>
        /// Send a navigation request.
        /// </summary>
        protected void SendNavigationRequestMessage(Uri uri) 
        {
            Messenger.Default.Send<Uri>(uri, "PageHome_NavigationRequest");
        }

    }
}