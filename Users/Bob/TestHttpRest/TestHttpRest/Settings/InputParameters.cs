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
namespace TestHttpRest.Settings {
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
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(HttpTest))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
    public partial class SmokeTask {
        
        private SmokeTaskType typeField;
        
        private SmokeTask innerTaskField;
        
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
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.cloud.com/InputParameters.xsd")]
    public enum SmokeTaskType {
        
        /// <remarks/>
        HttpTest,
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
        [System.Xml.Serialization.XmlElementAttribute("ScenarioParallelTaskSet")]
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
        
        private string tokenField;
        
        private int traceTypeField;
        
        private int traceLevelField;
        
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
    }
}
