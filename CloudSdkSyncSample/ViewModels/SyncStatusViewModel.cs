using CloudApiPublic.EventMessageReceiver;
using CloudApiPublic.Support;
using CloudSdkSyncSample.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace CloudSdkSyncSample.ViewModels
{
    public class SyncStatusViewModel
    {

        #region Events

        public event EventHandler<NotificationEventArgs> NotifySyncStatusWindowShouldClose;

        #endregion

        #region Commands

        /// <summary>
        /// Returns a command that hides the sync status window.
        /// </summary>
        RelayCommand<object> _commandDone;
        public ICommand CommandDone
        {
            get
            {
                if (_commandDone == null)
                {
                    _commandDone = new RelayCommand<object>(
                        param => this.Done(),
                        param => { return true; }
                        );
                }
                return _commandDone;
            }
        }


        #endregion

        #region Command Processing

        /// <summary>
        /// Hide the window
        /// </summary>
        private void Done()
        {
            NotifySyncStatusWindowShouldClose(this, null);
        }

        #endregion
    }
}
