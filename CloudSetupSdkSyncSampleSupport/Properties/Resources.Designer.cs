﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17929
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CloudSetupSdkSyncSampleSupport.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("CloudSetupSdkSyncSampleSupport.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to    &apos;
        ///   &apos;  CloudRestart.vbs
        ///   &apos;  Cloud Windows
        ///   &apos;
        ///   &apos;  Created by BobS.
        ///   &apos;  Copyright (c) Cloud.com. All rights reserved.
        ///   &apos;
        ///
        ///   &apos; Parameter: None
        ///   &apos; Call as &quot;C:\Windows\SysWOW64\cscript.exe&quot; //B //T:5 //Nologo &quot;&lt;path to this CloudRestart.vbs file&gt;&quot;
        ///   
        ///   Option Explicit
        ///
        ///   Const ForReading = 1
        ///   Const ForWriting = 2
        ///   Const ForAppending = 8
        ///   Const DeleteReadOnly = true
        ///   
        ///   Dim objFileSys
        ///   Dim objShell
        /// 
        ///   &apos; Global variables
        ///   Dim shouldTrace
        ///   Dim shouldCloudB [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string CloudSetupSdkSyncSampleCleanup {
            get {
                return ResourceManager.GetString("CloudSetupSdkSyncSampleCleanup", resourceCulture);
            }
        }
    }
}
