﻿using CloudApiPublic.Model;
using SyncTestServer.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace SyncTestServer
{
    public interface IServerStorage : INotifyPropertyChanged
    {
        void InitializeStorage(Action<string> storageKeyNoLongerPending, IEnumerable<InitialStorageData> initialData = null);
        bool AddUserFile(int userId, Guid deviceId, FilePath userRelativePath, byte[] MD5, long fileSize, out string storageKey, out bool filePending, out bool newUpload, Nullable<KeyValuePair<string, PendingMD5AndFileId>> previousFile = null);
        bool WriteFile(string storageKey, Stream toWrite, bool disposeStreamAfterWrite = true);
        bool MoveUserPathUsage(int userId, string storageKey, FilePath oldUserRelativePath, FilePath newUserRelativePath);
        bool RemoveUserUsageFromFile(string storageKey, int userId, FilePath userRelativePath);
        Stream ReadFile(string storageKey);
        bool IsStorageKeyPending(string storageKey);
        bool UpdateFileUsage(int userId, Guid deviceId, FilePath userRelativePath, byte[] oldMD5, byte[] newMD5, long newFileSize, out bool filePending, out bool newUpload, out string newStorageKey, out byte[] lastestMD5);
        IEnumerable<PurgedFile> PurgeUserPendingsByDevice(int userId, Guid deviceId);

        ////Pending should be tracked via output filePending bools from storage calls to set pending and callback Action<string> storageKeyNoLongerPending on InitializeStorage to clear pending
        //bool[] CheckPending(int userId, string[] storageKeys);
    }
}