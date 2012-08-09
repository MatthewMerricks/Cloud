//
//  CLRegexValidation.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;

namespace win_client.Common
{
    public static class CLRegexValidation
    {
        /// <summary>
        /// Regular expression, which is used to validate an E-Mail address.
        /// </summary>
        private const string MatchEmailPattern = "^.+?@.+$";
//           "^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
//           + "((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
//                [0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
//           + "([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
//                [0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
//           + "([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})$";

        private const string MatchStrongPassword =
            "^(?=.{8,})(?=.*[a-z])(?=.*[A-Z])(?!.*s).*$";

        private const string MatchUrlPattern = "#^http(s)?://[a-z0-9-_.]+\\.[a-z]{2,4}#i";

        /// <summary>
        /// Checks whether the given parameter is a valid URL.
        /// </summary>
        /// <param name="url">Parameter-string that contains a URL.</param>
        /// <returns>True, when Parameter-string is not null and 
        /// contains a valid URL;
        /// otherwise false.</returns>
        public static bool IsValidUrl(string url)
        {
            if(url != null) return Regex.IsMatch(url, MatchUrlPattern);
            else return false;
        }

        /// <summary>
        /// Checks whether the given Email-Parameter is a valid E-Mail address.
        /// </summary>
        /// <param name="email">Parameter-string that contains an E-Mail address.</param>
        /// <returns>True, when Parameter-string is not null and 
        /// contains a valid E-Mail address;
        /// otherwise false.</returns>
        public static bool IsEMail(string email)
        {
            if(email != null) return Regex.IsMatch(email, MatchEmailPattern);
            else return false;
        }

        /// <summary>
        /// Checks whether the given password parameter is strong enough.
        /// </summary>
        /// <param name="email">Parameter-string that contains a password to test.</param>
        /// <returns>True, when Parameter-string is strong enough.
        /// Otherwise false.</returns>
        public static bool IsXOK(string password)
        {
            if (password != null)
            {
                if (password.Length != 0)
                {
                    return Regex.IsMatch(password, MatchStrongPassword);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
