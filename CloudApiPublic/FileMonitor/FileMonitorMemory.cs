﻿
      //FileMonitorMemory.xsd and resulting generated code
      //Cloud

      //Created by DavidBruck.

      //Copyright (c) Cloud.com. All rights reserved.

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18034
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.0.30319.17929.
// 
namespace CloudApiPublic.FileMonitor
{
    using System.Xml.Serialization;


    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.cloud.com/FileMonitorMemory.xsd")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.cloud.com/FileMonitorMemory.xsd", IsNullable = false)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class FileMonitorMemory
    {

        private Copyright copyrightField;

        private Entry[] entriesField;

        /// <remarks/>
        public Copyright Copyright
        {
            get
            {
                return this.copyrightField;
            }
            set
            {
                this.copyrightField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Entries")]
        public Entry[] Entries
        {
            get
            {
                return this.entriesField;
            }
            set
            {
                this.entriesField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.cloud.com/FileMonitorMemory.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class Copyright
    {

        private string fileNameField;

        private string creatorField;

        /// <remarks/>
        public string FileName
        {
            get
            {
                return this.fileNameField;
            }
            set
            {
                this.fileNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Copyright")]
        public string Value
        {
            get
            {
                return "Implementation of FileMonitorMemory.xsd XML Schema. Cloud. Copyright (c) Cloud.com. All rights reserved.";
            }
            set
            {
            }
        }

        /// <remarks/>
        public string Creator
        {
            get
            {
                return this.creatorField;
            }
            set
            {
                this.creatorField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(WatcherChangeRenamed))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(WatcherChangeChanged))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(WatcherChangeDeleted))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(WatcherChangeCreated))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.cloud.com/FileMonitorMemory.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class WatcherChangeType
    {
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.cloud.com/FileMonitorMemory.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class WatcherChangeRenamed : WatcherChangeType
    {
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.cloud.com/FileMonitorMemory.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class WatcherChangeChanged : WatcherChangeType
    {
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.cloud.com/FileMonitorMemory.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class WatcherChangeDeleted : WatcherChangeType
    {
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.cloud.com/FileMonitorMemory.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class WatcherChangeCreated : WatcherChangeType
    {
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(CheckMetadataEntry))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(WatcherChangedEntry))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.cloud.com/FileMonitorMemory.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class Entry
    {
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.cloud.com/FileMonitorMemory.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class CheckMetadataEntry : Entry
    {

        private WatcherChangeType oldChangeTypeField;

        private bool oldExistsField;

        private bool oldExistsFieldSpecified;

        private bool newExistsField;

        private bool isFolderField;

        private string oldPathField;

        private string newPathField;

        private long sizeField;

        private bool sizeFieldSpecified;

        private System.DateTime lastTimeField;

        private bool lastTimeFieldSpecified;

        private System.DateTime creationTimeField;

        private bool creationTimeFieldSpecified;

        private bool oldIndexedField;

        private bool oldIndexedFieldSpecified;

        private bool newIndexedField;

        private bool newIndexedFieldSpecified;

        private bool possibleRenameField;

        private bool possibleRenameFieldSpecified;

        private WatcherChangeType newChangeTypeField;

        /// <remarks/>
        public WatcherChangeType OldChangeType
        {
            get
            {
                return this.oldChangeTypeField;
            }
            set
            {
                this.oldChangeTypeField = value;
            }
        }

        /// <remarks/>
        public bool OldExists
        {
            get
            {
                return this.oldExistsField;
            }
            set
            {
                this.oldExistsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool OldExistsSpecified
        {
            get
            {
                return this.oldExistsFieldSpecified;
            }
            set
            {
                this.oldExistsFieldSpecified = value;
            }
        }

        /// <remarks/>
        public bool NewExists
        {
            get
            {
                return this.newExistsField;
            }
            set
            {
                this.newExistsField = value;
            }
        }

        /// <remarks/>
        public bool IsFolder
        {
            get
            {
                return this.isFolderField;
            }
            set
            {
                this.isFolderField = value;
            }
        }

        /// <remarks/>
        public string OldPath
        {
            get
            {
                return this.oldPathField;
            }
            set
            {
                this.oldPathField = value;
            }
        }

        /// <remarks/>
        public string NewPath
        {
            get
            {
                return this.newPathField;
            }
            set
            {
                this.newPathField = value;
            }
        }

        /// <remarks/>
        public long Size
        {
            get
            {
                return this.sizeField;
            }
            set
            {
                this.sizeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool SizeSpecified
        {
            get
            {
                return this.sizeFieldSpecified;
            }
            set
            {
                this.sizeFieldSpecified = value;
            }
        }

        /// <remarks/>
        public System.DateTime LastTime
        {
            get
            {
                return this.lastTimeField;
            }
            set
            {
                this.lastTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool LastTimeSpecified
        {
            get
            {
                return this.lastTimeFieldSpecified;
            }
            set
            {
                this.lastTimeFieldSpecified = value;
            }
        }

        /// <remarks/>
        public System.DateTime CreationTime
        {
            get
            {
                return this.creationTimeField;
            }
            set
            {
                this.creationTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool CreationTimeSpecified
        {
            get
            {
                return this.creationTimeFieldSpecified;
            }
            set
            {
                this.creationTimeFieldSpecified = value;
            }
        }

        /// <remarks/>
        public bool OldIndexed
        {
            get
            {
                return this.oldIndexedField;
            }
            set
            {
                this.oldIndexedField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool OldIndexedSpecified
        {
            get
            {
                return this.oldIndexedFieldSpecified;
            }
            set
            {
                this.oldIndexedFieldSpecified = value;
            }
        }

        /// <remarks/>
        public bool NewIndexed
        {
            get
            {
                return this.newIndexedField;
            }
            set
            {
                this.newIndexedField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool NewIndexedSpecified
        {
            get
            {
                return this.newIndexedFieldSpecified;
            }
            set
            {
                this.newIndexedFieldSpecified = value;
            }
        }

        /// <remarks/>
        public bool PossibleRename
        {
            get
            {
                return this.possibleRenameField;
            }
            set
            {
                this.possibleRenameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool PossibleRenameSpecified
        {
            get
            {
                return this.possibleRenameFieldSpecified;
            }
            set
            {
                this.possibleRenameFieldSpecified = value;
            }
        }

        /// <remarks/>
        public WatcherChangeType NewChangeType
        {
            get
            {
                return this.newChangeTypeField;
            }
            set
            {
                this.newChangeTypeField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.cloud.com/FileMonitorMemory.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class WatcherChangedEntry : Entry
    {

        private string oldPathField;

        private string newPathField;

        private WatcherChangeType[] typesField;

        private bool folderOnlyField;

        /// <remarks/>
        public string OldPath
        {
            get
            {
                return this.oldPathField;
            }
            set
            {
                this.oldPathField = value;
            }
        }

        /// <remarks/>
        public string NewPath
        {
            get
            {
                return this.newPathField;
            }
            set
            {
                this.newPathField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Type", IsNullable = false)]
        public WatcherChangeType[] Types
        {
            get
            {
                return this.typesField;
            }
            set
            {
                this.typesField = value;
            }
        }

        /// <remarks/>
        public bool FolderOnly
        {
            get
            {
                return this.folderOnlyField;
            }
            set
            {
                this.folderOnlyField = value;
            }
        }
    }
}