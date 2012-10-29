using CloudApiPublic.Model;
using SyncTestServer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace SyncTestServer
{
    public sealed class PhysicalStorage : NotifiableObject<PhysicalStorage>, IServerStorage
    {
        private const string AppendTempName = "temp";
        private const string EmptyFileStorageKey = "0";

        private readonly Dictionary<string, PendingMD5AndFileId> storageKeyToFile = new Dictionary<string, PendingMD5AndFileId>();
        private readonly Dictionary<int, Dictionary<FilePath, LinkedList<KeyValuePair<long, byte[]>>>> userToRevisionHistory = new Dictionary<int, Dictionary<FilePath, LinkedList<KeyValuePair<long, byte[]>>>>();
        private readonly Dictionary<MD5AndFileSize, string> hashToStorageKey = new Dictionary<MD5AndFileSize, string>(MD5AndFileSize.Comparer);
        private readonly Dictionary<string, MD5AndFileSize> storageKeyToHash = new Dictionary<string, MD5AndFileSize>();
        private readonly Dictionary<int, Dictionary<Guid, Dictionary<string, KeyValuePair<Action<object>, object>>>> userPendingStorageKeys = new Dictionary<int, Dictionary<Guid, Dictionary<string, KeyValuePair<Action<object>, object>>>>();
        private readonly string storageFolder;
        private readonly string storageFolderTemp;
        private readonly DirectoryInfo storageInfo;
        private static int storageKeyCounter;
        private static string GetNewStorageKey()
        {
            return Interlocked.Increment(ref storageKeyCounter).ToString();
        }
        private readonly GenericHolder<bool> Initialized = new GenericHolder<bool>(false);

        private readonly Dictionary<FileIdAndUser, UpdateQueuedData> UpdateFileIdToQueuedData = new Dictionary<FileIdAndUser, UpdateQueuedData>(FileIdAndUser.Comparer);
        private sealed class UpdateQueuedData
        {
            public string StorageKey { get; set; }
            public int UserId { get; set; }
            public FilePath UserRelativePath { get; set; }
            public PendingMD5AndFileId FileId { get; set; }
        }

        public PhysicalStorage(string StorageFolder)
        {
            if (string.IsNullOrWhiteSpace(StorageFolder))
            {
                throw new NullReferenceException("StorageFolder cannot be null");
            }

            this.storageFolder = StorageFolder;
            this.storageInfo = new DirectoryInfo(StorageFolder);
            if (!this.storageInfo.Exists)
            {
                this.storageInfo.Create();
            }
            this.storageFolderTemp = StorageFolder + "\\" + AppendTempName;
            if (!Directory.Exists(this.storageFolderTemp))
            {
                Directory.CreateDirectory(this.storageFolderTemp);
            }
        }

        public void InitializeStorage(IEnumerable<InitialStorageData> initialData = null)
        {
            lock (Initialized)
            {
                if (Initialized.Value)
                {
                    throw new Exception("Already Initialized");
                }

                if (initialData != null)
                {
                    foreach (InitialStorageData currentData in initialData)
                    {
                        MD5AndFileSize newMD5 = new MD5AndFileSize(currentData.MD5, currentData.FileSize);
                        string newStorageKey;
                        if (!hashToStorageKey.TryGetValue(newMD5, out newStorageKey))
                        {
                            newStorageKey = (currentData.FileSize == 0
                                ? EmptyFileStorageKey
                                : GetNewStorageKey());
                            hashToStorageKey.Add(newMD5, newStorageKey);
                            storageKeyToHash.Add(newStorageKey, newMD5);
                        }

                        PendingMD5AndFileId existingFileId;
                        if (storageKeyToFile.TryGetValue(newStorageKey, out existingFileId))
                        {
                            existingFileId.AddUserUsage(currentData.UserId, currentData.RelativePath);
                        }
                        else
                        {
                            Guid newFileId = (currentData.FileSize == 0
                                ? Guid.Empty
                                : Guid.NewGuid());

                            existingFileId = new PendingMD5AndFileId(currentData.MD5,
                                newFileId,
                                currentData.UserId,
                                currentData.RelativePath,
                                false);

                            storageKeyToFile.Add(newStorageKey, existingFileId);
                        }
                        Dictionary<FilePath, LinkedList<KeyValuePair<long, byte[]>>> userHistory;
                        if (!userToRevisionHistory.TryGetValue(currentData.UserId, out userHistory))
                        {
                            userHistory = new Dictionary<FilePath, LinkedList<KeyValuePair<long, byte[]>>>(FilePathComparer.Instance);
                            userToRevisionHistory.Add(currentData.UserId, userHistory);
                        }
                        if (userHistory.ContainsKey(currentData.RelativePath))
                        {
                            throw new NotImplementedException("Multiple files provided with the same relative path: " + (currentData.RelativePath == null ? "{null}" : currentData.RelativePath.ToString()));
                        }

                        userHistory.Add(currentData.RelativePath,
                            new LinkedList<KeyValuePair<long, byte[]>>(new[] { new KeyValuePair<long, byte[]>(currentData.FileSize, currentData.MD5) }));
                    }
                }

                Initialized.Value = true;
            }
        }

        public bool IsStorageKeyPending(string storageKey)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("Not Initialized, call PopulateInitialData first");
                }
            }

            lock (storageKeyToFile)
            {
                PendingMD5AndFileId fileId;
                return storageKeyToFile.TryGetValue(storageKey,
                        out fileId)
                    && fileId.Pending;
            }
        }

        public bool AddUserFile(int userId, Guid deviceId, FilePath userRelativePath, byte[] MD5, long fileSize, out string storageKey, out bool filePending, out bool newUpload, Nullable<KeyValuePair<string, PendingMD5AndFileId>> previousFile = null)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("Not Initialized, call PopulateInitialData first");
                }
            }
            if (userRelativePath == null)
            {
                throw new NullReferenceException("userRelativePath cannot be null");
            }
            if (MD5 == null
                || MD5.Length != 16)
            {
                throw new ArgumentException("MD5 must be a 16-length byte array");
            }

            MD5AndFileSize hashIndex = new MD5AndFileSize(MD5, fileSize);
            lock (storageKeyToFile)
            {
                if (hashToStorageKey.TryGetValue(hashIndex, out storageKey))
                {
                    PendingMD5AndFileId existingFileId = storageKeyToFile[storageKey];
                    lock (existingFileId.SyncRoot)
                    {
                        filePending = existingFileId.Pending;
                        newUpload = false;
                        return existingFileId.AddUserUsage(userId, userRelativePath);
                    }
                }
                else
                {
                    storageKey = (fileSize == 0
                        ? EmptyFileStorageKey
                        : GetNewStorageKey());
                    PendingMD5AndFileId newFileId = new PendingMD5AndFileId(MD5, (fileSize == 0
                        ? Guid.Empty
                        : Guid.NewGuid()), userId, userRelativePath, fileSize != 0);
                    hashToStorageKey.Add(hashIndex, storageKey);
                    storageKeyToHash.Add(storageKey, hashIndex);
                    storageKeyToFile.Add(storageKey, newFileId);
                    newUpload = filePending = fileSize != 0;
                    if (filePending)
                    {
                        KeyValuePair<Action<object>, object> uploadCompletionAction;

                        if (previousFile != null)
                        {
                            KeyValuePair<string, PendingMD5AndFileId> castPreviousFile = (KeyValuePair<string, PendingMD5AndFileId>)previousFile;

                            UpdateFileIdToQueuedData[new FileIdAndUser(castPreviousFile.Value.FileId, userId)] = new UpdateQueuedData()
                            {
                                FileId = castPreviousFile.Value,
                                StorageKey = castPreviousFile.Key,
                                UserId = userId,
                                UserRelativePath = userRelativePath
                            };

                            uploadCompletionAction = new KeyValuePair<Action<object>, object>(state =>
                                {
                                    FileIdAndUser castState = (FileIdAndUser)state;
                                    UpdateQueuedData grabQueuedData;
                                    if (UpdateFileIdToQueuedData.TryGetValue(castState, out grabQueuedData))
                                    {
                                        RemoveFileById(grabQueuedData.StorageKey, grabQueuedData.UserId, grabQueuedData.UserRelativePath, grabQueuedData.FileId);
                                    }
                                }, new FileIdAndUser(castPreviousFile.Value.FileId, userId));
                        }
                        else
                        {
                            uploadCompletionAction = new KeyValuePair<Action<object>, object>(null, null);
                        }

                        Dictionary<Guid, Dictionary<string, KeyValuePair<Action<object>, object>>> otherPendingKeys;
                        if (userPendingStorageKeys.TryGetValue(userId, out otherPendingKeys))
                        {
                            Dictionary<string, KeyValuePair<Action<object>, object>> deviceKeys;
                            if (otherPendingKeys.TryGetValue(deviceId, out deviceKeys))
                            {
                                if (!deviceKeys.ContainsKey(storageKey))
                                {
                                    deviceKeys.Add(storageKey, uploadCompletionAction);
                                }
                            }
                            else
                            {
                                otherPendingKeys.Add(deviceId, new Dictionary<string, KeyValuePair<Action<object>, object>>()
                                {
                                    { storageKey, uploadCompletionAction }
                                });
                            }
                        }
                        else
                        {
                            userPendingStorageKeys.Add(userId,
                                new Dictionary<Guid, Dictionary<string, KeyValuePair<Action<object>, object>>>()
                                {
                                    {
                                        deviceId,
                                        new Dictionary<string, KeyValuePair<Action<object>, object>>()
                                            {
                                                { storageKey, uploadCompletionAction }
                                            }
                                    }
                                });
                        }
                    }
                    else if (previousFile != null)
                    {
                        KeyValuePair<string, PendingMD5AndFileId> castPreviousFile = (KeyValuePair<string, PendingMD5AndFileId>)previousFile;

                        RemoveFileById(castPreviousFile.Key, userId, userRelativePath, castPreviousFile.Value);
                    }

                    Dictionary<FilePath, LinkedList<KeyValuePair<long, byte[]>>> userHistory;
                    if (!userToRevisionHistory.TryGetValue(userId, out userHistory))
                    {
                        userHistory = new Dictionary<FilePath, LinkedList<KeyValuePair<long, byte[]>>>(FilePathComparer.Instance);
                        userToRevisionHistory.Add(userId, userHistory);
                    }
                    if (!userHistory.ContainsKey(userRelativePath))
                    {
                        userHistory.Add(userRelativePath,
                            new LinkedList<KeyValuePair<long, byte[]>>(new[] { new KeyValuePair<long, byte[]>(fileSize, MD5) }));
                    }

                    return true;
                }
            }
        }

        public bool UpdateFileUsage(int userId, Guid deviceId, FilePath userRelativePath, byte[] oldMD5, byte[] newMD5, long newFileSize, out bool filePending, out bool newUpload, out string newStorageKey, out byte[] lastestMD5)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("Not Initialized, call PopulateInitialData first");
                }
            }

            if (userRelativePath == null)
            {
                throw new NullReferenceException("userRelativePath cannot be null");
            }
            if (newMD5 == null
                || newMD5.Length != 16)
            {
                throw new ArgumentException("newMD5 must be a 16-length byte array");
            }
            if (newFileSize < 0)
            {
                throw new ArgumentException("newFileSize cannot be negative");
            }

            lock (storageKeyToFile)
            {
                Dictionary<FilePath, LinkedList<KeyValuePair<long, byte[]>>> userRevisions;
                LinkedList<KeyValuePair<long, byte[]>> fileRevisions;
                GenericHolder<Nullable<KeyValuePair<long, byte[]>>> firstRevision = new GenericHolder<Nullable<KeyValuePair<long,byte[]>>>(null);
                MD5AndFileSize previousHash;
                string previousStorageKey;
                PendingMD5AndFileId previousFile;
                lastestMD5 = null;
                if (oldMD5 != null
                    && oldMD5.Length == 16
                    && userToRevisionHistory.TryGetValue(userId, out userRevisions)
                    && userRevisions.TryGetValue(userRelativePath, out fileRevisions)
                    && fileRevisions.Count > 0
                    && (lastestMD5 = (((KeyValuePair<long, byte[]>)RetrieveOrPullFirstRevision(fileRevisions, firstRevision)).Value)) != null
                    && hashToStorageKey.TryGetValue(previousHash = new MD5AndFileSize(((KeyValuePair<long, byte[]>)RetrieveOrPullFirstRevision(fileRevisions, firstRevision)).Value, ((KeyValuePair<long, byte[]>)RetrieveOrPullFirstRevision(fileRevisions, firstRevision)).Key), out previousStorageKey)
                    && storageKeyToFile.TryGetValue(previousStorageKey, out previousFile)
                    && previousFile.GetUserUsagesByUser(userId).Contains(userRelativePath, FilePathComparer.Instance)
                    && SyncTestServer.Static.NativeMethods.memcmp(((KeyValuePair<long, byte[]>)RetrieveOrPullFirstRevision(fileRevisions, firstRevision)).Value,
                        oldMD5,
                        new UIntPtr((uint)16)) == 0)
                {
                    AddUserFile(userId, deviceId, userRelativePath, newMD5, newFileSize, out newStorageKey, out filePending, out newUpload, new KeyValuePair<string, PendingMD5AndFileId>(previousStorageKey, previousFile));

                    fileRevisions.AddFirst(new KeyValuePair<long, byte[]>(newFileSize, newMD5));

                    lastestMD5 = newMD5;

                    return true;
                }
                else
                {
                    filePending = false;
                    newUpload = false;
                    newStorageKey = null;
                    return false;
                }
            }
        }

        private static Nullable<KeyValuePair<long, byte[]>> RetrieveOrPullFirstRevision(LinkedList<KeyValuePair<long, byte[]>> allRevisions, GenericHolder<Nullable<KeyValuePair<long, byte[]>>> alreadyPulledRevision)
        {
            if (alreadyPulledRevision.Value == null)
            {
                alreadyPulledRevision.Value = allRevisions.First();
            }
            return alreadyPulledRevision.Value;
        }

        public bool WriteFile(string storageKey, Stream toWrite, bool disposeStreamAfterWrite = true)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("Not Initialized, call PopulateInitialData first");
                }
            }
            if (storageKey == EmptyFileStorageKey)
            {
                if (disposeStreamAfterWrite)
                {
                    toWrite.Dispose();
                }
                return false;
            }

            bool needsUpload;

            Guid fileId;
            lock (storageKeyToFile)
            {
                PendingMD5AndFileId existingFileId;
                if (!storageKeyToFile.TryGetValue(storageKey, out existingFileId))
                {
                    throw new KeyNotFoundException("Could not find fileId for storageKey: " + (storageKey ?? "{null}"));
                }
                fileId = existingFileId.FileId;

                lock (existingFileId.SyncRoot)
                {
                    needsUpload = existingFileId.Pending;
                }
            }

            if (needsUpload)
            {
                string fileIdString = fileId.ToString();
                string tempFile = storageFolderTemp + "\\" + fileIdString;
                using (FileStream storageFileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] data = new byte[CloudApiPublic.Static.FileConstants.BufferSize];
                    int read;
                    while ((read = toWrite.Read(data, 0, data.Length)) > 0)
                    {
                        storageFileStream.Write(data, 0, read);
                    }
                    storageFileStream.Flush();
                }

                lock (storageKeyToFile)
                {
                    PendingMD5AndFileId existingFileId;
                    if (!storageKeyToFile.TryGetValue(storageKey, out existingFileId))
                    {
                        throw new KeyNotFoundException("After completing upload, could not find fileId for storageKey: " + (storageKey ?? "{null}"));
                    }
                    fileId = existingFileId.FileId;

                    bool clearPending;

                    lock (existingFileId.SyncRoot)
                    {
                        if (existingFileId.Pending)
                        {
                            File.Move(tempFile, storageFolder + "\\" + fileIdString);
                            existingFileId.Pending = false;
                            clearPending = true;
                        }
                        else
                        {
                            File.Delete(tempFile);
                            clearPending = false;
                        }
                    }

                    if (clearPending)
                    {
                        foreach (int currentUserId in existingFileId.GetUserUsages().Select(currentUser => currentUser.Key))
                        {
                            Dictionary<Guid, Dictionary<string, KeyValuePair<Action<object>, object>>> pendingUserKeys;
                            if (userPendingStorageKeys.TryGetValue(currentUserId, out pendingUserKeys))
                            {
                                List<Guid> emptiedDevices = null;

                                foreach (KeyValuePair<Guid, Dictionary<string, KeyValuePair<Action<object>, object>>> pendingDeviceKeys in pendingUserKeys)
                                {
                                    KeyValuePair<Action<object>, object> uploadCompletionAction;
                                    if (pendingDeviceKeys.Value.TryGetValue(storageKey, out uploadCompletionAction))
                                    {
                                        pendingDeviceKeys.Value.Remove(storageKey);

                                        if (uploadCompletionAction.Key != null)
                                        {
                                            uploadCompletionAction.Key(uploadCompletionAction.Value);
                                        }

                                        if (pendingDeviceKeys.Value.Count == 0)
                                        {
                                            if (emptiedDevices == null)
                                            {
                                                emptiedDevices = new List<Guid>(new[] { pendingDeviceKeys.Key });
                                            }
                                            else
                                            {
                                                emptiedDevices.Add(pendingDeviceKeys.Key);
                                            }
                                        }
                                    }
                                }

                                if (emptiedDevices != null)
                                {
                                    emptiedDevices.ForEach(emptiedDevice => pendingUserKeys.Remove(emptiedDevice));
                                    if (pendingUserKeys.Count == 0)
                                    {
                                        userPendingStorageKeys.Remove(currentUserId);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (disposeStreamAfterWrite)
            {
                toWrite.Dispose();
            }

            return needsUpload;
        }

        public void RemoveUserUsageFromFile(string storageKey, int userId, FilePath userRelativePath)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("Not Initialized, call PopulateInitialData first");
                }
            }
            if (userRelativePath == null)
            {
                throw new NullReferenceException("userRelativePath cannot be null");
            }

            lock (storageKeyToFile)
            {
                PendingMD5AndFileId fileId;
                if (storageKeyToFile.TryGetValue(storageKey, out fileId))
                {
                    RemoveFileById(storageKey, userId, userRelativePath, fileId);

                    UpdateFileIdToQueuedData.Remove(new FileIdAndUser(fileId.FileId, userId));

                    Dictionary<FilePath, LinkedList<KeyValuePair<long, byte[]>>> userHistory;
                    if (userToRevisionHistory.TryGetValue(userId, out userHistory))
                    {
                        userHistory.Remove(userRelativePath);
                        if (userHistory.Count == 0)
                        {
                            userToRevisionHistory.Remove(userId);
                        }
                    }
                }
            }
        }

        private void RemoveFileById(string storageKey, int userId, FilePath userRelativePath, PendingMD5AndFileId fileId)
        {
            lock (storageKeyToFile)
            {
                lock (fileId.SyncRoot)
                {
                    if (fileId.RemoveUserUsage(userId, userRelativePath))
                    {
                        storageKeyToFile.Remove(storageKey);
                        MD5AndFileSize toRemoveMD5;
                        if (storageKeyToHash.TryGetValue(storageKey, out toRemoveMD5))
                        {
                            hashToStorageKey.Remove(toRemoveMD5);
                        }
                        storageKeyToHash.Remove(storageKey);
                    }

                    Dictionary<Guid, Dictionary<string, KeyValuePair<Action<object>, object>>> pendingUserKeys;
                    if (userPendingStorageKeys.TryGetValue(userId, out pendingUserKeys))
                    {
                        List<Guid> emptiedDevices = null;

                        foreach (KeyValuePair<Guid, Dictionary<string, KeyValuePair<Action<object>, object>>> pendingDeviceKeys in pendingUserKeys)
                        {
                            KeyValuePair<Action<object>, object> uploadCompletionAction;
                            if (pendingDeviceKeys.Value.TryGetValue(storageKey, out uploadCompletionAction))
                            {
                                pendingDeviceKeys.Value.Remove(storageKey);

                                if (pendingDeviceKeys.Value.Count == 0)
                                {
                                    if (emptiedDevices == null)
                                    {
                                        emptiedDevices = new List<Guid>(new[] { pendingDeviceKeys.Key });
                                    }
                                    else
                                    {
                                        emptiedDevices.Add(pendingDeviceKeys.Key);
                                    }
                                }
                            }
                        }

                        if (emptiedDevices != null)
                        {
                            emptiedDevices.ForEach(emptiedDevice => pendingUserKeys.Remove(emptiedDevice));
                            if (pendingUserKeys.Count == 0)
                            {
                                userPendingStorageKeys.Remove(userId);
                            }
                        }
                    }

                    if (!fileId.Pending
                        && storageKey != EmptyFileStorageKey)
                    {
                        string fileToDelete = storageFolder + "\\" + fileId.FileId.ToString();
                        fileId.GetDownloadingCount(new KeyValuePair<Action<object>, object>(state =>
                        {
                            File.Delete((string)state);
                        }, fileToDelete));
                    }
                }
            }
        }

        public Stream ReadFile(string storageKey)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("Not Initialized, call PopulateInitialData first");
                }
            }
            PendingMD5AndFileId existingFileId;
            lock (storageKeyToFile)
            {
                if (!storageKeyToFile.TryGetValue(storageKey, out existingFileId))
                {
                    throw new KeyNotFoundException("Could not find fileId for storageKey: " + (storageKey ?? "{null}"));
                }
                else if (existingFileId.Pending)
                {
                    throw new Exception("Cannot read from Pending file");
                }

                if (storageKey != EmptyFileStorageKey)
                {
                    lock (existingFileId.SyncRoot)
                    {
                        existingFileId.IncrementDownloadingCount();
                    }
                }
            }

            if (storageKey == EmptyFileStorageKey)
            {
                return new MemoryStream();
            }
            else
            {
                return new StreamHolderWithDisposalAction(new FileStream(storageFolder + "\\" + existingFileId.FileId.ToString(),
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read),
                    new KeyValuePair<Action<object>, object>(state =>
                    {
                        PendingMD5AndFileId castState = (PendingMD5AndFileId)state;
                        lock (castState.SyncRoot)
                        {
                            castState.DecrementDownloadingCount();
                        }
                    }, existingFileId));
            }
        }

        public bool MoveUserPathUsage(int userId, string storageKey, FilePath oldUserRelativePath, FilePath newUserRelativePath)
        {
            if (oldUserRelativePath == null)
            {
                throw new NullReferenceException("oldUserRelativePath cannot be null");
            }
            if (newUserRelativePath == null)
            {
                throw new NullReferenceException("newUserRelativePath cannot be null");
            }

            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("Not Initialized, call PopulateInitialData first");
                }
            }
            lock (storageKeyToFile)
            {
                PendingMD5AndFileId existingUsages;
                if (storageKeyToFile.TryGetValue(storageKey, out existingUsages)
                    && existingUsages.MoveUserUsage(userId, oldUserRelativePath, newUserRelativePath))
                {
                    FileIdAndUser idUser = new FileIdAndUser(existingUsages.FileId, userId);
                    UpdateQueuedData updateQueue;
                    if (UpdateFileIdToQueuedData.TryGetValue(idUser, out updateQueue))
                    {
                        updateQueue.UserRelativePath = newUserRelativePath;
                    }

                    Dictionary<FilePath, LinkedList<KeyValuePair<long, byte[]>>> userHistory;
                    if (userToRevisionHistory.TryGetValue(userId, out userHistory))
                    {
                        LinkedList<KeyValuePair<long, byte[]>> previousRevisions;
                        if (userHistory.TryGetValue(oldUserRelativePath, out previousRevisions))
                        {
                            userHistory.Remove(oldUserRelativePath);
                            userHistory[newUserRelativePath] = previousRevisions;
                        }
                    }

                    return true;
                }

                return false;
            }
        }
    }
}