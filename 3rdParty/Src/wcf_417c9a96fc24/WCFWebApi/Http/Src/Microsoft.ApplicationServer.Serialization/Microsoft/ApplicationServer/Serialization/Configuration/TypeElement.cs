//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Serialization.Configuration
{
    using System;
    using System.Runtime.Serialization.Configuration;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Serialization;

    public static class TypeElementExtensionMethods
    {
        [Fx.Tag.SecurityNote(Miscellaneous = "RequiresReview - Loads type given name in configuration."
            + " Since this information is used to determine whether a particular type is included as a known type,"
            + " changes to the logic should be reviewed.")]
        internal static Type GetType(this TypeElement typeElement, string rootType, Type[] typeArgs)
        {
            return GetType(rootType, typeArgs, typeElement.Type, typeElement.Index, typeElement.Parameters);
        }

        [Fx.Tag.SecurityNote(Miscellaneous = "RequiresReview - Loads type given name in configuration."
            + " Since this information is used to determine whether a particular type is included as a known type,"
            + " changes to the logic should be reviewed.")]
        internal static Type GetType(string rootType, Type[] typeArgs, string type, int index, ParameterElementCollection parameters)
        {
            if (String.IsNullOrEmpty(type))
            {
                if (typeArgs == null || index >= typeArgs.Length)
                {
                    int typeArgsCount = typeArgs == null ? 0 : typeArgs.Length;
                    if (typeArgsCount == 0)
                    {
                        throw Fx.Exception.Argument("", SR.KnownTypeConfigIndexOutOfBoundsZero(
                            rootType,
                            typeArgsCount,
                            index));
                    }
                    else
                    {
                        throw Fx.Exception.Argument("", SR.KnownTypeConfigIndexOutOfBounds(
                            rootType,
                            typeArgsCount,
                            index));
                    }
                }

                return typeArgs[index];
            }

            Type t = System.Type.GetType(type, true);
            if (t.IsGenericTypeDefinition)
            {
                if (parameters.Count != t.GetGenericArguments().Length)
                    throw Fx.Exception.Argument("", SR.KnownTypeConfigGenericParamMismatch(
                        type,
                        t.GetGenericArguments().Length,
                        parameters.Count));

                Type[] types = new Type[parameters.Count];
                for (int i = 0; i < types.Length; ++i)
                {
                    types[i] = parameters[i].GetType(rootType, typeArgs);
                }
                t = t.MakeGenericType(types);
            }
            return t;
        }
    }
}
