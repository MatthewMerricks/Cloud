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
    public class SyncStatusViewModel : ViewModelBase
    {
        #region Events

        public event EventHandler<NotificationEventArgs> NotifySyncStatusWindowShouldClose;

        #endregion

        #region Sync Status Properties

        // Upload properties
        public int TbFilesToUpload
        {
            get { return _tbFilesToUpload; }
            set
            {
                if (value == _tbFilesToUpload)
                {
                    return;
                }

                _tbFilesToUpload = value;

                base.OnPropertyChanged("TbFilesToUpload");
            }
        }
        private int _tbFilesToUpload = 0;

        public int TbFilesUploading
        {
            get { return _tbFilesUploading; }
            set
            {
                if (value == _tbFilesUploading)
                {
                    return;
                }

                _tbFilesUploading = value;

                base.OnPropertyChanged("TbFilesUploading");
            }
        }
        private int _tbFilesUploading = 0;

        public int TbTotalBytesToUpload
        {
            get { return _tbTotalBytesToUpload; }
            set
            {
                if (value == _tbTotalBytesToUpload)
                {
                    return;
                }

                _tbTotalBytesToUpload = value;

                base.OnPropertyChanged("TbTotalBytesToUpload");
            }
        }
        private int _tbTotalBytesToUpload = 0;

        public int TbBytesLeftToUpload
        {
            get { return _tbBytesLeftToUpload; }
            set
            {
                if (value == _tbBytesLeftToUpload)
                {
                    return;
                }

                _tbBytesLeftToUpload = value;

                base.OnPropertyChanged("TbBytesLeftToUpload");
            }
        }
        private int _tbBytesLeftToUpload = 0;

        // Download properties
        public int TbFilesToDownload
        {
            get { return _tbFilesToDownload; }
            set
            {
                if (value == _tbFilesToDownload)
                {
                    return;
                }

                _tbFilesToDownload = value;

                base.OnPropertyChanged("TbFilesToDownload");
            }
        }
        private int _tbFilesToDownload = 0;

        public int TbFilesDownloading
        {
            get { return _tbFilesDownloading; }
            set
            {
                if (value == _tbFilesDownloading)
                {
                    return;
                }

                _tbFilesDownloading = value;

                base.OnPropertyChanged("TbFilesDownloading");
            }
        }
        private int _tbFilesDownloading = 0;

        public int TbTotalBytesToDownload
        {
            get { return _tbTotalBytesToDownload; }
            set
            {
                if (value == _tbTotalBytesToDownload)
                {
                    return;
                }

                _tbTotalBytesToDownload = value;

                base.OnPropertyChanged("TbTotalBytesToDownload");
            }
        }
        private int _tbTotalBytesToDownload = 0;

        public int TbBytesLeftToDownload
        {
            get { return _tbBytesLeftToDownload; }
            set
            {
                if (value == _tbBytesLeftToDownload)
                {
                    return;
                }

                _tbBytesLeftToDownload = value;

                base.OnPropertyChanged("TbBytesLeftToDownload");
            }
        }
        private int _tbBytesLeftToDownload = 0;

        #endregion

        #region Commands

        /// <summary>
        /// Returns a command that hides the sync status window.
        /// </summary>
        private RelayCommand<object> _commandDone;
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
