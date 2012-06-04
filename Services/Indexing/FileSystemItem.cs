using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using win_client.Common;
using CloudApiPublic.Model;

namespace win_client.Services.Indexing
{
    public class FileSystemItem /*: NSManagedObject*/
    {
        private static string path;
        private static string revision;
        private static string createDate;
        private static string modifiedDate;
        private static string md5hash;
        private static bool is_Directory;
        private static bool is_Deleted;
        private static bool isPending;
        private static string size;
        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
            }
        }

        public string Revision
        {
            get
            {
                return revision;
            }
            set
            {
                revision = value;
            }
        }

        public string CreateDate
        {
            get
            {
                return createDate;
            }
            set
            {
                createDate = value;
            }
        }

        public string ModifiedDate
        {
            get
            {
                return modifiedDate;
            }
            set
            {
                modifiedDate = value;
            }
        }

        public string Md5hash
        {
            get
            {
                return md5hash;
            }
            set
            {
                md5hash = value;
            }
        }

        public bool Is_Directory
        {
            get
            {
                return is_Directory;
            }
            set
            {
                is_Directory = value;
            }
        }

        public bool Is_Deleted
        {
            get
            {
                return is_Deleted;
            }
            set
            {
                is_Deleted = value;
            }
        }

        public bool IsPending
        {
            get
            {
                return isPending;
            }
            set
            {
                isPending = value;
            }
        }

        public string Size
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
            }
        }

        public void Log()
        {
            Console.WriteLine("{path: %@}\n", this.Path);
            Console.WriteLine("{revision: %@}\n", this.Revision);
            Console.WriteLine("{createdDate: %@}\n", this.CreateDate);
            Console.WriteLine("{modifiedDate: %@}\n", this.ModifiedDate);
            Console.WriteLine("{md5hash: %@}\n", this.Md5hash);
            Console.WriteLine("{is_Directory: %@}\n", this.Is_Directory);
            Console.WriteLine("{is_Deleted: %@}\n", this.Is_Deleted);
            Console.WriteLine("{isPending: %@}\n", this.IsPending);
            Console.WriteLine("{eventType: %@}\n", this.Md5hash);
        }

        public static Dictionary<string, object> DictionaryFromFileSystemItem(FileSystemItem item)
        {
            Dictionary<string, object> metadataItem = new Dictionary<string, object>();
            string path = item.Path == null ? "" : item.Path;
            string revision = item.Revision == null ? "" : item.Revision;
            string createDate = item.CreateDate == null ? "" : item.CreateDate;
            string modifiedDate = item.ModifiedDate == null ? "" : item.ModifiedDate;
            string md5hash = item.Md5hash == null ? "" : item.Md5hash;
            string size = item.Size == null ? "0" : item.Size;
            bool isPending = item.IsPending;
            metadataItem.Add(CLDefinitions.CLMetadataCloudPath, path);
            metadataItem.Add(CLDefinitions.CLMetadataFileHash, md5hash);
            metadataItem.Add(CLDefinitions.CLMetadataFileRevision, revision);
            metadataItem.Add(CLDefinitions.CLMetadataFileCreateDate, createDate);
            metadataItem.Add(CLDefinitions.CLMetadataFileModifiedDate, modifiedDate);
            metadataItem.Add(CLDefinitions.CLMetadataFileSize, size);
            metadataItem.Add(CLDefinitions.CLMetadataIsPending, isPending);
            return metadataItem;
        }

    }
}
