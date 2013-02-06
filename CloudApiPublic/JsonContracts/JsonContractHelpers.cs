//
// JsonContractHelpers.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CloudApiPublic.JsonContracts
{
    internal static class JsonContractHelpers
    {
        public static DataContractJsonSerializer PushSerializer
        {
            get
            {
                lock (PushSerializerLocker)
                {
                    return _pushSerializer
                        ?? (_pushSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Push)));
                }
            }
        }
        private static DataContractJsonSerializer _pushSerializer = null;
        private static readonly object PushSerializerLocker = new object();

        public static DataContractJsonSerializer PushResponseSerializer
        {
            get
            {
                lock (PushResponseSerializerLocker)
                {
                    return _pushResponseSerializer
                        ?? (_pushResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.PushResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _pushResponseSerializer = null;
        private static readonly object PushResponseSerializerLocker = new object();

        public static DataContractJsonSerializer DownloadSerializer
        {
            get
            {
                lock (DownloadSerializerLocker)
                {
                    return _downloadSerializer
                        ?? (_downloadSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Download)));
                }
            }
        }
        private static DataContractJsonSerializer _downloadSerializer = null;
        private static readonly object DownloadSerializerLocker = new object();

        public static DataContractJsonSerializer GetMetadataResponseSerializer
        {
            get
            {
                lock (GetMetadataResponseSerializerLocker)
                {
                    return _getMetadataResponseSerializer
                        ?? (_getMetadataResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Metadata)));
                }
            }
        }
        private static DataContractJsonSerializer _getMetadataResponseSerializer = null;
        private static readonly object GetMetadataResponseSerializerLocker = new object();

        public static DataContractJsonSerializer ToSerializer
        {
            get
            {
                lock (ToSerializerLocker)
                {
                    return _toSerializer
                        ?? (_toSerializer = new DataContractJsonSerializer(typeof(JsonContracts.To)));
                }
            }
        }
        private static DataContractJsonSerializer _toSerializer = null;
        private static readonly object ToSerializerLocker = new object();

        public static DataContractJsonSerializer EventSerializer
        {
            get
            {
                lock (EventSerializerLocker)
                {
                    return _eventSerializer
                        ?? (_eventSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Event)));
                }
            }
        }
        private static DataContractJsonSerializer _eventSerializer = null;
        private static readonly object EventSerializerLocker = new object();

        public static DataContractJsonSerializer NotificationResponseSerializer
        {
            get
            {
                lock (NotificationResponseSerializerLocker)
                {
                    return _notificationResponseSerializer
                        ?? (_notificationResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.NotificationResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _notificationResponseSerializer = null;
        private static readonly object NotificationResponseSerializerLocker = new object();

        public static string NotificationResponseToJSON(JsonContracts.NotificationResponse notificationResponse)
        {
            using (MemoryStream stringStream = new MemoryStream())
            {
                NotificationResponseSerializer.WriteObject(stringStream, notificationResponse);
                stringStream.Position = 0;
                using (StreamReader stringReader = new StreamReader(stringStream))
                {
                    return stringReader.ReadToEnd();
                }
            }
        }

        public static JsonContracts.NotificationResponse ParseNotificationResponse(string notificationResponse)
        {
            MemoryStream stringStream = null;
            try
            {
                stringStream = new MemoryStream(Encoding.Unicode.GetBytes(notificationResponse));
                return (JsonContracts.NotificationResponse)NotificationResponseSerializer.ReadObject(stringStream);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (stringStream != null)
                {
                    stringStream.Dispose();
                }
            }
        }

        public static DataContractJsonSerializer PurgePendingSerializer
        {
            get
            {
                lock (PurgePendingSerializerLocker)
                {
                    return _purgePendingSerializer
                        ?? (_purgePendingSerializer = new DataContractJsonSerializer(typeof(JsonContracts.PurgePending)));
                }
            }
        }
        private static DataContractJsonSerializer _purgePendingSerializer = null;
        private static readonly object PurgePendingSerializerLocker = new object();

        public static DataContractJsonSerializer PendingResponseSerializer
        {
            get
            {
                lock (PendingResponseSerializerLocker)
                {
                    return _pendingResponseSerializer
                        ?? (_pendingResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.PendingResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _pendingResponseSerializer = null;
        private static readonly object PendingResponseSerializerLocker = new object();

        #region one-off contract serializers
        public static DataContractJsonSerializer FolderAddSerializer
        {
            get
            {
                lock (FolderAddSerializerLocker)
                {
                    return _folderAddSerializer
                        ?? (_folderAddSerializer = new DataContractJsonSerializer(typeof(JsonContracts.FolderAdd)));
                }
            }
        }
        private static DataContractJsonSerializer _folderAddSerializer = null;
        private static readonly object FolderAddSerializerLocker = new object();

        public static DataContractJsonSerializer FileAddSerializer
        {
            get
            {
                lock (FileAddSerializerLocker)
                {
                    return _fileAddSerializer
                        ?? (_fileAddSerializer = new DataContractJsonSerializer(typeof(JsonContracts.FileAdd)));
                }
            }
        }
        private static DataContractJsonSerializer _fileAddSerializer = null;
        private static readonly object FileAddSerializerLocker = new object();

        public static DataContractJsonSerializer FileModifySerializer
        {
            get
            {
                lock (FileModifySerializerLocker)
                {
                    return _fileModifySerializer
                        ?? (_fileModifySerializer = new DataContractJsonSerializer(typeof(JsonContracts.FileModify)));
                }
            }
        }
        private static DataContractJsonSerializer _fileModifySerializer = null;
        private static readonly object FileModifySerializerLocker = new object();

        public static DataContractJsonSerializer FileOrFolderDeleteSerializer
        {
            get
            {
                lock (FileOrFolderDeleteSerializerLocker)
                {
                    return _fileOrFolderDeleteSerializer
                        ?? (_fileOrFolderDeleteSerializer = new DataContractJsonSerializer(typeof(JsonContracts.FileOrFolderDelete)));
                }
            }
        }
        private static DataContractJsonSerializer _fileOrFolderDeleteSerializer = null;
        private static readonly object FileOrFolderDeleteSerializerLocker = new object();

        public static DataContractJsonSerializer FileOrFolderMoveSerializer
        {
            get
            {
                lock (FileOrFolderMoveSerializerLocker)
                {
                    return _fileOrFolderMoveSerializer
                        ?? (_fileOrFolderMoveSerializer = new DataContractJsonSerializer(typeof(JsonContracts.FileOrFolderMove)));
                }
            }
        }
        private static DataContractJsonSerializer _fileOrFolderMoveSerializer = null;
        private static readonly object FileOrFolderMoveSerializerLocker = new object();

        public static DataContractJsonSerializer FileOrFolderUndeleteSerializer
        {
            get
            {
                lock (FileOrFolderUndeleteSerializerLocker)
                {
                    return _fileOrFolderUndeleteSerializer
                        ?? (_fileOrFolderUndeleteSerializer = new DataContractJsonSerializer(typeof(JsonContracts.FileOrFolderUndelete)));
                }
            }
        }
        private static DataContractJsonSerializer _fileOrFolderUndeleteSerializer = null;
        private static readonly object FileOrFolderUndeleteSerializerLocker = new object();

        public static DataContractJsonSerializer FileVersionsSerializer
        {
            get
            {
                lock (FileVersionsSerializerLocker)
                {
                    return _fileVersionsSerializer
                        ?? (_fileVersionsSerializer = new DataContractJsonSerializer(typeof(JsonContracts.FileVersion[])));
                }
            }
        }
        private static DataContractJsonSerializer _fileVersionsSerializer = null;
        private static readonly object FileVersionsSerializerLocker = new object();

        ////Used bytes Serializer is depricated
        //
        //public static DataContractJsonSerializer UsedBytesSerializer
        //{
        //    get
        //    {
        //        lock (UsedBytesSerializerLocker)
        //        {
        //            return _usedBytesSerializer
        //                ?? (_usedBytesSerializer = new DataContractJsonSerializer(typeof(JsonContracts.UsedBytes)));
        //        }
        //    }
        //}
        //private static DataContractJsonSerializer _usedBytesSerializer = null;
        //private static readonly object UsedBytesSerializerLocker = new object();

        public static DataContractJsonSerializer FileCopySerializer
        {
            get
            {
                lock (FileCopySerializerLocker)
                {
                    return _fileCopySerializer
                        ?? (_fileCopySerializer = new DataContractJsonSerializer(typeof(JsonContracts.FileCopy)));
                }
            }
        }
        private static DataContractJsonSerializer _fileCopySerializer = null;
        private static readonly object FileCopySerializerLocker = new object();

        public static DataContractJsonSerializer PicturesSerializer
        {
            get
            {
                lock (PicturesSerializerLocker)
                {
                    return _picturesSerializer
                        ?? (_picturesSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Pictures)));
                }
            }
        }
        private static DataContractJsonSerializer _picturesSerializer = null;
        private static readonly object PicturesSerializerLocker = new object();

        public static DataContractJsonSerializer VideosSerializer
        {
            get
            {
                lock (VideosSerializerLocker)
                {
                    return _videosSerializer
                        ?? (_videosSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Videos)));
                }
            }
        }
        private static DataContractJsonSerializer _videosSerializer = null;
        private static readonly object VideosSerializerLocker = new object();

        public static DataContractJsonSerializer AudiosSerializer
        {
            get
            {
                lock (AudiosSerializerLocker)
                {
                    return _audiosSerializer
                        ?? (_audiosSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Audios)));
                }
            }
        }
        private static DataContractJsonSerializer _audiosSerializer = null;
        private static readonly object AudiosSerializerLocker = new object();

        public static DataContractJsonSerializer ArchivesSerializer
        {
            get
            {
                lock (ArchivesSerializerLocker)
                {
                    return _archivesSerializer
                        ?? (_archivesSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Archives)));
                }
            }
        }
        private static DataContractJsonSerializer _archivesSerializer = null;
        private static readonly object ArchivesSerializerLocker = new object();

        public static DataContractJsonSerializer RecentsSerializer
        {
            get
            {
                lock (RecentsSerializerLocker)
                {
                    return _recentsSerializer
                        ?? (_recentsSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Recents)));
                }
            }
        }
        private static DataContractJsonSerializer _recentsSerializer = null;
        private static readonly object RecentsSerializerLocker = new object();

        public static DataContractJsonSerializer SyncBoxUsageSerializer
        {
            get
            {
                lock (SyncBoxUsageSerializerLocker)
                {
                    return _syncBoxUsageSerializer
                        ?? (_syncBoxUsageSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncBoxUsage)));
                }
            }
        }
        private static DataContractJsonSerializer _syncBoxUsageSerializer = null;
        private static readonly object SyncBoxUsageSerializerLocker = new object();

        public static DataContractJsonSerializer FoldersSerializer
        {
            get
            {
                lock (FoldersSerializerLocker)
                {
                    return _foldersSerializer
                        ?? (_foldersSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Folders)));
                }
            }
        }
        private static DataContractJsonSerializer _foldersSerializer = null;
        private static readonly object FoldersSerializerLocker = new object();

        public static DataContractJsonSerializer FolderContentsSerializer
        {
            get
            {
                lock (FolderContentsSerializerLocker)
                {
                    return _folderContentsSerializer
                        ?? (_folderContentsSerializer = new DataContractJsonSerializer(typeof(JsonContracts.FolderContents)));
                }
            }
        }
        private static DataContractJsonSerializer _folderContentsSerializer = null;
        private static readonly object FolderContentsSerializerLocker = new object();

        public static DataContractJsonSerializer CreateSyncBoxSerializer
        {
            get
            {
                lock (CreateSyncBoxSerializerLocker)
                {
                    return _createSyncBoxSerializer
                        ?? (_createSyncBoxSerializer = new DataContractJsonSerializer(typeof(JsonContracts.CreateSyncBox)));
                }
            }
        }
        private static DataContractJsonSerializer _createSyncBoxSerializer = null;
        private static readonly object CreateSyncBoxSerializerLocker = new object();
        #endregion
    }
}