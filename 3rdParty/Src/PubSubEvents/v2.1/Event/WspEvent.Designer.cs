﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17929
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WspEvent {
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
    internal class WspEvent {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal WspEvent() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("WspEvent.WspEvent", typeof(WspEvent).Assembly);
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
        ///   Looks up a localized string similar to Cannot deserialize type of Value object.
        /// </summary>
        internal static string CannotDeserialize {
            get {
                return ResourceManager.GetString("CannotDeserialize", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot serialize type of Value object.
        /// </summary>
        internal static string CannotSerialize {
            get {
                return ResourceManager.GetString("CannotSerialize", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SerializationData object is in read state.
        /// </summary>
        internal static string InReadState {
            get {
                return ResourceManager.GetString("InReadState", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SerializationData object is in write state.
        /// </summary>
        internal static string InWriteState {
            get {
                return ResourceManager.GetString("InWriteState", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SerializationData object must be in read state to reset.
        /// </summary>
        internal static string MustBeInReadState {
            get {
                return ResourceManager.GetString("MustBeInReadState", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SerializationData object must be in read state for ToBytes.
        /// </summary>
        internal static string MustBeInReadStateForToBytes {
            get {
                return ResourceManager.GetString("MustBeInReadStateForToBytes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SerializationData object must be in read state for ToString.
        /// </summary>
        internal static string MustBeInReadStateForToString {
            get {
                return ResourceManager.GetString("MustBeInReadStateForToString", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown type for Value object.
        /// </summary>
        internal static string UnknownType {
            get {
                return ResourceManager.GetString("UnknownType", resourceCulture);
            }
        }
    }
}
