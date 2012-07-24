//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Serialization.Configuration
{
    using System;
    using System.Runtime.Serialization.Configuration;
    using Microsoft.ApplicationServer.Common;

    internal static class ParameterElementExtensionMethods
    {
        [Fx.Tag.SecurityNote(Miscellaneous = "RequiresReview - Loads type given name in configuration."
            + " Since this information is used to determine whether a particular type is included as a known type,"
            + " changes to the logic should be reviewed.")]
        internal static Type GetType(this ParameterElement parameterElement, string rootType, Type[] typeArgs)
        {
            return TypeElementExtensionMethods.GetType(rootType, typeArgs, parameterElement.Type, parameterElement.Index, parameterElement.Parameters);
        }
    }
}


