﻿//
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
using CloudApiPrivate.Model;
using CloudApiPrivate.Model.Settings;
using CloudApiPrivate.Static;
using CloudApiPublic.Model;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Collections.Generic;
using Dialog.Abstractions.Wpf.Intefaces;
using CloudApiPublic.Support;
using System.Resources;
using win_client.AppDelegate;

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
        private CLTrace _trace = CLTrace.Instance;
        private ResourceManager _rm;

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

            _rm = CLAppDelegate.Instance.ResourceManager;
            _trace = CLTrace.Instance;
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
        /// The <see cref="Password2" /> clear password.
        /// </summary>
        public const string Password2PropertyName = "Password2";
        private string _password2 = "";
        /// <summary>
        /// Sets the Password2 property.
        /// This is the clear password. 
        /// </summary>
        public string Password2
        {
            get
            {
                return "";
            }

            set
            {
                _password2 = value;
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
                // The password is scrambled at this point because we don't want it in the visual tree for
                // Snoop and other tools to see.  We need to get the password in the clear.  However, only the view knows how
                // to get the clear password.  Send the view a message to cause it to set the clear
                // password.  Upon receiving the message, the view will retrieve the password and invoke a
                // public write-only property on this ViewModel object.  The whole process is synchronous, so
                // we will have the clear password when the Send completes.
                CLAppMessages.Home_GetClearPasswordField.Send("");
                ValidatePassword(_password2);

                if (_password == value)
                {
                    return;
                }

                _password = value;
                RaisePropertyChanged(PasswordPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ViewGridContainer" /> property's name.
        /// </summary>
        public const string ViewGridContainerPropertyName = "ViewGridContainer";
        private Grid _viewGridContainer = null;

        /// <summary>
        /// Sets and gets the ViewGridContainer property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Grid ViewGridContainer
        {
            get
            {
                return _viewGridContainer;
            }

            set
            {
                if (_viewGridContainer == value)
                {
                    return;
                }

                _viewGridContainer = value;
                RaisePropertyChanged(ViewGridContainerPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="IsBusy" /> property's name.
        /// </summary>
        public const string IsBusyPropertyName = "IsBusy";

        private bool _isBusy = false;

        /// <summary>
        /// Sets and gets the IsBusy property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsBusy
        {
            get
            {
                return _isBusy;
            }

            set
            {
                if (_isBusy == value)
                {
                    return;
                }

                _isBusy = value;
                RaisePropertyChanged(IsBusyPropertyName);
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
                                              Uri nextPage = new System.Uri(CLConstants.kPageCreateNewAccount, System.UriKind.Relative);
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
#if SILVERLIGHT
                                              CLExtensionMethods.ForceValidation(((MainPage)App.Current.RootVisual).LayoutRoot);
#else
                                              var layoutRoot = LogicalTreeHelper.FindLogicalNode(Application.Current.MainWindow, "LayoutRoot") as UIElement; 
                                              CLExtensionMethods.ForceValidation(layoutRoot);
#endif
                                              if(!HasErrors)
                                              {
                                                  RequestLogin();
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

        /// <summary>
        /// Request login to the server with the email and password..
        /// The completion callback will occur on the main thread.
        /// </summary>
        private void RequestLogin()
        {
            IsBusy = true;                      // show the busy indicator

            // Create cloud device obj
            CLDevice clDevice = new CLDevice(System.Environment.MachineName);

            CLRegistration registration = new CLRegistration();
            registration.LinkNewDeviceWithLoginAsync(_eMail, _password2, clDevice, LoginCompleteCallback, 30.0, registration);
        }

        /// <summary>
        /// The login request to the server has completed.
        /// This callback will occur on the main thread.
        /// </summary>
        private void LoginCompleteCallback(CLRegistration clRegistration, bool isSuccess, CLError error)
        {
            IsBusy = false;                      // take the busy indicator down
            if (isSuccess)
            {

                // Save the information we have received.
                string akey = clRegistration.Token;
                string uuid = clRegistration.Uuid;
                string devn = clRegistration.LinkedDeviceName;
                CLAccount acct = clRegistration.LinkedAccount;
                SaveAccountInformationWithAccount(acct, devn, uuid, akey);

                // Navigate to the SelectStorage page.
                Uri nextPage = new System.Uri(CLConstants.kPageSelectStorageSize, System.UriKind.Relative);
                SendNavigationRequestMessage(nextPage);
            }
            else
            {
                // There was an error registering this user.  Display the error and leave the user on the same page.
                DisplayErrorMessage(error);
            }
        }

        private void SaveAccountInformationWithAccount(CLAccount acct, string deviceName, string uuid, string key)
        {
            // Merged 7/11/12
            // NSString *userName = [[account fullName] stringByAppendingFormat:@" (%@)", [account userName]];
            // NSMutableDictionary *accountDict = [NSMutableDictionary dictionary];
            // [accountDict setValue:userName forKey:@"user_name"];
            // [accountDict setValue:deviceName forKey:@"device_name"];
            // [accountDict setValue:key forKey:@"akey"];
            // [accountDict setValue:[NSNumber numberWithBool:YES] forKey:@"r_udid"];
            // [accountDict setValue:uuid forKey:@"uuid"];
            // [[CLSettings sharedSettings] saveAccountSettings:accountDict];
            //&&&&

            // NSString *userName = [[account fullName] stringByAppendingFormat:@" (%@)", [account userName]];
            string userName = acct.FullName + String.Format(" ({0})", acct.UserName);

            // NSMutableDictionary *accountDict = [NSMutableDictionary dictionary];
            Dictionary<string, object> accountDict = new Dictionary<string,object>();

            // [accountDict setValue:userName forKey:@"user_name"];
            // [accountDict setValue:deviceName forKey:@"device_name"];
            // [accountDict setValue:key forKey:@"akey"];
            // [accountDict setValue:[NSNumber numberWithBool:YES] forKey:@"r_udid"];
            // [accountDict setValue:uuid forKey:@"uuid"];
            accountDict.Add(Settings.kUserName, userName);
            accountDict.Add(Settings.kDeviceName, deviceName);
            accountDict.Add(Settings.kAKey, key);
            accountDict.Add(Settings.kUdidRegistered, "1");
            accountDict.Add(Settings.kUuid, uuid);

            // [[CLSettings sharedSettings] saveAccountSettings:accountDict];
            Settings.Instance.saveAccountSettings(accountDict);
        }

        /// <summary>
        /// Display an error message.
        /// </summary>
        private void DisplayErrorMessage(CLError error)
        {
            _trace.writeToLog(1, "PageHomeViewModel: DisplayErrorMessage:  Error: {0}.", error.errorDescription);

            var dialog = SimpleIoc.Default.GetInstance<IModalWindow>(CLConstants.kDialogBox_CloudMessageBoxView);
            IModalDialogService modalDialogService = SimpleIoc.Default.GetInstance<IModalDialogService>();
            IMessageBoxService messageBoxService = SimpleIoc.Default.GetInstance<IMessageBoxService>();
            modalDialogService.ShowDialog(dialog, new CloudMessageBoxViewModel
            {
                CloudMessageBoxView_Title = _rm.GetString("generalErrorTitle"),
                CloudMessageBoxView_WindowWidth = 450,
                CloudMessageBoxView_WindowHeight = 230,
                CloudMessageBoxView_HeaderText = _rm.GetString("loginErrorHeader"),
                CloudMessageBoxView_BodyText = error.errorDescription,
                CloudMessageBoxView_LeftButtonVisibility = Visibility.Collapsed,
                CloudMessageBoxView_RightButtonWidth = new GridLength(100),
                CloudMessageBoxView_RightButtonMargin = new Thickness(0, 0, 0, 0),
                CloudMessageBoxView_RightButtonContent = _rm.GetString("generalOkButtonContent"),
            },
            ViewGridContainer,
            returnedViewModelInstance =>
            {
                // User clicked OK.  Do nothing here.  Leave the user on the CreateNewAccount page.
            });
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