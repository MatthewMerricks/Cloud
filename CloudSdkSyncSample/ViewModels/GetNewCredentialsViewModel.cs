using Cloud;
using Cloud.Model;
using Cloud.Interfaces;
using Cloud.Static;
using Cloud.Support;
using SampleLiveSync.EventMessageReceiver;
using SampleLiveSync.Models;
using SampleLiveSync.Support;
using SampleLiveSync.Views;
using SampleLiveSync.Static;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SampleLiveSync.ViewModels
{
    public sealed class GetNewCredentialsViewModel : WorkspaceViewModel
    {
        #region Fields
        
        // Private fields
        private Window _mainWindow = null;
        private bool _windowClosed = false;

        private static readonly CLTrace _trace = CLTrace.Instance;

        #endregion

        #region Events

        public event EventHandler<NotificationEventArgs<CLError>> NotifyException;
        public event EventHandler<NotificationEventArgs<GenericHolder<bool>>> NotifyDialogResult;
		 
	    #endregion

        #region Constructors

        public GetNewCredentialsViewModel(Window mainWindow)
        {
            if (mainWindow == null)
            {
                throw new Exception("mainWindow must not be null");
            }
            _mainWindow = mainWindow;
            _mainWindow.Topmost = true;
            _mainWindow.Topmost = false;

            // Initialize trace
            CLTrace.Initialize(Properties.Settings.Default.TraceFolderFullPath, "SampleLiveSync", "log", Properties.Settings.Default.TraceLevel, Properties.Settings.Default.LogErrors);
        }

        #endregion

        #region Model Properties

        public string Key
        {
            get { return _key; }
            set
            {
                if (value == _key)
                {
                    return;
                }

                _key = value;

                base.OnPropertyChanged("Key");
            }
        }
        private string _key = String.Empty;

        public string Secret
        {
            get { return _secret; }
            set
            {
                if (value == _secret)
                {
                    return;
                }

                _secret = value;

                base.OnPropertyChanged("Secret");
            }
        }
        private string _secret = String.Empty;

        public string Token
        {
            get { return _token; }
            set
            {
                if (value == _token)
                {
                    return;
                }

                _token = value;

                base.OnPropertyChanged("Token");
            }
        }
        private string _token = String.Empty;

        #endregion

        #region Focus Properties

        public bool IsKeyFocused
        {
            get { return _isKeyFocused; }
            set
            {
                if (value == _isKeyFocused)
                {
                    _isKeyFocused = false;
                    base.OnPropertyChanged("IsKeyFocused");
                }

                _isKeyFocused = value;
                base.OnPropertyChanged("IsKeyFocused");
            }
        }
        private bool _isKeyFocused;

        public bool IsSecretFocused
        {
            get { return _isSecretFocused; }
            set
            {
                if (value == _isSecretFocused)
                {
                    _isSecretFocused = false;
                    base.OnPropertyChanged("IsSecretFocused");
                }

                _isSecretFocused = value;
                base.OnPropertyChanged("IsSecretFocused");
            }
        }
        private bool _isSecretFocused;

        public bool IsTokenFocused
        {
            get { return _isTokenFocused; }
            set
            {
                if (value == _isTokenFocused)
                {
                    _isTokenFocused = false;
                    base.OnPropertyChanged("IsTokenFocused");
                }

                _isTokenFocused = value;
                base.OnPropertyChanged("IsTokenFocused");
            }
        }
        private bool _isTokenFocused;

        #endregion

        #region Commands

        /// <summary>
        /// Returns a command that saves and exits the dialog
        /// </summary>
        public ICommand CommandOk
        {
            get
            {
                if (_commandOk == null)
                {
                    _commandOk = new RelayCommand<object>(
                        param => this.Ok(),
                        param => this.CanOk
                        );
                }
                return _commandOk;
            }
        }
        RelayCommand<object> _commandOk;

        /// <summary>
        /// Returns a command that cancels and exits the dialog
        /// </summary>
        public ICommand CommandCancel
        {
            get
            {
                if (_commandCancel == null)
                {
                    _commandCancel = new RelayCommand<object>(
                        param => this.Cancel(),
                        param => this.CanCancel
                        );
                }
                return _commandCancel;
            }
        }
        RelayCommand<object> _commandCancel;

        #endregion

        #region Action Methods

        /// <summary>
        /// Save the settings entered so far.
        /// </summary>
        private void Ok()
        {
            try
            {
                // Validate the Key.
                Key = Key.Trim();
                if (String.IsNullOrEmpty(Key) ||
                    !OnlyHexInString(Key) ||
                     Key.Length != 64)
                {
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = null, Message = "The Key must be a 64 character long string with only hexadecimal characters." });
                    this.IsKeyFocused = true;
                    return;
                }

                // Validate the Secret.
                // NOTE: This private key should not be handled this way.  It should be retrieved dynamically from a remote server, or protected in some other way.
                Secret = Secret.Trim();
                if (String.IsNullOrEmpty(Secret) ||
                    !OnlyHexInString(Secret) ||
                     Secret.Length != 64)
                {
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = null, Message = "The Secret must be a 64 character long string with only hexadecimal characters." });
                    this.IsSecretFocused = true;
                    return;
                }

                // Validate the Token.
                // NOTE: The token should not be handled this way.  It should be retrieved dynamically from a remote server, or protected in some other way.
                Token = Token.Trim();
                if (!String.IsNullOrEmpty(Token) &&
                    (!OnlyHexInString(Token) ||
                     Token.Length != 64))
                {
                    NotifyException(this, new NotificationEventArgs<CLError>() { Data = null, Message = "If the token is specified, it must be a 64 character long string with only hexadecimal characters." });
                    this.IsTokenFocused = true;
                    return;
                }

                // Set the result of this dialog.  Setting DialogResult on the window forces an immediate close.
                _windowClosed = true;                       // allow the window to close.
                NotifyDialogResult(this, new NotificationEventArgs<GenericHolder<bool>>() { Data = new GenericHolder<bool>(true), Message = null });
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "GetNewCredentialsViewModel: SaveSettings: ERROR: Exception: Msg: <{0}>.", ex.Message);
                NotifyException(this, new NotificationEventArgs<CLError>() { Data = error, Message = String.Format("Error: {0}.", ex.Message) });
            }
        }

        /// <summary>
        /// Cancel and exit the dialog.
        /// </summary>
        private void Cancel()
        {
            try
            {
                // Set the result of this dialog.  Setting DialogResult on the window forces an immediate close.
                _windowClosed = true;       // allow the window to close
                NotifyDialogResult(this, new NotificationEventArgs<GenericHolder<bool>>() { Data = new GenericHolder<bool>(false), Message = null });
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.Log(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "GetNewCredentialsViewModel: Exit: ERROR: Exception: Msg: <{0}>.", ex.Message);
            }
        }

        #endregion

        #region Command Helpers

        /// <summary>
        /// Returns true if the OK button should be active.
        /// </summary>
        private bool CanOk
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Returns true if the Cancel button should be active.
        /// </summary>
        private bool CanCancel
        {
            get
            {
                return true;
            }
        }

        #endregion

        #region Public Methods called by view
        
        /// <summary>
        /// The user clicked the "X" in the upper right corner of the view window.
        /// </summary>
        /// <returns>bool: Prevent the window close.</returns>
        public bool OnWindowClosing()
        {
            if (_windowClosed)
            {
                return false;           // allow the window close
            }

            // Forward this request to our own Exit method.
            Dispatcher dispatcher = Application.Current.Dispatcher;
            dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), () =>
            {
                Cancel();
            });

            return true;                // cancel the window close
        }

        #endregion

        #region Private Support Functions

        private bool OnlyHexInString(string test)
        {
            // For C-style hex notation (0xFF) use @"\A\b(0[xX])?[0-9a-fA-F]+\b\Z"
            return System.Text.RegularExpressions.Regex.IsMatch(test, @"\A\b[0-9a-fA-F]+\b\Z");
        }

        #endregion
    }
}