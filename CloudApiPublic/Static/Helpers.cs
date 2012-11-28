//
// Helpers.cs
// Cloud SDK Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace CloudApiPublic.Static
{
    /// <summary>
    /// Class containing commonly usable static helper methods
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Get the friendly name of this computer.
        /// </summary>
        /// <returns></returns>
        public static string GetComputerFriendlyName()
        {
            // Todo: should find an algorithm to generate a unique identifier for this device name
            return Environment.MachineName;
        }

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
        public static Stream CopyHttpWebResponseStreamAndClose(Stream inputStream)
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
        public static string QueryStringBuilder(IEnumerable<KeyValuePair<string, string>> queryStrings)
        {
            if (queryStrings == null)
            {
                return null;
            }

            StringBuilder toReturn = null;
            IEnumerator<KeyValuePair<string, string>> queryEnumerator = queryStrings.GetEnumerator();
            while (queryEnumerator.MoveNext())
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
        public static void RunActionWithRetries(Action toRun, bool throwExceptionOnFailure, int numRetries = 5, int millisecondsBetweenRetries = 50)
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
    }
}