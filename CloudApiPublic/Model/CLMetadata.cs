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
        private string _path;
        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                _path = value;
            }
        }

        private string _toPath;
        public string ToPath
        {
            get
            {
                return _toPath;
            }
            set
            {
                _toPath = value;
            }
        }

        private string _fromPath;
        public string FromPath
        {
            get
            {
                return _fromPath;
            }
            set
            {
                _fromPath = value;
            }
        }

        private string _targetPath;
        public string TargetPath
        {
            get { return _targetPath; }
            set { _targetPath = value; }
        }
        

        private string _revision;
        public string Revision
        {
            get
            {
                return _revision;
            }
            set
            {
                _revision = value;
            }
        }

        private string _createDate;
        public string CreateDate
        {
            get
            {
                return _createDate;
            }
            set
            {
                _createDate = value;
            }
        }

        private string _modifiedDate;
        public string ModifiedDate
        {
            get
            {
                return _modifiedDate;
            }
            set
            {
                _modifiedDate = value;
            }
        }

        private string _hash;
        public string Hash
        {
            get
            {
                return _hash;
            }
            set
            {
                _hash = value;
            }
        }

        private string _mime_type;
        public string Mime_type
        {
            get
            {
                return _mime_type;
            }
            set
            {
                _mime_type = value;
            }
        }

        private string _sid;
        public string Sid
        {
            get
            {
                return _sid;
            }
            set
            {
                _sid = value;
            }
        }

        private string _size;
        public string Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
            }
        }

        private string _storage_key;
        public string Storage_key
        {
            get
            {
                return _storage_key;
            }
            set
            {
                _storage_key = value;
            }
        }

        private string _lastEventID;
        public string LastEventID
        {
            get
            {
                return _lastEventID;
            }
            set
            {
                _lastEventID = value;
            }
        }

        private bool _isDirectory;
        public bool IsDirectory
        {
            get
            {
                return _isDirectory;
            }
            set
            {
                _isDirectory = value;
            }
        }

        private bool _isDeleted;
        public bool IsDeleted
        {
            get
            {
                return _isDeleted;
            }
            set
            {
                _isDeleted = value;
            }
        }

        private bool _isPending;
        public bool IsPending
        {
            get
            {
                return _isPending;
            }
            set
            {
                _isPending = value;
            }
        }

        public CLMetadata(Dictionary<string, object> json)
        {
            if (json.Count() > 0) {
                this.Path = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataCloudPath, null);
                this.ToPath = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataToPath, null);
                this.FromPath = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataFromPath, null);
                this.TargetPath = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataFileTarget, null);
                this.Revision = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataFileRevision, null);
                this.CreateDate = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataFileCreateDate, null);
                this.ModifiedDate = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataFileModifiedDate, null);
                this.IsDeleted = (bool)json.GetValueOrDefault(CLDefinitions.CLMetadataFileIsDeleted, false);
                this.IsDirectory = (bool)json.GetValueOrDefault(CLDefinitions.CLMetadataFileIsDirectory, false);
                this.Hash = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataFileHash, null);
                this.Size = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataFileSize, null);
                this.Storage_key = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataStorageKey, null);
                this.LastEventID = (string)json.GetValueOrDefault(CLDefinitions.CLMetadataLastEventID, null);
            }
        }

        public CLMetadata(bool /*FileSystemItem*/ fsItem)
        {
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
        public CLMetadata(string path)
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
        }

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
        } 


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


        //- (BOOL)isLink
        public bool IsLink()
        {
            //if (self.linkTargetPath != nil) {  // bug?
            if (TargetPath != null)
            {
                // return YES;
                return true;
            }
            //return NO;
            return false;
        }



    }
}
