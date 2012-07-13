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

// Merged 7/3/12
namespace CloudApiPublic.Model
{
    public class CLMetadata
    {
        public string Path
        {
            get
            {
                if (this.GetCloudPath == null)
                {
                    return null;
                }
                return this.GetCloudPath();
            }
        }
        public string ToPath
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.NewPath == null)
                {
                    return null;
                }
                return this.ChangeReference.NewPath.ToString();
            }
        }
        public string FromPath
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.OldPath == null)
                {
                    return null;
                }
                return this.ChangeReference.OldPath.ToString();
            }
        }
        public string TargetPath
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.LinkTargetPath == null)
                {
                    return null;
                }
                return this.ChangeReference.LinkTargetPath.ToString();
            }
        }
        public string Revision
        {
            get
            {
                if (this.ChangeReference == null)
                {
                    return null;
                }
                return this.ChangeReference.Revision;
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
                if (this.ChangeReference == null)
                {
                    return null;
                }
                return this.ChangeReference.StorageKey;
            }
        }
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
        public CLMetadata(Func<string> getLastSyncId, Func<string> getCloudPath, Dictionary<string, object> json, CLSyncHeader header, SyncDirection direction)
        {
            this.GetLastSyncId = getLastSyncId;
            this.GetCloudPath = getCloudPath;

            string jsonCloudPath;
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
            string jsonLastEventId;

            if (json.Count > 0)
            {
                jsonCloudPath = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataCloudPath, null);
                jsonToPath = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataToPath, null);
                jsonFromPath = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataFromPath, null);
                jsonTargetPath = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataFileTarget, null);
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
                long jsonLastEventIdTemp = (long)json.GetValueOrDefault(CLDefinitions.CLMetadataLastEventID, long.MinValue);
                jsonLastEventId = (jsonLastEventIdTemp == long.MinValue ? null : jsonLastEventIdTemp.ToString());
            }
            else
            {
                jsonCloudPath = null;
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
                jsonLastEventId = null;
            }

            CLMetadataProcessedInternals processedInternals = new CLMetadataProcessedInternals(this.GetCloudPath,
                jsonCreationDate,
                jsonModifiedDate,
                jsonSize,
                jsonLastEventId,
                jsonCloudPath,
                jsonFromPath,
                jsonToPath);

            this.ChangeReference = new FileChange()
            {
                Direction = direction,
                EventId = processedInternals.EventId ?? 0,
                Metadata = new FileMetadata()
                {
                    HashableProperties = new FileMetadataHashableProperties(jsonIsDirectory,
                        processedInternals.ModifiedDate,
                        processedInternals.CreationDate,
                        processedInternals.Size)
                },
                NewPath = processedInternals.RebuiltToPath,
                OldPath = processedInternals.RebuiltFromPath,
                Revision = jsonRevision,
                StorageKey = jsonStorageKey,
                Type = (header == null
                    || header.Action == null
                        ? FileChangeType.Modified
                        : (CLDefinitions.SyncHeaderDeletions.Contains(header.Action)
                            ? FileChangeType.Deleted
                            : (CLDefinitions.SyncHeaderCreations.Contains(header.Action)
                                ? FileChangeType.Created
                                : (CLDefinitions.SyncHeaderRenames.Contains(header.Action)
                                    ? FileChangeType.Renamed
                                    : FileChangeType.Modified))))
            };
        }

        public class CLMetadataProcessedInternals
        {
            public Nullable<DateTime> CreationDate { get; private set; }
            public Nullable<DateTime> ModifiedDate { get; private set; }
            public Nullable<long> Size { get; private set; }
            public Nullable<int> EventId { get; private set; }
            public string RebuiltToPath { get; private set; }
            public string RebuiltFromPath { get; private set; }

            public CLMetadataProcessedInternals(Func<string> GetCloudPath, string CreationDate, string ModifiedDate, string Size, string EventId, string CloudPath, string FromPath, string ToPath)
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
                int convertedEventId;
                if (int.TryParse(EventId, out convertedEventId))
                {
                    this.EventId = convertedEventId;
                }

                if (!string.IsNullOrWhiteSpace(CloudPath))
                {
                    FilePath cloudConvertedPath = CloudPath;

                    if (GetCloudPath != null)
                    {
                        string cloudPathCopy = GetCloudPath();

                        if (!string.IsNullOrWhiteSpace(ToPath))
                        {
                            FilePath toConvertedPath = ToPath;
                            FilePath convertedOverlaps = toConvertedPath.FindOverlappingPath(cloudConvertedPath);

                            if (FilePathComparer.Instance.Equals(cloudConvertedPath, convertedOverlaps))
                            {
                                if (!string.IsNullOrWhiteSpace(cloudPathCopy))
                                {
                                    FilePath rebuiltNewPath;
                                    rebuiltNewPath = toConvertedPath;

                                    // find the current item pair's key path which matches the old path,
                                    // but only store its child as oldPathChild
                                    while (toConvertedPath.Parent != null)
                                    {
                                        if (FilePathComparer.Instance.Equals(cloudConvertedPath, toConvertedPath.Parent))
                                        {
                                            toConvertedPath.Parent = cloudPathCopy;
                                            this.RebuiltToPath = rebuiltNewPath.ToString();
                                            break;
                                        }

                                        toConvertedPath = toConvertedPath.Parent;
                                    }
                                }
                            }
                        }
                        
                        if (!string.IsNullOrWhiteSpace(FromPath))
                        {
                            FilePath fromConvertedPath = FromPath;
                            FilePath convertedOverlaps = fromConvertedPath.FindOverlappingPath(cloudConvertedPath);

                            if (FilePathComparer.Instance.Equals(cloudConvertedPath, convertedOverlaps))
                            {
                                FilePath rebuiltNewPath;
                                rebuiltNewPath = fromConvertedPath;

                                // find the current item pair's key path which matches the old path,
                                // but only store its child as oldPathChild
                                while (fromConvertedPath.Parent != null)
                                {
                                    if (FilePathComparer.Instance.Equals(cloudConvertedPath, fromConvertedPath.Parent))
                                    {
                                        fromConvertedPath.Parent = cloudPathCopy;
                                        this.RebuiltFromPath = rebuiltNewPath.ToString();
                                        break;
                                    }

                                    fromConvertedPath = fromConvertedPath.Parent;
                                }
                            }
                        }
                    }
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
