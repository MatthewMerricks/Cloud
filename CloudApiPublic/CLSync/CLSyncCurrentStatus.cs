//
// CLSyncCurrentStatus.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic
{
    /// <summary>
    /// Contains properties describing the current state of CLSync at the time of retrieval
    /// </summary>
    public sealed class CLSyncCurrentStatus
    {
        public CLSyncCurrentState CurrentState
        {
            get
            {
                return _currentState;
            }
        }
        private readonly CLSyncCurrentState _currentState;

        public CLSyncTransferringFile[] DownloadingFiles
        {
            get
            {
                CLSyncTransferringFile[] originalFiles;
                lock (TransferringFilesLocker)
                {
                    if (_downloadingFiles == null)
                    {
                        SplitTransferringFiles();
                    }
                    originalFiles = _downloadingFiles;
                }
                CLSyncTransferringFile[] toReturn = new CLSyncTransferringFile[originalFiles.Length];
                originalFiles.CopyTo(toReturn, 0);
                return toReturn;
            }
        }
        public CLSyncTransferringFile[] UploadingFiles
        {
            get
            {
                CLSyncTransferringFile[] originalFiles;
                lock (TransferringFilesLocker)
                {
                    if (_downloadingFiles == null)
                    {
                        SplitTransferringFiles();
                    }
                    originalFiles = _uploadingFiles;
                }
                CLSyncTransferringFile[] toReturn = new CLSyncTransferringFile[originalFiles.Length];
                originalFiles.CopyTo(toReturn, 0);
                return toReturn;
            }
        }
        private void SplitTransferringFiles()
        {
            if (TransferringFiles == null
                || TransferringFiles.Length == 0)
            {
                _downloadingFiles = new CLSyncTransferringFile[0];
                _uploadingFiles = new CLSyncTransferringFile[0];
            }
            else
            {
                List<int> uploadingIndexes = new List<int>();

                for (int transferringIndex = 0; transferringIndex < TransferringFiles.Length; transferringIndex++)
                {
                    if (TransferringFiles[transferringIndex].Direction == SyncDirection.To)
                    {
                        uploadingIndexes.Add(transferringIndex);
                    }
                }

                _uploadingFiles = new CLSyncTransferringFile[uploadingIndexes.Count];
                _downloadingFiles = new CLSyncTransferringFile[TransferringFiles.Length - uploadingIndexes.Count];

                int downloadingFilesIndex = 0;
                int uploadingFilesIndex = 0;
                Nullable<int> currentUploadingIndex = null;
                IEnumerator<int> getUploadingIndex = uploadingIndexes.GetEnumerator();
                try
                {
                    IEnumerator<int> remainingUploadingIndex = getUploadingIndex;
                    for (int transferringIndex = 0; transferringIndex < TransferringFiles.Length; transferringIndex++)
                    {
                        if (remainingUploadingIndex != null
                            && currentUploadingIndex == null)
                        {
                            if (remainingUploadingIndex.MoveNext())
                            {
                                currentUploadingIndex = remainingUploadingIndex.Current;
                            }
                            else
                            {
                                remainingUploadingIndex = null;
                            }
                        }

                        if (currentUploadingIndex == null
                            || ((int)currentUploadingIndex) != transferringIndex)
                        {
                            _downloadingFiles[downloadingFilesIndex] = TransferringFiles[transferringIndex];
                            downloadingFilesIndex++;
                        }
                        else
                        {
                            currentUploadingIndex = null;
                            _uploadingFiles[uploadingFilesIndex] = TransferringFiles[transferringIndex];
                            uploadingFilesIndex++;
                        }
                    }
                }
                finally
                {
                    getUploadingIndex.Dispose();
                }
            }
        }
        private CLSyncTransferringFile[] _downloadingFiles = null;
        private CLSyncTransferringFile[] _uploadingFiles = null;
        private readonly CLSyncTransferringFile[] TransferringFiles;
        private readonly object TransferringFilesLocker = new object();

        internal CLSyncCurrentStatus(CLSyncCurrentState CurrentState,
            IEnumerable<CLSyncTransferringFile> TransferringFiles)
        {
            this._currentState = CurrentState;
            if (TransferringFiles == null)
            {
                this.TransferringFiles = new CLSyncTransferringFile[0];
            }
            else
            {
                this.TransferringFiles = TransferringFiles.ToArray();
            }
        }
    }
}