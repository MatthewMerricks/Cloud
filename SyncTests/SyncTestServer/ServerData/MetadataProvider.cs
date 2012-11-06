using CloudApiPublic.Model;
using SyncTestServer.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SyncTestServer
{
    public class MetadataProvider : IMetadataProvider
    {
        private readonly Dictionary<int, FilePathDictionary<FileMetadata>> UserMetadata = new Dictionary<int, FilePathDictionary<FileMetadata>>();
        private readonly Dictionary<int, List<UserEvent>> UserEvents = new Dictionary<int, List<UserEvent>>();
        private readonly Dictionary<string, Dictionary<int, List<UserEvent>>> PendingStorageKeyUsage = new Dictionary<string, Dictionary<int, List<UserEvent>>>();
        private readonly Dictionary<int, Dictionary<Guid, HashSet<string>>> CurrentlyQueryingUserEvents = new Dictionary<int, Dictionary<Guid, HashSet<string>>>();

        private IServerStorage Storage = null;
        private readonly GenericHolder<bool> Initialized = new GenericHolder<bool>(false);
        private Action UserWasNotLockedDetected = null;

        public long NewSyncIdBeforeStart
        {
            get
            {
                return Interlocked.Increment(ref lastSyncId);
            }
        }
        private long lastSyncId = 0;

        public void InitializeProvider(IServerStorage storage, IEnumerable<InitialMetadata> initialData = null, Action userWasNotLockedDetected = null)
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

                this.UserWasNotLockedDetected = userWasNotLockedDetected;

                if (initialData != null)
                {
                    long initialSyncId = NewSyncIdBeforeStart;

                    this.Storage.InitializeStorage(StorageKeyNoLongerPending,
                        initialData.Where(currentMetadata => !currentMetadata.Metadata.HashableProperties.IsFolder)
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

                        List<UserEvent> currentUserEvents;
                        if (!UserEvents.TryGetValue(currentMetadata.UserId, out currentUserEvents))
                        {
                            UserEvents.Add(currentMetadata.UserId,
                                currentUserEvents = new List<UserEvent>());
                        }
                        currentUserEvents.Add(new UserEvent(initialSyncId, new FileChange()
                        {
                            NewPath = currentMetadata.RelativePath,
                            Type = CloudApiPublic.Static.FileChangeType.Created,
                            Metadata = currentMetadata.Metadata
                        }, null));
                    }
                }
                else
                {
                    this.Storage.InitializeStorage(StorageKeyNoLongerPending);
                }

                Initialized.Value = true;
            }
        }

        private void StorageKeyNoLongerPending(string storageKey)
        {
            Dictionary<int, List<UserEvent>> currentKeyUsage;

            lock (PendingStorageKeyUsage)
            {
                if (!PendingStorageKeyUsage.TryGetValue(storageKey, out currentKeyUsage))
                {
                    return;
                }
            }

            Dictionary<int, FilePathDictionary<FileMetadata>> storageKeyUserMetadatas = new Dictionary<int, FilePathDictionary<FileMetadata>>();

            lock (UserMetadata)
            {
                lock (currentKeyUsage)
                {
                    foreach (KeyValuePair<int, List<UserEvent>> currentUserUsage in currentKeyUsage)
                    {
                        FilePathDictionary<FileMetadata> currentMetadataDictionary;
                        if (UserMetadata.TryGetValue(currentUserUsage.Key, out currentMetadataDictionary))
                        {
                            storageKeyUserMetadatas.Add(currentUserUsage.Key, currentMetadataDictionary);
                        }
                    }
                }
            }

            lock (currentKeyUsage)
            {
                foreach (KeyValuePair<int, List<UserEvent>> currentUserUsage in currentKeyUsage)
                {
                    FilePathDictionary<FileMetadata> currentUserMetadata;

                    bool emptyMetadata = !storageKeyUserMetadatas.TryGetValue(currentUserUsage.Key, out currentUserMetadata);
                    if (emptyMetadata)
                    {
                        CLError emptyMetadataError = FilePathDictionary<FileMetadata>.CreateAndInitialize(new FilePath(string.Empty),
                            out currentUserMetadata);
                    }

                    lock (currentUserMetadata)
                    {
                        List<UserEvent> currentUserEvents;
                        bool emptyUserEvents;
                        lock (UserEvents)
                        {
                            emptyUserEvents = !UserEvents.TryGetValue(currentUserUsage.Key, out currentUserEvents);
                            if (emptyUserEvents)
                            {
                                currentUserEvents = new List<UserEvent>();
                            }
                        }

                        lock (currentUserEvents)
                        {
                            lock (CurrentlyQueryingUserEvents)
                            {
                                Dictionary<Guid, HashSet<string>> simultaneousUserQueries;
                                if (CurrentlyQueryingUserEvents.TryGetValue(currentUserUsage.Key, out simultaneousUserQueries))
                                {
                                    foreach (HashSet<string> simultaneousUserQuery in simultaneousUserQueries.Values)
                                    {
                                        simultaneousUserQuery.Add(storageKey);
                                    }
                                }
                            }

                            if (!emptyUserEvents)
                            {
                                long maxSyncIdPlusOne;
                                if (currentUserEvents.Count == 0)
                                {
                                    maxSyncIdPlusOne = 1;
                                }
                                else
                                {
                                    maxSyncIdPlusOne = currentUserEvents[currentUserEvents.Count - 1].SyncId + ((long)1);
                                }

                                foreach (UserEvent completedUploadEvent in currentUserUsage.Value)
                                {
                                    int numberPreviousEvents;
                                    int binaryIndexOneGreater = currentUserEvents.BinarySearch(new UserEvent(completedUploadEvent.SyncId + ((long)1), null, null),
                                        UserEvent.SyncIdComparer);
                                    if (binaryIndexOneGreater < 0)
                                    {
                                        numberPreviousEvents = ~binaryIndexOneGreater;
                                    }
                                    else
                                    {
                                        numberPreviousEvents = binaryIndexOneGreater;
                                        while (numberPreviousEvents > 0)
                                        {
                                            if (currentUserEvents[numberPreviousEvents - 1].SyncId <= completedUploadEvent.SyncId)
                                            {
                                                break;
                                            }
                                            numberPreviousEvents--;
                                        }
                                    }

                                    for (int userAllEventsIndex = numberPreviousEvents; userAllEventsIndex < currentUserEvents.Count; userAllEventsIndex++)
                                    {
                                        UserEvent checkCurrentEvent = currentUserEvents[userAllEventsIndex];
                                        if (checkCurrentEvent.SyncId > completedUploadEvent.SyncId)
                                        {
                                            break;
                                        }
                                        else if (checkCurrentEvent.FileChange.InMemoryId == completedUploadEvent.FileChange.InMemoryId)
                                        {
                                            FilePathDictionary<object> trackEventPath;
                                            CLError createTrackEventPath = FilePathDictionary<object>.CreateAndInitialize(new FilePath(string.Empty), out trackEventPath);
                                            if (createTrackEventPath != null)
                                            {
                                                throw new AggregateException("Error creating trackEventPath", createTrackEventPath.GrabExceptions());
                                            }

                                            trackEventPath.Add(checkCurrentEvent.FileChange.NewPath, new object());

                                            List<int> removedRenames = new List<int>();

                                            bool breakAfterCurrentSubsequentEvent = false;

                                            foreach (KeyValuePair<UserEvent, int> subsequentEvent in currentUserEvents
                                                .Skip(userAllEventsIndex)
                                                .Select((currentEvent, currentIndex) => new KeyValuePair<UserEvent, int>(currentEvent, currentIndex + userAllEventsIndex)))
                                            {
                                                switch (subsequentEvent.Key.FileChange.Type)
                                                {
                                                    // other creations or modifications should not affect the path of the current user event,
                                                    // but a later revision of the same file may have completed instead
                                                    case CloudApiPublic.Static.FileChangeType.Created:
                                                    case CloudApiPublic.Static.FileChangeType.Modified:
                                                        KeyValuePair<FilePath, object> finalEventPath = trackEventPath.Single();
                                                        if (FilePathComparer.Instance.Equals(finalEventPath.Key, subsequentEvent.Key.FileChange.NewPath)
                                                            && subsequentEvent.Key.FileChange.Metadata != null
                                                            && !subsequentEvent.Key.FileChange.Metadata.HashableProperties.IsFolder)
                                                        {
                                                            lock (PendingStorageKeyUsage)
                                                            {
                                                                breakAfterCurrentSubsequentEvent = !PendingStorageKeyUsage.ContainsKey(subsequentEvent.Key.FileChange.Metadata.StorageKey)
                                                                    && subsequentEvent.Key.FileChange.InMemoryId > checkCurrentEvent.FileChange.InMemoryId;
                                                            }
                                                        }
                                                        break;

                                                    // deletion is handled by a silent deletion attempt (in case the event path or one of its parents are deleted)
                                                    // if it is successful, then the file was deleted and we can stop checking subsequentEvents
                                                    case CloudApiPublic.Static.FileChangeType.Deleted:
                                                        breakAfterCurrentSubsequentEvent = trackEventPath.Remove(subsequentEvent.Key.FileChange.NewPath);
                                                        break;

                                                    // rename is more complicated because it can throw first chance exceptions if it does not find the old path,
                                                    // so we make sure we can grab a hierarchy underneath the renamed path before performing the rename
                                                    case CloudApiPublic.Static.FileChangeType.Renamed:
                                                        FilePathHierarchicalNode<object> renameHierarchy;
                                                        CLError grabHierarchyError = trackEventPath.GrabHierarchyForPath(subsequentEvent.Key.FileChange.OldPath, out renameHierarchy, suppressException: true);
                                                        if (grabHierarchyError != null)
                                                        {
                                                            throw new AggregateException("Error grabbing renameHierarchy from trackEventPath", grabHierarchyError.GrabExceptions());
                                                        }
                                                        if (renameHierarchy != null)
                                                        {
                                                            if (completedUploadEvent.FileChange.Type == CloudApiPublic.Static.FileChangeType.Created
                                                                && FilePathComparer.Instance.Equals(trackEventPath.Keys.First(), subsequentEvent.Key.FileChange.OldPath))
                                                            {
                                                                removedRenames.Add(subsequentEvent.Value);
                                                            }

                                                            trackEventPath.Rename(subsequentEvent.Key.FileChange.OldPath, subsequentEvent.Key.FileChange.NewPath);
                                                        }
                                                        break;
                                                }

                                                if (breakAfterCurrentSubsequentEvent)
                                                {
                                                    break;
                                                }
                                            }

                                            if (!breakAfterCurrentSubsequentEvent)
                                            {
                                                bool convertToCreate;
                                                if (checkCurrentEvent.FileChange.Type == CloudApiPublic.Static.FileChangeType.Modified)
                                                {
                                                    if (checkCurrentEvent.PreviousMetadata == null)
                                                    {
                                                        convertToCreate = false;
                                                    }
                                                    else
                                                    {
                                                        lock (PendingStorageKeyUsage)
                                                        {
                                                            convertToCreate = !PendingStorageKeyUsage.ContainsKey(checkCurrentEvent.PreviousMetadata.StorageKey);
                                                        }

                                                        if (!convertToCreate)
                                                        {
                                                            FilePathDictionary<object> backwardsPath;
                                                            CLError createBackwardsPath = FilePathDictionary<object>.CreateAndInitialize(new FilePath(string.Empty), out backwardsPath);
                                                            if (createBackwardsPath != null)
                                                            {
                                                                throw new AggregateException("Error creating backwardsPath", createBackwardsPath.GrabExceptions());
                                                            }

                                                            backwardsPath.Add(checkCurrentEvent.FileChange.NewPath, new object());

                                                            foreach (UserEvent previousEvent in currentUserEvents
                                                                .Take(userAllEventsIndex)
                                                                .Reverse())
                                                            {
                                                                bool breakIteration = false;

                                                                if (previousEvent.FileChange != null
                                                                    && previousEvent.FileChange.Metadata != null)
                                                                {
                                                                    switch (previousEvent.FileChange.Type)
                                                                    {
                                                                        case CloudApiPublic.Static.FileChangeType.Created:
                                                                            KeyValuePair<FilePath, object> createdPath = backwardsPath.Single();
                                                                            if (FilePathComparer.Instance.Equals(createdPath.Key, previousEvent.FileChange.NewPath))
                                                                            {
                                                                                breakIteration = true;
                                                                                convertToCreate = true;
                                                                            }
                                                                            break;

                                                                        case CloudApiPublic.Static.FileChangeType.Modified:
                                                                            KeyValuePair<FilePath, object> modifiedPath = backwardsPath.Single();
                                                                            if (FilePathComparer.Instance.Equals(modifiedPath.Key, previousEvent.FileChange.NewPath))
                                                                            {
                                                                                if (previousEvent.PreviousMetadata == null)
                                                                                {
                                                                                    convertToCreate = true;
                                                                                    breakIteration = true;
                                                                                }
                                                                                else
                                                                                {
                                                                                    lock (PendingStorageKeyUsage)
                                                                                    {
                                                                                        breakIteration = !PendingStorageKeyUsage.ContainsKey(previousEvent.PreviousMetadata.StorageKey);
                                                                                    }
                                                                                }
                                                                            }
                                                                            break;

                                                                        case CloudApiPublic.Static.FileChangeType.Renamed:
                                                                            FilePathHierarchicalNode<object> renameHierarchy;
                                                                            CLError grabHierarchyError = backwardsPath.GrabHierarchyForPath(previousEvent.FileChange.NewPath, out renameHierarchy, suppressException: true);
                                                                            if (grabHierarchyError != null)
                                                                            {
                                                                                throw new AggregateException("Error grabbing renameHierarchy from backwardsPath", grabHierarchyError.GrabExceptions());
                                                                            }
                                                                            if (renameHierarchy != null)
                                                                            {
                                                                                backwardsPath.Rename(previousEvent.FileChange.NewPath, previousEvent.FileChange.OldPath);
                                                                            }
                                                                            break;

                                                                        case CloudApiPublic.Static.FileChangeType.Deleted:
                                                                            if (backwardsPath.Remove(previousEvent.FileChange.NewPath))
                                                                            {
                                                                                convertToCreate = true;
                                                                                breakIteration = true;
                                                                            }
                                                                            break;
                                                                    }
                                                                }

                                                                //previousEvent.PreviousMetadata.StorageKey

                                                                if (breakIteration)
                                                                {
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    convertToCreate = false;
                                                }

                                                bool removeOldLocation = true;

                                                if (emptyMetadata)
                                                {
                                                    if (convertToCreate)
                                                    {
                                                        checkCurrentEvent.FileChange.Type = CloudApiPublic.Static.FileChangeType.Created;
                                                    }

                                                    currentUserEvents.Add(new UserEvent(maxSyncIdPlusOne,
                                                        checkCurrentEvent.FileChange,
                                                        checkCurrentEvent.PreviousMetadata));
                                                }
                                                else
                                                {
                                                    KeyValuePair<FilePath, object> finalEventPath = trackEventPath.SingleOrDefault();
                                                    FileMetadata currentMetadata;
                                                    if (finalEventPath.Key != null // eventPath not removed and currentMetadataDictionary exists
                                                        && currentUserMetadata.TryGetValue(finalEventPath.Key, out currentMetadata)) // we have the updated path, try to grab the latest metadata
                                                    {
                                                        if (convertToCreate)
                                                        {
                                                            checkCurrentEvent.FileChange.Type = CloudApiPublic.Static.FileChangeType.Created;
                                                        }

                                                        currentUserEvents.Add(new UserEvent(maxSyncIdPlusOne,
                                                            checkCurrentEvent.FileChange,
                                                            currentMetadata));
                                                    }
                                                    else
                                                    {
                                                        removeOldLocation = false;
                                                    }
                                                }

                                                foreach (int removeRename in Enumerable.Reverse(removedRenames))
                                                {
                                                    currentUserEvents.RemoveAt(removeRename);
                                                }

                                                if (removeOldLocation)
                                                {
                                                    currentUserEvents.RemoveAt(userAllEventsIndex);
                                                }
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            lock (PendingStorageKeyUsage)
            {
                PendingStorageKeyUsage.Remove(storageKey);
            }
        }

        public bool AddFolderMetadata(long syncId, int userId, FilePath relativePathFromRoot, FileMetadata metadata)
        {
            bool isPending;
            bool newUpload;
            return AddMetadata(syncId, userId, Guid.Empty, relativePathFromRoot, metadata, out isPending, out newUpload);
        }

        public bool AddFileMetadata(long syncId, int userId, Guid deviceId, FilePath relativePathFromRoot, FileMetadata metadata, out bool isPending, out bool newUpload, byte[] MD5)
        {
            return AddMetadata(syncId, userId, deviceId, relativePathFromRoot, metadata, out isPending, out newUpload, MD5);
        }

        private bool AddMetadata(long syncId, int userId, Guid deviceId, FilePath relativePathFromRoot, FileMetadata metadata, out bool isPending, out bool newUpload, byte[] MD5 = null)
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
                if (MD5 != null)
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

                List<UserEvent> currentUserEvents;
                lock (UserEvents)
                {
                    if (!UserEvents.TryGetValue(userId, out currentUserEvents))
                    {
                        UserEvents.Add(userId,
                            currentUserEvents = new List<UserEvent>());
                    }
                }

                lock (currentUserEvents)
                {
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

                    UserEvent newEvent;
                    AddUserEvent(currentUserEvents, syncId, userId, new FileChange()
                        {
                            NewPath = relativePathFromRoot,
                            Type = CloudApiPublic.Static.FileChangeType.Created,
                            Metadata = metadata
                        }, null, out newEvent);


                    if (isPending)
                    {
                        AddPendingStorageKeyForUser(userId, metadata, newEvent);
                    }
                }

                return true;
            }
        }

        private void AddPendingStorageKeyForUser(int userId, FileMetadata metadata, UserEvent newEvent)
        {
            lock (PendingStorageKeyUsage)
            {
                Dictionary<int, List<UserEvent>> storageKeyPendingUsers;
                if (!PendingStorageKeyUsage.TryGetValue(metadata.StorageKey, out storageKeyPendingUsers))
                {
                    PendingStorageKeyUsage.Add(metadata.StorageKey,
                        storageKeyPendingUsers = new Dictionary<int, List<UserEvent>>());
                }

                lock (storageKeyPendingUsers)
                {
                    List<UserEvent> userPendings;
                    if (!storageKeyPendingUsers.TryGetValue(userId, out userPendings))
                    {
                        storageKeyPendingUsers.Add(userId,
                            userPendings = new List<UserEvent>());
                    }

                    userPendings.Add(newEvent);
                }
            }
        }

        /// <summary>
        /// Only call this within a lock on the user's metadata and a lock on the user's events
        /// </summary>
        /// <param name="syncId"></param>
        /// <param name="userId"></param>
        /// <param name="addedChange"></param>
        private void AddUserEvent(List<UserEvent> currentUserEvents, long syncId, int userId, FileChange addedChange, FileMetadata previousMetadata, out UserEvent newEvent)
        {
            newEvent = new UserEvent(syncId, addedChange, previousMetadata);

            if (currentUserEvents.Count == 0
                || currentUserEvents[currentUserEvents.Count - 1].SyncId <= syncId)
            {
                currentUserEvents.Add(newEvent);
            }
            // there should never be a higher sync id at the end of the user events list if the user was locked for this sync,
            // but this logic should keep the event list ordered
            else
            {
                if (this.UserWasNotLockedDetected != null)
                {
                    this.UserWasNotLockedDetected();
                }

                int indexBeforeHigherSyncId = -1;
                for (int currentEventIndex = currentUserEvents.Count - 2; currentEventIndex >= 0; currentEventIndex--)
                {
                    if (currentUserEvents[currentEventIndex].SyncId <= syncId)
                    {
                        indexBeforeHigherSyncId = currentEventIndex;
                        break;
                    }
                }

                UserEvent[] higherSyncIdEvents = currentUserEvents.Skip(indexBeforeHigherSyncId + 1).ToArray();

                currentUserEvents[indexBeforeHigherSyncId + 1] = newEvent;

                IEnumerator higherEventsEnumerator = higherSyncIdEvents.GetEnumerator();
                for (int moveUpEventIndex = indexBeforeHigherSyncId + 2; moveUpEventIndex < currentUserEvents.Count; moveUpEventIndex++)
                {
                    higherEventsEnumerator.MoveNext();
                    currentUserEvents[moveUpEventIndex] = (UserEvent)higherEventsEnumerator.Current;
                }
                higherEventsEnumerator.MoveNext();
                currentUserEvents.Add((UserEvent)higherEventsEnumerator.Current);
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

        public bool RecursivelyRemoveMetadata(long syncId, int userId, FilePath relativePathFromRoot)
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
                    List<UserEvent> currentUserEvents;
                    lock (UserEvents)
                    {
                        if (!UserEvents.TryGetValue(userId, out currentUserEvents))
                        {
                            UserEvents.Add(userId,
                                currentUserEvents = new List<UserEvent>());
                        }
                    }

                    lock (currentUserEvents)
                    {
                        Storage.RemoveUserUsageFromFile(removeMetadata.StorageKey,
                            userId,
                            relativePathFromRoot);

                        UserEvent newEvent;
                        AddUserEvent(currentUserEvents, syncId, userId, new FileChange()
                            {
                                NewPath = relativePathFromRoot,
                                Type = CloudApiPublic.Static.FileChangeType.Deleted,
                                Metadata = removeMetadata
                            }, removeMetadata, out newEvent);
                    }

                    return true;
                }
                return false;
            }
        }

        public bool RecursivelyRenameMetadata(long syncId, int userId, FilePath relativePathFrom, FilePath relativePathTo)
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

                    List<UserEvent> currentUserEvents;
                    lock (UserEvents)
                    {
                        if (!UserEvents.TryGetValue(userId, out currentUserEvents))
                        {
                            UserEvents.Add(userId,
                                currentUserEvents = new List<UserEvent>());
                        }
                    }

                    lock (currentUserEvents)
                    {
                        UserEvent newEvent;
                        AddUserEvent(currentUserEvents, syncId, userId, new FileChange()
                            {
                                OldPath = relativePathFrom,
                                NewPath = relativePathTo,
                                Type = CloudApiPublic.Static.FileChangeType.Renamed,
                                Metadata = movedMetadata
                            }, movedMetadata, out newEvent);
                    }

                    return true;
                }
                return false;
            }
        }

        public bool UpdateMetadata(long syncId, int userId, Guid deviceId, string revision, FilePath relativePathFromRoot, FileMetadata metadata, out bool isPending, out bool newUpload, out bool conflict, byte[] MD5 = null)
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
                if (MD5 != null)
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

                    List<UserEvent> currentUserEvents;
                    lock (UserEvents)
                    {
                        if (!UserEvents.TryGetValue(userId, out currentUserEvents))
                        {
                            UserEvents.Add(userId,
                                currentUserEvents = new List<UserEvent>());
                        }
                    }

                    lock (currentUserEvents)
                    {
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
                            metadata.StorageKey = storageKey;
                            conflict = false;
                            foundMetadata[relativePathFromRoot] = metadata;
                        }
                        else
                        {
                            conflict = true;
                        }
                        metadata.Revision = latestMD5
                            .Select(md5Byte => string.Format("{0:x2}", md5Byte))
                            .Aggregate((previousBytes, newByte) => previousBytes + newByte);

                        UserEvent newEvent;
                        AddUserEvent(currentUserEvents, syncId, userId, new FileChange()
                            {
                                NewPath = relativePathFromRoot,
                                Type = CloudApiPublic.Static.FileChangeType.Modified,
                                Metadata = metadata
                            }, previousMetadata, out newEvent);

                        if (isPending)
                        {
                            AddPendingStorageKeyForUser(userId, metadata, newEvent);
                        }
                    }
                }

                return true;
            }
        }

        public IEnumerable<FileChange> ChangesSinceSyncId(long syncId, int userId)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("MetadataProvider is not initialized");
                }
            }

            List<UserEvent> currentUserEvents;
            lock (UserEvents)
            {
                if (!UserEvents.TryGetValue(userId, out currentUserEvents))
                {
                    return Enumerable.Empty<FileChange>();
                }
            }

            UserEvent[] copiedEvents;
            HashSet<string> pendingStorageKeys = new HashSet<string>();

            Guid queryId = Guid.NewGuid();
            try
            {
                lock (currentUserEvents)
                {
                    int numberPreviousEvents;
                    int binaryIndexOneGreater = currentUserEvents.BinarySearch(new UserEvent(syncId + ((long)1), null, null),
                        UserEvent.SyncIdComparer);
                    if (binaryIndexOneGreater < 0)
                    {
                        numberPreviousEvents = ~binaryIndexOneGreater;
                    }
                    else
                    {
                        numberPreviousEvents = binaryIndexOneGreater;
                        while (numberPreviousEvents > 0)
                        {
                            if (currentUserEvents[numberPreviousEvents - 1].SyncId <= syncId)
                            {
                                break;
                            }
                            numberPreviousEvents--;
                        }
                    }

                    copiedEvents = currentUserEvents.Skip(numberPreviousEvents).ToArray();
                    lock (CurrentlyQueryingUserEvents)
                    {
                        Dictionary<Guid, HashSet<string>> currentUsersQueries;
                        if (CurrentlyQueryingUserEvents.TryGetValue(userId, out currentUsersQueries))
                        {
                            currentUsersQueries.Add(queryId, new HashSet<string>());
                        }
                        else
                        {
                            CurrentlyQueryingUserEvents.Add(userId,
                                currentUsersQueries = new Dictionary<Guid, HashSet<string>>()
                                {
                                    { queryId, new HashSet<string>() }
                                });
                        }
                    }
                }

                HashSet<string> userStorageKeys = new HashSet<string>(copiedEvents.Where(currentEvent => currentEvent.FileChange != null
                        && currentEvent.FileChange.Metadata != null
                        && (currentEvent.FileChange.Type == CloudApiPublic.Static.FileChangeType.Created
                            || currentEvent.FileChange.Type == CloudApiPublic.Static.FileChangeType.Modified)
                        && !currentEvent.FileChange.Metadata.HashableProperties.IsFolder)
                    .Select(currentEvent => currentEvent.FileChange.Metadata.StorageKey));

                lock (PendingStorageKeyUsage)
                {
                    foreach (string userStorageKey in userStorageKeys)
                    {
                        if (PendingStorageKeyUsage.ContainsKey(userStorageKey))
                        {
                            pendingStorageKeys.Add(userStorageKey);
                        }
                    }
                }

                lock (CurrentlyQueryingUserEvents)
                {
                    Dictionary<Guid, HashSet<string>> currentUsersQueries;
                    HashSet<string> currentQueryCompleted;
                    if (CurrentlyQueryingUserEvents.TryGetValue(userId, out currentUsersQueries)
                        && currentUsersQueries.TryGetValue(queryId, out currentQueryCompleted))
                    {
                        foreach (string simultaneouslyCompleted in currentQueryCompleted)
                        {
                            pendingStorageKeys.Add(simultaneouslyCompleted);
                        }
                        currentUsersQueries.Remove(queryId);
                    }
                }
            }
            catch
            {
                lock (CurrentlyQueryingUserEvents)
                {
                    Dictionary<Guid, HashSet<string>> currentUsersQueries;
                    if (CurrentlyQueryingUserEvents.TryGetValue(userId, out currentUsersQueries))
                    {
                        currentUsersQueries.Remove(queryId);
                    }
                }

                throw;
            }

            return copiedEvents.Where(currentEvent => currentEvent.FileChange == null
                    || currentEvent.FileChange.Metadata == null
                    || currentEvent.FileChange.Metadata.HashableProperties.IsFolder
                    || !pendingStorageKeys.Contains(currentEvent.FileChange.Metadata.StorageKey))
                .Select(currentEvent => currentEvent.FileChange);
        }

        public IEnumerable<KeyValuePair<FilePath, FileMetadata>> PurgeUserPendingsByDevice(int userId, Guid deviceId)
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
                    return Enumerable.Empty<KeyValuePair<FilePath, FileMetadata>>();
                }
            }

            Func<List<UserEvent>, FilePathDictionary<object>, KeyValuePair<UserEvent, int>, bool> checkLaterCompletion = (innerCurrentUserEvents, innerTrackEventPath, innerPreviousEvent) =>
                {
                    bool laterChangeComplete = false;

                    FilePathDictionary<object> forwardTrackEventPath;
                    CLError createForwardTrackEventPath = FilePathDictionary<object>.CreateAndInitialize(new FilePath(string.Empty), out forwardTrackEventPath);
                    if (createForwardTrackEventPath != null)
                    {
                        throw new AggregateException("Error creating forwardTrackEventPath", createForwardTrackEventPath.GrabExceptions());
                    }

                    forwardTrackEventPath.Add(innerTrackEventPath.Keys.Single(), new object());

                    foreach (UserEvent subsequentEvent in innerCurrentUserEvents.Skip(innerPreviousEvent.Value + 1))
                    {
                        if (subsequentEvent.FileChange != null
                            && subsequentEvent.FileChange.Metadata != null)
                        {
                            switch (subsequentEvent.FileChange.Type)
                            {
                                case CloudApiPublic.Static.FileChangeType.Created:
                                case CloudApiPublic.Static.FileChangeType.Modified:
                                    KeyValuePair<FilePath, object> finalEventPath = forwardTrackEventPath.Single();
                                    if (FilePathComparer.Instance.Equals(finalEventPath.Key, subsequentEvent.FileChange.NewPath)
                                        && subsequentEvent.FileChange.Metadata != null
                                        && !subsequentEvent.FileChange.Metadata.HashableProperties.IsFolder)
                                    {
                                        lock (PendingStorageKeyUsage)
                                        {
                                            laterChangeComplete = !PendingStorageKeyUsage.ContainsKey(subsequentEvent.FileChange.Metadata.StorageKey);
                                        }
                                    }
                                    break;

                                case CloudApiPublic.Static.FileChangeType.Deleted:
                                    laterChangeComplete = forwardTrackEventPath.Remove(subsequentEvent.FileChange.NewPath);
                                    break;

                                case CloudApiPublic.Static.FileChangeType.Renamed:
                                    FilePathHierarchicalNode<object> renameHierarchy;
                                    CLError grabHierarchyError = forwardTrackEventPath.GrabHierarchyForPath(subsequentEvent.FileChange.OldPath, out renameHierarchy, suppressException: true);
                                    if (grabHierarchyError != null)
                                    {
                                        throw new AggregateException("Error grabbing renameHierarchy from forwardTrackEventPath", grabHierarchyError.GrabExceptions());
                                    }
                                    if (renameHierarchy != null)
                                    {
                                        forwardTrackEventPath.Rename(subsequentEvent.FileChange.OldPath, subsequentEvent.FileChange.NewPath);
                                    }
                                    break;
                            }
                        }

                        if (laterChangeComplete)
                        {
                            break;
                        }
                    }

                    return laterChangeComplete;
                };

            List<KeyValuePair<FilePath, FileMetadata>> toReturn = new List<KeyValuePair<FilePath, FileMetadata>>();

            lock (foundMetadata)
            {
                IEnumerable<PurgedFile> purgedFiles = Storage.PurgeUserPendingsByDevice(userId, deviceId);

                if (purgedFiles == null)
                {
                    return Enumerable.Empty<KeyValuePair<FilePath, FileMetadata>>();
                }
                else
                {
                    List<UserEvent> currentUserEvents;
                    lock (UserEvents)
                    {
                        if (!UserEvents.TryGetValue(userId, out currentUserEvents))
                        {
                            return Enumerable.Empty<KeyValuePair<FilePath, FileMetadata>>();
                        }
                    }

                    lock (currentUserEvents)
                    {
                        foreach (PurgedFile currentPurged in purgedFiles)
                        {
                            if (currentPurged.IsValid
                                && !currentPurged.IncompletePurge)
                            {
                                FileMetadata matchingStorageKeyMetadata = null;
                                FileMetadata latestNonPendingMetadata = null;
                                FilePath removedPath = null;

                                FilePathDictionary<object> trackEventPath;
                                CLError createTrackEventPath = FilePathDictionary<object>.CreateAndInitialize(new FilePath(string.Empty), out trackEventPath);
                                if (createTrackEventPath != null)
                                {
                                    throw new AggregateException("Error creating trackEventPath", createTrackEventPath.GrabExceptions());
                                }

                                trackEventPath.Add(currentPurged.UserRelativePath, new object());

                                foreach (KeyValuePair<UserEvent, int> previousEvent in Enumerable.Reverse(currentUserEvents
                                    .Select((currentEvent, eventIndex) => new KeyValuePair<UserEvent, int>(currentEvent, eventIndex))))
                                {
                                    bool stopIteration = false;

                                    if (previousEvent.Key.FileChange != null
                                        && previousEvent.Key.FileChange.Metadata != null)
                                    {
                                        switch (previousEvent.Key.FileChange.Type)
                                        {
                                            case CloudApiPublic.Static.FileChangeType.Created:
                                                if (!previousEvent.Key.FileChange.Metadata.HashableProperties.IsFolder
                                                    && FilePathComparer.Instance.Equals(previousEvent.Key.FileChange.NewPath, trackEventPath.Keys.Single()))
                                                {
                                                    if (matchingStorageKeyMetadata == null
                                                        && previousEvent.Key.FileChange.Metadata.StorageKey == currentPurged.StorageKey)
                                                    {
                                                        if (checkLaterCompletion(currentUserEvents, trackEventPath, previousEvent))
                                                        {
                                                            stopIteration = true;
                                                        }
                                                        else
                                                        {
                                                            matchingStorageKeyMetadata = previousEvent.Key.FileChange.Metadata;
                                                        }
                                                    }

                                                    if (matchingStorageKeyMetadata != null)
                                                    {
                                                        stopIteration = true;
                                                        bool isPending;
                                                        lock (PendingStorageKeyUsage)
                                                        {
                                                            isPending = PendingStorageKeyUsage.ContainsKey(previousEvent.Key.FileChange.Metadata.StorageKey);
                                                        }
                                                        if (!isPending)
                                                        {
                                                            latestNonPendingMetadata = previousEvent.Key.FileChange.Metadata;
                                                        }
                                                    }
                                                }
                                                break;

                                            // two conditions for a matching delete, either we are along the track of the current file (found a matching storage key going backwards)
                                            // this should only handle error conditions where a file was traced backwards and we missed the creation event
                                            // if we are tracing the track of the current file then check if the remove covers the file to stop iterating
                                            // else if we are not tracking the track of the current file then check if the remove covers the file to start a new file track
                                            case CloudApiPublic.Static.FileChangeType.Deleted:
                                                if (matchingStorageKeyMetadata == null)
                                                {
                                                    if (trackEventPath.Remove(previousEvent.Key.FileChange.NewPath))
                                                    {
                                                        trackEventPath.Add(currentPurged.UserRelativePath, new object());
                                                    }
                                                }
                                                else if (trackEventPath.Remove(previousEvent.Key.FileChange.NewPath))
                                                {
                                                    removedPath = previousEvent.Key.FileChange.NewPath;
                                                    stopIteration = true;
                                                }
                                                break;

                                            case CloudApiPublic.Static.FileChangeType.Modified:
                                                if (!previousEvent.Key.FileChange.Metadata.HashableProperties.IsFolder // should be guaranteed since folders should not have modified events
                                                    && FilePathComparer.Instance.Equals(previousEvent.Key.FileChange.NewPath, trackEventPath.Keys.Single()))
                                                {
                                                    if (matchingStorageKeyMetadata == null
                                                        && previousEvent.Key.FileChange.Metadata.StorageKey == currentPurged.StorageKey)
                                                    {
                                                        if (checkLaterCompletion(currentUserEvents, trackEventPath, previousEvent))
                                                        {
                                                            stopIteration = true;
                                                        }
                                                        else
                                                        {
                                                            matchingStorageKeyMetadata = previousEvent.Key.FileChange.Metadata;
                                                        }
                                                    }

                                                    if (matchingStorageKeyMetadata != null)
                                                    {
                                                        bool isPending;
                                                        lock (PendingStorageKeyUsage)
                                                        {
                                                            isPending = PendingStorageKeyUsage.ContainsKey(previousEvent.Key.FileChange.Metadata.StorageKey);
                                                        }
                                                        if (!isPending)
                                                        {
                                                            stopIteration = true;
                                                            latestNonPendingMetadata = previousEvent.Key.FileChange.Metadata;
                                                        }
                                                    }
                                                }
                                                break;

                                            case CloudApiPublic.Static.FileChangeType.Renamed:
                                                FilePathHierarchicalNode<object> renameHierarchy;
                                                CLError grabHierarchyError = trackEventPath.GrabHierarchyForPath(previousEvent.Key.FileChange.NewPath, out renameHierarchy, suppressException: true);
                                                if (grabHierarchyError != null)
                                                {
                                                    throw new AggregateException("Error grabbing renameHierarchy from trackEventPath", grabHierarchyError.GrabExceptions());
                                                }
                                                if (renameHierarchy != null)
                                                {
                                                    if (matchingStorageKeyMetadata == null
                                                        && !previousEvent.Key.FileChange.Metadata.HashableProperties.IsFolder
                                                        && FilePathComparer.Instance.Equals(previousEvent.Key.FileChange.NewPath, trackEventPath.Keys.Single())
                                                        && previousEvent.Key.FileChange.Metadata.StorageKey == currentPurged.StorageKey)
                                                    {
                                                        if (checkLaterCompletion(currentUserEvents, trackEventPath, previousEvent))
                                                        {
                                                            stopIteration = true;
                                                        }
                                                        else
                                                        {
                                                            matchingStorageKeyMetadata = previousEvent.Key.FileChange.Metadata;
                                                        }
                                                    }
                                                    trackEventPath.Rename(previousEvent.Key.FileChange.NewPath, previousEvent.Key.FileChange.OldPath);
                                                }
                                                break;
                                        }
                                    }

                                    if (stopIteration)
                                    {
                                        break;
                                    }
                                }

                                if (matchingStorageKeyMetadata != null)
                                {
                                    toReturn.Add(new KeyValuePair<FilePath, FileMetadata>(trackEventPath.SingleOrDefault().Key ?? removedPath,
                                        matchingStorageKeyMetadata));

                                    if (latestNonPendingMetadata == null)
                                    {
                                        currentUserEvents.Add(new UserEvent(currentUserEvents[currentUserEvents.Count - 1].SyncId,
                                            new FileChange()
                                            {
                                                Metadata = matchingStorageKeyMetadata,
                                                NewPath = trackEventPath.SingleOrDefault().Key ?? removedPath,
                                                Type = CloudApiPublic.Static.FileChangeType.Deleted
                                            }, matchingStorageKeyMetadata));
                                    }
                                    else
                                    {
                                        currentUserEvents.Add(new UserEvent(currentUserEvents[currentUserEvents.Count - 1].SyncId,
                                            new FileChange()
                                            {
                                                Metadata = latestNonPendingMetadata,
                                                NewPath = trackEventPath.SingleOrDefault().Key,
                                                Type = CloudApiPublic.Static.FileChangeType.Modified
                                            }, matchingStorageKeyMetadata));
                                    }
                                }

                                lock (PendingStorageKeyUsage)
                                {
                                    Dictionary<int, List<UserEvent>> currentStorageUsers;
                                    if (PendingStorageKeyUsage.TryGetValue(currentPurged.StorageKey, out currentStorageUsers))
                                    {
                                        lock (currentStorageUsers)
                                        {
                                            List<UserEvent> currentUserPendings;
                                            if (currentStorageUsers.TryGetValue(userId, out currentUserPendings))
                                            {
                                                if (currentStorageUsers.Remove(userId))
                                                {
                                                    lock (CurrentlyQueryingUserEvents)
                                                    {
                                                        Dictionary<Guid, HashSet<string>> simultaneousUserQueries;
                                                        if (CurrentlyQueryingUserEvents.TryGetValue(userId, out simultaneousUserQueries))
                                                        {
                                                            foreach (HashSet<string> simultaneousUserQuery in simultaneousUserQueries.Values)
                                                            {
                                                                simultaneousUserQuery.Add(currentPurged.StorageKey);
                                                            }
                                                        }
                                                    }

                                                    if (currentStorageUsers.Count == 0)
                                                    {
                                                        PendingStorageKeyUsage.Remove(currentPurged.StorageKey);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return toReturn;
        }
    }
}