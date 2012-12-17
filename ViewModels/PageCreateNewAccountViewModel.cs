//
//  PageCreateNewAccountViewModel.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using GalaSoft.MvvmLight;
using win_client.Model;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using win_client.ViewModels;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;
using win_client.Common;
using System.Reflection;
using System.Linq;
using CloudApiPrivate.Model;
using CloudApiPrivate.Model.Settings;
using CloudApiPrivate.Static;
using CloudApiPublic;
using CloudApiPublic.Support;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using System.Collections.Generic;
using GalaSoft.MvvmLight.Ioc;
using Dialog.Abstractions.Wpf.Intefaces;
using System.Resources;
using win_client.AppDelegate;
using win_client.ViewModelHelpers;
using win_client.Resources;
using System.ComponentModel;
using System.Windows.Input;
using CleanShutdown.Helpers;
using System.Windows.Threading;

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
    public class PageCreateNewAccountViewModel : ValidatingViewModelBase
    {
        #region Instance Variables
        private readonly IDataService _dataService;
        private CLTrace _trace = CLTrace.Instance;
        private IModalWindow _dialog = null;        // for use with modal dialogs
        private bool _isShuttingDown = false;       // true: allow the shutdown if asked


        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageCreateNewAccountViewModel class.
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

                    SetFieldsFromSettings();
                    //&&&&               WelcomeTitle = item.Title;
                });

        }
        #endregion

        #region Bindable Properties

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
        /// The <see cref="EMail" /> property's name.
        /// </summary>
        public const string EMailPropertyName = "EMail";

        private string _eMail = "";

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
        /// The <see cref="FullName" /> property's name.
        /// </summary>
        public const string FullNamePropertyName = "FullName";

        private string _fullName = "";

        /// <summary>
        /// Sets and gets the FullName property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string FullName
        {
            get
            {
                return _fullName;
            }

            set
            {
                ValidateFullName(value);
                if(_fullName == value)
                {
                    return;
                }

                _fullName = value;
                Settings.Instance.UserFullName = _fullName;
                RaisePropertyChanged(FullNamePropertyName);
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
                CLAppMessages.CreateNewAccount_GetClearPasswordField.Send("");
                ValidatePassword(_password2);

                if(_password == value)
                {
                    return;
                }

                _password = value;
                RaisePropertyChanged(PasswordPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ConfirmPassword2" /> property's name.
        /// </summary>
        public const string ConfirmPassword2PropertyName = "ConfirmPassword2";
        private string _confirmPassword2 = "";
        /// <summary>
        /// Sets the ConfirmPassword2 property.
        /// This is the clear password. 
        /// </summary>
        public string ConfirmPassword2
        {
            get
            {
                return "";
            }

            set
            {
                _confirmPassword2 = value;
            }
        }

        /// <summary>
        /// The <see cref="ConfirmPassword" /> property's name.
        /// </summary>
        public const string ConfirmPasswordPropertyName = "ConfirmPassword";

        private string _confirmPassword = "";

        /// <summary>
        /// Sets and gets the ConfirmPassword property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ConfirmPassword
        {
            get
            {
                return _confirmPassword;
            }

            set
            {
                // The password is scrambled at this point because we don't want it in the visual tree for
                // Snoop and other tools to see.  We need to get the password in the clear.  However, only the view knows how
                // to get the clear password.  Send the view a message to cause it to set the clear
                // password.  Upon receiving the message, the view will retrieve the password and invoke a
                // public write-only property on this ViewModel object.  The whole process is synchronous, so
                // we will have the clear password when the Send completes.
                CLAppMessages.CreateNewAccount_GetClearConfirmPasswordField.Send("");
                ValidateConfirmPassword(_confirmPassword2);

                if(_confirmPassword == value)
                {
                    return;
                }

                _confirmPassword = value;
                RaisePropertyChanged(ConfirmPasswordPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ComputerName" /> property's name.
        /// </summary>
        public const string ComputerNamePropertyName = "ComputerName";

        private string _computerName = "";

        /// <summary>
        /// Sets and gets the ComputerName property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ComputerName
        {
            get
            {
                return _computerName;
            }

            set
            {
                ValidateComputerName(value);
                if(_computerName == value)
                {
                    return;
                }

                _computerName = value;
                Settings.Instance.DeviceName = _computerName;
                RaisePropertyChanged(ComputerNamePropertyName);
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
        private string _busyContent = "Creating account...";
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
        /// Back command from the PageCreateNewAccount page.
        /// </summary>
        private RelayCommand _pageCreateNewAccount_BackCommand;
        public RelayCommand PageCreateNewAccount_BackCommand
        {
            get
            {
                return _pageCreateNewAccount_BackCommand
                    ?? (_pageCreateNewAccount_BackCommand = new RelayCommand(
                                            () =>
                                            {
                                                Uri nextPage = new System.Uri(CLConstants.kPageHome, System.UriKind.Relative);
                                                CLAppMessages.PageCreateNewAccount_NavigationRequest.Send(nextPage);

                                            }));
            }
        }
        
        /// <summary>
        /// Continue command from the PageCreateNewAccount page.
        /// </summary>
        private RelayCommand _pageCreateNewAccount_ContinueCommand;
        public RelayCommand PageCreateNewAccount_ContinueCommand
        {
            get
            {
                return _pageCreateNewAccount_ContinueCommand
                    ?? (_pageCreateNewAccount_ContinueCommand = new RelayCommand(
                                            () =>
                                            {
                                                var layoutRoot = LogicalTreeHelper.FindLogicalNode(Application.Current.MainWindow, "LayoutRoot") as UIElement; 
                                                CLExtensionMethods.ForceValidation(layoutRoot);
                                                if(!HasErrors)
                                                {
                                                    // The user's entries are correct.  Process the form.
                                                    RequestNewRegistrationAsync();              // includes ProcessRegistration()
                                                }
                                                else
                                                {
                                                    CLAppMessages.CreateNewAccount_FocusToError.Send("");
                                                }
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
        /// Register this user.
        /// </summary>
        private void RequestNewRegistrationAsync()
        {
            IsBusy = true;                      // show the busy indicatorde
    
            // Get first and last name
            string[] names = FullName.Split(CLConstants.kDelimiterChars);
            string firstName = names[0];
            string lastName = names[names.Count() - 1];

            // Create cloud account obj
            CLAccount clAccount =  new CLAccount(EMail, firstName, lastName, _password2);
    
            // Create cloud device obj
            CLDevice clDevice = new CLDevice(ComputerName);
    
            // Request registration from Cloud SDK
            CLRegistration clRegistration = new CLRegistration();
    
            clRegistration.CreateNewAccountAsync(clAccount, clDevice, CreateNewAccountCompleteCallback, 30.0, clRegistration);
        }

        /// <summary>
        /// The request to the server has completed.
        /// This callback will occur on the main thread.
        /// </summary>
        private void CreateNewAccountCompleteCallback(CLRegistration clRegistration, bool isSuccess, CLError error)
        {
            IsBusy = false;                      // take the busy indicator down
            if (isSuccess)
            {
        
                // Save the information we have received.
                string akey = clRegistration.Token;
                string udid = clRegistration.Udid;
                string uuid = clRegistration.Uuid;
                string devn = clRegistration.LinkedDeviceName;
                CLAccount acct = clRegistration.LinkedAccount;
                saveAccountInformationWithAccount(acct, devn, uuid, akey, udid);

                // Navigate to the SelectStorage page.
                Uri nextPage = new System.Uri(CLConstants.kPageSelectStorageSize, System.UriKind.Relative);
                CLAppMessages.PageCreateNewAccount_NavigationRequest.Send(nextPage);
            } 
            else
            {
                // There was an error registering this user.  Display the error and leave the user on the same page.
                CLModalMessageBoxDialogs.Instance.DisplayModalErrorMessage(
                    errorMessage: error.errorDescription,
                    title: Resources.Resources.generalErrorTitle,
                    headerText: Resources.Resources.createNewAccountErrorHeader,
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


        /// <summary>
        /// Save the registration information to persistent settings.
        /// </summary>
        private void saveAccountInformationWithAccount(CLAccount account, string deviceName, string uuid, string akey, string udid)
        {
            string userName = account.FullName + String.Format(@" ({0})", account.UserName);
            Dictionary<string, object> accountDict = new Dictionary<string,object>();
            accountDict.Add(Settings.kUserName, userName);
            accountDict.Add(Settings.kDeviceName, deviceName);
            accountDict.Add(Settings.kAKey, akey);
            accountDict.Add(Settings.kUdid, udid);
            accountDict.Add(Settings.kUuid, uuid);
            Settings.Instance.saveAccountSettings(accountDict);
        }

        /// <summary>
        /// The page was navigated to.
        /// </summary>
        private RelayCommand _pageCreateNewAccount_NavigatedToCommand;
        public RelayCommand PageCreateNewAccount_NavigatedToCommand
        {
            get
            {
                return _pageCreateNewAccount_NavigatedToCommand
                    ?? (_pageCreateNewAccount_NavigatedToCommand = new RelayCommand(
                                            () =>
                                            {
                                                SetFieldsFromSettings();
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
        /// Validate the FullName property.
        /// </summary>
        private void ValidateFullName(string fullName)
        {
            RemoveAllErrorsForPropertyName("FullName");
            if(fullName.Length == 0)
            {
                AddError("FullName", "Please enter your full name.");
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
            else if(_confirmPassword2.Length != 0 && !String.Equals(password, _confirmPassword2, StringComparison.InvariantCulture))
            {
                RemoveAllErrorsForPropertyName("ConfirmPassword");
                AddError("ConfirmPassword", "The passwords don't match.");
            }
        }

        /// <summary>
        /// Validate the ConfirmPassword property.
        /// </summary>
        private void ValidateConfirmPassword(string confirmPassword)
        {
            RemoveAllErrorsForPropertyName("ConfirmPassword");
            if ((_password2.Length > 0 && !String.Equals(_password2, confirmPassword, StringComparison.InvariantCulture)) ||
                (_password2.Length == 0 && confirmPassword.Length != 0))
            {
                AddError("ConfirmPassword", "The passwords don't match.");
            }
        }

        /// <summary>
        /// Validate the ComputerName property.
        /// </summary>
        private void ValidateComputerName(string computerName)
        {
            RemoveAllErrorsForPropertyName("ComputerName");
            if(computerName.Length == 0)
            {
                AddError("ComputerName", "Please enter a name for this computer or device.");
            }
        }

        #endregion

        #region Supporting Functions

        /// <summary>
        /// Initialize the fields from the persistent settings.
        /// </summary>
        protected void SetFieldsFromSettings() 
        {
            _eMail = Settings.Instance.UserName;
            _fullName = Settings.Instance.UserFullName;
            _computerName = Settings.Instance.DeviceName;
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
                _trace.writeToLog(9, "PageCreateNewAccountViewModel: Prompt exit application: Entry.");
                if (_dialog.DialogResult.HasValue && _dialog.DialogResult.Value)
                {
                    // The user said yes.
                    _trace.writeToLog(9, "PageCreateNewAccountViewModel: Prompt exit application: User said yes.");

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