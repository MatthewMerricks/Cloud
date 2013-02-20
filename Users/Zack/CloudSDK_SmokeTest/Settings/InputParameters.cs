﻿//------------------------------------------------------------------------------
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
namespace CloudSDK_SmokeTest.Settings {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.cloud.com/InputParameters.xsd")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://www.cloud.com/InputParameters.xsd", IsNullable=false)]
    public partial class SmokeTest {
        
        private Copyright copyrightField;
        
        private InputParams inputParamsField;
        
        private Scenario scenarioField;
        
        /// <remarks/>
        public Copyright Copyright {
            get {
                return this.copyrightField;
            }
            set {
                this.copyrightField = value;
            }
        }
        
        /// <remarks/>
        public InputParams InputParams {
            get {
                return this.inputParamsField;
            }
            set {
                this.inputParamsField = value;
            }
        }
        
        /// <remarks/>
        public Scenario Scenario {
            get {
                return this.scenarioField;
            }
            set {
                this.scenarioField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
    public partial class Copyright {
        
        private string fileNameField;
        
        private CopyrightCopyright copyright1Field;
        
        private string creatorField;
        
        /// <remarks/>
        public string FileName {
            get {
                return this.fileNameField;
            }
            set {
                this.fileNameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Copyright")]
        public CopyrightCopyright Copyright1 {
            get {
                return this.copyright1Field;
            }
            set {
                this.copyright1Field = value;
            }
        }
        
        /// <remarks/>
        public string Creator {
            get {
                return this.creatorField;
            }
            set {
                this.creatorField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.cloud.com/InputParameters.xsd")]
    public enum CopyrightCopyright {
        
        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("Implementation of InputParameters.xsd XML Schema. Cloud. Copyright (c) Cloud.com." +
            " All rights reserved.")]
        ImplementationofInputParametersxsdXMLSchemaCloudCopyrightcCloudcomAllrightsreserved,
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
    public partial class ModificationObject {
        
        private ModificationObjectType typeField;
        
        /// <remarks/>
        public ModificationObjectType type {
            get {
                return this.typeField;
            }
            set {
                this.typeField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.cloud.com/InputParameters.xsd")]
    public enum ModificationObjectType {
        
        /// <remarks/>
        File,
        
        /// <remarks/>
        Folder,
        
        /// <remarks/>
        SyncBox,
        
        /// <remarks/>
        Session,
        
        /// <remarks/>
        Plan,
        
        /// <remarks/>
        None,
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(ListItems))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(HttpTest))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(CreateSyncBox))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(DownloadAllSyncBoxContent))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(Undelete))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(Rename))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(Deletion))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(Creation))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
    public partial class SmokeTask {
        
        private SmokeTaskType typeField;
        
        private SmokeTask innerTaskField;
        
        private ModificationObject objectTypeField;
        
        /// <remarks/>
        public SmokeTaskType type {
            get {
                return this.typeField;
            }
            set {
                this.typeField = value;
            }
        }
        
        /// <remarks/>
        public SmokeTask InnerTask {
            get {
                return this.innerTaskField;
            }
            set {
                this.innerTaskField = value;
            }
        }
        
        /// <remarks/>
        public ModificationObject ObjectType {
            get {
                return this.objectTypeField;
            }
            set {
                this.objectTypeField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.cloud.com/InputParameters.xsd")]
    public enum SmokeTaskType {
        
        /// <remarks/>
        CreateSyncBox,
        
        /// <remarks/>
        Creation,
        
        /// <remarks/>
        Deletion,
        
        /// <remarks/>
        DownloadAllSyncBoxContent,
        
        /// <remarks/>
        Rename,
        
        /// <remarks/>
        FileUndelete,
        
        /// <remarks/>
        HttpTest,
        
        /// <remarks/>
        ListItems,
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
    public partial class ListItems : SmokeTask {
        
        private ListItemsListType listTypeField;
        
        private int expectedCountField;
        
        private bool expectedCountFieldSpecified;
        
        /// <remarks/>
        public ListItemsListType ListType {
            get {
                return this.listTypeField;
            }
            set {
                this.listTypeField = value;
            }
        }
        
        /// <remarks/>
        public int ExpectedCount {
            get {
                return this.expectedCountField;
            }
            set {
                this.expectedCountField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ExpectedCountSpecified {
            get {
                return this.expectedCountFieldSpecified;
            }
            set {
                this.expectedCountFieldSpecified = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.cloud.com/InputParameters.xsd")]
    public enum ListItemsListType {
        
        /// <remarks/>
        Sessions,
        
        /// <remarks/>
        Plans,
        
        /// <remarks/>
        SyncBoxes,
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
    public partial class HttpTest : SmokeTask {
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
    public partial class CreateSyncBox : SmokeTask {
        
        private bool createNewField;
        
        /// <remarks/>
        public bool CreateNew {
            get {
                return this.createNewField;
            }
            set {
                this.createNewField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
    public partial class DownloadAllSyncBoxContent : SmokeTask {
        
        private string folderPathField;
        
        private string filePathField;
        
        /// <remarks/>
        public string FolderPath {
            get {
                return this.folderPathField;
            }
            set {
                this.folderPathField = value;
            }
        }
        
        /// <remarks/>
        public string FilePath {
            get {
                return this.filePathField;
            }
            set {
                this.filePathField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
    public partial class Undelete : SmokeTask {
        
        private string fullPathField;
        
        private string fileNameField;
        
        private string versionField;
        
        private bool isFileField;
        
        /// <remarks/>
        public string FullPath {
            get {
                return this.fullPathField;
            }
            set {
                this.fullPathField = value;
            }
        }
        
        /// <remarks/>
        public string FileName {
            get {
                return this.fileNameField;
            }
            set {
                this.fileNameField = value;
            }
        }
        
        /// <remarks/>
        public string Version {
            get {
                return this.versionField;
            }
            set {
                this.versionField = value;
            }
        }
        
        /// <remarks/>
        public bool IsFile {
            get {
                return this.isFileField;
            }
            set {
                this.isFileField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
    public partial class Rename : SmokeTask {
        
        private string oldNameField;
        
        private string newNameField;
        
        private string relativeDirectoryPathField;
        
        /// <remarks/>
        public string OldName {
            get {
                return this.oldNameField;
            }
            set {
                this.oldNameField = value;
            }
        }
        
        /// <remarks/>
        public string NewName {
            get {
                return this.newNameField;
            }
            set {
                this.newNameField = value;
            }
        }
        
        /// <remarks/>
        public string RelativeDirectoryPath {
            get {
                return this.relativeDirectoryPathField;
            }
            set {
                this.relativeDirectoryPathField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
    public partial class Deletion : SmokeTask {
        
        private string nameField;
        
        private string fullNameField;
        
        private string relativePathField;
        
        private string fullPathField;
        
        private bool deleteAllField;
        
        private bool deleteAllFieldSpecified;
        
        private int deleteCountField;
        
        private bool deleteCountFieldSpecified;
        
        private long idField;
        
        private bool idFieldSpecified;
        
        /// <remarks/>
        public string Name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        public string FullName {
            get {
                return this.fullNameField;
            }
            set {
                this.fullNameField = value;
            }
        }
        
        /// <remarks/>
        public string RelativePath {
            get {
                return this.relativePathField;
            }
            set {
                this.relativePathField = value;
            }
        }
        
        /// <remarks/>
        public string FullPath {
            get {
                return this.fullPathField;
            }
            set {
                this.fullPathField = value;
            }
        }
        
        /// <remarks/>
        public bool DeleteAll {
            get {
                return this.deleteAllField;
            }
            set {
                this.deleteAllField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool DeleteAllSpecified {
            get {
                return this.deleteAllFieldSpecified;
            }
            set {
                this.deleteAllFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        public int DeleteCount {
            get {
                return this.deleteCountField;
            }
            set {
                this.deleteCountField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool DeleteCountSpecified {
            get {
                return this.deleteCountFieldSpecified;
            }
            set {
                this.deleteCountFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        public long ID {
            get {
                return this.idField;
            }
            set {
                this.idField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool IDSpecified {
            get {
                return this.idFieldSpecified;
            }
            set {
                this.idFieldSpecified = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
    public partial class Creation : SmokeTask {
        
        private string nameField;
        
        private string pathField;
        
        /// <remarks/>
        public string Name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        public string Path {
            get {
                return this.pathField;
            }
            set {
                this.pathField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
    public partial class ParallelTaskSet {
        
        private SmokeTask[] itemsField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("SmokeTask")]
        public SmokeTask[] Items {
            get {
                return this.itemsField;
            }
            set {
                this.itemsField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
    public partial class Scenario {
        
        private ParallelTaskSet[] itemsField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("ScenarioTasks")]
        public ParallelTaskSet[] Items {
            get {
                return this.itemsField;
            }
            set {
                this.itemsField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
    public partial class InputParams {
        
        private string aPI_KeyField;
        
        private string aPI_SecretField;
        
        private int activeSyncBoxIDField;
        
        private string activeSync_FolderField;
        
        private string activeSync_TraceFolderField;
        
        private int manualSyncBoxIDField;
        
        private string manualSync_FolderField;
        
        private string manualSync_TraceFolderField;
        
        private string tokenField;
        
        private int traceTypeField;
        
        private int traceLevelField;
        
        private string settingsPathField;
        
        private string fileNameMappingFileField;
        
        /// <remarks/>
        public string API_Key {
            get {
                return this.aPI_KeyField;
            }
            set {
                this.aPI_KeyField = value;
            }
        }
        
        /// <remarks/>
        public string API_Secret {
            get {
                return this.aPI_SecretField;
            }
            set {
                this.aPI_SecretField = value;
            }
        }
        
        /// <remarks/>
        public int ActiveSyncBoxID {
            get {
                return this.activeSyncBoxIDField;
            }
            set {
                this.activeSyncBoxIDField = value;
            }
        }
        
        /// <remarks/>
        public string ActiveSync_Folder {
            get {
                return this.activeSync_FolderField;
            }
            set {
                this.activeSync_FolderField = value;
            }
        }
        
        /// <remarks/>
        public string ActiveSync_TraceFolder {
            get {
                return this.activeSync_TraceFolderField;
            }
            set {
                this.activeSync_TraceFolderField = value;
            }
        }
        
        /// <remarks/>
        public int ManualSyncBoxID {
            get {
                return this.manualSyncBoxIDField;
            }
            set {
                this.manualSyncBoxIDField = value;
            }
        }
        
        /// <remarks/>
        public string ManualSync_Folder {
            get {
                return this.manualSync_FolderField;
            }
            set {
                this.manualSync_FolderField = value;
            }
        }
        
        /// <remarks/>
        public string ManualSync_TraceFolder {
            get {
                return this.manualSync_TraceFolderField;
            }
            set {
                this.manualSync_TraceFolderField = value;
            }
        }
        
        /// <remarks/>
        public string Token {
            get {
                return this.tokenField;
            }
            set {
                this.tokenField = value;
            }
        }
        
        /// <remarks/>
        public int TraceType {
            get {
                return this.traceTypeField;
            }
            set {
                this.traceTypeField = value;
            }
        }
        
        /// <remarks/>
        public int TraceLevel {
            get {
                return this.traceLevelField;
            }
            set {
                this.traceLevelField = value;
            }
        }
        
        /// <remarks/>
        public string SettingsPath {
            get {
                return this.settingsPathField;
            }
            set {
                this.settingsPathField = value;
            }
        }
        
        /// <remarks/>
        public string FileNameMappingFile {
            get {
                return this.fileNameMappingFileField;
            }
            set {
                this.fileNameMappingFileField = value;
            }
        }
    }
}
