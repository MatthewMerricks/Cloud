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

        private string _version;
        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;
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
                this.Path = (string)json[CLDefinitions.CLMetadataCloudPath];
                this.ToPath = (string)json[CLDefinitions.CLMetadataToPath];
                this.FromPath = (string)json[CLDefinitions.CLMetadataFromPath];
                this.Revision = (string)json[CLDefinitions.CLMetadataFileRevision];
                this.CreateDate = (string)json[CLDefinitions.CLMetadataFileCreateDate];
                this.ModifiedDate = (string)json[CLDefinitions.CLMetadataFileModifiedDate];
                this.IsDeleted = (bool)json[CLDefinitions.CLMetadataFileIsDeleted];
                this.IsDirectory = (bool)json[CLDefinitions.CLMetadataFileIsDirectory];
                this.Hash = (string)json[CLDefinitions.CLMetadataFileHash];
                this.Size = (string)json[CLDefinitions.CLMetadataFileSize];
                this.Version = (string)json[CLDefinitions.CLMetadataVersion];
                this.Storage_key = (string)json[CLDefinitions.CLMetadataStorageKey];
                this.LastEventID = (string)json[CLDefinitions.CLMetadataLastEventID];
            }
        }

        public CLMetadata(bool /*FileSystemItem*/ fsItem)
        {
            //this.Path = fsItem.Path;
            //this.CreateDate = fsItem.CreateDate;
            //this.ModifiedDate = fsItem.ModifiedDate;
            //this.Revision = fsItem.Revision;
            //this.IsDeleted = (fsItem.Is_Deleted).BoolValue();
            //this.IsDirectory = (fsItem.Is_Directory).BoolValue();
            //this.Hash = fsItem.Md5hash;
            //this.IsPending = (fsItem.IsPending).BoolValue();
            //this.Size = fsItem.Size;
        }
        public static object DictionaryFromMetadataItem(CLMetadata item)
        {
            //NSMutableDictionary metadata = NSMutableDictionary.Dictionary();
            //if (item.Path != null) {
            //    metadata.SetObjectForKey(item.Path, "path");
            //}

            //if (item.ToPath != null) {
            //    metadata.SetObjectForKey(item.ToPath, "to_path");
            //}

            //if (item.FromPath != null) {
            //    metadata.SetObjectForKey(item.FromPath, "from_path");
            //}

            //if (item.Revision != null) {
            //    metadata.SetObjectForKey(item.Revision, "revision");
            //}

            //if (item.CreateDate != null) {
            //    metadata.SetObjectForKey(item.CreateDate, "created_date");
            //}

            //if (item.ModifiedDate != null) {
            //    metadata.SetObjectForKey(item.ModifiedDate, "modified_date");
            //}

            //if (item.IsDeleted == 0 || item.IsDeleted == 1) {
            //    metadata.SetObjectForKey(NSNumber.NumberWithBool(item.IsDeleted), "is_deleted");
            //}

            //if (item.IsDirectory == 0 || item.IsDirectory == 1) {
            //    metadata.SetObjectForKey(NSNumber.NumberWithBool(item.IsDirectory), "is_folder");
            //}

            //if (item.Hash != null) {
            //    metadata.SetObjectForKey(item.Hash, "file_hash");
            //}

            //if (item.Size != null) {
            //    metadata.SetObjectForKey(item.Size, "file_size");
            //}

            //if (item.Storage_key != null) {
            //    metadata.SetObjectForKey(item.Storage_key, "storage_key");
            //}

            //if (item.Version != null) {
            //    metadata.SetObjectForKey(item.Version, "version");
            //}

            //if (item.LastEventID != null) {
            //    metadata.SetObjectForKey(item.Version, "last_event_id");
            //}

            //return metadata;
            return new object();
        }

    }
}
