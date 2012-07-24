// <copyright file="DiagnosticUtility.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Web
{
    using System;
    using System.Globalization;

    internal static class DiagnosticUtility
    {
        public static string GetString(string format, params object[] args)
        {
            CultureInfo culture = SR.Culture;
            string text = format;
            if (args != null && args.Length > 0)
            {
                text = String.Format(culture, format, args);
            }

            return text;
        }

        internal static class ExceptionUtility
        {
            public static Exception ThrowHelperError(Exception e)
            {
                return e;
            }

            internal static void ThrowOnNull(object obj, string parameterName)
            {
                if (obj == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(parameterName));
                }
            }
        }
    }
}
