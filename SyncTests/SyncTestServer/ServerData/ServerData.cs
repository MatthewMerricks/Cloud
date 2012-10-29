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
    public class ServerData : NotifiableObject<ServerData>, IServerData
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

        public ServerData() { }

        public void InitializeServer(Model.ScenarioServer initialData)
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
                    new Tuple<string, XmlSchemaSet>("SyncTestScenario.xsd", ScenarioSchemas));

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
                        _metadataProvider.InitializeProvider(_serverStorage, startingMetadata);
                    }
                    #endregion
                }

                Initialized.Value = true;
            }
        }

        private static readonly XmlSerializer ScenarioServerSerializer = new XmlSerializer(typeof(Model.ScenarioServer));
        private static readonly XmlSchemaSet ScenarioSchemas = new XmlSchemaSet();
        private static readonly SingleActionSetter NeedToAddScenarioSchema = new SingleActionSetter();
    }
}