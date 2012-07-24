// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Web;

    internal static class StringExtensions
    {
        public static IEnumerable<string> SplitOnUpperCase(this string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                int startIndex = 0;
                int endIndex = startIndex + 1;
                int length = str.Length;

                while (endIndex < length)
                {
                    if (char.IsUpper(str, endIndex))
                    {
                        yield return str.Substring(startIndex, endIndex - startIndex);
                        startIndex = endIndex;
                    }
                    endIndex++;
                }
                yield return str.Substring(startIndex, endIndex - startIndex);
            }
        }

        public static NameValueCollection ParseAsQueryString(this string str)
        {
            return HttpUtility.ParseQueryString(str);
        }
    }
}
