﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17626
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common {
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
    internal class TraceCore {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal TraceCore() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.ApplicationServer.Common.TraceCore", typeof(TraceCore).Assembly);
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
        ///   Looks up a localized string similar to AppDomain unloading. AppDomain.FriendlyName {0}, ProcessName {1}, ProcessId {2}..
        /// </summary>
        internal static string AppDomainUnload {
            get {
                return ResourceManager.GetString("AppDomainUnload", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Handling an exception..
        /// </summary>
        internal static string HandledException {
            get {
                return ResourceManager.GetString("HandledException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Handling an exception..
        /// </summary>
        internal static string HandledExceptionWarning {
            get {
                return ResourceManager.GetString("HandledExceptionWarning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The system hit the limit set for throttle &apos;MaxConcurrentInstances&apos;. Limit for this throttle was set to {0}. Throttle value can be changed by modifying attribute &apos;maxConcurrentInstances&apos; in serviceThrottle element or by modifying &apos;MaxConcurrentInstances&apos; property on behavior ServiceThrottlingBehavior..
        /// </summary>
        internal static string MaxInstancesExceeded {
            get {
                return ResourceManager.GetString("MaxInstancesExceeded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An unexpected failure occurred. Applications should not attempt to handle this error. For diagnostic purposes, this English message is associated with the failure: {0}..
        /// </summary>
        internal static string ShipAssertExceptionMessage {
            get {
                return ResourceManager.GetString("ShipAssertExceptionMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Throwing an exception. Source {0}..
        /// </summary>
        internal static string ThrowingException {
            get {
                return ResourceManager.GetString("ThrowingException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Wrote to the EventLog..
        /// </summary>
        internal static string TraceCodeEventLogCritical {
            get {
                return ResourceManager.GetString("TraceCodeEventLogCritical", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Wrote to the EventLog..
        /// </summary>
        internal static string TraceCodeEventLogError {
            get {
                return ResourceManager.GetString("TraceCodeEventLogError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Wrote to the EventLog..
        /// </summary>
        internal static string TraceCodeEventLogInfo {
            get {
                return ResourceManager.GetString("TraceCodeEventLogInfo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Wrote to the EventLog..
        /// </summary>
        internal static string TraceCodeEventLogVerbose {
            get {
                return ResourceManager.GetString("TraceCodeEventLogVerbose", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Wrote to the EventLog..
        /// </summary>
        internal static string TraceCodeEventLogWarning {
            get {
                return ResourceManager.GetString("TraceCodeEventLogWarning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unhandled exception..
        /// </summary>
        internal static string UnhandledException {
            get {
                return ResourceManager.GetString("UnhandledException", resourceCulture);
            }
        }
    }
}
