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
using System.Text;
using System.Threading.Tasks;

namespace CloudApiPublic.Static
{
    /// <summary>
    /// Class containing commonly usable static helper methods
    /// </summary>
    public static class Helpers
    {
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

        private static readonly char[] ValidDateTimeStringChars = new char[]
        {
            '0',
            '1',
            '2',
            '3',
            '4',
            '5',
            '6',
            '7',
            '8',
            '9',
            '/',
            ':',
            'A',
            'M',
            'P',
            ' '
        };

        public static string CleanDateTimeString(string date)
        {
            if (date == null)
            {
                return null;
            }
            return new string(date.Where(currentChar =>
                    ValidDateTimeStringChars.Contains(currentChar))
                .ToArray());
        }

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
        public static string JavaScriptStringEncode(string value)
        {
            return JavaScriptStringEncode(value, false);
        }
        
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

        public static IEnumerable<T> DequeueAll<T>(this LinkedList<T> toDequeue)
        {
            while (toDequeue.Count > 0)
            {
                T toReturn = toDequeue.First();
                toDequeue.RemoveFirst();
                yield return toReturn;
            }
        }

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
    }
}