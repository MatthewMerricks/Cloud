// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common;

    internal static class UriTemplateExtensionMethods
    {
        private const string WildcardPath = "*";

        private enum UriTemplatePartType
        {
            Literal,
            Compound,
            Variable
        }

        public static bool IsWildcardPath(this UriTemplate template)
        {
            return IsWildcardPath(template.ToString());
        }

        private static UriTemplatePartType IdentifyPartType(string part)
        {
            // Identifying the nature of a string - Literal|Compound|Variable
            // Algorithem is based on the following steps:
            // - Finding the position of the first open curlly brace ('{') and close curlly brace ('}') 
            //    in the string
            // - If we don't find any this is a Literal
            // - otherwise, we validate that position of the close brace is at least two characters from 
            //    the position of the open brace
            // - Then we identify if we are dealing with a compound string or a single variable string
            //    + var name is not at the string start --> Compound
            //    + var name is shorter then the entire string (End < Length-2 or End==Length-2 
            //       and string ends with '/') --> Compound
            //    + otherwise --> Variable
            int varStartIndex = part.IndexOf("{", StringComparison.Ordinal);
            int varEndIndex = part.IndexOf("}", StringComparison.Ordinal);
            if (varStartIndex == -1)
            {
                if (varEndIndex != -1)
                {
                    throw Fx.Exception.AsError(new FormatException(
                        SR.UTInvalidFormatSegmentOrQueryPart(part)));
                }

                return UriTemplatePartType.Literal;
            }
            else
            {
                if (varEndIndex < varStartIndex + 2)
                {
                    throw Fx.Exception.AsError(new FormatException(
                       SR.UTInvalidFormatSegmentOrQueryPart(part)));
                }

                if (varStartIndex > 0)
                {
                    return UriTemplatePartType.Compound;
                }
                else if ((varEndIndex < part.Length - 2) ||
                    ((varEndIndex == part.Length - 2) && !part.EndsWith("/", StringComparison.Ordinal)))
                {
                    return UriTemplatePartType.Compound;
                }
                else
                {
                    return UriTemplatePartType.Variable;
                }
            }
        }

        private static bool IsWildcardPath(string path)
        {
            if (path.IndexOf('/') != -1)
            {
                return false;
            }

            UriTemplatePartType partType;
            return IsWildcardSegment(path, out partType);
        }

        private static bool IsWildcardSegment(string segment, out UriTemplatePartType type)
        {
            type = IdentifyPartType(segment);
            switch (type)
            {
                case UriTemplatePartType.Literal:
                    return string.Compare(segment, WildcardPath, StringComparison.Ordinal) == 0;

                case UriTemplatePartType.Compound:
                    return false;

                case UriTemplatePartType.Variable:
                    return segment.IndexOf(WildcardPath, StringComparison.Ordinal) == 1 &&
                            !segment.EndsWith("/", StringComparison.Ordinal) &&
                            segment.Length > WildcardPath.Length + 2;

                default:
                    Fx.Assert("Bad part type identification !");
                    return false;
            }
        }
    }
}
