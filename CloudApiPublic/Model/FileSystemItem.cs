//
//  FileSystemItem.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudApiPublic.Support;

namespace CloudApiPublic.Model
{
    public class FileSystemItem /*: NSManagedObject*/
    {
        public string Path
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
        public string Name
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.NewPath == null)
                {
                    return null;
                }
                return this.ChangeReference.NewPath.Name;
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
        public string Md5hash
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
        public bool Is_Directory
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
        public bool Is_Deleted
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
        public bool Is_Link
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.Metadata == null)
                {
                    throw new NullReferenceException("Neither ChangeReference nor its Metadata property can be null");
                }
                return this.ChangeReference.Metadata.LinkTargetPath != null;
            }
        }
        public bool IsPending
        {
            get
            {
                return this.ChangeReference != null;
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
        public string TargetPath
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.Metadata == null
                    || this.ChangeReference.Metadata.LinkTargetPath == null)
                {
                    return null;
                }
                return this.ChangeReference.Metadata.LinkTargetPath.ToString();
            }
        }
        public string Parent_path
        {
            get
            {
                if (this.ChangeReference == null
                    || this.ChangeReference.NewPath == null
                    || this.ChangeReference.NewPath.Parent == null)
                {
                    return null;
                }
                return this.ChangeReference.NewPath.Parent.ToString();
            }
        }
        public FileChange ChangeReference { get; set; }

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
        }
    }
}
