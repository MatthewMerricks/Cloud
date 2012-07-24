// <copyright file="Strings.Designer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.ServiceModel.Interop.Properties
{
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
    internal class Strings
    {
        private static global::System.Resources.ResourceManager resourceMan;

        private static global::System.Globalization.CultureInfo resourceCulture;

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings()
        {
        }

        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null))
                {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.ServiceModel.Interop.Properties.Strings", typeof(Strings).Assembly);
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
        internal static global::System.Globalization.CultureInfo Culture
        {
            get
            {
                return resourceCulture;
            }

            set
            {
                resourceCulture = value;
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to There is no binding named {0} at {1}.
        /// </summary>
        internal static string Binding_Not_Found
        {
            get
            {
                return ResourceManager.GetString("Binding_Not_Found", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Can not convert {0} to {1}.
        /// </summary>
        internal static string Conversion_Type_Not_Supported
        {
            get
            {
                return ResourceManager.GetString("Conversion_Type_Not_Supported", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Invalid encoding value {0}.
        /// </summary>
        internal static string Invalid_Encoding
        {
            get
            {
                return ResourceManager.GetString("Invalid_Encoding", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Algorithm type not supported.
        /// </summary>
        internal static string Not_Supported_Algorithm
        {
            get
            {
                return ResourceManager.GetString("Not_Supported_Algorithm", resourceCulture);
            }
        }
    }
}

