using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using FileMonitor;

namespace SQLIndexer
{
    public class IndexingAgent
    {
        #region private fields
        private string indexedPath = null;

        private static Dictionary<int, FileChangeType> changeEnums = null;
        private static Dictionary<FileChangeType, int> changeEnumsBackward = null;
        private static int changeCategoryId = 0;
        private static object changeEnumsLocker = new object();
        #endregion

        public static CLError CreateNewAndInitialize(out IndexingAgent newIndexer)
        {
            IndexingAgent newAgent;
            try
            {
                newIndexer = newAgent = new IndexingAgent();
            }
            catch (Exception ex)
            {
                newIndexer = (IndexingAgent)Helpers.DefaultForType(typeof(IndexingAgent));
                return ex;
            }
            try
            {
                lock (changeEnumsLocker)
                {
                    if (changeEnums == null)
                    {
                        int changeEnumsCount = System.Enum.GetNames(typeof(FileChangeType)).Length;
                        changeEnums = new Dictionary<int, FileChangeType>(changeEnumsCount);
                        changeEnumsBackward = new Dictionary<FileChangeType, int>(changeEnumsCount);
                        using (IndexDBEntities indexDB = new IndexDBEntities())
                        {
                            foreach (SQLEnum currentChangeEnum in indexDB.SQLEnums
                                .Include(((MemberExpression)((Expression<Func<SQLEnum, EnumCategory>>)(parent => parent.EnumCategory)).Body).Member.Name)
                                .Where(currentEnum => currentEnum.EnumCategory.Name == typeof(FileChangeType).Name))
                            {
                                changeCategoryId = currentChangeEnum.EnumCategoryId;
                                int forwardKey = currentChangeEnum.EnumId;
                                FileChangeType forwardValue = (FileChangeType)System.Enum.Parse(typeof(FileChangeType), currentChangeEnum.Name);
                                changeEnums.Add(forwardKey,
                                    forwardValue);
                                changeEnumsBackward.Add(forwardValue,
                                    forwardKey);
                            }
                        }
                        if (changeEnums.Count != changeEnumsCount)
                        {
                            throw new Exception("FileChangeType enumerations are not all found in the database");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        #region public methods
        public CLError StartInitialIndexing(Action<IEnumerable<KeyValuePair<FilePath, FileMetadata>>, IEnumerable<FileChange>> indexCompletionCallback,
            Func<string> getPath)
        {
            try
            {
                this.indexedPath = getPath();
                ThreadPool.QueueUserWorkItem(state => this.BuildIndex((Action<IEnumerable<KeyValuePair<FilePath, FileMetadata>>, IEnumerable<FileChange>>)state),
                    indexCompletionCallback);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        public CLError GetLastSyncStates(out FilePathDictionary<SyncedObject> syncStates)
        {
            try
            {
                using (IndexDBEntities indexDB = new IndexDBEntities())
                {
                    Sync lastSync = indexDB.Syncs
                        .OrderByDescending(currentSync => currentSync.SyncId)
                        .FirstOrDefault();

                    if (lastSync == null)
                    {
                        syncStates = (FilePathDictionary<SyncedObject>)Helpers.DefaultForType(typeof(FilePathDictionary<SyncedObject>));
                    }
                    else
                    {
                        CLError createDictError = FilePathDictionary<SyncedObject>.CreateAndInitialize(lastSync.RootPath,
                            out syncStates);
                        if (createDictError != null)
                        {
                            return createDictError;
                        }

                        foreach (SyncState currentSyncState in
                            indexDB.SyncStates
                                .Include(((MemberExpression)((Expression<Func<SyncState, FileSystemObject>>)(parent => parent.FileSystemObject)).Body).Member.Name)
                                .Include(((MemberExpression)((Expression<Func<SyncState, FileSystemObject>>)(parent => parent.ServerLinkedFileSystemObject)).Body).Member.Name))
                        {
                            syncStates.Add(currentSyncState.FileSystemObject.Path,
                                new SyncedObject()
                                {
                                    ServerLinkedPath = currentSyncState.ServerLinkedFileSystemObject == null
                                        ? null
                                        : currentSyncState.ServerLinkedFileSystemObject.Path,
                                    Metadata = new FileMetadata()
                                    {
                                        HashableProperties = new FileMetadataHashableProperties(currentSyncState.FileSystemObject.IsFolder,
                                            currentSyncState.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                            currentSyncState.FileSystemObject.CreationTime ??  new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                            currentSyncState.FileSystemObject.Size)
                                    }
                                });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                syncStates = null;
                return ex;
            }
            return null;
        }

        public CLError GetEventsSinceLastSync(out FilePathDictionary<FileChange> changeEvents)
        {
            try
            {
                using (IndexDBEntities indexDB = new IndexDBEntities())
                {
                    Sync lastSync = indexDB.Syncs
                        .OrderByDescending(currentSync => currentSync.SyncId)
                        .FirstOrDefault();
                    Nullable<Guid> lastSyncId = lastSync == null
                        ? (Nullable<Guid>)null
                        : lastSync.SyncId;

                    CLError createDictError = FilePathDictionary<FileChange>.CreateAndInitialize(lastSync == null
                            ? indexedPath
                            : lastSync.RootPath,
                        out changeEvents);
                    if (createDictError != null)
                    {
                        return createDictError;
                    }

                    foreach (Event currentChange in
                        indexDB.Events
                            .Include(((MemberExpression)((Expression<Func<Event, FileSystemObject>>)(parent => parent.FileSystemObject)).Body).Member.Name)
                            .Where(currentChange => (currentChange.SyncId == null && lastSyncId == null)
                                || (currentChange.SyncId == lastSyncId)))
                    {
                        changeEvents.Add(currentChange.FileSystemObject.Path,
                            new FileChange()
                            {
                                NewPath = currentChange.FileSystemObject.Path,
                                OldPath = currentChange.PreviousPath,
                                Type = changeEnums[currentChange.FileChangeTypeEnumId],
                                Metadata = new FileMetadata()
                                {
                                    HashableProperties = new FileMetadataHashableProperties(currentChange.FileSystemObject.IsFolder,
                                        currentChange.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                        currentChange.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                        currentChange.FileSystemObject.Size)
                                }
                            });
                    }
                }
            }
            catch (Exception ex)
            {
                changeEvents = (FilePathDictionary<FileChange>)Helpers.DefaultForType(typeof(FilePathDictionary<FileChange>));
                return ex;
            }
            return null;
        }

        public int AddEvent(FileChange newEvent)
        {
            if (newEvent.DoNotAddToSQLIndex)
            {
                return newEvent.EventId;
            }
            using (IndexDBEntities indexDB = new IndexDBEntities())
            {
                Sync lastSync = indexDB.Syncs
                    .OrderByDescending(currentSync => currentSync.SyncId)
                    .FirstOrDefault();

                Nullable<Guid> lastSyncId = lastSync == null
                    ? (Nullable<Guid>)null
                    : lastSync.SyncId;

                Event toAdd = new Event()
                {
                    SyncId = lastSyncId,
                    FileChangeTypeCategoryId = changeCategoryId,
                    FileChangeTypeEnumId = changeEnumsBackward[newEvent.Type],
                    FileSystemObject = new FileSystemObject()
                    {
                        CreationTime = newEvent.Metadata.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
                            ? (Nullable<DateTime>)null
                            : newEvent.Metadata.HashableProperties.CreationTime,
                        IsFolder = newEvent.Metadata.HashableProperties.IsFolder,
                        LastTime = newEvent.Metadata.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
                            ? (Nullable<DateTime>)null
                            : newEvent.Metadata.HashableProperties.LastTime,
                        Path = newEvent.NewPath.ToString(),
                        Size = newEvent.Metadata.HashableProperties.Size
                    },
                    PreviousPath = newEvent.OldPath == null
                        ? null
                        : newEvent.OldPath.ToString()
                };

                indexDB.Events.AddObject(toAdd);
                indexDB.SaveChanges();
                return toAdd.EventId;
            }
        }

        public CLError RemoveEventById(int eventId)
        {
            try
            {
                using (IndexDBEntities indexDB = new IndexDBEntities())
                {
                    Event toDelete = indexDB.Events.FirstOrDefault(currentEvent => currentEvent.EventId == eventId);
                    indexDB.DeleteObject(toDelete);
                    indexDB.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        public CLError RemoveEventsByIds(IEnumerable<int> eventIds)
        {
            try
            {
                int[] eventIdsArray = eventIds == null
                    ? new int[0]
                    : eventIds.ToArray();
                using (IndexDBEntities indexDB = new IndexDBEntities())
                {
                    foreach (Event toDelete in indexDB.Events.Where(currentEvent => eventIdsArray.Contains(currentEvent.EventId)))
                    {
                        indexDB.DeleteObject(toDelete);
                    }
                    indexDB.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        public CLError RecordCompletedSync(Guid syncId, IEnumerable<int> syncedEventIds, string newRootPath = null)
        {
            try
            {
                int[] syncedEventIdsEnumerated = syncedEventIds == null
                    ? new int[0]
                    : syncedEventIds.ToArray();

                using (IndexDBEntities indexDB = new IndexDBEntities())
                {
                    using (TransactionScope completionSync = new TransactionScope())
                    {

                        Sync lastSync = indexDB.Syncs
                            .OrderByDescending(currentSync => currentSync.SyncId)
                            .FirstOrDefault();
                        Nullable<Guid> lastSyncId = lastSync == null
                            ? (Nullable<Guid>)null
                            : lastSync.SyncId;
                        newRootPath = string.IsNullOrEmpty(newRootPath)
                            ? lastSync.RootPath
                            : newRootPath;

                        Sync newSync = new Sync()
                        {
                            SyncId = syncId,
                            RootPath = newRootPath
                        };

                        indexDB.Syncs.AddObject(newSync);

                        FilePathDictionary<FileMetadata> newSyncStates;
                        CLError newSyncStatesError = FilePathDictionary<FileMetadata>.CreateAndInitialize(newRootPath,
                            out newSyncStates);

                        Dictionary<string, string> serverRemappedPaths = new Dictionary<FilePath,string>();

                        if (lastSyncId != null)
                        {
                            foreach (SyncState currentState in indexDB.SyncStates
                                .Include(((MemberExpression)((Expression<Func<SyncState, FileSystemObject>>)(parent => parent.FileSystemObject)).Body).Member.Name)
                                .Include(((MemberExpression)((Expression<Func<SyncState, FileSystemObject>>)(parent => parent.ServerLinkedFileSystemObject)).Body).Member.Name)
                                .Where(currentSync => currentSync.SyncId == (Guid)lastSyncId))
                            {
                                if (currentState.ServerLinkedFileSystemObject != null)
                                {
                                    serverRemappedPaths.Add(currentState.FileSystemObject.Path,
                                        currentState.ServerLinkedFileSystemObject.Path);
                                }
                                newSyncStates.Add(currentState.FileSystemObject.Path,
                                    new FileMetadata()
                                    {
                                        HashableProperties = new FileMetadataHashableProperties(currentState.FileSystemObject.IsFolder,
                                            currentState.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                            currentState.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                            currentState.FileSystemObject.Size)
                                    });
                            }
                        }

                        Event[] existingEvents = indexDB.Events
                            .Include(((MemberExpression)((Expression<Func<Event, FileSystemObject>>)(parent => parent.FileSystemObject)).Body).Member.Name)
                            .Where(currentEvent => currentEvent.SyncId == lastSyncId)
                            .OrderBy(currentEvent => currentEvent.EventId)
                            .ToArray();

                        foreach (Event previousEvent in existingEvents)
                        {
                            if (syncedEventIdsEnumerated.Contains(previousEvent.EventId))
                            {
                                switch (changeEnums[previousEvent.FileChangeTypeEnumId])
                                {
                                    case FileChangeType.Created:
                                        newSyncStates.Add(previousEvent.FileSystemObject.Path,
                                            new FileMetadata()
                                            {
                                                HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                    previousEvent.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.Size)
                                            });
                                        break;
                                    case FileChangeType.Deleted:
                                        newSyncStates.Remove(previousEvent.FileSystemObject.Path);
                                        break;
                                    case FileChangeType.Modified:
                                        if (newSyncStates.ContainsKey(previousEvent.FileSystemObject.Path))
                                        {
                                            newSyncStates[previousEvent.FileSystemObject.Path].HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                    previousEvent.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.Size);
                                        }
                                        else
                                        {
                                            newSyncStates.Add(previousEvent.FileSystemObject.Path,
                                                new FileMetadata()
                                                {
                                                    HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                        previousEvent.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                        previousEvent.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                        previousEvent.FileSystemObject.Size)
                                                });
                                        }
                                        break;
                                    case FileChangeType.Renamed:
                                        if (newSyncStates.ContainsKey(previousEvent.FileSystemObject.Path))
                                        {
                                            if (newSyncStates.ContainsKey(previousEvent.PreviousPath))
                                            {
                                                newSyncStates.Remove(previousEvent.PreviousPath);
                                            }
                                            newSyncStates[previousEvent.FileSystemObject.Path].HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                    previousEvent.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.Size);
                                        }
                                        else if (newSyncStates.ContainsKey(previousEvent.PreviousPath))
                                        {
                                            newSyncStates.Rename(previousEvent.PreviousPath, previousEvent.FileSystemObject.Path);

                                            newSyncStates[previousEvent.FileSystemObject.Path].HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                    previousEvent.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                    previousEvent.FileSystemObject.Size);
                                        }
                                        else
                                        {
                                            newSyncStates.Add(previousEvent.FileSystemObject.Path,
                                                new FileMetadata()
                                                {
                                                    HashableProperties = new FileMetadataHashableProperties(previousEvent.FileSystemObject.IsFolder,
                                                        previousEvent.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                        previousEvent.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                                        previousEvent.FileSystemObject.Size)
                                                });
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                previousEvent.SyncId = syncId;
                            }
                        }

                        foreach (KeyValuePair<FilePath, FileMetadata> newSyncState in newSyncStates)
                        {
                            FileSystemObject newSyncedObject = new FileSystemObject()
                            {
                                CreationTime = newSyncState.Value.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                    ? (Nullable<DateTime>)null
                                    : newSyncState.Value.HashableProperties.CreationTime,
                                IsFolder = newSyncState.Value.HashableProperties.IsFolder,
                                LastTime = newSyncState.Value.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                    ? (Nullable<DateTime>)null
                                    : newSyncState.Value.HashableProperties.LastTime,
                                Path = newSyncState.Key.ToString(),
                                Size = newSyncState.Value.HashableProperties.Size
                            };
                            indexDB.FileSystemObjects.AddObject(newSyncedObject);

                            Nullable<int> serverRemappedObjectId = null;
                            if (serverRemappedPaths.ContainsKey(newSyncedObject.Path))
                            {
                                FileSystemObject serverSyncedObject = new FileSystemObject()
                                {
                                    CreationTime = newSyncState.Value.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                        ? (Nullable<DateTime>)null
                                        : newSyncState.Value.HashableProperties.CreationTime,
                                    IsFolder = newSyncState.Value.HashableProperties.IsFolder,
                                    LastTime = newSyncState.Value.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks
                                        ? (Nullable<DateTime>)null
                                        : newSyncState.Value.HashableProperties.LastTime,
                                    Path = serverRemappedPaths[newSyncedObject.Path],
                                    Size = newSyncState.Value.HashableProperties.Size
                                };
                                indexDB.FileSystemObjects.AddObject(serverSyncedObject);
                                indexDB.SaveChanges();
                                serverRemappedObjectId = serverSyncedObject.FileSystemObjectId
                            }
                            indexDB.SaveChanges();

                            indexDB.SyncStates.AddObject(new SyncState()
                            {
                                FileSystemObjectId = newSyncedObject.FileSystemObjectId,
                                ServerLinkedFileSystemObjectId = serverRemappedObjectId,
                                SyncId = newSync.SyncId
                            });
                        }

                        indexDB.SaveChanges();
                        completionSync.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        #endregion

        #region private methods
        private IndexingAgent() { }

        private void BuildIndex(Action<IEnumerable<KeyValuePair<FilePath, FileMetadata>>, IEnumerable<FileChange>> indexCompletionCallback)
        {
            FilePathDictionary<FileMetadata> indexPaths;
            CLError indexPathCreationError = FilePathDictionary<FileMetadata>.CreateAndInitialize(indexedPath,
                out indexPaths);
            using (IndexDBEntities indexDB = new IndexDBEntities())
            {
                // test code! remove this:
                Sync fakeSync = new Sync()
                {
                    RootPath = indexedPath
                };
                indexDB.Syncs.AddObject(fakeSync);
                FileSystemObject objectOne = new FileSystemObject()
                {
                    CreationTime = new DateTime(634756926821378618, DateTimeKind.Utc),
                    IsFolder = true,
                    LastTime = new DateTime(634756926831929221, DateTimeKind.Utc),
                    Path = "C:\\\\Users\\Public\\Documents\\CreateFileTests\\MyFolder"
                };
                FileSystemObject objectTwo = new FileSystemObject()
                {
                    CreationTime = new DateTime(634756926821578629, DateTimeKind.Utc),
                    IsFolder = false,
                    LastTime = new DateTime(634756926821598630, DateTimeKind.Utc),
                    Path = "C:\\\\Users\\Public\\Documents\\CreateFileTests\\UnlockedFile.txt",
                    Size = 14
                };
                FileSystemObject objectThree = new FileSystemObject()
                {
                    CreationTime = new DateTime(634756926821878646, DateTimeKind.Utc),
                    IsFolder = false,
                    LastTime = new DateTime(634756926821898647, DateTimeKind.Utc),
                    Path = "C:\\\\Users\\Public\\Documents\\CreateFileTests\\MyFolder\\StreamingFile.txt",
                    Size = 600
                };
                FileSystemObject objectFour = new FileSystemObject()
                {
                    CreationTime = new DateTime(634756926821628632, DateTimeKind.Utc),
                    IsFolder = false,
                    LastTime = new DateTime(634756926832019226, DateTimeKind.Utc),
                    Path = "C:\\\\Users\\Public\\Documents\\CreateFileTests\\File1.txt",
                    Size = 17
                };
                indexDB.FileSystemObjects.AddObject(objectOne);
                indexDB.FileSystemObjects.AddObject(objectTwo);
                indexDB.FileSystemObjects.AddObject(objectThree);
                indexDB.FileSystemObjects.AddObject(objectFour);
                indexDB.SaveChanges();
                indexDB.SyncStates.AddObject(new SyncState()
                {
                    FileSystemObjectId = objectOne.FileSystemObjectId,
                    SyncId = fakeSync.SyncId
                });
                indexDB.SyncStates.AddObject(new SyncState()
                {
                    FileSystemObjectId = objectTwo.FileSystemObjectId,
                    SyncId = fakeSync.SyncId
                });
                indexDB.SyncStates.AddObject(new SyncState()
                {
                    FileSystemObjectId = objectThree.FileSystemObjectId,
                    SyncId = fakeSync.SyncId
                });
                indexDB.SyncStates.AddObject(new SyncState()
                {
                    FileSystemObjectId = objectFour.FileSystemObjectId,
                    SyncId = fakeSync.SyncId
                });
                indexDB.SaveChanges();

                Sync lastSync = indexDB.Syncs
                    .OrderByDescending(currentSync => currentSync.SyncId)
                    .FirstOrDefault();
                Nullable<Guid> lastSyncId = lastSync == null
                    ? (Nullable<Guid>)null
                    : lastSync.SyncId;
                List<FileChange> changeList = new List<FileChange>();
                if (lastSync != null)
                {
                    foreach (SyncState currentSyncState in
                        indexDB.SyncStates
                            .Include(((MemberExpression)((Expression<Func<SyncState, FileSystemObject>>)(parent => parent.FileSystemObject)).Body).Member.Name)
                            .Where(syncState => (syncState.SyncId == null && lastSync.SyncId == null)
                                || (syncState.SyncId == lastSyncId)))
                    {
                        indexPaths.Add(currentSyncState.FileSystemObject.Path,
                            new FileMetadata()
                            {
                                HashableProperties = new FileMetadataHashableProperties(currentSyncState.FileSystemObject.IsFolder,
                                    currentSyncState.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                    currentSyncState.FileSystemObject.CreationTime ??  new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                    currentSyncState.FileSystemObject.Size)
                            });
                    }
                }
                lock (changeEnumsLocker)
                {
                    foreach (Event currentEvent in
                        indexDB.Events
                            .Include(((MemberExpression)((Expression<Func<Event, FileSystemObject>>)(parent => parent.FileSystemObject)).Body).Member.Name)
                            .Where(currentEvent => (currentEvent.SyncId == null && lastSyncId == null)
                                || (currentEvent.SyncId == lastSyncId)))
                    {
                        changeList.Add(new FileChange()
                        {
                            Metadata = new FileMetadata()
                            {
                                HashableProperties = new FileMetadataHashableProperties(currentEvent.FileSystemObject.IsFolder,
                                    currentEvent.FileSystemObject.LastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                    currentEvent.FileSystemObject.CreationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc),
                                    currentEvent.FileSystemObject.Size)
                            },
                            NewPath = currentEvent.FileSystemObject.Path,
                            OldPath = currentEvent.PreviousPath,
                            Type = changeEnums[currentEvent.FileChangeTypeEnumId],
                            DoNotAddToSQLIndex = true,
                            EventId = currentEvent.EventId
                        });
                    }
                }
                DirectoryInfo indexRootPath = new DirectoryInfo(indexedPath);
                foreach (string deletedPath in
                    indexPaths.Select(currentIndex => currentIndex.Key.ToString())
                        .Except(RecurseIndexDirectory(changeList, indexRootPath, indexPaths),
                            StringComparer.InvariantCulture))
                {
                    changeList.Add(new FileChange()
                    {
                        NewPath = deletedPath,
                        Type = FileChangeType.Deleted,
                        Metadata = indexPaths[deletedPath]
                    });
                }
                indexCompletionCallback(indexPaths,
                    changeList);
            }
        }

        private static FileMetadataHashableComparer fileComparer = new FileMetadataHashableComparer();

        private static IEnumerable<string> RecurseIndexDirectory(List<FileChange> changeList, DirectoryInfo currentDirectory, FilePathDictionary<FileMetadata> indexPaths)
        {
            List<string> filePathsFound = new List<string>();
            foreach (DirectoryInfo subDirectory in currentDirectory.EnumerateDirectories())
            {
                filePathsFound.Add(subDirectory.FullName);
                FileMetadataHashableProperties compareProperties = new FileMetadataHashableProperties(true,
                    // last time is the greater of the the last access time and the last write time
                    DateTime.Compare(subDirectory.LastAccessTimeUtc, subDirectory.LastWriteTimeUtc) > 0
                        ? subDirectory.LastAccessTimeUtc
                        : subDirectory.LastWriteTimeUtc,
                    subDirectory.CreationTimeUtc,
                    null);
                FileChange existingEvent = changeList.FirstOrDefault(currentChange => FilePathComparer.Instance.Equals(currentChange.NewPath, (FilePath)subDirectory));
                if (existingEvent == null)
                {
                    if (indexPaths.ContainsKey(subDirectory))
                    {
                        if (!fileComparer.Equals(compareProperties, indexPaths[subDirectory].HashableProperties))
                        {
                            changeList.Add(new FileChange()
                            {
                                NewPath = subDirectory.FullName,
                                Type = FileChangeType.Modified,
                                Metadata = new FileMetadata()
                                {
                                    HashableProperties = compareProperties
                                }
                            });
                        }
                    }
                    else
                    {
                        changeList.Add(new FileChange()
                        {
                            NewPath = subDirectory.FullName,
                            Type = FileChangeType.Created,
                            Metadata = new FileMetadata()
                            {
                                HashableProperties = compareProperties
                            }
                        });
                    }
                }
                else if (!fileComparer.Equals(compareProperties, existingEvent.Metadata.HashableProperties))
                {
                    existingEvent.DoNotAddToSQLIndex = false;
                }
                filePathsFound.AddRange(RecurseIndexDirectory(changeList,
                    subDirectory,
                    indexPaths));
            }
            foreach (FileInfo currentFile in currentDirectory.EnumerateFiles())
            {
                long currentFileLength = -1;
                bool fileLengthFailed = false;
                try
                {
                    currentFileLength = currentFile.Length;
                }
                catch
                {
                    fileLengthFailed = true;
                }
                if (!fileLengthFailed
                    && currentFileLength >= 0)
                {
                    filePathsFound.Add(currentFile.FullName);
                    FileMetadataHashableProperties compareProperties = new FileMetadataHashableProperties(false,
                        // last time is the greater of the the last access time and the last write time
                        DateTime.Compare(currentFile.LastAccessTimeUtc, currentFile.LastWriteTimeUtc) > 0
                            ? currentFile.LastAccessTimeUtc
                            : currentFile.LastWriteTimeUtc,
                        currentFile.CreationTimeUtc,
                        currentFileLength);
                    FileChange existingEvent = changeList.FirstOrDefault(currentChange => FilePathComparer.Instance.Equals(currentChange.NewPath, (FilePath)currentFile));
                    if (existingEvent == null)
                    {
                        if (indexPaths.ContainsKey(currentFile))
                        {
                            if (!FilePathComparer.Equals(compareProperties, indexPaths[currentFile].HashableProperties))
                            {
                                changeList.Add(new FileChange()
                                {
                                    NewPath = currentFile,
                                    Type = FileChangeType.Modified,
                                    Metadata = new FileMetadata()
                                    {
                                        HashableProperties = compareProperties
                                    }
                                });
                            }
                        }
                        else
                        {
                            changeList.Add(new FileChange()
                            {
                                NewPath = currentFile,
                                Type = FileChangeType.Created,
                                Metadata = new FileMetadata()
                                {
                                    HashableProperties = compareProperties
                                }
                            });
                        }
                    }
                    else if (!fileComparer.Equals(compareProperties, existingEvent.Metadata.HashableProperties))
                    {
                        existingEvent.DoNotAddToSQLIndex = false;
                    }
                }
            }
            return filePathsFound;
        }
        #endregion private methods
    }
}