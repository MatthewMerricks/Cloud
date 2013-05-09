﻿//TraceLog.xsd and resulting generated code
//Cloud

//Created by DavidBruck.

//Copyright (c) Cloud.com. All rights reserved.

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17929
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.0.30319.17929.
// 
namespace Cloud.Static
{
    using System.Xml.Serialization;


    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.cloud.com/TraceLog.xsd")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.cloud.com/TraceLog.xsd", IsNullable = false)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class Log
    {

        private Copyright[] copyrightField;

        private Entry[] entryField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Copyright")]
        public Copyright[] Copyright
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
        [System.Xml.Serialization.XmlElementAttribute("Entry")]
        public Entry[] Entry
        {
            get
            {
                return this.entryField;
            }
            set
            {
                this.entryField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.cloud.com/TraceLog.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class Copyright
    {

        private string fileNameField;

        private CopyrightCopyright copyright1Field;

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
        public CopyrightCopyright Copyright1
        {
            get
            {
                return this.copyright1Field;
            }
            set
            {
                this.copyright1Field = value;
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
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.cloud.com/TraceLog.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public enum CopyrightCopyright
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("Implementation of TraceLog.xsd XML Schema. Cloud. Copyright (c) Cloud.com. All ri" +
            "ghts reserved.")]
        ImplementationofTraceLogxsdXMLSchemaCloudCopyrightcCloudcomAllrightsreserved,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.cloud.com/TraceLog.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class TraceFileChange
    {

        private long eventIdField;

        private bool eventIdFieldSpecified;

        private long serverUidIdField;

        private string newPathField;

        private string oldPathField;

        private bool isFolderField;

        private bool isShareField;

        private bool isShareFieldSpecified;

        private int versionField;

        private bool versionFieldSpecified;

        private string parentServerUidField;

        private string mimeTypeField;

        private int permissionsField;

        private bool permissionsFieldSpecified;

        private TraceFileChangeType typeField;

        private System.DateTime lastTimeField;

        private bool lastTimeFieldSpecified;

        private System.DateTime creationTimeField;

        private bool creationTimeFieldSpecified;

        private long sizeField;

        private bool sizeFieldSpecified;

        private bool isSyncFromField;

        private string mD5Field;

        private string storageKeyField;

        private TraceFileChange[] dependenciesField;

        /// <remarks />
        public long EventId
        {
            get
            {
                return this.eventIdField;
            }
            set
            {
                this.eventIdField = value;
            }
        }

        /// <remarks />
        public long ServerUidId
        {
            get
            {
                return this.serverUidIdField;
            }
            set
            {
                this.serverUidIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool EventIdSpecified
        {
            get
            {
                return this.eventIdFieldSpecified;
            }
            set
            {
                this.eventIdFieldSpecified = value;
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
        public bool IsShare
        {
            get
            {
                return this.isShareField;
            }
            set
            {
                this.isShareField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool IsShareSpecified
        {
            get
            {
                return this.isShareFieldSpecified;
            }
            set
            {
                this.isShareFieldSpecified = value;
            }
        }

        /// <remarks/>
        public int Version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool VersionSpecified
        {
            get
            {
                return this.versionFieldSpecified;
            }
            set
            {
                this.versionFieldSpecified = value;
            }
        }

        /// <remarks />
        public string ParentServerUid
        {
            get
            {
                return this.parentServerUidField;
            }
            set
            {
                this.parentServerUidField = value;
            }
        }

        /// <remarks />
        public string MimeType
        {
            get
            {
                return this.mimeTypeField;
            }
            set
            {
                this.mimeTypeField = value;
            }
        }

        /// <remarks/>
        public int Permissions
        {
            get
            {
                return this.permissionsField;
            }
            set
            {
                this.permissionsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool PermissionsSpecified
        {
            get
            {
                return this.permissionsFieldSpecified;
            }
            set
            {
                this.permissionsFieldSpecified = value;
            }
        }

        /// <remarks/>
        public TraceFileChangeType Type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
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
        public bool IsSyncFrom
        {
            get
            {
                return this.isSyncFromField;
            }
            set
            {
                this.isSyncFromField = value;
            }
        }

        /// <remarks/>
        public string MD5
        {
            get
            {
                return this.mD5Field;
            }
            set
            {
                this.mD5Field = value;
            }
        }

        /// <remarks/>
        public string StorageKey
        {
            get
            {
                return this.storageKeyField;
            }
            set
            {
                this.storageKeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("FileChange", IsNullable = false)]
        public TraceFileChange[] Dependencies
        {
            get
            {
                return this.dependenciesField;
            }
            set
            {
                this.dependenciesField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.cloud.com/TraceLog.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public enum TraceFileChangeType
    {

        /// <remarks/>
        Created,

        /// <remarks/>
        Deleted,

        /// <remarks/>
        Renamed,

        /// <remarks/>
        Modified,
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(FileChangeFlowEntry))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(ServerUidRevisionEntry))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(CommunicationEntry))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.cloud.com/TraceLog.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class Entry
    {

        private int typeField;

        private System.DateTime timeField;

        private int processIdField;

        private int threadIdField;

        private long syncboxIdField;

        private bool syncboxIdFieldSpecified;

        /// <remarks/>
        public int Type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        public System.DateTime Time
        {
            get
            {
                return this.timeField;
            }
            set
            {
                this.timeField = value;
            }
        }

        /// <remarks/>
        public int ProcessId
        {
            get
            {
                return this.processIdField;
            }
            set
            {
                this.processIdField = value;
            }
        }

        /// <remarks/>
        public int ThreadId
        {
            get
            {
                return this.threadIdField;
            }
            set
            {
                this.threadIdField = value;
            }
        }

        /// <remarks/>
        public long SyncboxId
        {
            get
            {
                return this.syncboxIdField;
            }
            set
            {
                this.syncboxIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool SyncboxIdSpecified
        {
            get
            {
                return this.syncboxIdFieldSpecified;
            }
            set
            {
                this.syncboxIdFieldSpecified = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.cloud.com/TraceLog.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class ServerUidRevisionEntry : Entry
    {

        private string serverUid;

        /// <remarks/>
        public string ServerUid
        {
            get
            {
                return this.serverUid;
            }
            set
            {
                this.serverUid = value;
            }
        }

        private string revision;

        /// <remarks/>
        public string Revision
        {
            get
            {
                return this.revision;
            }
            set
            {
                this.revision = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.cloud.com/TraceLog.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class FileChangeFlowEntry : Entry
    {

        private FileChangeFlowEntryPositionInFlow positionInFlowField;

        private TraceFileChange[] fileChangesField;

        /// <remarks/>
        public FileChangeFlowEntryPositionInFlow PositionInFlow
        {
            get
            {
                return this.positionInFlowField;
            }
            set
            {
                this.positionInFlowField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("FileChange", IsNullable = false)]
        public TraceFileChange[] FileChanges
        {
            get
            {
                return this.fileChangesField;
            }
            set
            {
                this.fileChangesField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.cloud.com/TraceLog.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public enum FileChangeFlowEntryPositionInFlow
    {

        /// <remarks/>
        FileMonitorAddingToQueuedChanges,

        /// <remarks/>
        FileMonitorAddingBatchToSQL,

        /// <remarks/>
        SyncRunInitialErrors,

        /// <remarks/>
        FileMonitorGrabPreprocessedQueuedChanges,

        /// <remarks/>
        FileMonitorGrabPreprocessedFailureQueue,

        /// <remarks/>
        FileMonitorGrabPreprocessedProcessingChanges,

        /// <remarks/>
        FileMonitorGrabPreprocessedFailedOutList,

        /// <remarks/>
        GrabChangesQueuedChangesAddedToSQL,

        /// <remarks/>
        GrabChangesOutputChanges,

        /// <remarks/>
        GrabChangesOutputChangesInError,

        /// <remarks/>
        InitialRunFileTransfer,

        /// <remarks/>
        SyncRunPreprocessedEventsSynchronous,

        /// <remarks/>
        SyncRunPreprocessedEventsAsynchronous,

        /// <remarks/>
        SyncRunRequeuedFailuresBeforeCommunication,
        
        /// <remarks/>
        SyncRunErrorSyncFromForCommunication,

        /// <remarks/>
        SyncRunChangesForCommunication,

        /// <remarks/>
        CommunicationCompletedChanges,

        /// <remarks/>
        CommunicationIncompletedChanges,

        /// <remarks/>
        CommunicationChangesInError,

        /// <remarks/>
        SyncRunPostCommunicationDequeuedFailures,

        /// <remarks/>
        FileMonitorAssignDependenciesFailureQueue,

        /// <remarks/>
        FileMonitorAssignDependenciesProcessingChanges,

        /// <remarks/>
        FileMonitorAssignDependenciesFailedOutList,

        /// <remarks/>
        DependencyAssignmentOutputChanges,

        /// <remarks/>
        DependencyAssignmentTopLevelErrors,

        /// <remarks/>
        SyncRunPostCommunicationSynchronous,

        /// <remarks/>
        SyncRunPostCommunicationAsynchronous,

        /// <remarks/>
        SyncRunEndThingsThatWereDependenciesToQueue,

        /// <remarks/>
        SyncRunEndRequeuedFailures,

        /// <remarks/>
        UploadDownloadSuccess,

        /// <remarks/>
        UploadDownloadFailure,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.cloud.com/TraceLog.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class CommunicationEntry : Entry
    {

        private CommunicationEntryDirection directionField;

        private string uriField;

        private CommunicationEntryHeader[] headersField;

        private string bodyField;

        private int statusCodeField;

        private bool statusCodeFieldSpecified;

        /// <remarks/>
        public CommunicationEntryDirection Direction
        {
            get
            {
                return this.directionField;
            }
            set
            {
                this.directionField = value;
            }
        }

        /// <remarks/>
        public string Uri
        {
            get
            {
                return this.uriField;
            }
            set
            {
                this.uriField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Header", IsNullable = false)]
        public CommunicationEntryHeader[] Headers
        {
            get
            {
                return this.headersField;
            }
            set
            {
                this.headersField = value;
            }
        }

        /// <remarks/>
        public string Body
        {
            get
            {
                return this.bodyField;
            }
            set
            {
                this.bodyField = value;
            }
        }

        /// <remarks/>
        public int StatusCode
        {
            get
            {
                return this.statusCodeField;
            }
            set
            {
                this.statusCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool StatusCodeSpecified
        {
            get
            {
                return this.statusCodeFieldSpecified;
            }
            set
            {
                this.statusCodeFieldSpecified = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.cloud.com/TraceLog.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public enum CommunicationEntryDirection
    {

        /// <remarks/>
        Request,

        /// <remarks/>
        Response,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.cloud.com/TraceLog.xsd")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class CommunicationEntryHeader
    {

        private string keyField;

        private string valueField;

        /// <remarks/>
        public string Key
        {
            get
            {
                return this.keyField;
            }
            set
            {
                this.keyField = value;
            }
        }

        /// <remarks/>
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }
}