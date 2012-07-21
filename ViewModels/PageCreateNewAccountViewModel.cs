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
using MVVMProductsDemo.ViewModels;
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
using System.Collections.Generic;
using GalaSoft.MvvmLight.Ioc;
using Dialog.Abstractions.Wpf.Intefaces;
using System.Resources;
using win_client.AppDelegate;
using win_client.ViewModelHelpers;

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
        private ResourceManager _rm;

        private RelayCommand _pageCreateNewAccount_BackCommand;
        private RelayCommand _pageCreateNewAccount_ContinueCommand;
        private RelayCommand _pageCreateNewAccount_NavigatedToCommand;        

        #endregion

        #region Life Cycle
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

                    SetFieldsFromSettings();
                    //&&&&               WelcomeTitle = item.Title;
                });
            _rm =  CLAppDelegate.Instance.ResourceManager;
            _trace = CLTrace.Instance;

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
        /// The <see cref="BusyContent" /> property's name.
        /// </summary>
        public const string BusyContentPropertyName = "BusyContent";

        private bool _busyContent = false;

        /// <summary>
        /// Sets and gets the BusyContent property.
        /// Changes to that property's value raise the PropertyChanged event. 
          /// </summary>
        public bool BusyContent
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

        #endregion
      
        #region Commands
         
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
                                                Uri nextPage = new System.Uri(CLConstants.kPageHome, System.UriKind.Relative);
                                                SendNavigationRequestMessage(nextPage);
                                            }));
            }
        }
        
        /// <summary>
        /// Continue command from the PageCreateNewAccount page.
        /// </summary>
        public RelayCommand PageCreateNewAccount_ContinueCommand
        {
            get
            {
                return _pageCreateNewAccount_ContinueCommand
                    ?? (_pageCreateNewAccount_ContinueCommand = new RelayCommand(
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
                string uuid = clRegistration.Uuid;
                string devn = clRegistration.LinkedDeviceName;
                CLAccount acct = clRegistration.LinkedAccount;
                saveAccountInformationWithAccount(acct, devn, uuid, akey);

                // Navigate to the SelectStorage page.
                Uri nextPage = new System.Uri(CLConstants.kPageSelectStorageSize, System.UriKind.Relative);
                SendNavigationRequestMessage(nextPage);
            } 
            else
            {
                // There was an error registering this user.  Display the error and leave the user on the same page.
                CLModalErrorDialog.Instance.DisplayModalErrorMessage(error.errorDescription, _rm.GetString("generalErrorTitle"),
                                                  _rm.GetString("createNewAccountErrorHeader"), _rm.GetString("generalOkButtonContent"),
                                                  ViewGridContainer, returnedViewModelInstance =>
                                                  {
                                                      // Do nothing here when the user clicks the OK button.
                                                  });
            }
        }


        /// <summary>
        /// Save the registration information to persistent settings.
        /// </summary>
        private void saveAccountInformationWithAccount(CLAccount account, string deviceName, string uuid, string akey)
        {
            string userName = account.FullName + String.Format(@" ({0})", account.UserName);
            Dictionary<string, object> accountDict = new Dictionary<string,object>();
            accountDict.Add(Settings.kUserName, userName);
            accountDict.Add(Settings.kDeviceName, deviceName);
            accountDict.Add(Settings.kAKey, akey);
            accountDict.Add(Settings.kUdidRegistered, @"1");
            accountDict.Add(Settings.kUuid, uuid);
            Settings.Instance.saveAccountSettings(accountDict);
        }

        /// <summary>
        /// The page was navigated to.
        /// </summary>
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

        /// <summary>
        /// Send a navigation request.
        /// </summary>
        protected void SendNavigationRequestMessage(Uri uri) 
        {
            Messenger.Default.Send<Uri>(uri, "PageCreateNewAccount_NavigationRequest");
        }

        #endregion
    }
}