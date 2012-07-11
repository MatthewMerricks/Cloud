using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Model;
using CloudApiPublic.Support;

namespace CloudApiPublic.Model
{
    public class FileSystemItem /*: NSManagedObject*/
    {
        private static string path;
        private static string name;
        private static string revision;
        private static string createDate;
        private static string modifiedDate;
        private static string md5hash;
        private static bool is_Directory;
        private static bool is_Deleted;
        private static bool is_Link;
        private static bool isPending;
        private static string size;
        private static string targetPath;
        private static string parent_path;
        private static List<FileSystemItem> children;
        private static FileSystemItem parent;

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

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
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

        public bool Is_Link
        {
            get
            {
                return is_Link;
            }
            set
            {
                is_Link = value;
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

        public string TargetPath
        {
            get
            {
                return targetPath;
            }
            set
            {
                targetPath = value;
            }
        }

        public string Parent_path
        {
            get
            {
                return parent_path;
            }
            set
            {
                parent_path = value;
            }
        }

        public List<FileSystemItem> Children
        {
            get { return children; }
            set { children = value; }
        }

        public FileSystemItem Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent = value;
            }
        }


        public void Log()
        {
            CLTrace.Instance.writeToLog(9, "{path: {0}.", this.Path);
            CLTrace.Instance.writeToLog(9, "{name: {0}.", this.Name);
            CLTrace.Instance.writeToLog(9, "{revision: {0}.", this.Revision);
            CLTrace.Instance.writeToLog(9, "{createdDate: {0}.", this.CreateDate);
            CLTrace.Instance.writeToLog(9, "{modifiedDate: {0}.", this.ModifiedDate);
            CLTrace.Instance.writeToLog(9, "{md5hash: {0}.", this.Md5hash);
            CLTrace.Instance.writeToLog(9, "{is_Directory: {0}.", this.Is_Directory);
            CLTrace.Instance.writeToLog(9, "{is_Deleted: {0}.", this.Is_Deleted);
            CLTrace.Instance.writeToLog(9, "{is_Link: {0}.", this.Is_Link);
            CLTrace.Instance.writeToLog(9, "{isPending: {0}.", this.IsPending);
            CLTrace.Instance.writeToLog(9, "{size: {0}.", this.Size);
            CLTrace.Instance.writeToLog(9, "{target path: {0}.", this.TargetPath);
            CLTrace.Instance.writeToLog(9, "{parent path: {0}.", this.Parent_path);
            CLTrace.Instance.writeToLog(9, "{children: {0}.", this.Children);
            CLTrace.Instance.writeToLog(9, "{parent: {0}.", this.Parent);
        }
    }
}
