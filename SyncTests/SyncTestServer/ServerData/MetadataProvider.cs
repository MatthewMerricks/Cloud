using CloudApiPublic.Model;
using SyncTestServer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncTestServer
{
    public class MetadataProvider : IMetadataProvider
    {
        private readonly Dictionary<int, FilePathDictionary<FileMetadata>> UserMetadata = new Dictionary<int, FilePathDictionary<FileMetadata>>();

        private IServerStorage Storage = null;
        private readonly GenericHolder<bool> Initialized = new GenericHolder<bool>(false);

        public void InitializeProvider(IServerStorage storage, IEnumerable<InitialMetadata> initialData = null)
        {
            if (storage == null)
            {
                throw new NullReferenceException("storage cannot be null");
            }

            lock (Initialized)
            {
                if (Initialized.Value)
                {
                    throw new Exception("MetadataProvider already initialized");
                }
                this.Storage = storage;

                if (initialData != null)
                {
                    this.Storage.InitializeStorage(initialData.Where(currentMetadata => !currentMetadata.Metadata.HashableProperties.IsFolder)
                        .Select(currentMetadata => new InitialStorageData(currentMetadata.UserId,
                            currentMetadata.Metadata.StorageKey,
                            currentMetadata.MD5,
                            (long)currentMetadata.Metadata.HashableProperties.Size,
                            currentMetadata.RelativePath)));

                    foreach (InitialMetadata currentMetadata in initialData)
                    {
                        FilePathDictionary<FileMetadata> existingUser;
                        if (!UserMetadata.TryGetValue(currentMetadata.UserId, out existingUser))
                        {
                            SetupNewUser(currentMetadata.UserId, out existingUser);
                        }
                        existingUser[currentMetadata.RelativePath] = currentMetadata.Metadata;
                    }
                }
                else
                {
                    this.Storage.InitializeStorage();
                }

                Initialized.Value = true;
            }
        }

        public bool AddFolderMetadata(int userId, FilePath relativePathFromRoot, FileMetadata metadata)
        {
            bool isPending;
            bool newUpload;
            return AddMetadata(userId, Guid.Empty, relativePathFromRoot, metadata, out isPending, out newUpload);
        }

        public bool AddFileMetadata(int userId, Guid deviceId, FilePath relativePathFromRoot, FileMetadata metadata, out bool isPending, out bool newUpload, byte[] MD5)
        {
            return AddMetadata(userId, deviceId, relativePathFromRoot, metadata, out isPending, out newUpload, MD5);
        }

        private bool AddMetadata(int userId, Guid deviceId, FilePath relativePathFromRoot, FileMetadata metadata, out bool isPending, out bool newUpload, byte[] MD5 = null)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("MetadataProvider is not initialized");
                }
            }

            if (relativePathFromRoot == null)
            {
                throw new NullReferenceException("relativePathFromRoot cannot be null");
            }
            if (metadata == null)
            {
                throw new NullReferenceException("metadata cannot be null");
            }
            if (metadata.HashableProperties.IsFolder)
            {
                if(MD5 != null)
                {
                    throw new ArgumentException("MD5 should be null if metadata HashashableProperties IsFolder is true");
                }
                if (metadata.HashableProperties.Size != null)
                {
                    throw new ArgumentException("metadata HashableProperties Size should be null if metadata HashashableProperties IsFolder is true");
                }
            }
            else if (MD5 == null
                || MD5.Length != 16)
            {
                throw new ArgumentException("MD5 must be a 16-length byte array if metadata HashashableProperties IsFolder is false");
            }
            else if (metadata.HashableProperties.Size == null)
            {
                throw new NullReferenceException("metadata HashableProperties Size cannot be null if metadata HashableProperties IsFolder is false");
            }

            FilePathDictionary<FileMetadata> foundMetadata;
            lock (UserMetadata)
            {
                if (!UserMetadata.TryGetValue(userId, out foundMetadata))
                {
                    SetupNewUser(userId, out foundMetadata);
                }
            }

            lock (foundMetadata)
            {
                FileMetadata previousMetadata;
                if (foundMetadata.TryGetValue(relativePathFromRoot, out previousMetadata))
                {
                    isPending = Storage.IsStorageKeyPending(previousMetadata.StorageKey);
                    newUpload = false;
                    return false;
                }
                foundMetadata.Add(relativePathFromRoot, metadata);

                if (!metadata.HashableProperties.IsFolder)
                {
                    string addedStorageKey;
                    Storage.AddUserFile(userId,
                        deviceId,
                        relativePathFromRoot,
                        MD5,
                        (long)metadata.HashableProperties.Size,
                        out addedStorageKey,
                        out isPending,
                        out newUpload);
                    metadata.StorageKey = addedStorageKey;
                }
                else
                {
                    isPending = false;
                    newUpload = false;
                }

                return true;
            }
        }

        private void SetupNewUser(int userId, out FilePathDictionary<FileMetadata> newMetadata)
        {
            RecursiveFilePathDictionaryHandlers<FileMetadata, int> recursiveCallbacks = new RecursiveFilePathDictionaryHandlers<FileMetadata, int>(userId);
            recursiveCallbacks.RecursedDelete += (object sender, RecursiveDeleteArgs<FileMetadata, int> e) =>
            {
                if (e.DeletedValue != null
                    && !e.DeletedValue.HashableProperties.IsFolder)
                {
                    Storage.RemoveUserUsageFromFile(e.DeletedValue.StorageKey, e.UserState, e.DeletedPath);
                }
            };
            recursiveCallbacks.RecursedRename += (object sender, RecursiveRenameArgs<FileMetadata, int> e) =>
            {
                if (e.MovedValue != null
                    && !e.MovedValue.HashableProperties.IsFolder)
                {
                    Storage.MoveUserPathUsage(e.UserState, e.MovedValue.StorageKey, e.DeletedFromPath, e.AddedToPath);
                }
            };

            CLError errorCreatingMetadata = FilePathDictionary<FileMetadata>.CreateAndInitialize(new FilePath(string.Empty),
                out newMetadata,
                recursiveCallbacks.RecursiveDeleteCallback,
                recursiveCallbacks.RecursiveRenameCallback);
            if (errorCreatingMetadata != null)
            {
                throw new AggregateException("Error creating new FilePathDictionary<FileMetadata>", errorCreatingMetadata.GrabExceptions());
            }
            UserMetadata.Add(userId, newMetadata);
        }

        public bool TryGetMetadata(int userId, FilePath relativePathFromRoot, out FileMetadata metadata)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("MetadataProvider is not initialized");
                }
            }

            if (relativePathFromRoot == null)
            {
                throw new NullReferenceException("relativePathFromRoot cannot be null");
            }

            FilePathDictionary<FileMetadata> foundMetadata;
            lock (UserMetadata)
            {
                if (!UserMetadata.TryGetValue(userId, out foundMetadata))
                {
                    metadata = null;
                    return false;
                }
            }

            lock (foundMetadata)
            {
                return foundMetadata.TryGetValue(relativePathFromRoot, out metadata);
            }
        }

        public bool RecursivelyRemoveMetadata(int userId, FilePath relativePathFromRoot)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("MetadataProvider is not initialized");
                }
            }

            FilePathDictionary<FileMetadata> foundMetadata;
            lock (UserMetadata)
            {
                if (!UserMetadata.TryGetValue(userId, out foundMetadata))
                {
                    return false;
                }
            }

            lock (foundMetadata)
            {
                FileMetadata removeMetadata;
                if (foundMetadata.TryGetValue(relativePathFromRoot, out removeMetadata)
                    && foundMetadata.Remove(relativePathFromRoot))
                {
                    Storage.RemoveUserUsageFromFile(removeMetadata.StorageKey,
                        userId,
                        relativePathFromRoot);
                    return true;
                }
                return false;
            }
        }

        public bool RecursivelyRenameMetadata(int userId, FilePath relativePathFrom, FilePath relativePathTo)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("MetadataProvider is not initialized");
                }
            }

            FilePathDictionary<FileMetadata> foundMetadata;
            lock (UserMetadata)
            {
                if (!UserMetadata.TryGetValue(userId, out foundMetadata))
                {
                    return false;
                }
            }

            lock (foundMetadata)
            {
                CLError moveMetadataError;
                FileMetadata movedMetadata;
                if (foundMetadata.TryGetValue(relativePathFrom, out movedMetadata)
                    && (moveMetadataError = foundMetadata.Rename(relativePathFrom, relativePathTo)) == null)
                {
                    if (!movedMetadata.HashableProperties.IsFolder)
                    {
                        return Storage.MoveUserPathUsage(userId,
                            movedMetadata.StorageKey,
                            relativePathFrom,
                            relativePathTo);
                    }
                    return true;
                }
                return false;
            }
        }

        public bool UpdateMetadata(int userId, Guid deviceId, string revision, FilePath relativePathFromRoot, FileMetadata metadata, out bool isPending, out bool newUpload, out bool conflict, byte[] MD5 = null)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("MetadataProvider is not initialized");
                }
            }
            
            if (metadata.HashableProperties.IsFolder)
            {
                if(MD5 != null)
                {
                    throw new ArgumentException("MD5 should be null if metadata HashashableProperties IsFolder is true");
                }
                if (metadata.HashableProperties.Size != null)
                {
                    throw new ArgumentException("metadata HashableProperties Size should be null if metadata HashashableProperties IsFolder is true");
                }
            }
            else if (MD5 == null
                || MD5.Length != 16)
            {
                throw new ArgumentException("MD5 must be a 16-length byte array if metadata HashashableProperties IsFolder is false");
            }
            else if (metadata.Revision == null
                || metadata.Revision.Length != 32)
            {
                isPending = false;
                newUpload = false;
                conflict = true;
                return false;
            }

            FilePathDictionary<FileMetadata> foundMetadata;
            lock (UserMetadata)
            {
                if (!UserMetadata.TryGetValue(userId, out foundMetadata))
                {
                    isPending = false;
                    newUpload = false;
                    conflict = false;
                    return false;
                }
            }

            lock (foundMetadata)
            {
                FileMetadata previousMetadata;
                if (!foundMetadata.TryGetValue(relativePathFromRoot, out previousMetadata))
                {
                    isPending = false;
                    newUpload = false;
                    conflict = false;
                    return false;
                }

                if (metadata.HashableProperties.IsFolder)
                {
                    isPending = false;
                    newUpload = false;
                    conflict = false;
                    foundMetadata[relativePathFromRoot] = metadata;
                }
                else
                {
                    byte[] oldMD5 = Enumerable.Range(0, 32)
                        .Where(currentHex => currentHex % 2 == 0)
                        .Select(currentHex => Convert.ToByte(previousMetadata.Revision.Substring(currentHex, 2), 16))
                        .ToArray();

                    string storageKey;
                    byte[] latestMD5;
                    if (Storage.UpdateFileUsage(userId,
                        deviceId,
                        relativePathFromRoot,
                        oldMD5,
                        MD5,
                        (long)metadata.HashableProperties.Size,
                        out isPending,
                        out newUpload,
                        out storageKey,
                        out latestMD5))
                    {
                        conflict = true;
                    }
                    else
                    {
                        metadata.StorageKey = storageKey;
                        conflict = false;
                        foundMetadata[relativePathFromRoot] = metadata;
                    }
                    metadata.Revision = latestMD5
                        .Select(md5Byte => string.Format("{0:x2}", md5Byte))
                        .Aggregate((previousBytes, newByte) => previousBytes + newByte);
                }
                return true;
            }
        }
    }
}