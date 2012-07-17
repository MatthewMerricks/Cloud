//
//  CLMetadata.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CloudApiPublic.Static;
using System.Security.Cryptography;
using CloudApiPublic.Support;
using CloudApiPublic.Model;

// Merged 7/3/12
namespace CloudApiPublic.Model
{
    public class CLMetadata
    {
        public string Path
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.NewPath == null
                    || this.GetCloudPath == null)
                {
                    return null;
                }
                return this.ChangeReference.NewPath.GetRelativePath(this.GetCloudPath(), replaceWithForwardSlashes: true);
            }
        }
        public string ToPath
        {
            get
            {
                return this.Path;
            }
        }
        public string FromPath
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.OldPath == null
                    || this.GetCloudPath == null)
                {
                    return null;
                }
                return this.ChangeReference.OldPath.GetRelativePath(this.GetCloudPath(), replaceWithForwardSlashes: true);
            }
        }
        public string TargetPath
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.Metadata == null
                    || this.ChangeReference.Metadata.LinkTargetPath == null
                    || this.GetCloudPath == null)
                {
                    return null;
                }
                return this.ChangeReference.Metadata.LinkTargetPath.GetRelativePath(this.GetCloudPath(), replaceWithForwardSlashes: true);
            }
        }
        public string Revision
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.Metadata == null)
                {
                    return null;
                }
                return this.ChangeReference.Metadata.Revision;
            }
        }
        public string CreateDate
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.Metadata == null
                    || this.ChangeReference.Metadata.HashableProperties.CreationTime.Ticks == FileConstants.InvalidUtcTimeTicks)
                {
                    return null;
                }
                return this.ChangeReference.Metadata.HashableProperties.CreationTime.ToString("o");  // ISO 8601 format
            }
        }
        public string ModifiedDate
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.Metadata == null
                    || this.ChangeReference.Metadata.HashableProperties.LastTime.Ticks == FileConstants.InvalidUtcTimeTicks)
                {
                    return null;
                }
                return this.ChangeReference.Metadata.HashableProperties.LastTime.ToString("o");  // ISO 8601 format
            }
        }
        public string Hash
        {
            get
            {
                if (this.ChangeReference == null)
                {
                    return null;
                }
                string toReturn;
                this.ChangeReference.GetMD5LowercaseString(out toReturn);
                return toReturn;
            }
        }
        public string Mime_type
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.NewPath == null
                    || this.ChangeReference.Metadata == null
                    || this.ChangeReference.Metadata.HashableProperties.IsFolder)
                {
                    return null;
                }
                int extensionIndex = this.ChangeReference.NewPath.Name.LastIndexOf('.');
                if (extensionIndex >= 0)
                {
                    return this.ChangeReference.NewPath.Name.Substring(extensionIndex + 1);
                }
                else
                {
                    return "file";
                }
            }
        }
        public string Sid
        {
            get
            {
                if (this.GetLastSyncId == null)
                {
                    return null;
                }
                return this.GetLastSyncId();
            }
        }
        public string Size
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.Metadata == null
                    || this.ChangeReference.Metadata.HashableProperties.Size == null)
                {
                    return null;
                }
                return ((long)this.ChangeReference.Metadata.HashableProperties.Size).ToString();
            }
        }
        public string Storage_key
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.Metadata == null)
                {
                    return null;
                }
                return this.ChangeReference.Metadata.StorageKey;
            }
        }
        /// <summary>
        /// This property is probably returning the wrong event id since this.ChangeReference.EventId is a client event id, not the server one
        /// </summary>
        public string LastEventID
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.EventId == 0)// it is zero if it has not been set
                {
                    return null;
                }
                return this.ChangeReference.EventId.ToString();
            }
        }
        public bool IsDirectory
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.Metadata == null)
                {
                    throw new NullReferenceException("ChangeReference cannot be null and neither can its Metadata property");
                }
                return this.ChangeReference.Metadata.HashableProperties.IsFolder;
            }
        }
        public bool IsDeleted
        {
            get
            {
                if (this.ChangeReference == null)
                {
                    throw new NullReferenceException("ChangeReference cannot be null");
                }
                return this.ChangeReference.Type == FileChangeType.Deleted;
            }
        }
        public bool IsPending
        {
            get
            {
                return this.ChangeReference != null;
            }
        }
        public FileChange ChangeReference { get; set; }

        private Func<string> GetLastSyncId;
        private Func<string> GetCloudPath;

        public CLMetadata(Func<string> getLastSyncId, Func<string> getCloudPath)
        {
            this.GetLastSyncId = getLastSyncId;
            this.GetCloudPath = getCloudPath;
        }
        //&&&&public CLMetadata(Func<string> getLastSyncId, Func<string> getCloudPath, Dictionary<string, object> json, CLSyncHeader header, SyncDirection direction)
        public CLMetadata(CLEvent evt, Dictionary<string, object> json, CLSyncHeader header, SyncDirection direction, FileChange existingChange = null)
        {
            this.GetLastSyncId = evt.GetLastSyncId;
            this.GetCloudPath = evt.GetCloudPath;

            string jsonToPath;
            string jsonFromPath;
            string jsonTargetPath;
            string jsonRevision;
            string jsonCreationDate;
            string jsonModifiedDate;
            bool jsonIsDeleted;
            bool jsonIsDirectory;
            string jsonHash;
            string jsonSize;
            string jsonStorageKey;

            if (json.Count > 0)
            {
                jsonToPath = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataCloudPath, null);
                if (jsonToPath == String.Empty)
                {
                    jsonToPath = null;
                }
                else if (string.IsNullOrWhiteSpace(jsonToPath))
                {
                    jsonToPath = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataToPath, null);
                }
                jsonFromPath = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataFromPath, null);
                if (jsonFromPath == String.Empty)
                {
                    jsonFromPath = null;
                }               
                jsonTargetPath = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataFileTarget, null);
                if (jsonTargetPath == String.Empty)
                {
                    jsonTargetPath = null;
                }
                jsonRevision = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataFileRevision, null);
                DateTime jsonCreationDateTemp = ((DateTime)json.GetValueOrDefault(CLDefinitions.CLMetadataFileCreateDate, new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc)));
                jsonCreationDate = (jsonCreationDateTemp.Ticks == FileConstants.InvalidUtcTimeTicks ? null : jsonCreationDateTemp.ToUniversalTime().ToString("o"));
                DateTime jsonModifiedDateTemp = ((DateTime)json.GetValueOrDefault(CLDefinitions.CLMetadataFileModifiedDate, new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc)));
                jsonModifiedDate = (jsonModifiedDateTemp.Ticks == FileConstants.InvalidUtcTimeTicks ? null : jsonModifiedDateTemp.ToUniversalTime().ToString("o"));
                jsonIsDeleted = (bool)json.GetValueOrDefault(CLDefinitions.CLMetadataFileIsDeleted, false);
                jsonIsDirectory = (bool)json.GetValueOrDefault(CLDefinitions.CLMetadataFileIsDirectory, false);
                jsonHash = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataFileHash, null);
                long jsonSizeTemp = (long)json.GetValueOrDefault(CLDefinitions.CLMetadataFileSize, long.MinValue);
                jsonSize = (jsonSizeTemp == long.MinValue ? null : jsonSizeTemp.ToString());
                jsonStorageKey = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataStorageKey, null);
            }
            else
            {
                jsonToPath = null;
                jsonFromPath = null;
                jsonTargetPath = null;
                jsonRevision = null;
                jsonCreationDate = null;
                jsonModifiedDate = null;
                jsonIsDeleted = false;
                jsonIsDirectory = false;
                jsonHash = null;
                jsonSize = null;
                jsonStorageKey = null;
            }

            CLMetadataProcessedInternals processedInternals = new CLMetadataProcessedInternals(jsonCreationDate,
                jsonModifiedDate,
                jsonSize);

            Func<FileMetadataHashableProperties> buildMetadataProperties = () =>
                {
                    return new FileMetadataHashableProperties(jsonIsDirectory,
                        processedInternals.ModifiedDate,
                        processedInternals.CreationDate,
                        processedInternals.Size);
                };
            Func<FileMetadata> buildMetadata = () =>
                {
                    return new FileMetadata()
                    {
                        HashableProperties = buildMetadataProperties(),
                        LinkTargetPath = jsonTargetPath,
                        Revision = jsonRevision,
                        StorageKey = jsonStorageKey
                    };
                };
            Func<FileChangeType> getChangeType = () =>
                {
                    return header == null
                        || header.Action == null
                            ? FileChangeType.Modified
                            : (CLDefinitions.SyncHeaderDeletions.Contains(header.Action)
                                ? FileChangeType.Deleted
                                : (CLDefinitions.SyncHeaderCreations.Contains(header.Action)
                                    ? FileChangeType.Created
                                    : (CLDefinitions.SyncHeaderRenames.Contains(header.Action)
                                        ? FileChangeType.Renamed
                                        : FileChangeType.Modified)));
                };

            string cloudPath = this.GetCloudPath();

            if (existingChange != null)
            {
                this.ChangeReference = existingChange;

                if (this.ChangeReference.Metadata == null)
                {
                    this.ChangeReference.Metadata = buildMetadata();
                }
                else
                {
                    this.ChangeReference.Metadata.HashableProperties = buildMetadataProperties();
                    this.ChangeReference.Metadata.LinkTargetPath = jsonTargetPath;
                    this.ChangeReference.Metadata.Revision = jsonRevision;
                    this.ChangeReference.Metadata.StorageKey = jsonStorageKey;
                }

                this.ChangeReference.Direction = direction;
                this.ChangeReference.NewPath = cloudPath + jsonToPath;
                this.ChangeReference.OldPath = string.IsNullOrWhiteSpace(jsonFromPath) ? null : cloudPath + jsonFromPath;
                this.ChangeReference.Type = getChangeType();
            }
            else
            {
                this.ChangeReference = new FileChange()
                {
                    Direction = direction,
                    Metadata = buildMetadata(),
                    NewPath = cloudPath + jsonToPath,
                    OldPath = string.IsNullOrWhiteSpace(jsonFromPath) ? null : cloudPath + jsonFromPath,
                    Type = getChangeType()
                };
            }
        }

        public class CLMetadataProcessedInternals
        {
            public Nullable<DateTime> CreationDate { get; private set; }
            public Nullable<DateTime> ModifiedDate { get; private set; }
            public Nullable<long> Size { get; private set; }

            public CLMetadataProcessedInternals(string CreationDate, string ModifiedDate, string Size)
            {
                DateTime convertedCreationDate;
                if (DateTime.TryParse(CreationDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out convertedCreationDate)) // ISO 8601
                {
                    this.CreationDate = convertedCreationDate;
                }
                DateTime convertedModifiedDate;
                if (DateTime.TryParse(ModifiedDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out convertedModifiedDate)) // ISO 8601
                {
                    this.ModifiedDate = convertedModifiedDate;
                }
                long convertedSize;
                if (long.TryParse(Size, out convertedSize))
                {
                    this.Size = convertedSize;
                }
            }
        }

        public CLMetadata(Func<string> getLastSyncId, Func<string> getCloudPath, FileSystemItem fsItem)
        {
            if (fsItem != null)
            {
                this.ChangeReference = fsItem.ChangeReference;
            }

            this.GetLastSyncId = getLastSyncId;
            this.GetCloudPath = getCloudPath;

            //TODO: Implement this constructor when we have a FileSystemItem from the index service.
            //this.Path = fsItem.Path;
            //this.CreateDate = fsItem.CreateDate;
            //this.ModifiedDate = fsItem.ModifiedDate;
            //this.Revision = fsItem.Revision;
            //this.IsDeleted = (fsItem.Is_Deleted).BoolValue();
            //this.IsDirectory = (fsItem.Is_Directory).BoolValue();
            //this.Hash = fsItem.Md5hash;
            //this.IsPending = (fsItem.IsPending).BoolValue();
            //this.Size = fsItem.Size;
            //this.TargetPath = fsItem.targetPath;
        }

        //- (id)initWithAttributesFromPath:(NSString *)path
        /* path is not sufficient to build a metadata object */
        /*public CLMetadata(string path)
        {
            //self = [super init];
            //if (self) {
            //    NSDictionary *fsItemAttributes = [NSDictionary attributesForItemAtPath:path];
            //    self.path         = [fsItemAttributes objectForKey:CLMetadataCloudPath];
            //    self.isDirectory  = [[fsItemAttributes objectForKey:CLMetadataFileIsDirectory] boolValue];
            //    self.createDate   = [fsItemAttributes objectForKey:CLMetadataFileCreateDate];
            //    self.modifiedDate = [fsItemAttributes objectForKey:CLMetadataFileModifiedDate];
            //    self.revision     = [fsItemAttributes objectForKey:CLMetadataFileRevision];
            //    if (self.isDirectory) {
            //        if ([self.path hasSuffix:@"/"] == NO){
            //            self.path = [self.path stringByAppendingString:@"/"];
            //        }
            //    }else {
            //        self.hash         = [fsItemAttributes objectForKey:CLMetadataFileHash];
            //        self.size         = [fsItemAttributes objectForKey:CLMetadataFileSize];
            //    }
            //}
            //return self;
            CLError err = null;
            try
            {
                if (File.Exists(path))
                {
                    this.Path = path;
                    this.IsDirectory = true;
                    this.CreateDate = Directory.GetCreationTimeUtc(path).ToString("o");        // ISO 8601 format
                    this.ModifiedDate = Directory.GetLastWriteTimeUtc(path).ToString("o");        // ISO 8601 format
                    this.Revision = "";     //TODO: ???

                    FileAttributes attr = System.IO.File.GetAttributes(path);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        if (!this.Path.EndsWith("\\"))
                        {
                            this.Path += "\\";
                        }
                    }
                    else
                    {
                        this.Hash = GetMD5HashFromFile(path);                           // may take a LONG time
                        FileInfo info = new FileInfo(path);
                        this.Size = info.Length.ToString();
                    }
                }
                else
                {
                    err += new Exception("Item <" + path + "> does not exist.");
                }
            }
            catch (Exception e)
            {
                err += e;
            }

            if (err != null)
            {
                CLTrace.Instance.writeToLog(1, "CLMetadata: CLMetadata: ERROR: {0}, Code: {1}.", err.errorDescription, err.errorCode);
            }
        }*/

        /* MD5 should be retrieved under sync-processing lock */
        /*
        public static string GetMD5HashFromFile(string filename)
        {
            FileStream file = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite); 
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var buffer = md5.ComputeHash(file);
                file.Close();

                var sb = new StringBuilder();
                for (int i = 0; i < buffer.Length; i++)
                {
                    sb.Append(buffer[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }*/

        //- (NSString *)description {
        public string description()
        {
            // return [NSString stringWithFormat:@"\tStatic Path: %@\n\tTo Path: %@\n\tFrom Path: %@\n\tCreate Date: %@\n\tModified Date: %@\n\tSize: %@\n\tHash: %@", self.path, self.toPath, self.fromPath, self.createDate, self.modifiedDate, self.size, self.hash];
            return String.Format("\tStatic Path: {0}\n\tTo Path: {1}\n\tFrom Path: {2}\n\tCreate Date: {3}\n\tModified Date: {4}\n\tSize: {5}\n\tHash: {6}", Path, ToPath, FromPath, CreateDate, ModifiedDate, Size, Hash);
        }

        public static Dictionary<string, object> DictionaryFromMetadataItem(CLMetadata item)
        {
            //NSMutableDictionary metadata = NSMutableDictionary.Dictionary();
            //if (item.Path != null) {
            //    metadata.SetObjectForKey(item.Path, CLDefinitions.CLMetadataCloudPath);
            //}

            //if (item.ToPath != null) {
            //    metadata.SetObjectForKey(item.ToPath, CLDefinitions.CLMetadataToPath);
            //}

            //if (item.FromPath != null) {
            //    metadata.SetObjectForKey(item.FromPath, CLDefinitions.CLMetadataFromPath);
            //}

            //if (item.Revision != null) {
            //    metadata.SetObjectForKey(item.TargetPath, CLDefinitions.CLMetadataFileTarget);
            //}

            //if (item.Revision != null) {
            //    metadata.SetObjectForKey(item.Revision, CLDefinitions.CLMetadataFileRevision);
            //}

            //if (item.CreateDate != null) {
            //    metadata.SetObjectForKey(item.CreateDate, CLDefinitions.CLMetadataFileCreateDate);
            //}

            //if (item.ModifiedDate != null) {
            //    metadata.SetObjectForKey(item.ModifiedDate, CLDefinitions.CLMetadataFileModifiedDate);
            //}

            //if (item.IsDeleted == 0 || item.IsDeleted == 1) {
            //    metadata.SetObjectForKey(NSNumber.NumberWithBool(item.IsDeleted), CLDefinitions.CLMetadataFileIsDeleted);
            //}

            //if (item.IsDirectory == 0 || item.IsDirectory == 1) {
            //    metadata.SetObjectForKey(NSNumber.NumberWithBool(item.IsDirectory), CLDefinitions.CLMetadataFileIsDirectory);
            //}

            //if (item.Hash != null) {
            //    metadata.SetObjectForKey(item.Hash, CLDefinitions.CLMetadataFileHash);
            //}

            //if (item.Size != null) {
            //    metadata.SetObjectForKey(item.Size, CLDefinitions.CLMetadataFileSize);
            //}

            //if (item.Storage_key != null) {
            //    metadata.SetObjectForKey(item.Storage_key, CLDefinitions.CLMetadataStorageKey);
            //}

            //if (item.LastEventID != null) {
            //    metadata.SetObjectForKey(item.LastEventId, CLDefinitions.CLMetadataLastEventID);
            //}

            //return metadata;

            Dictionary<string, object> metadata = new Dictionary<string, object>();
            metadata.Add(CLDefinitions.CLMetadataCloudPath, item.Path);
            metadata.Add(CLDefinitions.CLMetadataToPath, item.ToPath);
            metadata.Add(CLDefinitions.CLMetadataFromPath, item.FromPath);
            metadata.Add(CLDefinitions.CLMetadataFileTarget, item.TargetPath);
            metadata.Add(CLDefinitions.CLMetadataFileRevision, item.Revision);
            metadata.Add(CLDefinitions.CLMetadataFileCreateDate, item.CreateDate);
            metadata.Add(CLDefinitions.CLMetadataFileModifiedDate, item.ModifiedDate);
            metadata.Add(CLDefinitions.CLMetadataFileIsDeleted, item.IsDeleted);
            metadata.Add(CLDefinitions.CLMetadataFileIsDirectory, item.IsDirectory);
            metadata.Add(CLDefinitions.CLMetadataFileHash, item.Hash);
            metadata.Add(CLDefinitions.CLMetadataFileSize, item.Size);
            metadata.Add(CLDefinitions.CLMetadataStorageKey, item.Storage_key);
            metadata.Add(CLDefinitions.CLMetadataLastEventID, item.LastEventID);

            return metadata;
        }


        ////- (BOOL)isLink
        //public bool IsLink()
        //{
        //    //if (self.linkTargetPath != nil) {  // bug?
        //    if (TargetPath != null)
        //    {
        //        // return YES;
        //        return true;
        //    }
        //    //return NO;
        //    return false;
        //}
        public bool IsLink
        {
            get
            {
                return TargetPath != null;
            }
        }
    }
}
