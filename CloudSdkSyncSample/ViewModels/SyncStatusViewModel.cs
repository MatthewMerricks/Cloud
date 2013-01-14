using CloudApiPublic;
using CloudApiPublic.EventMessageReceiver;
using CloudApiPublic.Static;
using CloudApiPublic.Support;
using CloudSdkSyncSample.Static;
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

        // Sync status property to control the view's sync icon.
        // Upload properties
        public SyncStates SyncStatus
        {
            get { return _syncStatus; }
            set
            {
                if (value == _syncStatus)
                {
                    return;
                }

                _syncStatus = value;

                base.OnPropertyChanged("SyncStatus");
            }
        }
        private SyncStates _syncStatus = 0;

        // Upload properties
        public string TbFilesToUpload
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
        private string _tbFilesToUpload = "";

        public string TbFilesUploading
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
        private string _tbFilesUploading = "";

        public string TbTotalBytesToUpload
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
        private string _tbTotalBytesToUpload = "";

        public string TbBytesLeftToUpload
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
        private string _tbBytesLeftToUpload = "";

        // Download properties
        public string TbFilesToDownload
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
        private string _tbFilesToDownload = "";

        public string TbFilesDownloading
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
        private string _tbFilesDownloading = "";

        public string TbTotalBytesToDownload
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
        private string _tbTotalBytesToDownload = "";

        public string TbBytesLeftToDownload
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
        private string _tbBytesLeftToDownload = "";

        #endregion

        #region Public Methods

        /// <summary>
        /// The sync status has changed.  Get the changed status from the SyncBox.
        /// </summary>
        /// <param name="userState">This is the instance of the SyncBox (CLSync) whose status has changed.</param>
        public void OnSyncStatusUpdated(object userState)
        {
            CLSync syncBox = userState as CLSync;
            if (syncBox != null)
            {
                // Set the overall sync status
                CLSyncCurrentStatus currentStatus;
                syncBox.GetCLSyncCurrentStatus(out currentStatus);
                if (currentStatus.CurrentState == CLSyncCurrentState.Idle)
                {
                    SyncStatus = SyncStates.Synced;
                }
                else
                {
                    SyncStatus = SyncStates.Syncing;
                }

                // Aggregate the upload status
                long totalByteProgressUpload = 0;
                long totalBytesQueuedUpload = 0;
                long totalFilesCurrentlyUploading = 0;
                foreach (CLSyncTransferringFile indexFile in currentStatus.UploadingFiles)
                {
                    totalByteProgressUpload += indexFile.ByteProgress;
                    totalBytesQueuedUpload += indexFile.TotalByteSize;
                    totalFilesCurrentlyUploading += (indexFile.ByteProgress > 0) ? 1 : 0;
                }

                // Aggregate the download status
                long totalByteProgressDownload = 0;
                long totalBytesQueuedDownload = 0;
                long totalFilesCurrentlyDownloading = 0;
                foreach (CLSyncTransferringFile indexFile in currentStatus.DownloadingFiles)
                {
                    totalByteProgressDownload += indexFile.ByteProgress;
                    totalBytesQueuedDownload += indexFile.TotalByteSize;
                    totalFilesCurrentlyDownloading += (indexFile.ByteProgress > 0) ? 1 : 0;
                }

                // Update the properties
                TbTotalBytesToUpload = String.Format("{0:n0}", totalBytesQueuedUpload);
                TbTotalBytesToDownload = String.Format("{0:n0}", totalBytesQueuedDownload);

                TbBytesLeftToUpload = String.Format("{0:n0}", totalBytesQueuedUpload - totalByteProgressUpload);
                TbBytesLeftToDownload = String.Format("{0:n0}", totalBytesQueuedDownload - totalByteProgressDownload);

                TbFilesToUpload = String.Format("{0:n0}", currentStatus.UploadingFiles.Length);
                TbFilesToDownload = String.Format("{0:n0}", currentStatus.DownloadingFiles.Length);

                TbFilesUploading = String.Format("{0:n0}", totalFilesCurrentlyUploading);
                TbFilesDownloading = String.Format("{0:n0}", totalFilesCurrentlyDownloading);
            }
        }

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
