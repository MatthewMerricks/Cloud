﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18034
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System.Xml.Serialization;

// 
// This source code was auto-generated by xsd, Version=4.0.30319.17929.
// 


/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.cloud.com/InputParameters.xsd")]
[System.Xml.Serialization.XmlRootAttribute(Namespace="http://www.cloud.com/InputParameters.xsd", IsNullable=false)]
public partial class SmokeTest : object, System.ComponentModel.INotifyPropertyChanged {
    
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
            this.RaisePropertyChanged("Copyright");
        }
    }
    
    /// <remarks/>
    public InputParams InputParams {
        get {
            return this.inputParamsField;
        }
        set {
            this.inputParamsField = value;
            this.RaisePropertyChanged("InputParams");
        }
    }
    
    /// <remarks/>
    public Scenario Scenario {
        get {
            return this.scenarioField;
        }
        set {
            this.scenarioField = value;
            this.RaisePropertyChanged("Scenario");
        }
    }
    
    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    
    protected void RaisePropertyChanged(string propertyName) {
        System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
        if ((propertyChanged != null)) {
            propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
public partial class Copyright : object, System.ComponentModel.INotifyPropertyChanged {
    
    private string activeSync_FolderField;
    
    /// <remarks/>
    public string ActiveSync_Folder {
        get {
            return this.activeSync_FolderField;
        }
        set {
            this.activeSync_FolderField = value;
            this.RaisePropertyChanged("ActiveSync_Folder");
        }
    }
    
    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    
    protected void RaisePropertyChanged(string propertyName) {
        System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
        if ((propertyChanged != null)) {
            propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}

/// <remarks/>
[System.Xml.Serialization.XmlIncludeAttribute(typeof(HttpTest))]
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
public partial class SmokeTask : object, System.ComponentModel.INotifyPropertyChanged {
    
    private SmokeTaskType typeField;
    
    /// <remarks/>
    public SmokeTaskType type {
        get {
            return this.typeField;
        }
        set {
            this.typeField = value;
            this.RaisePropertyChanged("type");
        }
    }
    
    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    
    protected void RaisePropertyChanged(string propertyName) {
        System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
        if ((propertyChanged != null)) {
            propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
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
public partial class ParallelTaskSet : object, System.ComponentModel.INotifyPropertyChanged {
    
    private SmokeTask[] itemsField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("SmokeTask")]
    public SmokeTask[] Items {
        get {
            return this.itemsField;
        }
        set {
            this.itemsField = value;
            this.RaisePropertyChanged("Items");
        }
    }
    
    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    
    protected void RaisePropertyChanged(string propertyName) {
        System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
        if ((propertyChanged != null)) {
            propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
public partial class Scenario : object, System.ComponentModel.INotifyPropertyChanged {
    
    private ParallelTaskSet[] itemsField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("ScenarioTasks")]
    public ParallelTaskSet[] Items {
        get {
            return this.itemsField;
        }
        set {
            this.itemsField = value;
            this.RaisePropertyChanged("Items");
        }
    }
    
    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    
    protected void RaisePropertyChanged(string propertyName) {
        System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
        if ((propertyChanged != null)) {
            propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.cloud.com/InputParameters.xsd")]
public partial class InputParams : object, System.ComponentModel.INotifyPropertyChanged {
    
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
            this.RaisePropertyChanged("API_Key");
        }
    }
    
    /// <remarks/>
    public string API_Secret {
        get {
            return this.aPI_SecretField;
        }
        set {
            this.aPI_SecretField = value;
            this.RaisePropertyChanged("API_Secret");
        }
    }
    
    /// <remarks/>
    public int ActiveSyncBoxID {
        get {
            return this.activeSyncBoxIDField;
        }
        set {
            this.activeSyncBoxIDField = value;
            this.RaisePropertyChanged("ActiveSyncBoxID");
        }
    }
    
    /// <remarks/>
    public string ActiveSync_Folder {
        get {
            return this.activeSync_FolderField;
        }
        set {
            this.activeSync_FolderField = value;
            this.RaisePropertyChanged("ActiveSync_Folder");
        }
    }
    
    /// <remarks/>
    public string ActiveSync_TraceFolder {
        get {
            return this.activeSync_TraceFolderField;
        }
        set {
            this.activeSync_TraceFolderField = value;
            this.RaisePropertyChanged("ActiveSync_TraceFolder");
        }
    }
    
    /// <remarks/>
    public string Token {
        get {
            return this.tokenField;
        }
        set {
            this.tokenField = value;
            this.RaisePropertyChanged("Token");
        }
    }
    
    /// <remarks/>
    public int TraceType {
        get {
            return this.traceTypeField;
        }
        set {
            this.traceTypeField = value;
            this.RaisePropertyChanged("TraceType");
        }
    }
    
    /// <remarks/>
    public int TraceLevel {
        get {
            return this.traceLevelField;
        }
        set {
            this.traceLevelField = value;
            this.RaisePropertyChanged("TraceLevel");
        }
    }
    
    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    
    protected void RaisePropertyChanged(string propertyName) {
        System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
        if ((propertyChanged != null)) {
            propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
