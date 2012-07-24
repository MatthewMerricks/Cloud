// <copyright file="SG.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>.

namespace System.Json
{
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    internal static class SG
    {
        public static string GetString(string format, params object[] args)
        {
            global::System.Globalization.CultureInfo culture = SR.Culture;
            string text = format;
            if (args != null && args.Length > 0)
            {
                text = String.Format(culture, format, args);
            }

            return text;
        }
    }
}
