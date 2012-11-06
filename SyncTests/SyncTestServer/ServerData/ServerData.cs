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
                initialDataDoc.Validate(ScenarioSchemas, new ValidationEventHandler((sender, e) =>
                    {
                        validationErrors.Add(e.Message);
                    }));

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

        public IEnumerable<CloudApiPublic.JsonContracts.Event> GrabEventsAfterLastSync(CloudApiPublic.JsonContracts.Push request, User currentUser, long newSyncId)
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
            if (string.IsNullOrWhiteSpace(request.LastSyncId))
            {
                throw new NullReferenceException("Push request LastSyncId cannot be null");
            }
            long lastSyncId;
            if (!long.TryParse(request.LastSyncId, out lastSyncId))
            {
                throw new ArgumentException("Push request LastSyncId cannot be parsed into a long");
            }

            if (request.RelativeRootPath != null
                && (request.RelativeRootPath.Length == 0
                    || request.RelativeRootPath[request.RelativeRootPath.Length - 1] != '/'
                    || request.RelativeRootPath.Substring(0, request.RelativeRootPath.Length - 1) != string.Empty))
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
        #endregion

        private static readonly XmlSerializer ScenarioServerSerializer = new XmlSerializer(typeof(Model.ScenarioServer));
        private static readonly XmlSchemaSet ScenarioSchemas = new XmlSchemaSet();
        private static readonly SingleActionSetter NeedToAddScenarioSchema = new SingleActionSetter(false);
    }
}