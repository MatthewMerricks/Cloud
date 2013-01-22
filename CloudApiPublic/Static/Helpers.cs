//
// Helpers.cs
// Cloud SDK Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
using CloudApiPublic.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace CloudApiPublic.Static
{
    /// <summary>
    /// Class containing commonly usable static helper methods
    /// </summary>
    public static class Helpers
    {
        private static CLTrace _trace = CLTrace.Instance;

        ///// <summary>
        ///// Get the friendly name of this computer.
        ///// </summary>
        ///// <returns></returns>
        //public static string GetComputerFriendlyName()
        //{
        //    // Todo: should find an algorithm to generate a unique identifier for this device name
        //    return Environment.MachineName;
        //}

        /// <summary>
        /// Creates a default instance of a provided type for use with populating out parameters when exceptions are thrown
        /// </summary>
        /// <param name="toDefault">Type to return</param>
        /// <returns>Default value of provided type</returns>
        public static object DefaultForType(Type toDefault)
        {
            if (!toDefault.IsValueType
                || Nullable.GetUnderlyingType(toDefault) != null)
            {
                return null;// nullable types
            }
            return Activator.CreateInstance(toDefault);//non-nullable type
        }

        /// <summary>
        /// Creates a default instance of a provided type for use with populating out parameters when exceptions are thrown
        /// </summary>
        /// <typeparam name="T">Type to return</typeparam>
        /// <returns>Default value of provided type</returns>
        public static T DefaultForType<T>()
        {
            return (T)DefaultForType(typeof(T));
        }

        /// <summary>
        /// Gets the name from the type of the input object, even if the input reference is null
        /// </summary>
        /// <typeparam name="T">Inferred typed for generic object</typeparam>
        /// <param name="toName">Possibly null object, make sure it was in the proper reference type (not boxed)</param>
        /// <returns>Returns the name of the type of the object</returns>
        public static string GetTypeNameEvenForNulls<T>(T toName)
        {
            return (typeof(T)).Name;
        }

        /// <summary>
        /// MethodInfo for the generic-typed Helpers.DefaultForType(of T); this can be used for compiling dynamic expressions
        /// </summary>
        public static readonly MethodInfo DefaultForTypeInfo = typeof(Helpers)
            .GetMethod("DefaultForType",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[] { typeof(Type) },
                null);

        //private static readonly char[] ValidDateTimeStringChars = new char[]
        //{
        //    '0',
        //    '1',
        //    '2',
        //    '3',
        //    '4',
        //    '5',
        //    '6',
        //    '7',
        //    '8',
        //    '9',
        //    '/',
        //    ':',
        //    'A',
        //    'M',
        //    'P',
        //    ' ',
        //    'Z'
        //};

        //public static string CleanDateTimeString(string date)
        //{
        //    if (date == null)
        //    {
        //        return null;
        //    }
        //    return new string(date.Where(currentChar =>
        //            ValidDateTimeStringChars.Contains(currentChar))
        //        .ToArray());
        //}

        #region System.Web.HttpUtility.JavaScriptStringEncode
        /*
         * The two System.Web.HttpUtility.JavaScriptStringEncode method overloads have been copied from the Mono project on August 3rd 2011
         * as the .NET 4.0 Client Profile does not include the System.Web.dll
         * They have been put in a different namespace, CloudApiPublic.Static, in a different static class, Helpers
         * Source: https://github.com/mono/mono/blob/master/mcs/class/System.Web/System.Web/HttpUtility.cs
         */

        //
        // Two System.Web.HttpUtility.JavaScriptStringEncode method overloads
        // (Moved to a different namespace, CloudApiPublic.Static, in a different static class, Helpers)
        //
        // Authors:
        //   Patrik Torstensson (Patrik.Torstensson@labs2.com)
        //   Wictor Wilén (decode/encode functions) (wictor@ibizkit.se)
        //   Tim Coleman (tim@timcoleman.com)
        //   Gonzalo Paniagua Javier (gonzalo@ximian.com)
        //
        // Copyright (C) 2005-2010 Novell, Inc (http://www.novell.com)
        //
        // Permission is hereby granted, free of charge, to any person obtaining
        // a copy of this software and associated documentation files (the // "Software"), to deal in the Software without restriction, including
        // without limitation the rights to use, copy, modify, merge, publish,
        // distribute, sublicense, and/or sell copies of the Software, and to
        // permit persons to whom the Software is furnished to do so, subject to
        // the following conditions:
        //
        // The above copyright notice and this permission notice shall be
        // included in all copies or substantial portions of the Software.
        //
        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
        // EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
        // MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
        // NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
        // LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
        // OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
        // WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
        /// <summary>
        /// Copy of System.Web.HttpUtility.JavaScriptStringEncode copied under rights from Mono so we do not need .NET 4.0 full:
        /// Encodes string content so it can be used as a json tag value
        /// </summary>
        /// <param name="value">String content to encode</param>
        /// <returns>Returns as encoded</returns>
        public static string JavaScriptStringEncode(string value)
        {
            return JavaScriptStringEncode(value, false);
        }

        /// <summary>
        /// Copy of System.Web.HttpUtility.JavaScriptStringEncode copied under rights from Mono so we do not need .NET 4.0 full:
        /// Encodes string content so it can be used as a json tag value
        /// </summary>
        /// <param name="value">String content to encode</param>
        /// <param name="addDoubleQuotes">Whether to add double quotes around the return value such as for a json tag value</param>
        /// <returns>Returns as encoded</returns>
        public static string JavaScriptStringEncode(string value, bool addDoubleQuotes)
        {
            if (String.IsNullOrEmpty(value))
            {
                return addDoubleQuotes ? "\"\"" : String.Empty;
            }

            int len = value.Length;
            bool needEncode = false;
            char c;
            for (int i = 0; i < len; i++)
            {
                c = value[i];
                if (c >= 0 && c <= 31 || c == 34 || c == 39 || c == 60 || c == 62 || c == 92)
                {
                    needEncode = true;
                    break;
                }
            }

            if (!needEncode)
            {
                return addDoubleQuotes ? "\"" + value + "\"" : value;
            }

            var sb = new StringBuilder();
            if (addDoubleQuotes)
            {
                sb.Append('"');
            }

            for (int i = 0; i < len; i++)
            {
                c = value[i];
                if (c >= 0 && c <= 7 || c == 11 || c >= 14 && c <= 31 || c == 39 || c == 60 || c == 62)
                {
                    sb.AppendFormat("\\u{0:x4}", (int)c);
                }
                else
                {
                    switch ((int)c)
                    {
                        case 8:
                            sb.Append("\\b");
                            break;
                        case 9:
                            sb.Append("\\t");
                            break;
                        case 10:
                            sb.Append("\\n");
                            break;
                        case 12:
                            sb.Append("\\f");
                            break;
                        case 13:
                            sb.Append("\\r");
                            break;
                        case 34:
                            sb.Append("\\\"");
                            break;
                        case 92:
                            sb.Append("\\\\");
                            break;
                        default:
                            sb.Append(c);
                            break;
                    }
                }
            }

            if (addDoubleQuotes)
            {
                sb.Append('"');
            }

            return sb.ToString();
        }
        #endregion

        /// <summary>
        /// Extension in the fashion of other Enumerable expressions in System.Linq which filters a list to remove duplicates of items by a provided selector using the selector type's default IEqualityComparer
        /// </summary>
        /// <typeparam name="TSource">Type of the input and output enumerables</typeparam>
        /// <typeparam name="TKey">Type of the property used for distict selection</typeparam>
        /// <param name="source">Extension source parameter for the pre-filtered input enumerable</param>
        /// <param name="keySelector">Selector for parameter used for distict comparison</param>
        /// <returns>Returns enumerable filtered for duplicates</returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        /// <summary>
        /// Copies everything from a Stream (such as an HttpWebRequest ResponseStream) into a new Stream (boxed from MemoryStream) to allow for added functionality such as Seek and Write
        /// </summary>
        /// <param name="inputStream">Stream to copy and then close</param>
        /// <returns>Returns the copied Stream</returns>
        internal static Stream CopyHttpWebResponseStreamAndClose(Stream inputStream)
        {
            byte[] buffer = new byte[FileConstants.BufferSize];
            MemoryStream ms = new MemoryStream();

            int count = inputStream.Read(buffer, 0, FileConstants.BufferSize);
            while (count > 0)
            {
                ms.Write(buffer, 0, count);
                count = inputStream.Read(buffer, 0, FileConstants.BufferSize);
            }
            ms.Position = 0;
            inputStream.Close();
            return ms;
        }

        /// <summary>
        /// Extension for dequeueing one item at a time off the top of a generic-typed LinkedList(of T) and yield returning it, runs until all items are dequeued
        /// </summary>
        /// <typeparam name="T">The generic type parameter of provided input parameter</typeparam>
        /// <param name="toDequeue">Input list to dequeue</param>
        /// <returns>Returns enumerable of dequeued items</returns>
        public static IEnumerable<T> DequeueAll<T>(this LinkedList<T> toDequeue)
        {
            while (toDequeue.Count > 0)
            {
                T toReturn = toDequeue.First();
                toDequeue.RemoveFirst();
                yield return toReturn;
            }
        }

        /// <summary>
        /// Builds the query string portion for a url by pairs of keys to values, keys and values must already be url-encoded
        /// </summary>
        /// <param name="queryStrings">Pairs of keys and values for query string</param>
        /// <returns>Returns query string (with no additional url-encoding of keys or values)</returns>
        internal static string QueryStringBuilder(IEnumerable<KeyValuePair<string, string>> queryStrings)
        {
            if (queryStrings == null)
            {
                return null;
            }

            StringBuilder toReturn = null;
            IEnumerator<KeyValuePair<string, string>> queryEnumerator = queryStrings.GetEnumerator();
            while (queryEnumerator.MoveNext())
            {
                if (queryEnumerator.Current.Key != null
                    || queryEnumerator.Current.Value != null)
                {
                    if (toReturn == null)
                    {
                        toReturn = new StringBuilder("?");
                    }
                    else
                    {
                        toReturn.Append("&");
                    }

                    toReturn.Append(queryEnumerator.Current.Key + "=" + queryEnumerator.Current.Value);
                }
            }

            if (toReturn == null)
            {
                return string.Empty;
            }

            return toReturn.ToString();
        }

        /// <summary>
        /// Determines whether two DateTimes are within one second of one another using (DateTime instance).CompareTo
        /// </summary>
        public static bool DateTimesWithinOneSecond(DateTime firstTime, DateTime secondTime)
        {
            if (firstTime == null && secondTime == null)
            {
                return true;
            }
            if (firstTime == null || secondTime == null)
            {
                return false;
            }

            int initialCompare = firstTime.CompareTo(secondTime);
            if (initialCompare == 0)
            {
                return true;
            }
            else if (initialCompare < 0)
            {
                DateTime firstDateLater = firstTime.Add(TimeSpan.FromSeconds(1));
                if (firstDateLater.CompareTo(secondTime) < 0)
                {
                    return false;
                }
                return true;
            }
            else
            {
                DateTime firstDateEarlier = firstTime.Subtract(TimeSpan.FromSeconds(1));
                if (firstDateEarlier.CompareTo(secondTime) > 0)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Wraps an Action under a try/catch with a customizable number of retry attempts with optional delay in between, finally silences or rethrows the last error
        /// </summary>
        /// <param name="toRun"></param>
        /// <param name="throwExceptionOnFailure"></param>
        /// <param name="numRetries"></param>
        /// <param name="millisecondsBetweenRetries"></param>
        internal static void RunActionWithRetries(Action toRun, bool throwExceptionOnFailure, int numRetries = 5, int millisecondsBetweenRetries = 50)
        {
            if (toRun == null)
            {
                if (throwExceptionOnFailure)
                {
                    throw new NullReferenceException("toRun cannot be null");
                }
                return;
            }

            for (int retryCounter = numRetries - 1; retryCounter >= 0; retryCounter--)
            {
                try
                {
                    toRun();
                    return;
                }
                catch
                {
                    if (retryCounter == 0)
                    {
                        if (throwExceptionOnFailure)
                        {
                            throw;
                        }
                    }
                    else if (millisecondsBetweenRetries > 0
                        && millisecondsBetweenRetries > 0)
                    {
                        Thread.Sleep(millisecondsBetweenRetries);
                    }
                }
            }
        }

        #region encrypt/decrypt strings
        // create and initialize a crypto algorithm 
        private static SymmetricAlgorithm getAlgorithm(string password)
        {
            SymmetricAlgorithm algorithm = Rijndael.Create();
            Rfc2898DeriveBytes rdb = new Rfc2898DeriveBytes(
                password, new byte[] { 
                    0x53,0x6f,0x64,0x69,0x75,0x6d,0x20,             // salty goodness 
                    0x43,0x68,0x6c,0x6f,0x72,0x69,0x64,0x65 
                }
            );
            algorithm.Padding = PaddingMode.ISO10126;
            algorithm.Key = rdb.GetBytes(32);
            algorithm.IV = rdb.GetBytes(16);
            return algorithm;
        }

        /// <summary>
        /// provides simple encryption of a string, with a given password 
        /// </summary>
        public static string EncryptString(string clearText, string password)
        {
            SymmetricAlgorithm algorithm = getAlgorithm(password);
            byte[] clearBytes = System.Text.Encoding.Unicode.GetBytes(clearText);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, algorithm.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(clearBytes, 0, clearBytes.Length);
            cs.Close();
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// provides simple decryption of a string, with a given password
        /// </summary>
        public static string DecryptString(string cipherText, string password)
        {
            SymmetricAlgorithm algorithm = getAlgorithm(password);
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, algorithm.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(cipherBytes, 0, cipherBytes.Length);
            cs.Close();
            return System.Text.Encoding.Unicode.GetString(ms.ToArray());
        }
        #endregion

        /// <summary>
        /// Extension method to generate a new DateTime from an existing DateTime except only copies with accuracy to the second, rounding down (has no milliseconds or nanoseconds)
        /// </summary>
        public static DateTime DropSubSeconds(this DateTime toTruncate)
        {
            return new DateTime(toTruncate.Year,
                toTruncate.Month,
                toTruncate.Day,
                toTruncate.Hour,
                toTruncate.Minute,
                toTruncate.Second,
                toTruncate.Kind);
        }

        /// <summary>
        /// Generic-typed method to run Convert.ChangeType on an object to convert it to the generic type; will throw exceptions on conversion failure or trying to convert null to a non-nullable type
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <param name="toConvert">Object to convert</param>
        /// <returns>Returns converted object</returns>
        public static T ConvertTo<T>(object toConvert)
        {
            return (T)ConvertTo(toConvert, typeof(T));
        }

        /// <summary>
        /// Runs Convert.ChangeType on an object to convert it to the specified type; will throw exceptions on conversion failure; will return a null for null input even if the specified type is non-nullable
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <param name="toConvert">Object to convert</param>
        /// <returns>Returns converted object</returns>
        public static object ConvertTo(object toConvert, Type newType)
        {
            if (toConvert == null)
            {
                return null;
            }

            return Convert.ChangeType(toConvert, Nullable.GetUnderlyingType(newType) ?? newType);
        }

        /// <summary>
        /// MethodInfo for the generic-typed Helpers.ConvertTo(of T); this can be used for compiling dynamic expressions
        /// </summary>
        public static readonly MethodInfo ConvertToInfo = typeof(Helpers)
            .GetMethod("ConvertTo",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(object) },
                null);

        /// <summary>
        /// Uses Win32 API to accurately determine the location of the mouse cursor relative to the origin of the specified Visual element
        /// </summary>
        /// <param name="relativeTo">Element for origin base for the cursor point</param>
        /// <returns>Returns the mouse cursor position relative to the Visual element origin</returns>
        public static Point CorrectGetPosition(Visual relativeTo)
        {
            try
            {
                CloudApiPublic.Static.NativeMethods.POINT win32Point = new NativeMethods.POINT();
                NativeMethods.GetCursorPos(win32Point);
                return relativeTo.PointFromScreen(new Point(win32Point.x, win32Point.y));
            }
            catch
            {
                return new Point(double.MaxValue, double.MaxValue);
            }
        }

        /// <summary>
        /// Pulls the name of the currently running application (such as for AppData directory names); first from AssemblyProduct, second from AssemblyTitle, and last from (executing Assembly).GetName()
        /// </summary>
        public static string GetDefaultNameFromApplicationName()
        {
            System.Reflection.Assembly entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            foreach (System.Reflection.AssemblyProductAttribute currentProductAttribute in
                entryAssembly.GetCustomAttributes(typeof(System.Reflection.AssemblyProductAttribute), false).OfType<System.Reflection.AssemblyProductAttribute>())
            {
                if (!string.IsNullOrWhiteSpace(currentProductAttribute.Product))
                {
                    return currentProductAttribute.Product;
                }
            }
            foreach (System.Reflection.AssemblyTitleAttribute currentTitleAttribute in
                entryAssembly.GetCustomAttributes(typeof(System.Reflection.AssemblyTitleAttribute), false).OfType<System.Reflection.AssemblyTitleAttribute>())
            {
                if (!string.IsNullOrWhiteSpace(currentTitleAttribute.Title))
                {
                    return currentTitleAttribute.Title;
                }
            }
            return entryAssembly.GetName().Name;
        }

        /// <summary>
        /// Parses a hexadecimal string into an array of bytes to return, string must contain only multiples of 2 hexadecimal characters or be null/empty for a null return
        /// </summary>
        /// <param name="hashString">Hexadecimal characters to parse or null/empty</param>
        /// <returns>Returns parsed bytes from input string or null</returns>
        public static byte[] ParseHexadecimalStringToByteArray(string hashString)
        {
            // case for null return: empty or null input string
            if (string.IsNullOrWhiteSpace(hashString))
            {
                return null;
            }

            // verify string by Regex for validity, throwing an exception for invalid
            if (!System.Text.RegularExpressions.Regex.IsMatch(hashString, // hash string to check
                "^([a-f\\d]{2})+$", // wrapped in start/end so the entire string must match; in parentheses: 2 hexadecimal characters, outside: one or more of 2 hexadecimal characters <-- sum of logic, entire string must be multiples of 2 hexadecimal characters
                System.Text.RegularExpressions.RegexOptions.Compiled // faster for repeatedly running the same pattern, but slower the first time
                    | System.Text.RegularExpressions.RegexOptions.CultureInvariant // a-f must be standard across all locales
                    | System.Text.RegularExpressions.RegexOptions.IgnoreCase)) // ignore case (alternative: optionally do not ignore case and add A-F to the regex pattern)
            {
                throw new ArgumentException("hashString must be in a hexadecimal string format with no seperator characters and be 16 bytes in length (32 characters)");
            }

            #region optional fix to allow for a string with an odd number of hex chars
            char[] hexChars;
            //if (hashString.Length % 2 == 1)
            //{
            //    hexChars = new char[hashString.Length + 1];
            //    hexChars[0] = '0';
            //    hashString.ToCharArray().CopyTo(hexChars, 1);
            //}
            //else
            //{
            hexChars = hashString.ToCharArray();
            //}
            #endregion

            // define an int for the length of the input string
            int hexCharLength = hexChars.Length;
            // define the return byte array with half the length of the input string
            byte[] hexBuffer = new byte[hexCharLength / 2 + hexCharLength % 2];

            // define an int for where to place the current parsed byte from the string
            int hexBufferIndex = 0;
            // loop through the characters of the input string, skipping alternately
            for (int charIndex = 0; charIndex < hexCharLength - 1; charIndex += 2)
            {
                // parse the current character as half a byte into the current output index
                hexBuffer[hexBufferIndex] = byte.Parse(hexChars[charIndex].ToString(), // current character as a string
                    System.Globalization.NumberStyles.HexNumber, // parse as a hexadecimal number
                    System.Globalization.CultureInfo.InvariantCulture); // a-f must be static across locales

                // first char in a byte is the high half so bitshift it into place
                hexBuffer[hexBufferIndex] <<= 4;

                // parse the character after the current character as the other half a byte into the current output index (will be the low half)
                hexBuffer[hexBufferIndex] += byte.Parse(hexChars[charIndex + 1].ToString(), // current character as a string
                    System.Globalization.NumberStyles.HexNumber, // parse as a hexadecimal number
                    System.Globalization.CultureInfo.InvariantCulture); // a-f must be static across locales

                // increment the index into the output array for the next byte
                hexBufferIndex++;
            }

            // return the parsed return array
            return hexBuffer;
        }

        /// <summary>
        /// Checks the length of a path to sync to prevent download failures for long server paths.
        /// Uses a dynamic length based on directory so the same structure can be used across all supported versions of Windows: XP and up.
        /// For example C:\Documents and Settings\MyLongUserNameIsHere\BigApplicationName is the maximum length and translates to C:\Users\MyLongUserNameIsHere\BigApplicationName which is artificially more restricted (65 chars versus 48 chars)
        /// </summary>
        /// <param name="syncRootFullPath">Full path of directory to sync</param>
        /// <param name="tooLongChars">(output) Number of characters over the max limit given the current directory and full path length</param>
        /// <returns>Returns an error if the provided path was too long, otherwise null</returns>
        public static CLError CheckSyncRootLength(string syncRootFullPath, out int tooLongChars)
        {
            tooLongChars = 0; // base max length for root is 65 characters (excluding trailing slash), anything beyond that are "too long characters"
            try
            {
                FilePath rootPathObject = syncRootFullPath;

                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); // \Users\[current user]
                string usersDirectory = userProfile.Substring(0, userProfile.LastIndexOf('\\')); // \Users
                FilePath usersDirectoryObject = usersDirectory;

                if (rootPathObject.Contains(usersDirectoryObject, true))
                {
                    if (usersDirectory.EndsWith("Users", StringComparison.InvariantCultureIgnoreCase)) // Vista and up
                    {
                        if (FilePathComparer.Instance.Equals(usersDirectoryObject, rootPathObject))
                        {
                            if (syncRootFullPath.Length > 48) // 17 characters more restrictive: LEN("Documents and Settings") - LEN("Users")
                            {
                                tooLongChars = syncRootFullPath.Length - 48;
                                throw new Exception();
                            }
                        }
                        else
                        {
                            string partAfterUsers = rootPathObject.GetRelativePath(usersDirectoryObject, false);
                            int nextSlashAfterUserProfile = partAfterUsers.IndexOf("\\", 1);

                            // store if user is Public since Public translated back to All Users which adds 3 characters more restriction
                            bool userIsPublic = partAfterUsers.Equals("\\Public", StringComparison.InvariantCultureIgnoreCase)
                                || (nextSlashAfterUserProfile != -1
                                    && string.Equals(partAfterUsers.Substring(1, nextSlashAfterUserProfile - 1),
                                        "Public",
                                        StringComparison.InvariantCultureIgnoreCase));

                            if (nextSlashAfterUserProfile != -1)
                            {
                                int slashAfterNextComponent = partAfterUsers.IndexOf('\\', nextSlashAfterUserProfile + 1);
                                string entireNextComponent = partAfterUsers.Substring(nextSlashAfterUserProfile + 1);
                                if (entireNextComponent.Equals("Documents", StringComparison.InvariantCultureIgnoreCase)
                                    || (slashAfterNextComponent != -1
                                        && string.Equals(partAfterUsers.Substring(nextSlashAfterUserProfile + 1, slashAfterNextComponent - nextSlashAfterUserProfile - 1),
                                            "Documents",
                                            StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    if (syncRootFullPath.Length > (userIsPublic ? 42 : 45)) // 20 characters more restrictive: LEN("Documents and Settings\<user>\My Documents") - LEN("Users\<user>\Documents")
                                    {
                                        tooLongChars = syncRootFullPath.Length - (userIsPublic ? 42 : 45);
                                        throw new Exception();
                                    }
                                }
                                else if (entireNextComponent.Equals("Pictures", StringComparison.InvariantCultureIgnoreCase)
                                    || (slashAfterNextComponent != -1
                                        && string.Equals(partAfterUsers.Substring(nextSlashAfterUserProfile + 1, slashAfterNextComponent - nextSlashAfterUserProfile - 1),
                                            "Pictures",
                                            StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    if (syncRootFullPath.Length > (userIsPublic ? 29 : 32)) // 33 characters more restrictive: LEN("Documents and Settings\<user>\My Documents\My Pictures") - LEN("Users\<user>\Pictures")
                                    {
                                        tooLongChars = syncRootFullPath.Length - (userIsPublic ? 29 : 32);
                                        throw new Exception();
                                    }
                                }
                                else if (entireNextComponent.Equals("Music", StringComparison.InvariantCultureIgnoreCase)
                                    || (slashAfterNextComponent != -1
                                        && string.Equals(partAfterUsers.Substring(nextSlashAfterUserProfile + 1, slashAfterNextComponent - nextSlashAfterUserProfile - 1),
                                            "Music",
                                            StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    if (syncRootFullPath.Length > (userIsPublic ? 29 : 32)) // 33 characters more restrictive: LEN("Documents and Settings\<user>\My Documents\My Music") - LEN("Users\<user>\Music")
                                    {
                                        tooLongChars = syncRootFullPath.Length - (userIsPublic ? 29 : 32);
                                        throw new Exception();
                                    }
                                }
                                else if (!userIsPublic // Public user does not have AppData
                                    && (entireNextComponent.Equals("AppData", StringComparison.InvariantCultureIgnoreCase)
                                        || (slashAfterNextComponent != -1
                                            && string.Equals(partAfterUsers.Substring(nextSlashAfterUserProfile + 1, slashAfterNextComponent - nextSlashAfterUserProfile - 1),
                                                "AppData",
                                                StringComparison.InvariantCultureIgnoreCase))))
                                {
                                    if (slashAfterNextComponent == -1)
                                    {
                                        if (syncRootFullPath.Length > 65) // not more restrictive because Windows XP has no equivalent for the exact path "%userprofile%\AppData"
                                        {
                                            tooLongChars = syncRootFullPath.Length - 65;
                                            throw new Exception();
                                        }
                                    }
                                    else
                                    {
                                        string partAfterAppData = partAfterUsers.Substring(slashAfterNextComponent);
                                        int nextSlashAfterAppData = partAfterAppData.IndexOf("\\", 1);
                                        string entireAppDataFolder = partAfterUsers.Substring(slashAfterNextComponent + 1);
                                        if (entireAppDataFolder.Equals("Roaming", StringComparison.InvariantCultureIgnoreCase)
                                            || (nextSlashAfterAppData != -1
                                                && string.Equals(partAfterAppData.Substring(1, nextSlashAfterAppData - 1),
                                                    "Roaming",
                                                    StringComparison.InvariantCultureIgnoreCase)))
                                        {
                                            if (partAfterAppData.StartsWith("\\Roaming" +
                                                    ((FilePath)Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)).GetRelativePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), false),
                                                StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                if (syncRootFullPath.Length > 65) // not more restrictive because Windows XP actually has a shorter equivalent path for "%userprofile%\AppData\Roaming\Microsoft\Windows\Start Menu" which is "%userprofile%\Start Menu"
                                                {
                                                    tooLongChars = syncRootFullPath.Length - 65;
                                                    throw new Exception();
                                                }
                                            }
                                            else
                                            {
                                                if (syncRootFullPath.Length > 47) // 18 characters more restrictive: LEN("Documents and Settings\<user>\Application Data") - LEN("Users\<user>\AppData\Roaming")
                                                {
                                                    tooLongChars = syncRootFullPath.Length - 47;
                                                    throw new Exception();
                                                }
                                            }
                                        }
                                        // The following is an extremely restrictive case to be compatible between Windows XP and later: Local application data would only allow 7 dynamic characters (such as a 5 character username and a 1 character application name or 7 character username and no application name)
                                        else if (entireAppDataFolder.Equals("Local", StringComparison.InvariantCultureIgnoreCase)
                                            || (nextSlashAfterAppData != -1
                                                && string.Equals(partAfterAppData.Substring(1, nextSlashAfterAppData - 1),
                                                    "Local",
                                                    StringComparison.InvariantCultureIgnoreCase)))
                                        {
                                            if (syncRootFullPath.Length > 30) // 35 characters more restrictive: LEN("Documents and Settigns\<user>\Local Settings\Application Data") - LEN("Users\<user>\AppData\Local")
                                            {
                                                tooLongChars = syncRootFullPath.Length - 30;
                                                throw new Exception();
                                            }
                                        }
                                        // Remaining cases inside AppData which are not Local or Roaming, none have equivalents in Windows XP
                                        else
                                        {
                                            if (syncRootFullPath.Length > 65) // not more restrictive than Windows XP because AppData only has equivalents for Local and Roaming
                                            {
                                                tooLongChars = syncRootFullPath.Length - 65;
                                                throw new Exception();
                                            }
                                        }
                                    }
                                }
                                else if (syncRootFullPath.Length > (userIsPublic ? 45 : 48)) // 17 characters more restrictive: LEN("Documents and Settings") - LEN("Users")
                                {
                                    tooLongChars = syncRootFullPath.Length - (userIsPublic ? 45 : 48);
                                    throw new Exception();
                                }
                            }
                            else if (syncRootFullPath.Length > (userIsPublic ? 45 : 48)) // 17 characters more restrictive: LEN("Documents and Settings") - LEN("Users")
                            {
                                tooLongChars = syncRootFullPath.Length - (userIsPublic ? 45 : 48);
                                throw new Exception();
                            }
                        }
                    }
                    else // "Documents and Settings" for XP
                    {
                        if (FilePathComparer.Instance.Equals(usersDirectoryObject, rootPathObject))
                        {
                            if (syncRootFullPath.Length > 65)
                            {
                                tooLongChars = syncRootFullPath.Length - 65;
                                throw new Exception();
                            }
                        }
                        else
                        {
                            string partAfterUsers = rootPathObject.GetRelativePath(usersDirectoryObject, false);
                            int nextSlashAfterUserProfile = partAfterUsers.IndexOf("\\", 1);

                            if (nextSlashAfterUserProfile != -1
                                && !string.Equals(partAfterUsers.Substring(1, nextSlashAfterUserProfile - 1),
                                    "Public",
                                    StringComparison.InvariantCultureIgnoreCase))
                            {
                                int slashAfterNextComponent = partAfterUsers.IndexOf('\\', nextSlashAfterUserProfile + 1);
                                string entireNextComponent = partAfterUsers.Substring(nextSlashAfterUserProfile + 1);
                                if (entireNextComponent.Equals("Start Menu", StringComparison.InvariantCultureIgnoreCase)
                                    || (slashAfterNextComponent != -1
                                        && string.Equals(partAfterUsers.Substring(nextSlashAfterUserProfile + 1, slashAfterNextComponent - nextSlashAfterUserProfile - 1),
                                            "Start Menu",
                                            StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    if (syncRootFullPath.Length > 48) // 17 characters more restrictive: LEN("Users\<user>\AppData\Roaming\Microsoft\Windows\Start Menu") - LEN("Documents and Settings\<user>\Start Menu")
                                    {
                                        tooLongChars = syncRootFullPath.Length - 48;
                                        throw new Exception();
                                    }
                                }
                                else if (syncRootFullPath.Length > 65)
                                {
                                    tooLongChars = syncRootFullPath.Length - 65;
                                    throw new Exception();
                                }
                            }
                            else if (syncRootFullPath.Length > 65)
                            {
                                tooLongChars = syncRootFullPath.Length - 65;
                                throw new Exception();
                            }
                        }
                    }
                }
                else
                {
                    if (usersDirectory.EndsWith("Users", StringComparison.InvariantCultureIgnoreCase)) // Vista and up
                    {
                        if (rootPathObject.Contains(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), true)
                            || rootPathObject.Contains(Environment.GetFolderPath(Environment.SpecialFolder.CommonTemplates), true))
                        {
                            if (syncRootFullPath.Length > 62) // 3 characters more restrictive: LEN("Documents and Settings\All Users") - LEN("ProgramData\Microsoft\Windows") !! note: ProgramData could also contain Start Menu and Templates before but we calculate by most restriction
                            {
                                tooLongChars = syncRootFullPath.Length - 62;
                                throw new Exception();
                            }
                        }
                        else if (syncRootFullPath.Length > 65)
                        {
                            tooLongChars = syncRootFullPath.Length - 65;
                            throw new Exception();
                        }
                    }
                    else
                    {
                        if (rootPathObject.Contains(Environment.GetEnvironmentVariable("SystemDrive") + "\\ProgramData\\Start Menu", true)
                            && rootPathObject.Contains(Environment.GetEnvironmentVariable("SystemDrive") + "\\ProgramData\\Templates", true))
                        {
                            if (syncRootFullPath.Length > 44)
                            {
                                tooLongChars = syncRootFullPath.Length - 44;
                                throw new Exception();
                            }
                        }
                        else if (syncRootFullPath.Length > 65)
                        {
                            tooLongChars = syncRootFullPath.Length - 65;
                            throw new Exception();
                        }
                    }
                }
            }
            catch
            {
                return new ArgumentException("syncRootFullPath is too long by " + tooLongChars.ToString() + " character" + (tooLongChars == 1 ? string.Empty : "s"));
            }
            return null;
        }

        /// <summary>
        /// Checks for a bad full path for sync (does not check if path was too long for root which must be done after this check);
        /// Path cannot have empty directory portions (i.e. C:\NextWillBeEmpty\\LastWasEmpty;
        /// Path cannot have a trailing slash (i.e. C:\SeeNextSlash\);
        /// Root of path cannot represent anything besides a drive letter (i.e. \RelativePath\);
        /// Root of path cannot represent anything besides a fixed drive or a removable disk (i.e. net use X: \\computer name\share name)
        /// </summary>
        /// <param name="rootPathObject">Full path of directory to sync</param>
        /// <returns>Returns an error if the provided path was bad, otherwise null</returns>
        public static CLError CheckForBadPath(FilePath rootPathObject)
        {
            try
            {
                FilePath checkForEmptyName = rootPathObject;
                while (checkForEmptyName != null)
                {
                    if (string.IsNullOrEmpty(checkForEmptyName.Name))
                    {
                        throw new ArgumentException("settings.CloudRoot cannot have an empty directory name for any directory in the parent path hierarchy and cannot have a trailing slash");
                    }

                    if (checkForEmptyName.Parent == null)
                    {
                        if (!Regex.IsMatch(checkForEmptyName.Name,
                            "^[a-z]:\\\\$",
                            RegexOptions.CultureInvariant
                                | RegexOptions.Compiled
                                | RegexOptions.IgnoreCase))
                        {
                            throw new ArgumentException("settings.CloudRoot must start at a drive letter");
                        }

                        DriveInfo rootDrive = new DriveInfo(checkForEmptyName.Name.Substring(0, 1));
                        switch (rootDrive.DriveType)
                        {
                            case DriveType.CDRom:
                            case DriveType.Network:
                            case DriveType.NoRootDirectory:
                            case DriveType.Ram:
                            case DriveType.Unknown:
                                throw new ArgumentException("settings.CloudRoot root drive letter represents an invalid DriveType: " + rootDrive.DriveType.ToString());
                        }
                    }

                    checkForEmptyName = checkForEmptyName.Parent;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        #region Choose and Maintain Trace File Names
        
        internal class TraceFile
        {
            public DateTime dateFromFileName { get; set; }
            public string fullPath { get; set; }
        }

        internal static readonly object LogFileLocker = new object();
        internal static Dictionary<string, Nullable<DateTime>> DictLastDayLogCreatedByTraceCategory = new Dictionary<string, DateTime?>();

        /// <summary>
        /// This function creates the target trace file and maintains up to 10 daily trace files per category.
        /// Trace file names will look like:
        ///     "[TraceLocation]\Trace-2012-11-27-[TraceCategory]-[UserDeviceId].
        /// </summary>
        /// <remarks>The calling method should wrap this private helper in a try/catch.</remarks>
        /// <param name="TraceLocation">The full path of the directory to contain the trace files.</param>
        /// <param name="UserDeviceId">The relevant device ID, or null.</param>
        /// <param name="TraceCategory">The trace category.  This will appear in the trace file name.</param>
        /// <param name="FileExtensionWithoutPeriod">The file extension to use.  e.g., "log" or "xml".</param>
        /// <param name="OnNewTraceFile">An action that will be driven when a new trace file is created.</param>
        /// <param name="OnPreviousCompletion">An action that will be driven on the old trace file when a trace file rolls over.</param>
        /// <param name="SyncBoxId">The relevant sync box id, or null</param>
        /// <returns>string: The full path and filename.ext of the trace file to use.</returns>
        internal static string CheckLogFileExistance(string TraceLocation, Nullable<long> SyncBoxId, string UserDeviceId, string TraceCategory, string FileExtensionWithoutPeriod, Action<TextWriter, string, Nullable<long>, string> OnNewTraceFile, Action<TextWriter> OnPreviousCompletion)
        {
            // Get the last day we created a trace file for this category
            if (String.IsNullOrWhiteSpace(TraceCategory))
            {
                throw new Exception("TraceCategory must not be null");
            }
            Nullable<DateTime> LastDayLogCreated;
            bool isValueOk = DictLastDayLogCreatedByTraceCategory.TryGetValue(TraceCategory, out LastDayLogCreated);
            if (!isValueOk)
            {
                LastDayLogCreated = null;
            }

            // Build the base of this category's trace file search pattern.  This will be something like:
            // <TraceLocation>\Trace-2012-11-27-<TraceCategory>.  The search path will be something like:
            // <TraceLocation>\Trace-????-??-??-<TraceCategory>.
            // Make sure TraceLocation ends with a backslash.
            string localTraceLocation;
            if (TraceLocation.EndsWith(@"\"))
            {
                localTraceLocation = TraceLocation;
            }
            else
            {
                localTraceLocation = TraceLocation + @"\";
            }

            // store the current date (UTC)
            DateTime currentDate = DateTime.UtcNow.Date;

            // Build the base full path without the extension.
            string logLocationBaseForCategoryWithoutExtension = localTraceLocation + "Trace-" +
                currentDate.ToString("yyyy-MM-dd-") + // formats to "YYYY-MM-DD-"
                TraceCategory;
            FileInfo logFileBaseForCategoryWithoutExtension = new FileInfo(logLocationBaseForCategoryWithoutExtension);

            // Build the search string for enumeration within the directory.
            string logFilenameExtensionSearchString = "Trace-????-??-??-" + TraceCategory + "*." + FileExtensionWithoutPeriod;

            // Build the final full path of the trace file with filename and extension.
            string finalLocation = logFileBaseForCategoryWithoutExtension.FullName +

                // Removed device id from trace file name since now my trace files have SyncBoxId for every entry -David
                //(UserDeviceId == null ? "" : "-" + UserDeviceId) +

                "." + FileExtensionWithoutPeriod;

            bool logAlreadyExists = File.Exists(finalLocation);

            lock (LogFileLocker)
            {
                if (!logAlreadyExists
                    || LastDayLogCreated == null
                    || currentDate.Year != ((DateTime)LastDayLogCreated).Year
                    || currentDate.Month != ((DateTime)LastDayLogCreated).Month
                    || currentDate.Day != ((DateTime)LastDayLogCreated).Day)
                {
                    // if the parent directory of the log file does not exist then create it
                    if (!logFileBaseForCategoryWithoutExtension.Directory.Exists)
                    {
                        logFileBaseForCategoryWithoutExtension.Directory.Create();
                    }

                    // create a list for storing all the dates encoded into existing log file names
                    List<TraceFile> logTraceFilesToPossiblyDelete = new List<TraceFile>();

                    // define boolean for whether the existing list of logs contains the current date,
                    // defaulting to it not being found
                    bool currentDateFound = false;

                    // loop through all files within the parent directory of the log files
                    foreach (FileInfo currentFile in logFileBaseForCategoryWithoutExtension.Directory.EnumerateFiles(logFilenameExtensionSearchString))
                    {
                        // pull out the portion of the file name of the date;
                        // should be in the format YYYY-MM-DD
                        string nameDatePortion = currentFile.Name.Substring(6, 10);

                        // run a series of int.TryParse on the date portions of the file name
                        int nameDateYear;
                        if (int.TryParse(nameDatePortion.Substring(0, 4), out nameDateYear))
                        {
                            int nameDateMonth;
                            if (int.TryParse(nameDatePortion.Substring(5, 2), out nameDateMonth))
                            {
                                int nameDateDay;
                                if (int.TryParse(nameDatePortion.Substring(8), out nameDateDay))
                                {
                                    // all date time part parsing was successful,
                                    // but it is still possible one of the components is outside an acceptable range to construct a datetime

                                    try
                                    {
                                        // create the DateTime from parts
                                        DateTime nameDate = new DateTime(nameDateYear, nameDateMonth, nameDateDay, currentDate.Hour, currentDate.Minute, currentDate.Second, DateTimeKind.Utc);
                                        // if the date portion of the parsed DateTime each match the same portions of the current date,
                                        // then mark the currentDateFound as true
                                        if (nameDate.Year == currentDate.Year
                                            && nameDate.Month == currentDate.Month
                                            && nameDate.Day == currentDate.Day)
                                        {
                                            currentDateFound = true;
                                        }
                                        // add the parsed DateTime to the list of all log files found
                                        logTraceFilesToPossiblyDelete.Add(new TraceFile { dateFromFileName = nameDate, fullPath = currentFile.FullName });
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                        }
                    }

                    const int keepCount = 10;
                    int currentCount = currentDateFound ? 0 : 1;
                    bool lastTraceClosed = currentDateFound;

                    // loop through the log files older than the most recent 10
                    foreach (TraceFile logToRemove in logTraceFilesToPossiblyDelete.OrderByDescending(thisFile => thisFile.dateFromFileName.Ticks))
                    {
                        // Get the full path of this file that we might delete.
                        string currentDeletePath = logToRemove.fullPath;

                        if (currentCount < keepCount)
                        {
                            if (!lastTraceClosed)
                            {
                                lastTraceClosed = true;

                                if (OnPreviousCompletion != null)
                                {
                                    try
                                    {
                                        using (TextWriter logWriter = File.AppendText(currentDeletePath))
                                        {
                                            OnPreviousCompletion(logWriter);
                                            //logWriter.Write(Environment.NewLine + "</Log>");
                                        }
                                    }
                                    catch
                                    {
                                    }
                                }

                                try
                                {
                                    using (TextWriter logWriter = File.AppendText(currentDeletePath))
                                    {
                                        logWriter.Write(Environment.NewLine + "</Log>");
                                    }
                                }
                                catch
                                {
                                }
                            }
                            currentCount++;
                        }
                        else
                        {
                            // attempt to delete the current, old log file
                            try
                            {
                                File.Delete(currentDeletePath);
                            }
                            catch
                            {
                            }
                        }
                    }

                    DictLastDayLogCreatedByTraceCategory[TraceCategory] = currentDate;
                }

                if (!logAlreadyExists)
                {
                    // if the parent directory of the log file does not exist then create it
                    if (!logFileBaseForCategoryWithoutExtension.Directory.Exists)
                    {
                        logFileBaseForCategoryWithoutExtension.Directory.Create();
                    }

                    if (OnNewTraceFile != null)
                    {
                        try
                        {
                            using (TextWriter logWriter = File.CreateText(finalLocation))
                            {
                                OnNewTraceFile(logWriter, finalLocation, SyncBoxId, UserDeviceId);
                                //logWriter.Write(LogXmlStart(finalLocation,
                                //    "UDid: {" + UserDeviceId + "}, UUid: {" + UniqueUserId + "}"));
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return finalLocation;
        }
        #endregion

        #region Extend Dispatcher

        /// <summary>
        /// Extend Dispatcher.*
        /// Delayed invocation on the UI thread without arguments.
        /// </summary>
        public static void DelayedInvoke(this Dispatcher dispatcher, TimeSpan delay, Action action)
        {
            Thread thread = new Thread(DoDelayedInvokeByAction);
            thread.Start(new Tuple<Dispatcher, TimeSpan, Action>(dispatcher, delay, action));
        }

        ///<summary>
        ///Private delayed invocation by action.
        ///</summary>
        private static void DoDelayedInvokeByAction(object parameter)
        {
            Tuple<Dispatcher, TimeSpan, Action> parameterData = (Tuple<Dispatcher, TimeSpan, Action>)parameter;

            Thread.Sleep(parameterData.Item2);

            parameterData.Item1.BeginInvoke(parameterData.Item3);
        }

        /// <summary>
        /// Delayed invocation on the UI thread with arguments.
        /// </summary>
        public static void DelayedInvoke(this Dispatcher dispatcher, TimeSpan delay, System.Delegate d, params object[] args)
        {
            Thread thread = new Thread(DoDelayedInvokeByDelegate);
            thread.Start(new Tuple<Dispatcher, TimeSpan, System.Delegate, object[]>(dispatcher, delay, d, args));
        }

        /// <summary>
        /// Private delayed invocation by delegate.
        /// </summary>
        private static void DoDelayedInvokeByDelegate(object parameter)
        {
            Tuple<Dispatcher, TimeSpan, System.Delegate, object[]> parameterData = (Tuple<Dispatcher, TimeSpan, System.Delegate, object[]>)parameter;

            Thread.Sleep(parameterData.Item2);

            parameterData.Item1.BeginInvoke(parameterData.Item3, parameterData.Item4);
        }
        #endregion

        /// <summary>
        /// Extend string to format a user-viewable string to represent a number of bytes.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string FormatBytes(long bytes)
        {
            if (bytes == 1)
            {
                return "1 Byte"; // special case to remove the plural
            }

            const int scale = 1024;
            long max = (long)Math.Pow(scale, FormatBytesOrders.Length - 1);

            foreach (string order in FormatBytesOrders)
            {
                if (bytes > max)
                {
                    return string.Format("{0:##.##} {1}", decimal.Divide(bytes, max), order);
                }
                else if (bytes == max)
                {
                    return string.Format("1 {0}", order);
                }

                max /= scale;
            }
            return "0 Bytes"; // default for bytes that are less than or equal to zero
        }
        private static readonly string[] FormatBytesOrders = new string[] { "GB", "MB", "KB", "Bytes" };

        public static bool IsCastableTo(this Type from, Type to)
        {
            if (to.IsAssignableFrom(from))
            {
                return true;
            }
            var methods = from.GetMethods(BindingFlags.Public | BindingFlags.Static)
                              .Where(
                                  m => m.ReturnType == to &&
                                       m.Name == "op_Implicit" ||
                                       m.Name == "op_Explicit"
                              );
            return methods.Count() > 0;
        }

        private static readonly Encoding UTF8WithoutBOM = new UTF8Encoding(false);

        /// <summary>
        /// Write an embedded resource file out to the file system as a real file
        /// </summary>
        /// <param name="assembly">The assembly containing the resource.</param>
        /// <param name="resourceName">The name of the resource.</param>
        /// <param name="targetFileFullPath">The full path of the target file.</param>
        /// <returns>int: 0: success.  Otherwise, error code.</returns>
        public static int WriteResourceFileToFilesystemFile(Assembly storeAssembly, string resourceName, string targetFileFullPath)
        {
            try
            {
                _trace.writeToLog(9, "Helpers: WriteResourceFileToFilesystemFile: Entry: resource: {0}. targetFileFullPath: {1}.", resourceName, targetFileFullPath);
                _trace.writeToLog(9, "Helpers: WriteResourceFileToFilesystemFile: storeAssembly.GetName(): <{0}>.", storeAssembly.GetName());
                _trace.writeToLog(9, "Helpers: WriteResourceFileToFilesystemFile: storeAssembly.GetName().Name: <{0}>.", storeAssembly.GetName() != null ? storeAssembly.GetName().Name : "ERROR: Not Set!");

                Func<Assembly, string, Stream> findStreamIfNull = (assemblyToSearch, fileName) =>
                {
                    string matchedName = assemblyToSearch.GetManifestResourceNames()
                            .FirstOrDefault(currentResource => currentResource.EndsWith(fileName, StringComparison.InvariantCultureIgnoreCase));
                    if (matchedName == null)
                    {
                        return null;
                    }
                    return assemblyToSearch.GetManifestResourceStream(matchedName);
                };

                using (Stream txtStream = storeAssembly.GetManifestResourceStream(storeAssembly.GetName().Name + ".Resources." + resourceName)
                    ?? findStreamIfNull(storeAssembly, resourceName))
                {
                    if (txtStream == null)
                    {
                        _trace.writeToLog(1, "Helpers: WriteResourceFileToFilesystemFile: ERROR: txtStream null.");
                        return 1;
                    }

                    using (TextReader txtReader = new StreamReader(txtStream,
                        Encoding.Unicode,
                        true,
                        4096))
                    {
                        if (txtReader == null)
                        {
                            _trace.writeToLog(1, "Helpers: WriteResourceFileToFilesystemFile: ERROR: txtReader null.");
                            return 2;
                        }

                        using (StreamWriter tempStream = new StreamWriter(targetFileFullPath, false, UTF8WithoutBOM, 4096))
                        {
                            if (tempStream == null)
                            {
                                _trace.writeToLog(1, "Helpers: WriteResourceFileToFilesystemFile: ERROR: tempStream null.");
                                return 3;
                            }

                            char[] streamBuffer = new char[4096];
                            int readAmount;

                            while ((readAmount = txtReader.ReadBlock(streamBuffer, 0, 4096)) > 0)
                            {
                                _trace.writeToLog(9, "Helpers: WriteResourceFileToFilesystemFile: Write {0} bytes to the .vbs file.", readAmount);
                                tempStream.Write(streamBuffer, 0, readAmount);
                            }

                            _trace.writeToLog(9, "Helpers: WriteResourceFileToFilesystemFile: Finished writing the .vbs file.");
                        }
                    }
                }

                // For some reason, Windows is dozing (WinDoze?).  The file we just wrote does not immediately appear in the
                // file system, and the process we will launch next won't find it.  Wait until we can see it in the file system.  ????
                for (int i = 0; i < 10; i++)
                {
                    if (System.IO.File.Exists(targetFileFullPath))
                    {
                        break;
                    }

                    Thread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(CLTrace.Instance.TraceLocation, CLTrace.Instance.LogErrors);
                _trace.writeToLog(1, "Helpers: WriteResourceFileToFilesystemFile: ERROR: Exception.  Msg: <{0}>.", ex.Message);
                return 4;
            }

            _trace.writeToLog(1, "Helpers: WriteResourceFileToFilesystemFile: Exit successfully.");
            return 0;
        }

        public static string Get32BitProgramFilesFolderPath()
        {
            // Determine whether 32-bit or 64-bit architecture
            if (IntPtr.Size == 4)
            {
                // 32-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            }
            else
            {
                // 64-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            }
        }

        public static string Get64BitProgramFilesFolderPath()
        {
            // Determine whether 32-bit or 64-bit architecture
            if (IntPtr.Size == 4)
            {
                // 32-bit 
                // XP seems to return an empty string for the ProgramFilesX86 special folder.
                string path = null;
                path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (String.IsNullOrWhiteSpace(path))
                {
                    path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    return path;
                }
                return path;
            }
            else
            {
                // 64-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            }
        }

        public static string Get32BitCommonProgramFilesFolderPath()
        {
            // Determine whether 32-bit or 64-bit architecture
            if (IntPtr.Size == 4)
            {
                // 32-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
            }
            else
            {
                // 64-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
            }
        }

        public static string Get64BitCommonProgramFilesFolderPath()
        {
            // Determine whether 32-bit or 64-bit architecture
            if (IntPtr.Size == 4)
            {
                // 32-bit 
                // XP seems to return an empty string for the CommonProgramFilesX86 special folder.
                string path = null;
                path = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
                if (String.IsNullOrWhiteSpace(path))
                {
                    path = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
                    return path;
                }
                return path;
            }
            else
            {
                // 64-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
            }
        }

        public static string Get32BitSystemFolderPath()
        {
            // Determine whether 32-bit or 64-bit architecture
            if (IntPtr.Size == 4)
            {
                // 32-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.System);
            }
            else
            {
                // 64-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            }
        }

        /// <summary>
        /// Generate the signed token for the platform auth Authorization header.
        /// </summary>
        /// <param name="secret">Secret from credential</param>
        /// <param name="httpMethod">The HTTP method.  e.g.: "POST".</param>
        /// <param name="pathAndQueryStringAndFragment">The HTTP path, query string and fragment.  The path is required.</param>
        /// <param name="serverUrl">The server URL.</param>
        /// <returns></returns>
        internal static string GenerateAuthorizationHeaderToken(string secret, string httpMethod, string pathAndQueryStringAndFragment)
        {
            string toReturn = String.Empty;
            try
            {
                string methodPath = String.Empty;
                string queryString = String.Empty;

                // Determine the methodPath and the queryString
                char[] delimiterChars = { '?' };
                string[] parts = pathAndQueryStringAndFragment.Split(delimiterChars);
                if (parts.Length > 1)
                {
                    methodPath = parts[0].Trim();
                    queryString = parts[parts.Length - 1].Trim();
                }
                else
                {
                    methodPath = pathAndQueryStringAndFragment;
                }

                // Build the string that we will hash.
                string stringToHash = 
                        CLDefinitions.AuthorizationFormatType +
                        "\n" +
                        httpMethod.ToUpper() +
                        "\n" +
                        methodPath +
                        "\n" +

                        //// cannot use query string due to server bug, they are using an unescaped query string for hashing
                        //queryString

                        // temporary only until server bug is fixed and original query string is used for hash instead of using one unescaped
                        Uri.UnescapeDataString(queryString);

                // Hash the string
                byte[] secretByte = Encoding.UTF8.GetBytes(secret);
                HMACSHA256 hmac = new HMACSHA256(secretByte);
                byte[] stringToHashBytes = Encoding.UTF8.GetBytes(stringToHash);
                byte[] hashMessage = hmac.ComputeHash(stringToHashBytes);
                toReturn = ByteToString(hashMessage);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(CLTrace.Instance.TraceLocation, CLTrace.Instance.LogErrors);
                CLTrace.Instance.writeToLog(1, "Helpers: Gen: ERROR. Exception.  Msg: <{0}>.", ex.Message);
            }

            return toReturn;
        }

        /// <summary>
        /// Convert a byte array to a string.
        /// </summary>
        /// <param name="buff"></param>
        /// <returns></returns>
        public static string ByteToString(byte[] buff)
        {
            if (buff == null)
            {
                return null;
            }

            char[] toReturn = new char[buff.Length * 2];

            for (int i = 0; i < buff.Length; i++)
            {
                string currentByte = buff[i].ToString("X2"); // hex format
                int firstCharIndex = i * 2;
                toReturn[firstCharIndex] = currentByte[0];
                toReturn[firstCharIndex + 1] = currentByte[1];
            }

            return new string(toReturn);
        }

        /// <summary>
        /// Delete all of the files and folders in the given directory, but not the top directory itself.
        /// </summary>
        /// <param name="topDirectory">The directory to search.</param>
        /// <returns>CLError.  An error or null.</returns>
        public static CLError DeleteEverythingInDirectory(string topDirectory)
        {
            try
            {
                if (Directory.Exists(topDirectory))
                {
                    string[] files = Directory.GetFiles(topDirectory);
                    foreach (string file in files)
                    {
                        File.Delete(file);
                    }

                    var di = new DirectoryInfo(topDirectory);
                    DirectoryInfo[] subDirs = di.GetDirectories();
                    foreach (DirectoryInfo dirInfo in subDirs)
                    {
                        var directoryName = dirInfo.Name;
                        var fromPath = Path.Combine(topDirectory, directoryName);

                        // delete all files and folders recursively
                        Directory.Delete(fromPath, recursive: true);
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "Helpers: DeleteEverythingInDirectory: ERROR: Exception.  Msg: <{0}>.", ex.Message);
                return error;
            }
            return null;
        }

        /// <summary>
        /// Get the full path of the folder which will be used to store files while they are downloading.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        /// <param name="syncBoxId">ID of the SyncBox</param>
        /// <returns>string: The full path of the temp download directory.</returns>
        /// <remarks>Can throw.</remarks>
        internal static string GetTempFileDownloadPath(ICLSyncSettingsAdvanced settings, long syncBoxId)
        {
            string toReturn = "";
            try
            {
                if (settings == null)
                {
                    throw new NullReferenceException("settings cannot be null");
                }
                if (string.IsNullOrEmpty(settings.DeviceId))
                {
                    throw new NullReferenceException("settings DeviceId cannot be null");
                }

                // Gather the path info
                string sAppName = Helpers.GetDefaultNameFromApplicationName().Trim();
                string sLocalDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create).Trim();
                string sUniqueFolderName = syncBoxId.ToString() +"-" + settings.DeviceId.Trim();
                string sDataDir = sLocalDir + "\\" + sAppName + "\\" + sUniqueFolderName;
                string sTempDownloadDir = sDataDir + "\\" + CLDefinitions.kTempDownloadFolderName;

                // Determine the directory to use for the temporary downloaded files
                string sTempDownloadFolderToUse;
                if (!String.IsNullOrWhiteSpace(settings.TempDownloadFolderFullPath))
                {
                    sTempDownloadFolderToUse = settings.TempDownloadFolderFullPath.Trim();
                }
                else
                {
                    sTempDownloadFolderToUse = sTempDownloadDir;
                }

                toReturn = sTempDownloadFolderToUse;
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(CLTrace.Instance.TraceLocation, CLTrace.Instance.LogErrors);
                CLTrace.Instance.writeToLog(1, "Helpers: GetTempFileDownloadPath: ERROR. Exception.  Msg: <{0}>.", ex.Message);
                throw ex;
            }

            return toReturn;
        }

        /// <summary>
        /// Get the full path of the folder which will be used to store the database file.
        /// </summary>
        /// <param name="DeviceId">Unique ID of this device</param>
        /// <param name="SyncBoxId">ID of the SyncBox</param>
        /// <returns>string: The full path of the directory which will be used for the database file.</returns>
        /// <remarks>Can throw.</remarks>
        internal static string GetDefaultDatabasePath(string DeviceId, long SyncBoxId)
        {
            try
            {
                if (string.IsNullOrEmpty(DeviceId))
                {
                    throw new NullReferenceException("DeviceId cannot be null");
                }

                // Gather the path info
                string sAppName = Helpers.GetDefaultNameFromApplicationName().Trim();
                string sLocalDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create).Trim();
                string sUniqueFolderName = SyncBoxId.ToString() + "-" + DeviceId.Trim();
                return sLocalDir + "\\" + sAppName + "\\" + sUniqueFolderName;
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(CLTrace.Instance.TraceLocation, CLTrace.Instance.LogErrors);
                CLTrace.Instance.writeToLog(1, "Helpers: GetTempFileDownloadPath: ERROR. Exception.  Msg: <{0}>.", ex.Message);
                throw ex;
            }
        }
    }
}