//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
namespace Microsoft.ApplicationServer.Serialization
{
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Security;
    using Microsoft.ApplicationServer.Common;

    [Fx.Tag.SecurityNote(Critical = "Class holds static instances used for code generation during serialization."
        + " Static fields are marked SecurityCritical or readonly to prevent data from being modified or leaked to other components in appdomain.",
        Safe = "All get-only properties marked safe since they only need to be protected for write.")]
    static class XmlFormatGeneratorStatics
    {

        [SecurityCritical]
        static MethodInfo extensionDataSetExplicitMethodInfo;
        internal static MethodInfo ExtensionDataSetExplicitMethodInfo
        {
            [SecuritySafeCritical]
            get
            {
                if (extensionDataSetExplicitMethodInfo == null)
                    extensionDataSetExplicitMethodInfo = typeof(IExtensibleDataObject).GetMethod(Globals.ExtensionDataSetMethod);
                return extensionDataSetExplicitMethodInfo;
            }
        }
    }
}
