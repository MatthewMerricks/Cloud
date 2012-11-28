//
// PhysicalStorage.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using SyncTestServer.Model;
using SyncTestServer.Static;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace SyncTestServer
{
    public sealed class PhysicalStorage : IServerStorage
    {
        private const string AppendTempName = "temp";
        private const string EmptyFileStorageKey = "0";

        private readonly Dictionary<string, PendingMD5AndFileId> storageKeyToFile = new Dictionary<string, PendingMD5AndFileId>();
        private readonly Dictionary<int, Dictionary<FilePath, LinkedList<MD5AndFileSize>>> userToRevisionHistory = new Dictionary<int, Dictionary<FilePath, LinkedList<MD5AndFileSize>>>();
        private readonly Dictionary<MD5AndFileSize, string> hashToStorageKey = new Dictionary<MD5AndFileSize, string>(MD5AndFileSize.Comparer);
        private readonly Dictionary<string, MD5AndFileSize> storageKeyToHash = new Dictionary<string, MD5AndFileSize>();
        private readonly Dictionary<int, Dictionary<Guid, Dictionary<string, KeyValuePair<Action<object>, object>>>> userPendingStorageKeys = new Dictionary<int, Dictionary<Guid, Dictionary<string, KeyValuePair<Action<object>, object>>>>();
        private readonly string storageFolder;
        private readonly string storageFolderTemp;
        private readonly DirectoryInfo storageInfo;
        private static int storageKeyCounter = 0;
        private static string GetNewStorageKey()
        {
            return Interlocked.Increment(ref storageKeyCounter).ToString();
        }
        private readonly GenericHolder<bool> Initialized = new GenericHolder<bool>(false);

        private Action<string> storageKeyNoLongerPending = null;

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

        public void InitializeStorage(Action<string> storageKeyNoLongerPending, IEnumerable<InitialStorageData> initialData = null)
        {
            if (storageKeyNoLongerPending == null)
            {
                throw new NullReferenceException("storageKeyNoLongerPending cannot be null");
            }

            lock (Initialized)
            {
                if (Initialized.Value)
                {
                    throw new Exception("Already Initialized");
                }

                this.storageKeyNoLongerPending = storageKeyNoLongerPending;

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
                        Dictionary<FilePath, LinkedList<MD5AndFileSize>> userHistory;
                        if (!userToRevisionHistory.TryGetValue(currentData.UserId, out userHistory))
                        {
                            userHistory = new Dictionary<FilePath, LinkedList<MD5AndFileSize>>(FilePathComparer.Instance);
                            userToRevisionHistory.Add(currentData.UserId, userHistory);
                        }
                        if (userHistory.ContainsKey(currentData.RelativePath))
                        {
                            throw new NotImplementedException("Multiple files provided with the same relative path: " + (currentData.RelativePath == null ? "{null}" : currentData.RelativePath.ToString()));
                        }

                        userHistory.Add(currentData.RelativePath,
                            new LinkedList<MD5AndFileSize>(new[] { new MD5AndFileSize(currentData.MD5, currentData.FileSize) }));
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

                    Dictionary<FilePath, LinkedList<MD5AndFileSize>> userHistory;
                    if (!userToRevisionHistory.TryGetValue(userId, out userHistory))
                    {
                        userHistory = new Dictionary<FilePath, LinkedList<MD5AndFileSize>>(FilePathComparer.Instance);
                        userToRevisionHistory.Add(userId, userHistory);
                    }
                    if (!userHistory.ContainsKey(userRelativePath))
                    {
                        userHistory.Add(userRelativePath,
                            new LinkedList<MD5AndFileSize>(new[] { new MD5AndFileSize(MD5, fileSize) }));
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
                Dictionary<FilePath, LinkedList<MD5AndFileSize>> userRevisions;
                LinkedList<MD5AndFileSize> fileRevisions;
                GenericHolder<Nullable<MD5AndFileSize>> firstRevision = new GenericHolder<Nullable<MD5AndFileSize>>(null);
                MD5AndFileSize previousHash;
                string previousStorageKey;
                PendingMD5AndFileId previousFile;
                lastestMD5 = null;
                Dictionary<Guid, Dictionary<string, KeyValuePair<Action<object>, object>>> userPendings;
                if (oldMD5 != null
                    && oldMD5.Length == 16
                    && userToRevisionHistory.TryGetValue(userId, out userRevisions)
                    && userRevisions.TryGetValue(userRelativePath, out fileRevisions)
                    && fileRevisions.Count > 0
                    && (lastestMD5 = (((MD5AndFileSize)RetrieveOrPullFirstRevision(fileRevisions, firstRevision)).MD5)) != null
                    && hashToStorageKey.TryGetValue(previousHash = new MD5AndFileSize(((MD5AndFileSize)RetrieveOrPullFirstRevision(fileRevisions, firstRevision)).MD5, ((MD5AndFileSize)RetrieveOrPullFirstRevision(fileRevisions, firstRevision)).FileSize), out previousStorageKey)
                    && storageKeyToFile.TryGetValue(previousStorageKey, out previousFile)
                    && previousFile.GetUserUsagesByUser(userId).Contains(userRelativePath, FilePathComparer.Instance)
                    && SyncTestServer.Static.NativeMethods.memcmp(((MD5AndFileSize)RetrieveOrPullFirstRevision(fileRevisions, firstRevision)).MD5,
                        oldMD5,
                        new UIntPtr((uint)16)) == 0
                    && !(userPendingStorageKeys.TryGetValue(userId, out userPendings)
                        && userPendings.Values.Any(userPending => userPending.ContainsKey(previousStorageKey))))
                {
                    AddUserFile(userId, deviceId, userRelativePath, newMD5, newFileSize, out newStorageKey, out filePending, out newUpload, new KeyValuePair<string, PendingMD5AndFileId>(previousStorageKey, previousFile));

                    fileRevisions.AddFirst(new MD5AndFileSize(newMD5, newFileSize));

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

        private static Nullable<MD5AndFileSize> RetrieveOrPullFirstRevision(LinkedList<MD5AndFileSize> allRevisions, GenericHolder<Nullable<MD5AndFileSize>> alreadyPulledRevision)
        {
            if (alreadyPulledRevision.Value == null)
            {
                alreadyPulledRevision.Value = allRevisions.First();
            }
            return alreadyPulledRevision.Value;
        }

        public bool WriteFile(Stream toWrite, string storageKey, long contentLength, byte[] MD5, int userId, bool disposeStreamAfterWrite = true)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("Not Initialized, call PopulateInitialData first");
                }
            }
            if (contentLength < 0)
            {
                throw new ArgumentException("contentLength cannot negative");
            }
            if (MD5 == null
                || MD5.Length != 16)
            {
                throw new ArgumentException("contentMD5 must be a 16-length byte array");
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
                if (!storageKeyToFile.TryGetValue(storageKey, out existingFileId)
                    || NativeMethods.memcmp(existingFileId.MD5, MD5, new UIntPtr((uint)16)) != 0
                    || existingFileId.GetUserUsagesByUser(userId).Count() == 0)
                {
                    // Behavior of existing server is to the appear to the client that the upload worked correctly when it didn't

                    byte[] deadBuffer = new byte[CloudApiPublic.Static.FileConstants.BufferSize];
                    int deadRead = 0;
                    do
                    {
                        deadRead = toWrite.Read(deadBuffer, 0, deadBuffer.Length);
                    } while (deadRead > 0);
                    if (disposeStreamAfterWrite)
                    {
                        toWrite.Dispose();
                    }
                    return true;

                    ////Server does not have an error with a bad upload, so do not throw exception
                    ////Also, this exception only describes one of the cases that would reach this code
                    //throw new KeyNotFoundException("Could not find fileId for storageKey: " + (storageKey ?? "{null}"));
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

                global::System.Security.Cryptography.MD5 md5Hasher = global::System.Security.Cryptography.MD5.Create();

                long countFileSize = 0;

                using (FileStream storageFileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] data = new byte[CloudApiPublic.Static.FileConstants.BufferSize];
                    byte[] hashOut = new byte[CloudApiPublic.Static.FileConstants.BufferSize];
                    int read;
                    while ((read = toWrite.Read(data, 0, data.Length)) > 0)
                    {
                        countFileSize += read;
                        storageFileStream.Write(data, 0, read);
                        md5Hasher.TransformBlock(data, 0, read, hashOut, 0);
                    }

                    md5Hasher.TransformFinalBlock(CloudApiPublic.Static.FileConstants.EmptyBuffer, 0, 0);
                    storageFileStream.Flush();
                }

                if (countFileSize != contentLength
                    || NativeMethods.memcmp(md5Hasher.Hash, MD5, new UIntPtr((uint)16)) != 0)
                {
                    // Behavior of existing server is to the appear to the client that the upload worked correctly when it didn't

                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch
                    {
                    }

                    if (disposeStreamAfterWrite)
                    {
                        toWrite.Dispose();
                    }
                    return true;
                }

                List<string> storageKeysNoLongerPending = new List<string>();

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
                            storageKeyToFile[storageKey] = existingFileId;
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
                                        if (pendingDeviceKeys.Value.Remove(storageKey))
                                        {
                                            storageKeysNoLongerPending.Add(storageKey);
                                        }

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

                if (storageKeysNoLongerPending.Count > 0)
                {
                    ThreadPool.QueueUserWorkItem(noLongerPendingState =>
                        {
                            KeyValuePair<List<string>, Action<string>> castNoLongerPending = (KeyValuePair<List<string>, Action<string>>)noLongerPendingState;
                            foreach (string currentNoLongerPending in castNoLongerPending.Key)
                            {
                                castNoLongerPending.Value(currentNoLongerPending);
                            }
                        }, new KeyValuePair<List<string>, Action<string>>(storageKeysNoLongerPending, storageKeyNoLongerPending));
                }
            }

            if (disposeStreamAfterWrite)
            {
                toWrite.Dispose();
            }

            return needsUpload;
        }

        public bool RemoveUserUsageFromFile(string storageKey, int userId, FilePath userRelativePath)
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

            bool fileRemoved = false;

            lock (storageKeyToFile)
            {
                PendingMD5AndFileId fileId;
                if (storageKeyToFile.TryGetValue(storageKey, out fileId))
                {
                    fileRemoved = RemoveFileById(storageKey, userId, userRelativePath, fileId);

                    UpdateFileIdToQueuedData.Remove(new FileIdAndUser(fileId.FileId, userId));

                    Dictionary<FilePath, LinkedList<MD5AndFileSize>> userHistory;
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

            return fileRemoved;
        }

        private bool RemoveFileById(string storageKey, int userId, FilePath userRelativePath, PendingMD5AndFileId fileId)
        {
            bool fileRemoved;

            lock (storageKeyToFile)
            {
                lock (fileId.SyncRoot)
                {
                    fileRemoved = fileId.RemoveUserUsage(userId, userRelativePath);

                    if (fileRemoved)
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
                            if (pendingDeviceKeys.Value.Remove(storageKey))
                            {
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

                    if ((!fileId.IsValid || !fileId.Pending)
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

            return fileRemoved;
        }

        public Stream ReadFile(string storageKey, int userId, out long fileSize)
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
                    fileSize = 0;
                    return null;
                    //throw new KeyNotFoundException("Could not find fileId for storageKey: " + (storageKey ?? "{null}"));
                }
                else if (existingFileId.Pending)
                {
                    fileSize = 0;
                    return null;
                    //throw new Exception("Cannot read from Pending file");
                }
                else if (existingFileId.GetUserUsagesByUser(userId).Count() == 0)
                {
                    fileSize = 0;
                    return null;
                    //throw new Exception("storageKey does not belong to userId");
                }

                MD5AndFileSize fileHash;
                if (!storageKeyToHash.TryGetValue(storageKey, out fileHash))
                {
                    fileSize = 0;
                    return null;
                }

                fileSize = fileHash.FileSize;

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

                    Dictionary<FilePath, LinkedList<MD5AndFileSize>> userHistory;
                    if (userToRevisionHistory.TryGetValue(userId, out userHistory))
                    {
                        LinkedList<MD5AndFileSize> previousRevisions;
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

        //public bool[] CheckPending(int userId, string[] storageKeys)
        //{
        //    if (storageKeys == null)
        //    {
        //        throw new NullReferenceException("storageKeys cannot be null");
        //    }

        //    lock (Initialized)
        //    {
        //        if (!Initialized.Value)
        //        {
        //            throw new Exception("Not Initialized, call PopulateInitialData first");
        //        }
        //    }

        //    if (storageKeys.Length == 0)
        //    {
        //        return new bool[0];
        //    }

        //    lock (storageKeyToFile)
        //    {
        //        bool[] toReturn = new bool[storageKeys.Length];

        //        Dictionary<Guid, Dictionary<string, KeyValuePair<Action<object>, object>>> userPendings;
        //        if (userPendingStorageKeys.TryGetValue(userId, out userPendings))
        //        {
        //            Dictionary<string, IEnumerable<int>> storageKeyIndexes = storageKeys.Select((currentStorageKey, keyIndex) => new KeyValuePair<string, int>(currentStorageKey, keyIndex))
        //                .GroupBy(currentStorageKey => currentStorageKey.Key)
        //                .ToDictionary(currentStorageKey => currentStorageKey.Key, currentStorageKey => currentStorageKey.Select(storageKeyIndex => storageKeyIndex.Value));

        //            foreach (string userPendingStorageKey in userPendings.SelectMany(currentUserPending => currentUserPending.Value.Keys))
        //            {
        //                IEnumerable<int> pendingIndexes;
        //                if (storageKeyIndexes.TryGetValue(userPendingStorageKey, out pendingIndexes))
        //                {
        //                    foreach (int pendingIndex in pendingIndexes)
        //                    {
        //                        toReturn[pendingIndex] = true;
        //                    }
        //                }
        //            }
        //        }

        //        return toReturn;
        //    }
        //}

        public IEnumerable<PurgedFile> PurgeUserPendingsByDevice(int userId, Guid deviceId)
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
                Dictionary<Guid, Dictionary<string, KeyValuePair<Action<object>, object>>> userPendings;
                Dictionary<string, KeyValuePair<Action<object>, object>> devicePendings;
                if (userPendingStorageKeys.TryGetValue(userId, out userPendings)
                    && userPendings.TryGetValue(deviceId, out devicePendings))
                {
                    //KeyValuePair<string, KeyValuePair<Action<object>, object>>[] copiedStorageKeys = devicePendings.ToArray();
                    userPendings.Remove(deviceId);
                    if (userPendings.Count == 0)
                    {
                        userPendingStorageKeys.Remove(userId);
                    }

                    List<PurgedFile> toReturn = new List<PurgedFile>();

                    foreach (KeyValuePair<string, KeyValuePair<Action<object>, object>> currentCopiedKey in devicePendings)
                    {
                        long fileSize;
                        MD5AndFileSize fileHash;
                        Nullable<MD5AndFileSize> nullableFileHash;
                        byte[] fileMD5;
                        if (storageKeyToHash.TryGetValue(currentCopiedKey.Key, out fileHash))
                        {
                            fileSize = fileHash.FileSize;
                            fileMD5 = fileHash.MD5;
                            nullableFileHash = fileHash;
                        }
                        else
                        {
                            fileSize = -1;
                            fileMD5 = null;
                            nullableFileHash = null;
                        }

                        PendingMD5AndFileId fileId;
                        if (storageKeyToFile.TryGetValue(currentCopiedKey.Key, out fileId))
                        {
                            if (fileMD5 == null)
                            {
                                fileMD5 = fileId.MD5;
                            }

                            IEnumerable<FilePath> userUsages = fileId.GetUserUsagesByUser(userId);
                            if (userUsages == null
                                || userUsages.Count() == 0)
                            {
                                toReturn.Add(new PurgedFile(true, currentCopiedKey.Key, fileSize, fileMD5, null));
                            }
                            else
                            {
                                Func<string, int, FilePath, long, byte[], PurgedFile> removeUsageAndReturnPurgedFile = (storageKey, innerUserId, userRelativePath, innerFileSize, innerFileMD5) =>
                                    {
                                        return new PurgedFile(RemoveUserUsageFromFile(storageKey, innerUserId, userRelativePath),
                                            storageKey, innerFileSize, innerFileMD5, userRelativePath);
                                    };

                                toReturn.AddRange(userUsages.Select(userUsage => removeUsageAndReturnPurgedFile(currentCopiedKey.Key, userId, userUsage, fileSize, fileMD5)));
                            }
                        }
                        else
                        {
                            toReturn.Add(new PurgedFile(true, currentCopiedKey.Key, fileSize, fileMD5, null));
                        }
                    }

                    return toReturn;
                }
                else
                {
                    return Enumerable.Empty<PurgedFile>();
                }
            }
        }

        public bool DoesFileHaveEarlierRevisionOfUndeletedFile(long syncId, int userId, FilePath userRelativePath, long fileSize, byte[] MD5, out byte[] latestMD5)
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
            if (fileSize < 0)
            {
                throw new ArgumentException("fileSize cannot be negative");
            }

            lock (storageKeyToFile)
            {
                Dictionary<FilePath, LinkedList<MD5AndFileSize>> usersHistory;
                if (!userToRevisionHistory.TryGetValue(userId, out usersHistory))
                {
                    latestMD5 = null;
                    return false;
                }

                LinkedList<MD5AndFileSize> fileHistory;
                if (!usersHistory.TryGetValue(userRelativePath, out fileHistory))
                {
                    latestMD5 = null;
                    return false;
                }

                foreach (MD5AndFileSize currentRevision in fileHistory)
                {
                    if (currentRevision.FileSize == fileSize
                            && NativeMethods.memcmp(currentRevision.MD5, MD5, new UIntPtr((uint)16)) == 0)
                    {
                        string storageKey;
                        if (!hashToStorageKey.TryGetValue(currentRevision, out storageKey))
                        {
                            latestMD5 = null;
                            return false;
                        }

                        PendingMD5AndFileId fileId;
                        if (!storageKeyToFile.TryGetValue(storageKey, out fileId))
                        {
                            latestMD5 = null;
                            return false;
                        }

                        latestMD5 = fileHistory.First.Value.MD5;
                        return true;
                    }
                }

                latestMD5 = null;
                return false;
            }
        }
    }
}