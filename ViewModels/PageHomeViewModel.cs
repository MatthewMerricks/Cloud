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
using win_client.ViewModels;
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
using win_client.ViewModelHelpers;
using win_client.Resources;
using System.ComponentModel;
using System.Windows.Input;
using CleanShutdown.Messaging;
using CleanShutdown.Helpers;
using System.Windows.Threading;
using CloudApiPublic.Static;

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
        #region Private Instance Variables

        private readonly IDataService _dataService;

        private CLTrace _trace = CLTrace.Instance;
        private IModalWindow _dialog = null;        // for use with modal dialogs
        private bool _isShuttingDown = false;       // true: allow the shutdown if asked

        #endregion

        #region Life Cycle

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

        #endregion
        #region Bindable Properties

        /// <summary>
        /// The <see cref="EMail" /> property's name.
        /// </summary>
        public const string EMailPropertyName = "EMail";
        private string _eMail = "";
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
        /// The <see cref="BusyContent" /> property's name.
        /// </summary>
        public const string BusyContentPropertyName = "BusyContent";
        private string _busyContent = "Signing in...";
        public string BusyContent
        {
            get
            {
                return _busyContent;
            }

            set
            {
                if (_busyContent == value)
                {
                    return;
                }

                _busyContent = value;
                RaisePropertyChanged(BusyContentPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="WindowCloseOk" /> property's name.
        /// </summary>
        public const string WindowCloseOkPropertyName = "WindowCloseOk";
        private bool _windowCloseOk = false;
        public bool WindowCloseOk
        {
            get
            {
                return _windowCloseOk;
            }

            set
            {
                if (_windowCloseOk == value)
                {
                    return;
                }

                _windowCloseOk = value;
                RaisePropertyChanged(WindowCloseOkPropertyName);
            }
        }

        #endregion

        #region Relay Commands

        /// <summary>
        /// Create new account from the PageHome page.
        /// </summary>
        private RelayCommand _pageHome_CreateNewAccountCommand;
        public RelayCommand PageHome_CreateNewAccountCommand
        {
            get
            {
                return _pageHome_CreateNewAccountCommand 
                    ?? (_pageHome_CreateNewAccountCommand = new RelayCommand(
                                          () =>
                                          {
                                              Uri nextPage = new System.Uri(CLConstants.kPageCreateNewAccount, System.UriKind.Relative);
                                              CLAppMessages.PageHome_NavigationRequest.Send(nextPage);
                                          }));
            }
        }

        /// <summary>
        /// Sign in to an existing account from the PageHome page.
        /// </summary>
        private RelayCommand _pageHome_SignInCommand;
        public RelayCommand PageHome_SignInCommand
        {
            get
            {
                return _pageHome_SignInCommand
                    ?? (_pageHome_SignInCommand = new RelayCommand(
                                          () =>
                                          {
                                              var layoutRoot = LogicalTreeHelper.FindLogicalNode(Application.Current.MainWindow, "LayoutRoot") as UIElement; 
                                              CLExtensionMethods.ForceValidation(layoutRoot);
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
        private RelayCommand _pageHome_NavigatedToCommand;
        public RelayCommand PageHome_NavigatedToCommand
        {
            get
            {
                return _pageHome_NavigatedToCommand
                    ?? (_pageHome_NavigatedToCommand = new RelayCommand(
                                            () =>
                                            {
                                                EMail = String.Empty;
                                                Password = String.Empty;
                                            }));
            }
        }


        /// <summary>
        /// The user pressed the ESC key.
        /// </summary>
        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                return _cancelCommand
                    ?? (_cancelCommand = new RelayCommand(
                                          () =>
                                          {
                                              // The user pressed the Esc key.
                                              OnClosing();
                                          }));
            }
        }

        /// <summary>
        /// The window wants to close.  The user clicked the 'X'.
        /// This will set the bindable property WindowCloseOk if we will not handle this event.
        /// </summary>
        private ICommand _windowCloseRequested;
        public ICommand WindowCloseRequested
        {
            get
            {
                return _windowCloseRequested
                    ?? (_windowCloseRequested = new RelayCommand(
                                          () =>
                                          {
                                              // Handle the request and set the property.
                                              WindowCloseOk = OnClosing();
                                          }));
            }
        }

        #endregion

        /// <summary>
        /// Request login to the server with the email and password..
        /// The completion callback will occur on the main thread.
        /// </summary>
        private void RequestLogin()
        {
            IsBusy = true;                      // show the busy indicator

            // Create cloud device obj
            CLDevice clDevice = new CLDevice(Helpers.GetComputerFriendlyName());

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
                CLAppMessages.PageHome_NavigationRequest.Send(nextPage);
            }
            else
            {
                // There was an error registering this user.  Display the error and leave the user on the same page.
                CLModalMessageBoxDialogs.Instance.DisplayModalErrorMessage(
                    errorMessage: error.errorDescription,
                    title: Resources.Resources.generalErrorTitle,
                    headerText: Resources.Resources.loginErrorHeader,
                    rightButtonContent: Resources.Resources.generalOkButtonContent,
                    rightButtonIsDefault: true,
                    rightButtonIsCancel: true,
                    container: ViewGridContainer,
                    dialog: out _dialog,
                    actionOkButtonHandler: 
                        returnedViewModelInstance =>
                        {
                            // Do nothing here when the user clicks the OK button.
                        }
                );
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
            if (String.IsNullOrEmpty(password))
            {
                AddError("Password", "The password must not be empty.");
            }
        }

        #endregion


        #region Support Functions

        /// <summary>
        /// Implement window closing logic.
        /// <remarks>Note: This function will be called twice when the user clicks the Cancel button, and only once when the user
        /// clicks the 'X'.  Be careful to check for the "already cleaned up" case.</remarks>
        /// <<returns>true to allow the automatic Window.Close action.</returns>
        /// </summary>
        private bool OnClosing()
        {
            // Clean-up logic here.

            // Just allow the shutdown if we have already decided to do it.
            if (_isShuttingDown)
            {
                return true;
            }

            // The Register/Login window is closing.  Warn the user and allow him to cancel the close.
            CLModalMessageBoxDialogs.Instance.DisplayModalShutdownPrompt(container: ViewGridContainer, dialog: out _dialog, actionResultHandler: returnedViewModelInstance =>
                    {
                        _trace.writeToLog(9, "PageHomeViewModel: Prompt exit application: Entry.");
                        if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                        {
                            // The user said yes.
                            _trace.writeToLog(9, "PageHomeViewModel: Prompt exit application: User said yes.");

                            // Shut down tha application
                            _isShuttingDown = true;         // allow the shutdown if asked

                            // It is tempting to call ShutdownService.RequestShutdown() here, but this dialog
                            // is still active and would prevent the shutdown.  Allow the dialog to fully close
                            // and then request the shutdown.
                            Dispatcher dispatcher = CLAppDelegate.Instance.MainDispatcher;
                            dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () => 
                            {
                                ShutdownService.RequestShutdown();
                            });
                        }
                    });

            return false;                // cancel the automatic Window close.
        }

        #endregion
    }
}