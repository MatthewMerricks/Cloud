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

namespace Cloud.JsonContracts
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
                        ?? (_getMetadataResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxMetadataResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _getMetadataResponseSerializer = null;
        private static readonly object GetMetadataResponseSerializerLocker = new object();

        public static DataContractJsonSerializer GetStatusResponseSerializer
        {
            get
            {
                lock (GetStatusResponseSerializerLocker)
                {
                    return _getStatusResponseSerializer
                        ?? (_getMetadataResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxStatusResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _getStatusResponseSerializer = null;
        private static readonly object GetStatusResponseSerializerLocker = new object();
         
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
                        ?? (_eventSerializer = new DataContractJsonSerializer(typeof(JsonContracts.FileChangeResponse)));
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

        public static DataContractJsonSerializer NotificationUnsubscribeRequestSerializer
        {
            get
            {
                lock (NotificationUnsubscribeRequestSerializerLocker)
                {
                    return _notificationUnsubscribeRequestSerializer
                        ?? (_notificationUnsubscribeRequestSerializer = new DataContractJsonSerializer(typeof(JsonContracts.NotificationUnsubscribeRequest)));
                }
            }
        }
        private static DataContractJsonSerializer _notificationUnsubscribeRequestSerializer = null;
        private static readonly object NotificationUnsubscribeRequestSerializerLocker = new object();

        public static DataContractJsonSerializer NotificationUnsubscribeResponseSerializer
        {
            get
            {
                lock (NotificationUnsubscribeResponseSerializerLocker)
                {
                    return _notificationUnsubscribeResponseSerializer
                        ?? (_notificationUnsubscribeResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.NotificationUnsubscribeResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _notificationUnsubscribeResponseSerializer = null;
        private static readonly object NotificationUnsubscribeResponseSerializerLocker = new object();

        public static DataContractJsonSerializer SyncboxCreateRequestSerializer
        {
            get
            {
                lock (SyncboxCreateRequestSerializerLocker)
                {
                    return _syncboxCreateRequestSerializer
                        ?? (_syncboxCreateRequestSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxCreateRequest)));
                }
            }
        }
        private static DataContractJsonSerializer _syncboxCreateRequestSerializer = null;
        private static readonly object SyncboxCreateRequestSerializerLocker = new object();

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


        public static DataContractJsonSerializer FileOrFolderDeletesSerializer
        {
            get
            {
                lock (FileOrFolderDeletesSerializerLocker)
                {
                    return _fileOrFolderDeletesSerializer
                        ?? (_fileOrFolderDeletesSerializer = new DataContractJsonSerializer(typeof(JsonContracts.FileOrFolderDeletes)));
                }
            }
        }
        private static DataContractJsonSerializer _fileOrFolderDeletesSerializer = null;
        private static readonly object FileOrFolderDeletesSerializerLocker = new object();

        public static DataContractJsonSerializer FileDeleteRequestSerializer
        {
            get
            {
                lock (FileDeleteRequestSerializerLocker)
                {
                    return _fileDeleteRequestSerializer
                        ?? (_fileDeleteRequestSerializer = new DataContractJsonSerializer(typeof(JsonContracts.FileDeleteRequest)));
                }
            }
        }
        private static DataContractJsonSerializer _fileDeleteRequestSerializer = null;
        private static readonly object FileDeleteRequestSerializerLocker = new object();

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

        public static DataContractJsonSerializer FileOrFolderMovesSerializer
        {
            get
            {
                lock (FileOrFolderMovesSerializerLocker)
                {
                    return _fileOrFolderMovesSerializer
                        ?? (_fileOrFolderMovesSerializer = new DataContractJsonSerializer(typeof(JsonContracts.FileOrFolderMoves)));
                }
            }
        }
        private static DataContractJsonSerializer _fileOrFolderMovesSerializer = null;
        private static readonly object FileOrFolderMovesSerializerLocker = new object();

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
        #endregion

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

        public static DataContractJsonSerializer PicturesSerializer
        {
            get
            {
                lock (PicturesSerializerLocker)
                {
                    return _picturesSerializer
                        ?? (_picturesSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxGetAllImageItemsResponse)));
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
                        ?? (_videosSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxGetAllVideoItemsResponse)));
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
                        ?? (_audiosSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxGetAllAudioItemsResponse)));
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
                        ?? (_recentsSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxGetRecentsResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _recentsSerializer = null;
        private static readonly object RecentsSerializerLocker = new object();

        public static DataContractJsonSerializer SyncboxUsageSerializer
        {
            get
            {
                lock (SyncboxUsageSerializerLocker)
                {
                    return _syncboxUsageSerializer
                        ?? (_syncboxUsageSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxUsageResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _syncboxUsageSerializer = null;
        private static readonly object SyncboxUsageSerializerLocker = new object();

        public static DataContractJsonSerializer SyncboxUpdatePlanResponseSerializer
        {
            get
            {
                lock (SyncboxUpdatePlanResponseSerializerLocker)
                {
                    return _syncboxUpdatePlanResponseSerializer
                        ?? (_syncboxUpdatePlanResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxUpdateStoragePlanResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _syncboxUpdatePlanResponseSerializer = null;
        private static readonly object SyncboxUpdatePlanResponseSerializerLocker = new object();

        public static DataContractJsonSerializer SessionCreateResponseSerializer
        {
            get
            {
                lock (SessionCreateResponseSerializerLocker)
                {
                    return _sessionCreateResponseSerializer
                        ?? (_sessionCreateResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.CredentialsSessionCreateResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _sessionCreateResponseSerializer = null;
        private static readonly object SessionCreateResponseSerializerLocker = new object();

        public static DataContractJsonSerializer SyncboxUpdateResponseSerializer
        {
            get
            {
                lock (SyncboxUpdateResponseSerializerLocker)
                {
                    return _syncboxUpdateResponseSerializer
                        ?? (_syncboxUpdateResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _syncboxUpdateResponseSerializer = null;
        private static readonly object SyncboxUpdateResponseSerializerLocker = new object();

        public static DataContractJsonSerializer SyncboxUpdatePlanRequestSerializer
        {
            get
            {
                lock (SyncboxUpdatePlanRequestSerializerLocker)
                {
                    return _syncboxUpdatePlanRequestSerializer
                        ?? (_syncboxUpdatePlanRequestSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxUpdateStoragePlanRequest)));
                }
            }
        }
        private static DataContractJsonSerializer _syncboxUpdatePlanRequestSerializer = null;
        private static readonly object SyncboxUpdatePlanRequestSerializerLocker = new object();

        public static DataContractJsonSerializer SyncboxUpdateRequestSerializer
        {
            get
            {
                lock (SyncboxUpdateRequestSerializerLocker)
                {
                    return _syncboxUpdateRequestSerializer
                        ?? (_syncboxUpdateRequestSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxUpdateRequest)));
                }
            }
        }
        private static DataContractJsonSerializer _syncboxUpdateRequestSerializer = null;
        private static readonly object SyncboxUpdateRequestSerializerLocker = new object();

        public static DataContractJsonSerializer SessionCreateRequestSerializer
        {
            get
            {
                lock (SessionCreateRequestSerializerLocker)
                {
                    return _sessionCreateRequestSerializer
                        ?? (_sessionCreateRequestSerializer = new DataContractJsonSerializer(typeof(JsonContracts.CredentialsSessionCreateRequest)));
                }
            }
        }
        private static DataContractJsonSerializer _sessionCreateRequestSerializer = null;
        private static readonly object SessionCreateRequestSerializerLocker = new object();

        public static DataContractJsonSerializer SessionCreateAllRequestSerializer
        {
            get
            {
                lock (SessionCreateAllRequestSerializerLocker)
                {
                    return _sessionCreateAllRequestSerializer
                        ?? (_sessionCreateAllRequestSerializer = new DataContractJsonSerializer(typeof(JsonContracts.CredentialsSessionCreateAllRequest)));
                }
            }
        }
        private static DataContractJsonSerializer _sessionCreateAllRequestSerializer = null;
        private static readonly object SessionCreateAllRequestSerializerLocker = new object();

        public static DataContractJsonSerializer SessionDeleteRequestSerializer
        {
            get
            {
                lock (SessionDeleteRequestSerializerLocker)
                {
                    return _sessionDeleteRequestSerializer
                        ?? (_sessionDeleteRequestSerializer = new DataContractJsonSerializer(typeof(JsonContracts.CredentialsSessionDeleteRequest)));
                }
            }
        }
        private static DataContractJsonSerializer _sessionDeleteRequestSerializer = null;
        private static readonly object SessionDeleteRequestSerializerLocker = new object();

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
                        ?? (_folderContentsSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxFolderContentsResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _folderContentsSerializer = null;
        private static readonly object FolderContentsSerializerLocker = new object();

        public static DataContractJsonSerializer AuthenticationErrorResponseSerializer
        {
            get
            {
                lock (AuthenticationErrorResponseSerializerLocker)
                {
                    return _authenticationErrorResponseSerializer
                        ?? (_authenticationErrorResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.AuthenticationErrorResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _authenticationErrorResponseSerializer = null;
        private static readonly object AuthenticationErrorResponseSerializerLocker = new object();

        //// deprecated
        //
        ////public static DataContractJsonSerializer AuthenticationErrorMessageSerializer
        ////{
        ////    get
        ////    {
        ////        lock (AuthenticationErrorMessageSerializerLocker)
        ////        {
        ////            return _authenticationErrorMessageSerializer
        ////                ?? (_authenticationErrorMessageSerializer = new DataContractJsonSerializer(typeof(JsonContracts.AuthenticationErrorMessage)));
        ////        }
        ////    }
        ////}
        ////private static DataContractJsonSerializer _authenticationErrorMessageSerializer = null;
        ////private static readonly object AuthenticationErrorMessageSerializerLocker = new object();

        #region platform management
        public static DataContractJsonSerializer CreateSyncboxSerializer
        {
            get
            {
                lock (CreateSyncboxSerializerLocker)
                {
                    return _createSyncboxSerializer
                        ?? (_createSyncboxSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _createSyncboxSerializer = null;
        private static readonly object CreateSyncboxSerializerLocker = new object();

        public static DataContractJsonSerializer SyncboxMoveFilesOrFoldersResponseSerializer
        {
            get
            {
                lock (SyncboxMoveFilesOrFoldersResponseSerializerLocker)
                {
                    return _syncboxMoveFilesOrFoldersResponseSerializer
                        ?? (_syncboxMoveFilesOrFoldersResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxMoveFilesOrFoldersResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _syncboxMoveFilesOrFoldersResponseSerializer = null;
        private static readonly object SyncboxMoveFilesOrFoldersResponseSerializerLocker = new object();


        public static DataContractJsonSerializer SyncboxDeleteFilesResponseSerializer
        {
            get
            {
                lock (SyncboxDeleteFilesResponseSerializerLocker)
                {
                    return _syncboxDeleteFilesResponseSerializer
                        ?? (_syncboxDeleteFilesResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxDeleteFilesResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _syncboxDeleteFilesResponseSerializer = null;
        private static readonly object SyncboxDeleteFilesResponseSerializerLocker = new object();

        public static DataContractJsonSerializer ListSyncboxesSerializer
        {
            get
            {
                lock (ListSyncboxesSerializerLocker)
                {
                    return _listSyncboxesSerializer
                        ?? (_listSyncboxesSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxListResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _listSyncboxesSerializer = null;
        private static readonly object ListSyncboxesSerializerLocker = new object();

        public static DataContractJsonSerializer ListPlansSerializer
        {
            get
            {
                lock (ListPlansSerializerLocker)
                {
                    return _listPlansSerializer
                        ?? (_listPlansSerializer = new DataContractJsonSerializer(typeof(JsonContracts.StoragePlanListResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _listPlansSerializer = null;
        private static readonly object ListPlansSerializerLocker = new object();

        public static DataContractJsonSerializer ListSessionsSerializer
        {
            get
            {
                lock (ListSessionsSerializerLocker)
                {
                    return _listSessionsSerializer
                        ?? (_listSessionsSerializer = new DataContractJsonSerializer(typeof(JsonContracts.CredentialsListSessionsResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _listSessionsSerializer = null;
        private static readonly object ListSessionsSerializerLocker = new object();

        public static DataContractJsonSerializer SessionShowSerializer
        {
            get
            {
                lock (SessionShowSerializerLocker)
                {
                    return _sessionShowSerializer
                        ?? (_sessionShowSerializer = new DataContractJsonSerializer(typeof(JsonContracts.CredentialsSessionGetForKeyResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _sessionShowSerializer = null;
        private static readonly object SessionShowSerializerLocker = new object();

        public static DataContractJsonSerializer SessionDeleteSerializer
        {
            get
            {
                lock (SessionDeleteSerializerLocker)
                {
                    return _sessionDeleteSerializer
                        ?? (_sessionDeleteSerializer = new DataContractJsonSerializer(typeof(JsonContracts.CredentialsSessionDeleteResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _sessionDeleteSerializer = null;
        private static readonly object SessionDeleteSerializerLocker = new object();

        public static DataContractJsonSerializer SyncboxMetadataSerializer
        {
            get
            {
                lock (SyncboxMetadataSerializerLocker)
                {
                    return _syncboxMetadataSerializer
                        ?? (_syncboxMetadataSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxMetadata)));
                }
            }
        }
        private static DataContractJsonSerializer _syncboxMetadataSerializer = null;
        private static readonly object SyncboxMetadataSerializerLocker = new object();

        public static DataContractJsonSerializer SyncboxQuotaSerializer
        {
            get
            {
                lock (SyncboxQuotaSerializerLocker)
                {
                    return _syncboxQuotaSerializer
                        ?? (_syncboxQuotaSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxQuota)));
                }
            }
        }
        private static DataContractJsonSerializer _syncboxQuotaSerializer = null;
        private static readonly object SyncboxQuotaSerializerLocker = new object();

        public static DataContractJsonSerializer SyncboxDeleteSerializer
        {
            get
            {
                lock (SyncboxDeleteSerializerLocker)
                {
                    return _syncboxDeleteSerializer
                        ?? (_syncboxDeleteSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxIdOnly)));
                }
            }
        }
        private static DataContractJsonSerializer _syncboxDeleteSerializer = null;
        private static readonly object SyncboxDeleteSerializerLocker = new object();

        public static DataContractJsonSerializer UserRegistrationRequestSerializer
        {
            get
            {
                lock (UserRegistrationRequestSerializerLocker)
                {
                    return _userRegistrationRequestSerializer
                        ?? (_userRegistrationRequestSerializer = new DataContractJsonSerializer(typeof(JsonContracts.UserRegistrationRequest)));
                }
            }
        }
        private static DataContractJsonSerializer _userRegistrationRequestSerializer = null;
        private static readonly object UserRegistrationRequestSerializerLocker = new object();

        public static DataContractJsonSerializer UserRegistrationResponseSerializer
        {
            get
            {
                lock (UserRegistrationResponseSerializerLocker)
                {
                    return _userRegistrationResponseSerializer
                        ?? (_userRegistrationResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.UserRegistrationResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _userRegistrationResponseSerializer = null;
        private static readonly object UserRegistrationResponseSerializerLocker = new object();

        public static DataContractJsonSerializer DeviceRequestSerializer
        {
            get
            {
                lock (DeviceRequestSerializerLocker)
                {
                    return _deviceRequestSerializer
                        ?? (_deviceRequestSerializer = new DataContractJsonSerializer(typeof(JsonContracts.DeviceRequest)));
                }
            }
        }
        private static DataContractJsonSerializer _deviceRequestSerializer = null;
        private static readonly object DeviceRequestSerializerLocker = new object();

        public static DataContractJsonSerializer DeviceResponseSerializer
        {
            get
            {
                lock (DeviceResponseSerializerLocker)
                {
                    return _deviceResponseSerializer
                        ?? (_deviceResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.DeviceResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _deviceResponseSerializer = null;
        private static readonly object DeviceResponseSerializerLocker = new object();

        public static DataContractJsonSerializer LinkDeviceFirstTimeRequestSerializer
        {
            get
            {
                lock (LinkDeviceFirstTimeRequestSerializerLocker)
                {
                    return _linkDeviceFirstTimeRequestSerializer
                        ?? (_linkDeviceFirstTimeRequestSerializer = new DataContractJsonSerializer(typeof(JsonContracts.LinkDeviceFirstTimeRequest)));
                }
            }
        }
        private static DataContractJsonSerializer _linkDeviceFirstTimeRequestSerializer = null;
        private static readonly object LinkDeviceFirstTimeRequestSerializerLocker = new object();

        public static DataContractJsonSerializer LinkDeviceFirstTimeResponseSerializer
        {
            get
            {
                lock (LinkDeviceFirstTimeResponseSerializerLocker)
                {
                    return _linkDeviceFirstTimeResponseSerializer
                        ?? (_linkDeviceFirstTimeResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.LinkDeviceFirstTimeResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _linkDeviceFirstTimeResponseSerializer = null;
        private static readonly object LinkDeviceFirstTimeResponseSerializerLocker = new object();

        public static DataContractJsonSerializer SyncboxAuthResponseSerializer
        {
            get
            {
                lock (SyncboxAuthResponseSerializerLocker)
                {
                    return _syncboxAuthResponseSerializer
                        ?? (_syncboxAuthResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.SyncboxAuthResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _syncboxAuthResponseSerializer = null;
        private static readonly object SyncboxAuthResponseSerializerLocker = new object();

        public static DataContractJsonSerializer LinkDeviceRequestSerializer
        {
            get
            {
                lock (LinkDeviceRequestSerializerLocker)
                {
                    return _linkDeviceRequestSerializer
                        ?? (_linkDeviceRequestSerializer = new DataContractJsonSerializer(typeof(JsonContracts.LinkDeviceRequest)));
                }
            }
        }
        private static DataContractJsonSerializer _linkDeviceRequestSerializer = null;
        private static readonly object LinkDeviceRequestSerializerLocker = new object();

        public static DataContractJsonSerializer LinkDeviceResponseSerializer
        {
            get
            {
                lock (LinkDeviceResponseSerializerLocker)
                {
                    return _linkDeviceResponseSerializer
                        ?? (_linkDeviceResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.LinkDeviceResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _linkDeviceResponseSerializer = null;
        private static readonly object LinkDeviceResponseSerializerLocker = new object();

        public static DataContractJsonSerializer UnlinkDeviceRequestSerializer
        {
            get
            {
                lock (UnlinkDeviceRequestSerializerLocker)
                {
                    return _unlinkDeviceRequestSerializer
                        ?? (_unlinkDeviceRequestSerializer = new DataContractJsonSerializer(typeof(JsonContracts.UnlinkDeviceRequest)));
                }
            }
        }
        private static DataContractJsonSerializer _unlinkDeviceRequestSerializer = null;
        private static readonly object UnlinkDeviceRequestSerializerLocker = new object();

        public static DataContractJsonSerializer UnlinkDeviceResponseSerializer
        {
            get
            {
                lock (UnlinkDeviceResponseSerializerLocker)
                {
                    return _unlinkDeviceResponseSerializer
                        ?? (_unlinkDeviceResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.UnlinkDeviceResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _unlinkDeviceResponseSerializer = null;
        private static readonly object UnlinkDeviceResponseSerializerLocker = new object();
        #endregion
    }
}