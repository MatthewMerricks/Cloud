using CloudApiPublic.Model;
using SyncTestServer.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Xml;
using System.IO;

namespace SyncTestServer
{
    public class ServerData : IServerData
    {
        public ObservableCollection<User> Users
        {
            get
            {
                return _users;
            }
        }
        private readonly ObservableCollection<User> _users = new ObservableCollection<User>();

        public IServerStorage ServerStorage
        {
            get
            {
                return _serverStorage;
            }
        }
        private IServerStorage _serverStorage;

        public IMetadataProvider MetadataProvider
        {
            get
            {
                return _metadataProvider;
            }
        }
        private IMetadataProvider _metadataProvider;

        private readonly GenericHolder<bool> Initialized = new GenericHolder<bool>(false);

        #region IServerData members
        public void InitializeServer(Model.ScenarioServer initialData, Action userWasNotLockedDetected = null)
        {
            lock (Initialized)
            {
                if (Initialized.Value)
                {
                    throw new Exception("Already Initialized");
                }

                NeedToAddScenarioSchema.TrySet((Action<Tuple<string, XmlSchemaSet>>)(setState =>
                    {
                        using (XmlReader schemaReader = XmlReader.Create(setState.Item1))
                        {
                            setState.Item2.Add(null, schemaReader);
                        }
                    }),
                    new Tuple<string, XmlSchemaSet>("Model\\SyncTestScenario.xsd", ScenarioSchemas));

                List<string> validationErrors = new List<string>();

                XDocument initialDataDoc = new XDocument();
                using (XmlWriter initialDocWriter = initialDataDoc.CreateWriter())
                {
                    ScenarioServerSerializer.Serialize(initialDocWriter, initialData);
                }
                initialDataDoc.Validate(ScenarioSchemas, (sender, e) =>
                    {
                        validationErrors.Add(e.Message);
                    });

                if (validationErrors.Count > 0)
                {
                    System.Windows.MessageBox.Show("XML schema validation error on initialData:" + Environment.NewLine +
                        string.Join(Environment.NewLine, validationErrors.ToArray()) + Environment.NewLine +
                        "Ceased processing initialData");
                }
                else
                {
                    List<InitialMetadata> startingMetadata = new List<InitialMetadata>();

                    #region add users
                    foreach (ServerUserType currentUser in initialData.Users)
                    {
                        string addError = null;

                        int userIdParsed;
                        if (int.TryParse(currentUser.UUid, out userIdParsed))
                        {
                            User addUser = new User(userIdParsed)
                            {
                                Username = currentUser.Username,
                                Password = currentUser.Password
                            };

                            foreach (ServerDeviceType currentDevice in currentUser.Devices)
                            {
                                Guid deviceIdParsed;
                                if (Guid.TryParse(currentDevice.UDid, out deviceIdParsed))
                                {
                                    addUser.Devices.Add(new Device()
                                    {
                                        Id = deviceIdParsed,
                                        FriendlyName = currentDevice.FriendlyName,
                                        AuthorizationKey = currentDevice.AKey
                                    });
                                }
                                else
                                {
                                    addError = "Unable to parse currentUser UDid as an Guid: " + currentDevice.UDid;
                                    break;
                                }
                            }

                            if (addError == null)
                            {
                                switch (currentUser.CloudFolder.Type)
                                {
                                    case StorageType.PhysicalLocation:
                                        PhysicalLocationCloudFolderType castCurrentUserFolder = currentUser.CloudFolder as PhysicalLocationCloudFolderType;
                                        if (castCurrentUserFolder == null)
                                        {
                                            addError = "Unable to cast CloudFolder of currentUser as PhysicalLocationCloudFolderType: " + currentUser.CloudFolder.GetType().FullName;
                                        }
                                        else
                                        {
                                            switch (castCurrentUserFolder.InitialData.Type)
                                            {
                                                case InitialCloudFolderDataTypeType.BuildFromCloudFolder:
                                                    BuildInitialDataFromCloudFolderType castCloudFolderType = castCurrentUserFolder.InitialData as BuildInitialDataFromCloudFolderType;
                                                    if (castCloudFolderType == null)
                                                    {
                                                        addError = "Unable to cast InitialData of currentUser as BuildInitialDataFromCloudFolderType: " + castCurrentUserFolder.InitialData.GetType().ToString();
                                                    }
                                                    else
                                                    {
                                                        System.Windows.MessageBox.Show("Probably not a good idea to build ServerData from CloudFolder, building anyways");
                                                        FilePathDictionary<Tuple<FileMetadata, byte[]>> filesAndFolders;
                                                        CLError createFilesAndFoldersError = FilePathDictionary<Tuple<FileMetadata, byte[]>>.CreateAndInitialize(castCurrentUserFolder.RootPath,
                                                            out filesAndFolders);
                                                        if (createFilesAndFoldersError == null)
                                                        {
                                                            Exception fillInFromFolderError = SyncTestServer.Static.Helpers.FillInMetadataDictionaryFromPhysicalPath(filesAndFolders, castCurrentUserFolder.RootPath);
                                                            if (fillInFromFolderError == null)
                                                            {
                                                                foreach (KeyValuePair<FilePath, Tuple<FileMetadata, byte[]>> currentUserItem in filesAndFolders)
                                                                {
                                                                    startingMetadata.Add(new InitialMetadata(currentUserItem.Value.Item1,
                                                                        userIdParsed,
                                                                        currentUserItem.Value.Item2,
                                                                        currentUserItem.Key));
                                                                }
                                                            }
                                                            else
                                                            {
                                                                addError = fillInFromFolderError.Message;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            addError = createFilesAndFoldersError.errorDescription;
                                                        }
                                                    }
                                                    break;
                                                case InitialCloudFolderDataTypeType.DefinedValues:
                                                    DefinedValuesForCloudFolderType castDefinedValuesType = castCurrentUserFolder.InitialData as DefinedValuesForCloudFolderType;
                                                    if (castDefinedValuesType == null)
                                                    {
                                                        addError = "Unable to cast InitialData of currentUser as DefinedValuesForCloudFolderType: " + castCurrentUserFolder.InitialData.GetType().ToString();
                                                    }
                                                    else
                                                    {
                                                        foreach (FileOrFolderType currentDefinedPath in castDefinedValuesType.PathsWithMetadata)
                                                        {
                                                            if (currentDefinedPath.IsFolder)
                                                            {
                                                                FilePath folderPath;
                                                                FolderType currentDefinedFolder = currentDefinedPath as FolderType;
                                                                if (currentDefinedFolder == null)
                                                                {
                                                                    addError = "Unable to cast currentDefinedPath of castDefinedValuesType as FolderType: " + currentDefinedPath.GetType().Name;
                                                                    break;
                                                                }
                                                                else if (currentDefinedFolder.RelativePathFromRoot == null)
                                                                {
                                                                    addError = "RelativePathFrom in currentDefinedFolder cannot be null";
                                                                    break;
                                                                }
                                                                else if ((folderPath = SyncTestServer.Static.Helpers.BuildFilePathFromEmptyRootRelativePath(currentDefinedFolder.RelativePathFromRoot)) == null)
                                                                {
                                                                    addError = "Unable to parse RelativePathFrom in currentDefinedFolder into FilePath: " + currentDefinedFolder.RelativePathFromRoot;
                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    startingMetadata.Add(new InitialMetadata(new CloudApiPublic.Model.FileMetadata()
                                                                        {
                                                                            HashableProperties = new CloudApiPublic.Model.FileMetadataHashableProperties(true,
                                                                                currentDefinedFolder.UTCCreatedDateTime,
                                                                                currentDefinedFolder.UTCCreatedDateTime,
                                                                                null)
                                                                        }, userIdParsed,
                                                                        null,
                                                                        folderPath));
                                                                }
                                                            }
                                                            else
                                                            {
                                                                FileType currentDefinedFile = currentDefinedPath as FileType;
                                                                if (currentDefinedFile == null)
                                                                {
                                                                    addError = "Unable to cast currentDefinedPath of castDefinedValuesType as FileType: " + currentDefinedPath.GetType().Name;
                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    FilePath filePath;
                                                                    long parseStorageKey;
                                                                    if (long.TryParse(currentDefinedFile.StorageKey, out parseStorageKey))
                                                                    {
                                                                        if (currentDefinedFile.RelativePathFromRoot == null)
                                                                        {
                                                                            addError = "RelativePathFrom in currentDefinedFile cannot be null";
                                                                            break;
                                                                        }
                                                                        else if ((filePath = SyncTestServer.Static.Helpers.BuildFilePathFromEmptyRootRelativePath(currentDefinedFile.RelativePathFromRoot)) == null)
                                                                        {
                                                                            addError = "Unable to parse RelativePathFrom in currentDefinedFile into FilePath: " + currentDefinedFile.RelativePathFromRoot;
                                                                            break;
                                                                        }
                                                                        else if (currentDefinedFile.MD5 == null
                                                                            || currentDefinedFile.MD5.Length != 32)
                                                                        {
                                                                            addError = "Unable to parse MD5 in currentDefinedFile into 16-length byte array: " + (currentDefinedFile.MD5 == null ? "{null}" : currentDefinedFile.MD5);
                                                                            break;
                                                                        }
                                                                        else
                                                                        {
                                                                            byte[] parsedMD5 = Enumerable.Range(0, 32)
                                                                                .Where(currentHex => currentHex % 2 == 0)
                                                                                .Select(currentHex => Convert.ToByte(currentDefinedFile.MD5.Substring(currentHex, 2), 16))
                                                                                .ToArray();

                                                                            startingMetadata.Add(new InitialMetadata(new CloudApiPublic.Model.FileMetadata()
                                                                                {
                                                                                    HashableProperties = new CloudApiPublic.Model.FileMetadataHashableProperties(false,
                                                                                        currentDefinedFile.UTCModifiedDateTime,
                                                                                        currentDefinedFile.UTCCreatedDateTime,
                                                                                        currentDefinedFile.Size),
                                                                                    StorageKey = currentDefinedFile.StorageKey,
                                                                                    Revision = currentDefinedFile.MD5
                                                                                },
                                                                                userIdParsed,
                                                                                parsedMD5,
                                                                                filePath));
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        addError = "Unable to parse StorageKey of currentDefinedFile as long: " + (currentDefinedFile.StorageKey ?? "{null}");
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    break;
                                                default:
                                                    addError = "Unknown InitialCloudFolderDataTypeType for currentUser: " + castCurrentUserFolder.InitialData.Type.ToString();
                                                    break;
                                            }
                                        }
                                        break;
                                    default:
                                        addError = "Unknown CloudFolderTypeType for currentUser: " + currentUser.CloudFolder.Type.ToString();
                                        break;
                                }
                            }

                            if (addError == null)
                            {
                                _users.Add(addUser);
                            }
                        }
                        else
                        {
                            addError = "Unable to parse currentUser UUid as an int: " + currentUser.UUid;
                        }

                        if (addError != null)
                        {
                            System.Windows.MessageBox.Show(addError + Environment.NewLine + "currentUser not added");
                        }
                    }
                    #endregion

                    #region server storage
                    switch (initialData.Storage.Type)
                    {
                        case StorageType.PhysicalLocation:
                            ServerPhysicalStorageType initialPhysicalStorage = initialData.Storage as ServerPhysicalStorageType;
                            if (initialPhysicalStorage == null)
                            {
                                _serverStorage = null;

                                System.Windows.MessageBox.Show("Unable to cast initialData Storage as ServerPhysicalStorageType");
                            }
                            else
                            {
                                _serverStorage = new PhysicalStorage(initialPhysicalStorage.RootPath);
                            }

                            break;

                        default:
                            _serverStorage = null;

                            System.Windows.MessageBox.Show("Unknown initialData Storage Type: " + initialData.Storage.Type.ToString());
                            break;
                    }
                    #endregion

                    #region server metadata
                    if (_serverStorage == null)
                    {
                        System.Windows.MessageBox.Show("Unable to create metadata store because serverStorage is null");
                    }
                    else
                    {
                        _metadataProvider = new MetadataProvider();
                        _metadataProvider.InitializeProvider(_serverStorage, startingMetadata, userWasNotLockedDetected);
                    }
                    #endregion
                }

                Initialized.Value = true;
            }
        }

        public User FindUserByAKey(string akey, out Device specificDevice)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("Not Initialized, call InitializeServer first");
                }
            }

            User toReturn;
            Device outDevice = null;
            lock (Users)
            {
                toReturn = Users.FirstOrDefault(currentUser => currentUser.Devices.Any(currentDevice => (outDevice = currentDevice).AuthorizationKey.Equals(akey, StringComparison.InvariantCultureIgnoreCase)));
            }
            if (toReturn == null)
            {
                specificDevice = null;
            }
            else
            {
                specificDevice = outDevice;
            }
            return toReturn;
        }

        public IEnumerable<CloudApiPublic.JsonContracts.File> PurgePendingFiles(User currentUser, CloudApiPublic.JsonContracts.PurgePending request, out bool deviceNotInUser)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("Not Initialized, call InitializeServer first");
                }
            }

            if (currentUser == null)
            {
                throw new NullReferenceException("currentUser cannot be null");
            }
            if (request == null)
            {
                throw new NullReferenceException("request cannot be null");
            }
            if (request.DeviceId == null)
            {
                throw new NullReferenceException("PurgePending request DeviceId cannot be null");
            }
            Guid currentDevice;
            if (!Guid.TryParse(request.DeviceId, out currentDevice))
            {
                throw new ArgumentException("PurgePending request DeviceId must be parsable to Guid");
            }

            lock (currentUser)
            {
                if (!currentUser.Devices.Any(userDevice => userDevice.Id == currentDevice))
                {
                    deviceNotInUser = true;
                    return null;
                }
                else
                {
                    deviceNotInUser = false;
                    return _metadataProvider.PurgeUserPendingsByDevice(currentUser.Id, currentDevice)
                        .Select(currentPending =>
                            {
                                return new CloudApiPublic.JsonContracts.File()
                                {
                                    Metadata = JsonMetadataByMetadata(currentPending.Key, null, currentPending.Value)
                                };
                            })
                        .ToArray();
                }
            }
        }

        public long NewSyncIdBeforeStart
        {
            get
            {
                lock (Initialized)
                {
                    if (!Initialized.Value)
                    {
                        throw new Exception("Not Initialized, call InitializeServer first");
                    }
                }
                return _metadataProvider.NewSyncIdBeforeStart;
            }
        }

        private static CloudApiPublic.JsonContracts.Metadata JsonMetadataByMetadata(FilePath newPath, FilePath oldPath, FileMetadata metadata)
        {
            if (newPath == null)
            {
                throw new NullReferenceException("newPath cannot be null");
            }
            if (metadata == null)
            {
                throw new NullReferenceException("metadata cannot be null");
            }

            return new CloudApiPublic.JsonContracts.Metadata()
                {
                    RelativePath = (oldPath == null
                        ? newPath.ToString().Replace('\\', '/') +
                            (metadata.HashableProperties.IsFolder
                                ? "/"
                                : string.Empty)
                        : null),
                    RelativeFromPath = (oldPath == null
                        ? null
                        : oldPath.ToString().Replace('\\', '/') +
                            (metadata.HashableProperties.IsFolder
                                ? "/"
                                : string.Empty)),
                    RelativeToPath = (oldPath == null
                        ? null
                        : newPath.ToString().Replace('\\', '/') +
                            (metadata.HashableProperties.IsFolder
                                ? "/"
                                : string.Empty)),
                    Deleted = false,
                    MimeType = ((metadata.HashableProperties.IsFolder
                            || oldPath != null)
                        ? null
                        : "application/octet-stream"),
                    CreatedDate = (oldPath == null ? metadata.HashableProperties.CreationTime : (Nullable<DateTime>)null),
                    ModifiedDate = (oldPath == null
                        ? (metadata.HashableProperties.IsFolder
                            ? metadata.HashableProperties.CreationTime
                            : metadata.HashableProperties.LastTime)
                        : (Nullable<DateTime>)null),
                    IsFolder = metadata.HashableProperties.IsFolder,
                    Revision = (oldPath == null ? metadata.Revision : null),
                    Hash = (oldPath == null ? metadata.Revision : null),
                    StorageKey = (oldPath == null ? metadata.StorageKey : null),
                    Version = "6",
                    Size = (oldPath == null ? metadata.HashableProperties.Size : null)
                };
        }

        public IEnumerable<CloudApiPublic.JsonContracts.Event> GrabEventsAfterLastSync(string lastSyncIdString, string relativeRootPath, User currentUser, long newSyncId)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("Not Initialized, call InitializeServer first");
                }
            }

            if (currentUser == null)
            {
                throw new NullReferenceException("currentUser cannot be null");
            }
            if (lastSyncIdString == null)
            {
                throw new NullReferenceException("lastSyncIdString cannot be null");
            }
            long lastSyncId;
            if (!long.TryParse(lastSyncIdString, out lastSyncId))
            {
                throw new ArgumentException("lastSyncIdString cannot be parsed into a long");
            }

            if (relativeRootPath != null
                && (relativeRootPath.Length == 0
                    || relativeRootPath[relativeRootPath.Length - 1] != '/'
                    || relativeRootPath.Substring(0, relativeRootPath.Length - 1) != string.Empty))
            {
                throw new NotImplementedException("Have not implemented grabbing events except from the relative root path, \"/\"");
            }

            return _metadataProvider.ChangesSinceSyncId(lastSyncId, currentUser.Id)
                .Where(currentEvent => currentEvent.Metadata != null
                    && currentEvent.NewPath != null)
                .Select(currentEvent => new CloudApiPublic.JsonContracts.Event()
                {
                    Header = new CloudApiPublic.JsonContracts.Header()
                    {
                        Action = SyncTestServer.Static.Helpers.GetEventAction(currentEvent.Type, currentEvent.Metadata.HashableProperties.IsFolder, (currentEvent.Metadata.LinkTargetPath == null ? null : currentEvent.Metadata.LinkTargetPath.ToString().Replace('\\', '/'))),
                        SyncId = newSyncId.ToString()
                    },
                    Metadata = JsonMetadataByMetadata(currentEvent.NewPath, currentEvent.OldPath, currentEvent.Metadata)
                });
        }

        public void ApplyClientEventToServer(long syncId, User currentUser, Device currentDevice, CloudApiPublic.JsonContracts.Event toEvent)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("Not Initialized, call InitializeServer first");
                }
            }

            if (currentUser == null)
            {
                throw new NullReferenceException("currentUser cannot be null");
            }
            if (currentDevice == null)
            {
                throw new NullReferenceException("currentDevice cannot be null");
            }
            if (toEvent == null)
            {
                throw new NullReferenceException("toApply cannot be null");
            }

            toEvent.Header = new CloudApiPublic.JsonContracts.Header()
            {
                EventId = toEvent.EventId,
                Action = toEvent.Action
            };

            toEvent.EventId = null;
            toEvent.Action = null;
            
            FileChange newChange = new FileChange()
                {
                    EventId = toEvent.Header.EventId ?? 0,
                    NewPath = GenerateFilePathFromForwardSlashRelativePath(CloudApiPublic.Model.CLDefinitions.SyncHeaderRenames.Contains(toEvent.Header.Action)
                        ? toEvent.Metadata.RelativeToPath
                        : toEvent.Metadata.RelativePath),
                    OldPath = (CloudApiPublic.Model.CLDefinitions.SyncHeaderRenames.Contains(toEvent.Header.Action)
                        ? GenerateFilePathFromForwardSlashRelativePath(toEvent.Metadata.RelativeFromPath)
                        : null),
                    Type = ParseEventStringToType(toEvent.Header.Action),
                    Metadata = new FileMetadata()
                    {
                        HashableProperties = new FileMetadataHashableProperties((bool)toEvent.Metadata.IsFolder,
                            (((bool)toEvent.Metadata.IsFolder)
                                ? toEvent.Metadata.CreatedDate
                                : toEvent.Metadata.ModifiedDate),
                            toEvent.Metadata.CreatedDate,
                            toEvent.Metadata.Size),
                        LinkTargetPath = GenerateFilePathFromForwardSlashRelativePath(toEvent.Metadata.TargetPath),
                        Revision = toEvent.Metadata.Revision,
                        StorageKey = toEvent.Metadata.StorageKey
                    }
                };

            byte[] newMD5 = toEvent.Metadata.Hash == null
                ? null
                : Enumerable.Range(0, 32)
                    .Where(currentHex => currentHex % 2 == 0)
                    .Select(currentHex => Convert.ToByte(toEvent.Metadata.Hash.Substring(currentHex, 2), 16))
                    .ToArray();
            CLError setMD5Error = newChange.SetMD5(newMD5);
            if (setMD5Error != null)
            {
                throw new AggregateException("Error setting MD5 on newChange", setMD5Error.GrabExceptions());
            }

            switch (newChange.Type)
            {
                case CloudApiPublic.Static.FileChangeType.Created:
                    if (newChange.Metadata.HashableProperties.IsFolder)
                    {
                        FileMetadata existingMetadata;
                        if (_metadataProvider.TryGetMetadata(currentUser.Id, newChange.NewPath, out existingMetadata))
                        {
                            if (existingMetadata.HashableProperties.IsFolder)
                            {
                                toEvent.Header.Status = CLDefinitions.CLEventTypeExists;
                            }
                            else
                            {
                                toEvent.Header.Status = CLDefinitions.CLEventTypeConflict;
                            }
                        }
                        else if (_metadataProvider.AddFolderMetadata(syncId, currentUser.Id, newChange.NewPath, newChange.Metadata))
                        {
                            toEvent.Header.Status = CLDefinitions.CLEventTypeAccepted;
                        }
                        else
                        {
                            toEvent.Header.Status = CLDefinitions.CLEventTypeConflict;
                        }
                    }
                    else
                    {
                        bool isPending;
                        bool newUpload;
                        FileMetadata existingMetadata;
                        if (_metadataProvider.TryGetMetadata(currentUser.Id, newChange.NewPath, out existingMetadata))
                        {
                            byte[] latestMD5;
                            if (existingMetadata.HashableProperties.IsFolder
                                || existingMetadata.Revision == null)
                            {
                                toEvent.Header.Status = CLDefinitions.CLEventTypeConflict;
                            }
                            else if (existingMetadata.Revision.Equals(toEvent.Metadata.Hash, StringComparison.InvariantCultureIgnoreCase))
                            {
                                toEvent.Header.Status = CLDefinitions.CLEventTypeDuplicate;
                            }
                            else if (_serverStorage.DoesFileHaveEarlierRevisionOfUndeletedFile(syncId, currentUser.Id, newChange.NewPath, (long)newChange.Metadata.HashableProperties.Size, newMD5, out latestMD5))
                            {
                                toEvent.Metadata = new CloudApiPublic.JsonContracts.Metadata()
                                {
                                    CreatedDate = existingMetadata.HashableProperties.CreationTime,
                                    Deleted = false,
                                    Hash = latestMD5
                                        .Select(md5Byte => string.Format("{0:x2}", md5Byte))
                                        .Aggregate((previousBytes, newByte) => previousBytes + newByte),
                                    IsFolder = false,
                                    ModifiedDate = existingMetadata.HashableProperties.LastTime,
                                    RelativePath = toEvent.Metadata.RelativePath,
                                    Revision = existingMetadata.Revision,
                                    Size = existingMetadata.HashableProperties.Size,
                                    StorageKey = existingMetadata.StorageKey,
                                    Version = "1.0",
                                    TargetPath = (existingMetadata.LinkTargetPath == null
                                        ? null
                                        : existingMetadata.LinkTargetPath.ToString())
                                };

                                toEvent.Header.Status = CLDefinitions.CLEventTypeDownload;
                            }
                            else
                            {
                                toEvent.Header.Status = CLDefinitions.CLEventTypeConflict;
                            }
                        }
                        else
                        {
                            if (_metadataProvider.AddFileMetadata(syncId, currentUser.Id, currentDevice.Id, newChange.NewPath, newChange.Metadata, out isPending, out newUpload, newMD5))
                            {
                                if (isPending)
                                {
                                    if (newUpload)
                                    {
                                        toEvent.Header.Status = CLDefinitions.CLEventTypeUpload;
                                    }
                                    else
                                    {
                                        toEvent.Header.Status = CLDefinitions.CLEventTypeUploading;
                                    }
                                }
                                else
                                {
                                    toEvent.Header.Status = CLDefinitions.CLEventTypeDuplicate;
                                }
                            }
                            else
                            {
                                toEvent.Header.Status = CLDefinitions.CLEventTypeConflict;
                            }

                            toEvent.Metadata.Revision = newChange.Metadata.Revision;
                            toEvent.Metadata.StorageKey = newChange.Metadata.StorageKey;
                        }
                    }
                    break;

                case CloudApiPublic.Static.FileChangeType.Deleted:
                    FileMetadata deleteMetadata;
                    if (!newChange.Metadata.HashableProperties.IsFolder
                        && _metadataProvider.TryGetMetadata(currentUser.Id, newChange.NewPath, out deleteMetadata)
                        && deleteMetadata.Revision != newChange.Metadata.Revision)
                    {
                        toEvent.Header.Status = CLDefinitions.CLEventTypeConflict;
                    }
                    else if (_metadataProvider.RecursivelyRemoveMetadata(syncId, currentUser.Id, newChange.NewPath))
                    {
                        toEvent.Header.Status = CLDefinitions.CLEventTypeAccepted;
                    }
                    else
                    {
                        toEvent.Header.Status = CLDefinitions.CLEventTypeNotFound;
                    }
                    break;

                case CloudApiPublic.Static.FileChangeType.Modified:
                    bool modifiedPending;
                    bool modifiedNew;
                    bool modifiedConflict;
                    if (_metadataProvider.UpdateMetadata(syncId, currentUser.Id, currentDevice.Id, newChange.Metadata.Revision, newChange.NewPath, newChange.Metadata, out modifiedPending, out modifiedNew, out modifiedConflict, newMD5))
                    {
                        if (modifiedConflict)
                        {
                            toEvent.Header.Status = CLDefinitions.CLEventTypeConflict;
                        }
                        else if (newChange.Metadata.HashableProperties.IsFolder)
                        {
                            toEvent.Header.Status = CLDefinitions.CLEventTypeAccepted;
                        }
                        else if (modifiedPending)
                        {
                            if (modifiedNew)
                            {
                                toEvent.Header.Status = CLDefinitions.CLEventTypeUpload;
                            }
                            else
                            {
                                toEvent.Header.Status = CLDefinitions.CLEventTypeUploading;
                            }
                        }
                        else
                        {
                            toEvent.Header.Status = CLDefinitions.CLEventTypeDuplicate;
                        }
                        
                        toEvent.Metadata.Revision = newChange.Metadata.Revision;
                        toEvent.Metadata.StorageKey = newChange.Metadata.StorageKey;
                    }
                    else
                    {
                        toEvent.Header.Status = CLDefinitions.CLEventTypeNotFound;
                    }
                    break;

                case CloudApiPublic.Static.FileChangeType.Renamed:
                    FileMetadata renameMetadata;
                    if (!newChange.Metadata.HashableProperties.IsFolder
                        && _metadataProvider.TryGetMetadata(currentUser.Id, newChange.NewPath, out renameMetadata)
                        && renameMetadata.Revision != newChange.Metadata.Revision)
                    {
                        toEvent.Header.Status = CLDefinitions.CLEventTypeConflict;
                    }
                    else if (_metadataProvider.RecursivelyRenameMetadata(syncId, currentUser.Id, newChange.OldPath, newChange.NewPath))
                    {
                        toEvent.Header.Status = CLDefinitions.CLEventTypeAccepted;
                    }
                    else
                    {
                        toEvent.Header.Status = CLDefinitions.CLEventTypeNotFound;
                    }
                    break;

                default:
                    throw new Exception("Unknown FileChangeType newChange.Type: " + newChange.Type.ToString());
            }
        }

        public bool WriteUpload(Stream toWrite, string storageKey, long contentLength, string contentMD5, User currentUser, bool disposeStreamAfterWrite = true)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("Not Initialized, call InitializeServer first");
                }
            }

            if (contentMD5 == null)
            {
                throw new NullReferenceException("contentMD5 cannot be null");
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(contentMD5, "[a-fA-F\\d]{32}", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant))
            {
                throw new ArgumentException("contentMD5 must be 32 hexadecimal characters");
            }
            if (currentUser == null)
            {
                throw new ArgumentException("currentUser cannot be null");
            }

            return _serverStorage.WriteFile(toWrite,
                storageKey,
                contentLength,
                Enumerable.Range(0, 32)
                    .Where(currentHex => currentHex % 2 == 0)
                    .Select(currentHex => Convert.ToByte(contentMD5.Substring(currentHex, 2), 16))
                    .ToArray(),
                currentUser.Id,
                false);
        }

        public Stream GetDownload(string storageKey, User currentUser, out long fileSize)
        {
            lock (Initialized)
            {
                if (!Initialized.Value)
                {
                    throw new Exception("Not Initialized, call InitializeServer first");
                }
            }

            if (currentUser == null)
            {
                throw new NullReferenceException("currentUser cannot be null");
            }

            return _serverStorage.ReadFile(storageKey, currentUser.Id, out fileSize);
        }
        #endregion

        private static FilePath GenerateFilePathFromForwardSlashRelativePath(string relativePath)
        {
            if (relativePath == null)
            {
                return null;
            }

            return GenerateFilePathFromParts(relativePath.Split('/'));
        }

        private static FilePath GenerateFilePathFromParts(string[] pathParts, Nullable<int> partsIndex = null)
        {
            int nonNullIndex;
            if (partsIndex == null)
            {
                nonNullIndex = pathParts.Length - 1;
            }
            else
            {
                nonNullIndex = (int)partsIndex;

                if (nonNullIndex < 0)
                {
                    return null;
                }
            }

            return new FilePath(pathParts[nonNullIndex], GenerateFilePathFromParts(pathParts, nonNullIndex - 1));
        }

        private static CloudApiPublic.Static.FileChangeType ParseEventStringToType(string actionString)
        {
            if (CloudApiPublic.Model.CLDefinitions.SyncHeaderCreations.Contains(actionString))
            {
                return CloudApiPublic.Static.FileChangeType.Created;
            }
            if (CloudApiPublic.Model.CLDefinitions.SyncHeaderDeletions.Contains(actionString))
            {
                return CloudApiPublic.Static.FileChangeType.Deleted;
            }
            if (CloudApiPublic.Model.CLDefinitions.SyncHeaderModifications.Contains(actionString))
            {
                return CloudApiPublic.Static.FileChangeType.Modified;
            }
            if (CloudApiPublic.Model.CLDefinitions.SyncHeaderRenames.Contains(actionString))
            {
                return CloudApiPublic.Static.FileChangeType.Renamed;
            }
            throw new ArgumentException("eventString was not parsable to FileChangeType: " + actionString);
        }

        private static readonly XmlSerializer ScenarioServerSerializer = new XmlSerializer(typeof(Model.ScenarioServer));
        private static readonly XmlSchemaSet ScenarioSchemas = new XmlSchemaSet();
        private static readonly SingleActionSetter NeedToAddScenarioSchema = new SingleActionSetter(false);
    }
}