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

        private RelayCommand _pageCreateNewAccount_BackCommand;
        private RelayCommand _pageCreateNewAccount_ContinueCommand;

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
                RaisePropertyChanged(FullNamePropertyName);
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
                ValidateConfirmPassword(value);
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
                RaisePropertyChanged(ComputerNamePropertyName);
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
                                                Uri nextPage = new System.Uri("/PageHome", System.UriKind.Relative);
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
                                                ForceValidation(((MainPage)App.Current.RootVisual).LayoutRoot);
                                                if(!HasErrors)
                                                {
                                                    Uri nextPage = new System.Uri("/PageSelectStorageSize", System.UriKind.Relative);
                                                    SendNavigationRequestMessage(nextPage);
                                                }
                                                else
                                                {
                                                    AppMessages.CreateNewAccount_FocusToError.Send("");
                                                }
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
            if(!RegexValidation.IsEMail(eMail))
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
            if(!RegexValidation.IsXOK(password))
            {
                AddError("Password", "Please enter a password with at least 8 characters, including a least one number, one lower case character and one upper case character.");
            }
            else if(ConfirmPassword.Length != 0 && !String.Equals(Password, ConfirmPassword, StringComparison.CurrentCulture))
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
            if ((Password.Length > 0 && !String.Equals(Password, confirmPassword, StringComparison.CurrentCulture)) ||
                (Password.Length == 0 && confirmPassword.Length != 0))
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
        /// Validate the whole UI tree.
        /// </summary>
        private void ForceValidation(UIElement element)
        {
            for(int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                UIElement child = (UIElement)VisualTreeHelper.GetChild(element, i);
                ForceValidation(child);
            }

            BindingExpression bindingExpression = null;

            string uiElementType = element.GetType().ToString();
            switch(uiElementType)
            {
                case "System.Windows.Controls.TextBox":
                    bindingExpression = ((TextBox)element).GetBindingExpression(TextBox.TextProperty);
                    break;

                case "System.Windows.Controls.PasswordBox":
                    bindingExpression = ((PasswordBox)element).GetBindingExpression(PasswordBox.PasswordProperty);
                    break;

                case "System.Windows.Controls.RadioButton":
                    bindingExpression = ((RadioButton)element).GetBindingExpression(RadioButton.IsCheckedProperty);
                    break;
            }

            if(bindingExpression == null || bindingExpression.ParentBinding == null) return;
            if(!bindingExpression.ParentBinding.ValidatesOnNotifyDataErrors) return;

            bindingExpression.UpdateSource();
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