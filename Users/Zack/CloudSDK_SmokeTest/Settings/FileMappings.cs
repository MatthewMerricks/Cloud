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
namespace CloudSDK_SmopkeTest.Settings {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://tempuri.org/FileMappings.xsd")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://tempuri.org/FileMappings.xsd", IsNullable=false)]
    public partial class AllMappings {
        
        private MappingRecords mappingRecordsField;
        
        /// <remarks/>
        public MappingRecords MappingRecords {
            get {
                return this.mappingRecordsField;
            }
            set {
                this.mappingRecordsField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://tempuri.org/FileMappings.xsd")]
    public partial class MappingRecords {
        
        private PathMappingElement[] itemsField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("PathMappingElement")]
        public PathMappingElement[] Items {
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
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://tempuri.org/FileMappings.xsd")]
    public partial class PathMappingElement {
        
        private string idField;
        
        private string localPathField;
        
        private string serverPathField;
        
        /// <remarks/>
        public string ID {
            get {
                return this.idField;
            }
            set {
                this.idField = value;
            }
        }
        
        /// <remarks/>
        public string LocalPath {
            get {
                return this.localPathField;
            }
            set {
                this.localPathField = value;
            }
        }
        
        /// <remarks/>
        public string ServerPath {
            get {
                return this.serverPathField;
            }
            set {
                this.serverPathField = value;
            }
        }
    }
}