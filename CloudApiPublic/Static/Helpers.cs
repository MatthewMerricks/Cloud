//
// Helpers.cs
// Cloud SDK Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Interfaces;
using Cloud.JsonContracts;
using Cloud.Model;
using Cloud.REST;
using Cloud.Support;
using Cloud.Sync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Cloud.Static
{
    extern alias SimpleJsonBase;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using Cloud.SQLIndexer.Model;
    using System.Windows;
    using System.Security.Principal;
    /// <summary>
    /// Class containing commonly usable static helper methods
    /// </summary>
    public static class Helpers
    {
        private static CLTrace _trace = CLTrace.Instance;

        /// <summary>
        /// User callback function to request new credentials.
        /// </summary>
        /// <param name="userState"></param>
        /// <returns></returns>
        public delegate CLCredential ReplaceExpiredCredential(object userState);

        internal delegate CLCredential GetCurrentCredentialDelegate();
        internal delegate void SetCurrentCredentialDelegate(CLCredential credential);

        internal sealed class RequestNewCredentialInfo
        {
            public Dictionary<int, EnumRequestNewCredentialStates> ProcessingStateByThreadId { get; set; }
            public ReplaceExpiredCredential GetNewCredentialCallback { get; set; }
            public object GetNewCredentialCallbackUserState { get; set; }
            public GetCurrentCredentialDelegate GetCurrentCredentialCallback { get; set; }
            public SetCurrentCredentialDelegate SetCurrentCredentialCallback { get; set; }
        }

        // not using ReaderWriterLockSlim because this is a static context, and the Slim version is IDisposable
        public static bool AllHaltedOnUnrecoverableError
        {
            get
            {
                allHaltedOnUnrecoverableErrorLocker.AcquireReaderLock(-1);

                try
                {
                    return _allHaltedOnUnrecoverableError;
                }
                finally
                {
                    allHaltedOnUnrecoverableErrorLocker.ReleaseReaderLock();
                }
            }
        }
        private static bool _allHaltedOnUnrecoverableError = false;
        private static readonly ReaderWriterLock allHaltedOnUnrecoverableErrorLocker = new ReaderWriterLock();
        public static void HaltAllOnUnrecoverableError()
        {
            allHaltedOnUnrecoverableErrorLocker.AcquireWriterLock(-1);

            try
            {
                _allHaltedOnUnrecoverableError = true;
            }
            finally
            {
                allHaltedOnUnrecoverableErrorLocker.ReleaseWriterLock();
            }
        }

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
         * They have been put in a different namespace, Cloud.Static, in a different static class, Helpers
         * Source: https://github.com/mono/mono/blob/master/mcs/class/System.Web/System.Web/HttpUtility.cs
         */

        //
        // Two System.Web.HttpUtility.JavaScriptStringEncode method overloads
        // (Moved to a different namespace, Cloud.Static, in a different static class, Helpers)
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
        /// compares two hashes
        /// </summary>
        public static bool IsEqualHashes(byte[] left, byte[] right)
        {
            if (left == right)
            {
                return true;
            } 
            
            if (left == null || right == null) 
            {
                return false; // one is null but not both
            }
            
            if (left.Length != right.Length)
            {
                return false; // not equal in length
            }

            bool isEqual = (NativeMethods.memcmp(left, right, new UIntPtr((uint)left.Length)) == 0);
            return isEqual;
        }

        /// <summary>
        /// Helper class to accumulate hashes; 
        /// A hash over all the bytes in the input stream and optional intermediate hashes of blocks of max bytes are calculated simultaneously;
        /// The resulting hashes array can be retrieved after Finalize is called;
        /// </summary>
        /// <example>
        ///     Md5Hasher hasher = new Md5Hasher();   // no intermediate hashes
        ///     
        ///     Md5Hasher hasher = new Md5Hasher(maxBytesToHash);   // intermediate hashes for each maxBytesToHash bytes in the input stream
        ///     hasher.OnIntermediateHash = (byte[] hash, int index) => {...};
        ///     
        ///     while ((bytesRead = stream.Read(buffer, 0, buffer.Length) != 0) 
        ///     {
        ///         hashes.Update(buffer, bytesRead);
        ///     }
        ///     byte[] hash = hasher.Hash;
        ///     byte[][] hashes = hasher.IntermediateHashes; // null if no intermediate hashes
        /// </example>
        public class Md5Hasher : IDisposable
        {
            private bool _disposed = false;

            private MD5 _totalMd5;

            private int _maxBytesToHash;
            private MD5 _md5;
            private int _bytesHashed;
            private List<byte[]> _hashes;

            private byte[] _hash;
            private byte[][] _intermediateHashes;

            public delegate void IntermediateHashDelegate(byte[] hash, int index);

            /// <summary>
            ///  notification on each intermediate hash calculated
            /// </summary>
            public event IntermediateHashDelegate OnIntermediateHash;

            /// <summary>
            ///  total hash of the stream
            /// </summary>
            public byte[] Hash
            {
                get { return _hash; }      
            }
            /// <summary>
            ///  intermediate hashes of the stream
            /// </summary>
            public byte[][] IntermediateHashes
            {
                get { return _intermediateHashes; }      
            }

            /// <summary>
            /// constructs a hasher for the whole stream only;
            /// </summary>
            public Md5Hasher()
            {
                Init_(0);
            }

            ~Md5Hasher()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        _totalMd5.Dispose();
                        _totalMd5 = null;

                        _md5.Dispose();
                        _md5 = null;

                        _hashes.Clear();
                        _hashes = null;

                        _hash = null;
                        _intermediateHashes = null;
                    }

                    _bytesHashed = 0;

                    _disposed = true;
                }
            }

            /// <summary>
            /// constructs a hasher for the whole stream and intermediate hashes for every maxBytesToHash in the input stream
            /// </summary>
            public Md5Hasher(int maxBytesToHash)
            {
                Init_(maxBytesToHash);
            }

            private void Init_(int maxBytesToHash)
            {
                _totalMd5 = MD5.Create(); 

                _maxBytesToHash = maxBytesToHash;
                _md5 = null;
                _bytesHashed = 0;
                _hashes = null;

                _hash = null;
                _intermediateHashes = null;
            }

            /// <summary>
            /// feeds data to hash;
            /// calling Update after Finalize is undefined;
            /// </summary>
            public void Update(byte[] buffer, int bytesRead)
            {
                if (_totalMd5 == null) {
                    // assert: Update() should not be called after Finalize()/Dispose()
                    const string assertMessage = "_totalMd5 cannot be null";
                    MessageEvents.FireNewEventMessage(assertMessage,
                        EventMessageLevel.Important,
                        new Model.EventMessages.ErrorInfo.HaltAllOfCloudSDKErrorInfo());
                    throw new NullReferenceException(assertMessage);
                }

                _totalMd5.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                UpdateIntermediateHashes_(buffer, bytesRead);
            }

            private void UpdateIntermediateHashes_(byte[] buffer, int bytesRead)
            {
                if (_maxBytesToHash <= 0)
                {
                    return; // no intermediate hashes
                }

                for (int totalBytesWritten = 0, bytesWritten = 0; totalBytesWritten < bytesRead; totalBytesWritten += bytesWritten)
                {
                    if (_md5 == null)
                    {
                        _md5 = MD5.Create();
                    }
                    bytesWritten = Math.Min(_maxBytesToHash - _bytesHashed, bytesRead - totalBytesWritten);
                    _md5.TransformBlock(buffer, totalBytesWritten, bytesWritten, buffer, totalBytesWritten);
                    _bytesHashed += bytesWritten;
                    if (_bytesHashed == _maxBytesToHash)
                    {
                        _md5.TransformFinalBlock(FileConstants.EmptyBuffer, 0, 0);

                        byte[] hash = _md5.Hash;

                        if (_hashes == null)
                        {
                            _hashes = new List<byte[]>();
                        }
                        if (OnIntermediateHash != null)
                        {
                            OnIntermediateHash(hash, _hashes.Count);
                        }
                        _hashes.Add(hash);

                        _md5.Dispose();
                        _md5 = null;
                        _bytesHashed = 0;
                    }
                }
            }

            /// <summary>
            /// finalizes hashing and releases resources
            /// </summary>
            public void FinalizeHashes()
            {
                _totalMd5.TransformFinalBlock(FileConstants.EmptyBuffer, 0, 0);
                _hash = _totalMd5.Hash;

                _intermediateHashes = _hashes == null || _hashes.Count == 0 ? null : _hashes.ToArray();
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
        internal static void RunActionWithRetries<T>(Action<T> toRun, T toRunState, bool throwExceptionOnFailure, int numRetries = 5, int millisecondsBetweenRetries = 50)
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
                    toRun(toRunState);
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
        /// Gets a random integer between 0 and 10,000,000 which can be used to append a random number of sub-second ticks to a DateTime
        /// </summary>
        internal static int GetRandomNumberOfTicksLessThanASecond()
        {
            return MillisecondsRandom.Next((int)TimeSpan.TicksPerSecond);
        }
        private static readonly Random MillisecondsRandom = new Random(Environment.MachineName.GetHashCode());

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
                Cloud.Static.NativeMethods.POINT win32Point = new NativeMethods.POINT();
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
            try
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
            catch
            {
                try
                {
                    // The calling thread may be from a native COM application.  If that is the case, the GetEntryAssembly() method throws an exception.
                    // Get the application name via another method.
                    // PInvoke:
                    //    StringBuilder exePath = new StringBuilder(1024);
                    //    int exePathLen = NativeMethods.GetModuleFileName(IntPtr.Zero, exePath, exePath.Capacity);
                    return System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                }
                catch
                {
                    // The PInvoke method failed too.  Return a placeholder string.
                    return "PossiblyUnmanagedCode";
                }
            }
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

                        char rootDriveLetter = checkForEmptyName.Name[0];
                        if (!char.IsUpper(rootDriveLetter))
                        {
                            throw new ArgumentException("settings.CloudRoot must start with a upper-case drive letter");
                        }
                        DriveInfo rootDrive = new DriveInfo(new string(new[] { rootDriveLetter }));
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
                                //    "DeviceUuid: {" + UserDeviceId + "}, SyncBoxId: {" + UniqueUserId + "}"));
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

        /// <summary>
        /// Validates a directory path's case-sensitivity with an existing folder on disk
        /// </summary>
        /// <param name="path">Path to query</param>
        /// <param name="matches">Whether the path matches perfectly (including case-sensitivity) with a path on disk</param>
        /// <returns>Any error which occurred while checking the disk</returns>
        public static CLError DirectoryMatchesCaseWithDisk(FilePath path, out bool matches)
        {
            try
            {
                if (path == null)
                {
                    throw new NullReferenceException("path cannot be null");
                }

                NativeMethods.WIN32_FIND_DATA fileData;
                SafeSearchHandle searchHandle = null;
                try
                {
                    searchHandle = NativeMethods.FindFirstFileEx(//"\\\\?\\" + // Allows searching paths up to 32,767 characters in length, but not supported on XP
                        path.ToString(),
                        NativeMethods.FINDEX_INFO_LEVELS.FindExInfoStandard,// Basic would be optimal but it's only supported in Windows 7 on up
                        out fileData,
                        NativeMethods.FINDEX_SEARCH_OPS.FindExSearchLimitToDirectories,
                        IntPtr.Zero,
                        NativeMethods.FINDEX_ADDITIONAL_FLAGS.FIND_FIRST_EX_CASE_SENSITIVE);

                    matches = !searchHandle.IsInvalid;
                }
                finally
                {
                    if (searchHandle != null
                        && !searchHandle.IsInvalid)
                    {
                        searchHandle.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                matches = Helpers.DefaultForType<bool>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Hamming weight function for 32-bits
        /// </summary>
        /// <param name="toWeigh">32-bit integer to weigh</param>
        /// <returns>Returns number of set bits in the input</returns>
        internal static int NumberOfSetBits(int toWeigh)
        {
            toWeigh = toWeigh - ((toWeigh >> 1) & 0x55555555);
            toWeigh = (toWeigh & 0x33333333) + ((toWeigh >> 2) & 0x33333333);
            return (((toWeigh + (toWeigh >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        /// <summary>
        /// Debug function to assert whether a tree of dependencies for a FileChange has a change with an equivalent dependency;
        /// throws an InvalidOperationException of a duplicate is found as dependent to itself
        /// </summary>
        public static void CheckFileChangeDependenciesForDuplicates(FileChange toCheck)
        {
            FileChangeWithDependencies castToCheck = toCheck as FileChangeWithDependencies;
            if (castToCheck != null
                && castToCheck.DependenciesCount > 0)
            {
                Array.ForEach(castToCheck.Dependencies,
                    innerDependency =>
                    {
                        if (innerDependency == toCheck)
                        {
                            throw new InvalidOperationException("Dependency of a FileChange is the same as the FileChange itself");
                        }
                        CheckFileChangeDependenciesForDuplicates(innerDependency);
                    });
            }
        }

        #region ProcessHttp
        #region readonly fields
        /// <summary>
        /// hash set for http communication methods which are good when the status is ok, created, or not modified
        /// </summary>
        internal static readonly HashSet<HttpStatusCode> HttpStatusesOkCreatedNotModifiedNoContent = new HashSet<HttpStatusCode>(new[]
            {
                HttpStatusCode.OK,
                HttpStatusCode.Created,
                HttpStatusCode.NotModified,

                // the following two are both considered no content on our servers
                HttpStatusCode.NoContent,
                ((HttpStatusCode)CLDefinitions.CustomNoContentCode)
            });

        /// <summary>
        /// hash set for http communication methods which are good when the status is ok or accepted
        /// </summary>
        internal static readonly HashSet<HttpStatusCode> HttpStatusesOkAccepted = new HashSet<HttpStatusCode>(new[]
            {
                HttpStatusCode.OK,
                HttpStatusCode.Accepted
            });

        private static readonly Dictionary<Type, DataContractJsonSerializer> SerializableRequestTypes = new Dictionary<Type, DataContractJsonSerializer>()
        {
            { typeof(JsonContracts.Download), JsonContractHelpers.DownloadSerializer },
            { typeof(JsonContracts.PurgePending), JsonContractHelpers.PurgePendingSerializer },
            { typeof(JsonContracts.Push), JsonContractHelpers.PushSerializer },
            { typeof(JsonContracts.To), JsonContractHelpers.ToSerializer },
            
            #region one-offs
            { typeof(JsonContracts.FolderAdd), JsonContractHelpers.FolderAddSerializer },

            { typeof(JsonContracts.FileAdd), JsonContractHelpers.FileAddSerializer },
            { typeof(JsonContracts.FileModify), JsonContractHelpers.FileModifySerializer },

            { typeof(JsonContracts.FileOrFolderDelete), JsonContractHelpers.FileOrFolderDeleteSerializer },
            { typeof(JsonContracts.FileOrFolderMove), JsonContractHelpers.FileOrFolderMoveSerializer },
            { typeof(JsonContracts.FileOrFolderUndelete), JsonContractHelpers.FileOrFolderUndeleteSerializer },
            #endregion

            { typeof(JsonContracts.FileCopy), JsonContractHelpers.FileCopySerializer },

            #region platform management
            { typeof(JsonContracts.SyncBoxHolder), JsonContractHelpers.CreateSyncBoxSerializer },
            { typeof(JsonContracts.SyncBoxMetadata), JsonContractHelpers.SyncBoxMetadataSerializer },
            { typeof(JsonContracts.SyncBoxQuota), JsonContractHelpers.SyncBoxQuotaSerializer },
            { typeof(JsonContracts.SyncBoxIdOnly), JsonContractHelpers.SyncBoxDeleteSerializer },
            { typeof(JsonContracts.SyncBoxUpdatePlanRequest), JsonContractHelpers.SyncBoxUpdatePlanRequestSerializer },
            { typeof(JsonContracts.SyncBoxUpdateRequest), JsonContractHelpers.SyncBoxUpdateRequestSerializer },
            { typeof(JsonContracts.SessionCreateRequest), JsonContractHelpers.SessionCreateRequestSerializer },
            { typeof(JsonContracts.SessionCreateAllRequest), JsonContractHelpers.SessionCreateAllRequestSerializer },
            { typeof(JsonContracts.SessionDeleteRequest), JsonContractHelpers.SessionDeleteRequestSerializer },
            { typeof(JsonContracts.NotificationUnsubscribeRequest), JsonContractHelpers.NotificationUnsubscribeRequestSerializer },
            { typeof(JsonContracts.UserRegistrationRequest), JsonContractHelpers.UserRegistrationRequestSerializer },
            { typeof(JsonContracts.DeviceRequest), JsonContractHelpers.DeviceRequestSerializer },
            { typeof(JsonContracts.LinkDeviceFirstTimeRequest), JsonContractHelpers.LinkDeviceFirstTimeRequestSerializer},
            { typeof(JsonContracts.LinkDeviceRequest), JsonContractHelpers.LinkDeviceRequestSerializer},
            { typeof(JsonContracts.UnlinkDeviceRequest), JsonContractHelpers.UnlinkDeviceRequestSerializer},
            #endregion
        };

        // dictionary to find which Json contract serializer to use given a provided input type
        private static readonly Dictionary<Type, DataContractJsonSerializer> SerializableResponseTypes = new Dictionary<Type, DataContractJsonSerializer>()
        {
            { typeof(JsonContracts.Metadata), JsonContractHelpers.GetMetadataResponseSerializer },
            { typeof(JsonContracts.NotificationResponse), JsonContractHelpers.NotificationResponseSerializer },
            { typeof(JsonContracts.PendingResponse), JsonContractHelpers.PendingResponseSerializer },
            { typeof(JsonContracts.PushResponse), JsonContractHelpers.PushResponseSerializer },
            { typeof(JsonContracts.To), JsonContractHelpers.ToSerializer },
            { typeof(JsonContracts.Event), JsonContractHelpers.EventSerializer },
            { typeof(JsonContracts.FileVersion[]), JsonContractHelpers.FileVersionsSerializer },
            //{ typeof(JsonContracts.UsedBytes), JsonContractHelpers.UsedBytesSerializer }, // deprecated
            { typeof(JsonContracts.Pictures), JsonContractHelpers.PicturesSerializer },
            { typeof(JsonContracts.Videos), JsonContractHelpers.VideosSerializer },
            { typeof(JsonContracts.Audios), JsonContractHelpers.AudiosSerializer },
            { typeof(JsonContracts.Archives), JsonContractHelpers.ArchivesSerializer },
            { typeof(JsonContracts.Recents), JsonContractHelpers.RecentsSerializer },
            { typeof(JsonContracts.SyncBoxUsage), JsonContractHelpers.SyncBoxUsageSerializer },
            { typeof(JsonContracts.Folders), JsonContractHelpers.FoldersSerializer },
            { typeof(JsonContracts.FolderContents), JsonContractHelpers.FolderContentsSerializer },
            { typeof(JsonContracts.AuthenticationErrorResponse), JsonContractHelpers.AuthenticationErrorResponseSerializer },
            //{ typeof(JsonContracts.AuthenticationErrorMessage), JsonContractHelpers.AuthenticationErrorMessageSerializer }, // deprecated

            #region platform management
            { typeof(JsonContracts.SyncBoxHolder), JsonContractHelpers.CreateSyncBoxSerializer },
            { typeof(JsonContracts.ListSyncBoxes), JsonContractHelpers.ListSyncBoxesSerializer },
            { typeof(JsonContracts.ListPlansResponse), JsonContractHelpers.ListPlansSerializer },
            { typeof(JsonContracts.SyncBoxUpdatePlanResponse), JsonContractHelpers.SyncBoxUpdatePlanResponseSerializer },
            { typeof(JsonContracts.SessionCreateResponse), JsonContractHelpers.SessionCreateResponseSerializer },
            { typeof(JsonContracts.ListSessionsResponse), JsonContractHelpers.ListSessionsSerializer },
            { typeof(JsonContracts.SessionShowResponse), JsonContractHelpers.SessionShowSerializer },
            { typeof(JsonContracts.SessionDeleteResponse), JsonContractHelpers.SessionDeleteSerializer },
            { typeof(JsonContracts.NotificationUnsubscribeResponse), JsonContractHelpers.NotificationUnsubscribeResponseSerializer },
            { typeof(JsonContracts.UserRegistrationResponse), JsonContractHelpers.UserRegistrationResponseSerializer},
            { typeof(JsonContracts.DeviceResponse), JsonContractHelpers.DeviceResponseSerializer},
            { typeof(JsonContracts.LinkDeviceFirstTimeResponse), JsonContractHelpers.LinkDeviceFirstTimeResponseSerializer},
            { typeof(JsonContracts.SyncBoxAuthResponse), JsonContractHelpers.SyncBoxAuthResponseSerializer},
            { typeof(JsonContracts.LinkDeviceResponse), JsonContractHelpers.LinkDeviceResponseSerializer},
            { typeof(JsonContracts.UnlinkDeviceResponse), JsonContractHelpers.UnlinkDeviceResponseSerializer},
            #endregion
        };
        #endregion

        /// <summary>
        /// event handler fired upon transfer buffer clears for uploads/downloads to relay to the global event
        /// </summary>
        internal static void HandleUploadDownloadStatus(CLStatusFileTransferUpdateParameters status, FileChange eventSource, Nullable<long> syncBoxId, string deviceId)
        {
            // validate parameter which can throw an exception in this method

            if (eventSource == null)
            {
                throw new NullReferenceException("eventSource cannot be null");
            }

            // direction of communication determines which event to fire
            if (eventSource.Direction == SyncDirection.To)
            {
                MessageEvents.UpdateFileUpload(
                    eventId: eventSource.EventId, // the id for the event
                    parameters: status, // the event arguments describing the status change
                    SyncBoxId: syncBoxId,
                    DeviceId: deviceId);
            }
            else
            {
                MessageEvents.UpdateFileDownload(
                    eventId: eventSource.EventId, // the id for the event
                    parameters: status,  // the event arguments describing the status change
                    SyncBoxId: syncBoxId,
                    DeviceId: deviceId);
            }
        }

        /// <summary>
        /// forwards to the main HTTP REST routine helper method which processes the actual communication, but only where the return type is object
        /// </summary>
        internal static object ProcessHttp(object requestContent, // JSON contract object to serialize and send up as the request content, if any
            string serverUrl, // the server URL
            string serverMethodPath, // the server method path
            requestMethod method, // type of HTTP method (get vs. put vs. post)
            int timeoutMilliseconds, // time before communication timeout (does not restrict time for the upload or download of files)
            uploadDownloadParams uploadDownload, // parameters if the method is for a file upload or download, or null otherwise
            HashSet<HttpStatusCode> validStatusCodes, // a HashSet with HttpStatusCodes which should be considered all possible successful return codes from the server
            ref CLHttpRestStatus status, // reference to the successful/failed state of communication
            ICLSyncSettingsAdvanced CopiedSettings, // used for device id, trace settings, and client version
            CLCredential Credential, // contains key/secret for authorization
            Nullable<long> SyncBoxId, // unique id for the sync box on the server
            RequestNewCredentialInfo RequestNewCredentialInfo = null)
        {
            return ProcessHttp<object>(requestContent,
                serverUrl,
                serverMethodPath,
                method,
                timeoutMilliseconds,
                uploadDownload,
                validStatusCodes,
                ref status,
                CopiedSettings,
                Credential,
                SyncBoxId,
                RequestNewCredentialInfo);
        }
        
        /// <summary>
        /// HTTP REST routine helper method which handles temporary credential extension and retries of the original request.
        /// T should be the type of the JSON contract object which an be deserialized from the return response of the server if any, otherwise use string/object type which will be filled in as the entire string response
        /// </summary>
        internal static T ProcessHttp<T>(object requestContent, // JSON contract object to serialize and send up as the request content, if any
            string serverUrl, // the server URL
            string serverMethodPath, // the server method path
            requestMethod method, // type of HTTP method (get vs. put vs. post)
            int timeoutMilliseconds, // time before communication timeout (does not restrict time for the upload or download of files)
            uploadDownloadParams uploadDownload, // parameters if the method is for a file upload or download, or null otherwise
            HashSet<HttpStatusCode> validStatusCodes, // a HashSet with HttpStatusCodes which should be considered all possible successful return codes from the server
            ref CLHttpRestStatus status, // reference to the successful/failed state of communication
            ICLSyncSettingsAdvanced CopiedSettings, // used for device id, trace settings, and client version
            CLCredential Credential, // contains key/secret for authorization
            Nullable<long> SyncBoxId,  // unique id for the sync box on the server
            RequestNewCredentialInfo RequestNewCredentialInfo = null)
            where T : class // restrict T to an object type to allow default null return
        {
            // Part 1 of the "request new credential" processing.  This processing is invoked when temporary token credentials time out.
            // In Part 1, we validate the caller's extra parameters, and we add this thread to a dictionary provided by the caller.
            // The information added to the dictionary will be used in parrt 2 below.
            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (RequestNewCredentialInfo != null)
            {
                // The caller wants to handle requests for new a new temporary credential.  Validate the parameters.
                if (RequestNewCredentialInfo.GetCurrentCredentialCallback == null)
                {
                    throw new ArgumentNullException("The GetCurrentCredentialCallback must not be null");
                }
                if (RequestNewCredentialInfo.GetNewCredentialCallback == null)
                {
                    throw new ArgumentNullException("The GetNewCredentialCallback must not be null");
                }
                if (RequestNewCredentialInfo.SetCurrentCredentialCallback == null)
                {
                    throw new ArgumentNullException("The SetNewCredentialCallback must not be null");
                }
                if (RequestNewCredentialInfo.ProcessingStateByThreadId == null)
                {
                    throw new ArgumentNullException("The ProcessingStateByThreadId must not be null");
                }

                lock (RequestNewCredentialInfo.ProcessingStateByThreadId)
                {
                    // Get the current credential under the lock.  It may have changed.
                    Credential = RequestNewCredentialInfo.GetCurrentCredentialCallback();

                    // Add this thread to the dictionary provided by the caller.
                    RequestNewCredentialInfo.ProcessingStateByThreadId[threadId] = EnumRequestNewCredentialStates.RequestNewCredential_NotSet;
                }
            }

            // Now call the original core processHttp.
            T toReturn = ProcessHttpInner<T>(requestContent,
                        serverUrl,
                        serverMethodPath,
                        method,
                        timeoutMilliseconds,
                        uploadDownload,
                        validStatusCodes,
                        ref status,
                        CopiedSettings,
                        Credential,
                        SyncBoxId);

            // Part 2 of the "request new credential" processing.  This processing is invoked when temporary token credentials time out.
            // Here we watch for the "token expired" error.  We will ask the caller for a new temporary credential 
            if (RequestNewCredentialInfo != null)
            {
                EnumRequestNewCredentialStates localThreadState;

                lock (RequestNewCredentialInfo.ProcessingStateByThreadId)
                {

                    // Get this thread's value entry in ProcessingStateByThreadId
                    localThreadState = RequestNewCredentialInfo.ProcessingStateByThreadId[threadId];

                    // Remove this thread's entry from the ProcessingStateByThreadId dictionary
                    RequestNewCredentialInfo.ProcessingStateByThreadId.Remove(threadId);

                    // Special handling if this is a 401 NotAuthorized code with the "expired credentials" error enumeration.
                    if (status == CLHttpRestStatus.NotAuthorizedExpiredCredentials)
                    {
                        // If this thread's state is RequestNewCredential_NotSet, then this thread is the first in under the
                        // lock to handle this expired credential.
                        if (localThreadState == EnumRequestNewCredentialStates.RequestNewCredential_NotSet)
                        {
                            // We will call back to the caller to have them go off to a server and produce new credentials.
                            bool fErrorOccured = false;
                            try
                            {
                                // Call back to the caller to get a new credential
                                Credential = RequestNewCredentialInfo.GetNewCredentialCallback(RequestNewCredentialInfo.GetNewCredentialCallbackUserState);

                                // Set the credential back to the caller.
                                RequestNewCredentialInfo.SetCurrentCredentialCallback(Credential);
                            }
                            catch (Exception ex)
                            {
                                CLTrace.Instance.writeToLog(1, "Helpers: ProcessHttp<>: ERROR. Exception from GetNewCredentialCallback.  Msg: <{0}>.", ex.Message);
                                fErrorOccured = true;
                            }

                            // If an error occurred, we will bubble the original 401 status back to the caller.  We will also tell all other threads
                            // to bubble their statuses back to the caller as well.
                            EnumRequestNewCredentialStates newStateToSet;
                            if (fErrorOccured)
                            {
                                newStateToSet = EnumRequestNewCredentialStates.RequestNewCredential_BubbleResult;
                            }
                            // Otherwise, we retrieved a new credential successfully.  We will retry ourselves, and we will tell all other threads to retry as well.
                            else
                            {
                                newStateToSet = EnumRequestNewCredentialStates.RequestNewCredential_Retry;
                            }

                            // Set this new state for everyone.
                            localThreadState = newStateToSet;
                            foreach (KeyValuePair<int, EnumRequestNewCredentialStates> pair in RequestNewCredentialInfo.ProcessingStateByThreadId)
                            {
                                RequestNewCredentialInfo.ProcessingStateByThreadId[pair.Key] = newStateToSet;
                            }
                        }
                    }
                }

                // Here we will retry the original operation if we decided to do that under the lock.
                if (localThreadState == EnumRequestNewCredentialStates.RequestNewCredential_Retry)
                {
                    // Retry the original operation.
                    toReturn = ProcessHttpInner<T>(requestContent,
                                serverUrl,
                                serverMethodPath,
                                method,
                                timeoutMilliseconds,
                                uploadDownload,
                                validStatusCodes,
                                ref status,
                                CopiedSettings,
                                Credential,
                                SyncBoxId);
                }
            }

            return toReturn;
        }

        /// <summary>
        /// main HTTP REST routine helper method which processes the actual communication
        /// T should be the type of the JSON contract object which an be deserialized from the return response of the server if any, otherwise use string/object type which will be filled in as the entire string response
        /// </summary>
        internal static T ProcessHttpInner<T>(object requestContent, // JSON contract object to serialize and send up as the request content, if any
            string serverUrl, // the server URL
            string serverMethodPath, // the server method path
            requestMethod method, // type of HTTP method (get vs. put vs. post)
            int timeoutMilliseconds, // time before communication timeout (does not restrict time for the upload or download of files)
            uploadDownloadParams uploadDownload, // parameters if the method is for a file upload or download, or null otherwise
            HashSet<HttpStatusCode> validStatusCodes, // a HashSet with HttpStatusCodes which should be considered all possible successful return codes from the server
            ref CLHttpRestStatus status, // reference to the successful/failed state of communication
            ICLSyncSettingsAdvanced CopiedSettings, // used for device id, trace settings, and client version
            CLCredential Credential, // contains key/secret for authorization
            Nullable<long> SyncBoxId) // unique id for the sync box on the server
            where T : class // restrict T to an object type to allow default null return
        {
            if (AllHaltedOnUnrecoverableError)
            {
                throw new InvalidOperationException("Cannot do anything with the Cloud SDK if Helpers.AllHaltedOnUnrecoverableError is set");
            }

            // check that the temp download folder exists, if not, create it
            if (uploadDownload != null
                && uploadDownload is downloadParams)
            {
                if (string.IsNullOrEmpty(((downloadParams)uploadDownload).TempDownloadFolderPath))
                {
                    throw new NullReferenceException("uploadDownload TempDownloadFolderPath cannot be null");
                }

                if (!Directory.Exists(((downloadParams)uploadDownload).TempDownloadFolderPath))
                {
                    Directory.CreateDirectory(((downloadParams)uploadDownload).TempDownloadFolderPath);
                }
            }

            // create the main request object for the provided uri location
            HttpWebRequest httpRequest = (HttpWebRequest)HttpWebRequest.Create(serverUrl + serverMethodPath);

            #region set request parameters
            // switch case to set the HTTP method (GET vs. POST vs. PUT); throw exception if not supported yet
            switch (method)
            {
                case requestMethod.get:
                    httpRequest.Method = CLDefinitions.HeaderAppendMethodGet;
                    break;
                case requestMethod.post:
                    httpRequest.Method = CLDefinitions.HeaderAppendMethodPost;
                    break;
                case requestMethod.put:
                    httpRequest.Method = CLDefinitions.HeaderAppendMethodPut;
                    break;

                default:
                    throw new ArgumentException("Unknown method: " + method.ToString());
            }

            httpRequest.UserAgent = CLDefinitions.HeaderAppendCloudClient; // set client
            // Add the client type and version.  For the Windows client, it will be Wnn.  e.g., W01 for the 0.1 client.
            httpRequest.Headers[CLDefinitions.CLClientVersionHeaderName] = OSVersionInfo.GetClientVersionHttpHeader(CopiedSettings.ClientVersion);
            httpRequest.Headers[CLDefinitions.HeaderKeyAuthorization] = CLDefinitions.HeaderAppendCWS0 +
                                CLDefinitions.HeaderAppendKey +
                                Credential.Key + ", " +
                                CLDefinitions.HeaderAppendSignature +
                                        Helpers.GenerateAuthorizationHeaderToken(
                                            Credential.Secret,
                                            httpMethod: httpRequest.Method,
                                            pathAndQueryStringAndFragment: serverMethodPath) +
                // Add token if specified
                                            (!String.IsNullOrEmpty(Credential.Token) ?
                                                    CLDefinitions.HeaderAppendToken + Credential.Token :
                                                    String.Empty);
            httpRequest.SendChunked = false; // do not send chunked
            httpRequest.Timeout = timeoutMilliseconds; // set timeout by input parameter, timeout does not apply to the amount of time it takes to perform uploading or downloading of a file

            // declare the bytes for the serialized request body content
            byte[] requestContentBytes;

            // for any communication which is not a file upload, determine the bytes which will be sent up in the request
            if (uploadDownload == null ||
                !(uploadDownload is uploadParams))
            {
                // if there is no content for the request (such as for an HTTP Get method call), then set the bytes as null
                if (requestContent == null)
                {
                    requestContentBytes = null;
                }
                // else if there is content for the request, then serialize the requestContent object and store the bytes to send up
                else
                {
                    // declare a string for the request body content
                    string requestString;
                    // create a stream for serializing the request object
                    using (MemoryStream requestMemory = new MemoryStream())
                    {
                        // serialize the request object into the stream with the appropriate serializer based on the input type, and if the type is not supported then throw an exception

                        Type requestType = requestContent.GetType();
                        DataContractJsonSerializer getRequestSerializer;
                        if (!SerializableRequestTypes.TryGetValue(requestType, out getRequestSerializer))
                        {
                            throw new ArgumentException("Unknown requestContent Type: " + requestType.FullName);
                        }

                        getRequestSerializer.WriteObject(requestMemory, requestContent);

                        // grab the string from the serialized data
                        requestString = Encoding.Default.GetString(requestMemory.ToArray());

                        if (requestType.GetCustomAttributes(typeof(JsonContracts.ContainsMetadataDictionaryAttribute), false).Length == 1)
                        {
                            SimpleJsonBase.SimpleJson.JsonObject myDeserialized = SimpleJsonBase.SimpleJson.SimpleJson.DeserializeObject(requestString) as SimpleJsonBase.SimpleJson.JsonObject;

                            if (myDeserialized == null)
                            {
                                throw new NullReferenceException("Deserialized JsonObject null from requestString");
                            }

                            foreach (KeyValuePair<string, object> myDeserializedPair in myDeserialized)
                            {
                                CleanTypeKeys(myDeserializedPair.Value);
                            }

                            requestString = SimpleJsonBase.SimpleJson.SimpleJson.SerializeObject(myDeserialized);
                        }
                    }

                    // grab the bytes for the serialized request body content
                    requestContentBytes = Encoding.UTF8.GetBytes(requestString);

                    // configure request parameters based on a json request body content

                    httpRequest.ContentType = CLDefinitions.HeaderAppendContentTypeJson; // the request body content is json-formatted
                    httpRequest.ContentLength = requestContentBytes.LongLength; // set the size of the request content
                    httpRequest.Headers[CLDefinitions.HeaderKeyContentEncoding] = CLDefinitions.HeaderAppendContentEncoding; // the json content is utf8 encoded
                }
            }
            // else if communication is for a file upload, then set the appropriate request parameters
            else
            {
                httpRequest.ContentType = CLDefinitions.HeaderAppendContentTypeBinary; // content will be direct binary stream
                httpRequest.ContentLength = uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0; // content length will be file size
                httpRequest.Headers[CLDefinitions.HeaderAppendStorageKey] = uploadDownload.ChangeToTransfer.Metadata.StorageKey; // add header for destination location of file
                httpRequest.Headers[CLDefinitions.HeaderAppendContentMD5] = ((uploadParams)uploadDownload).Hash; // set MD5 content hash for verification of upload stream
                httpRequest.KeepAlive = true; // do not close connection (is this needed?)
                requestContentBytes = null; // do not write content bytes since they will come from the Stream inside the upload object
            }
            #endregion

            #region trace request
            // if communication is supposed to be traced, then trace it
            if ((CopiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
            {
                // trace communication for the current request
                ComTrace.LogCommunication(CopiedSettings.TraceLocation, // location of trace file
                    CopiedSettings.DeviceId, // device id
                    SyncBoxId, // user id
                    CommunicationEntryDirection.Request, // direction is request
                    serverUrl + serverMethodPath, // location for the server method
                    true, // trace is enabled
                    httpRequest.Headers, // headers of request
                    ((uploadDownload != null && uploadDownload is uploadParams) // special condition for the request body content based on whether this is a file upload or not
                        ? "---File upload started---" // truncate the request body content to a predefined string so that the entire uploaded file is not written as content
                        : (requestContentBytes == null // condition on whether there were bytes to write in the request content body
                            ? null // if there were no bytes to write in the request content body, then log for none
                            : Encoding.UTF8.GetString(requestContentBytes))), // if there were no bytes to write in the request content body, then log them (in string form)
                    null, // no status code for requests
                    CopiedSettings.TraceExcludeAuthorization, // whether or not to exclude authorization information (like the authentication key)
                    httpRequest.Host, // host value which would be part of the headers (but cannot be pulled from headers directly)
                    ((requestContentBytes != null || (uploadDownload != null && uploadDownload is uploadParams))
                        ? httpRequest.ContentLength.ToString() // if the communication had bytes to upload from an input object or a stream to upload for a file, then set the content length value which would be part of the headers (but cannot be pulled from headers directly)
                        : null), // else if the communication would not have any request content, then log no content length header
                    (httpRequest.Expect == null ? "100-continue" : httpRequest.Expect), // expect value which would be part of the headers (but cannot be pulled from headers directly)
                    (httpRequest.KeepAlive ? "Keep-Alive" : "Close")); // keep-alive value which would be part of the headers (but cannot be pulled from headers directly)
            }
            #endregion

            // status setup is for file uploads and downloads which fire event callbacks to fire global status events
            #region status setup
            // define size to be used for status update event callbacks
            long storeSizeForStatus;
            // declare the time when the transfer started (inaccurate for file downloads since the time is set before the request for the download and not before the download actually starts)
            DateTime transferStartTime;

            // if this communiction is not for a file upload or download, then the status parameters won't be used and can be set as nothing
            if (uploadDownload == null)
            {
                storeSizeForStatus = 0;
                transferStartTime = DateTime.MinValue;
            }
            // else if this communication is for a file upload or download, then set the status event parameters
            else
            {
                // check to make sure this is in fact an upload or download
                if (!(uploadDownload is uploadParams)
                    && !(uploadDownload is downloadParams))
                {
                    throw new ArgumentException("uploadDownload must be either upload or download");
                }

                // set the status event parameters

                storeSizeForStatus = uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0; // pull size from the change to transfer
                transferStartTime = DateTime.Now; // use the current local time as transfer start time
            }
            #endregion

            #region write request
            // if this communication is for a file upload or download, then process its request accordingly
            if (uploadDownload != null)
            {
                // get the request stream
                Stream httpRequestStream = null;

                // try/finally process the upload request (which actually uploads the file) or download request, finally dispose the request stream if it was set
                try
                {
                    // if the current communication is file upload, then upload the file
                    if (uploadDownload is uploadParams)
                    {
                        if (uploadDownload.StatusUpdate != null
                            && uploadDownload.StatusUpdateId != null)
                        {
                            try
                            {
                                uploadDownload.StatusUpdate((Guid)uploadDownload.StatusUpdateId,
                                    uploadDownload.ChangeToTransfer.EventId,
                                    uploadDownload.ChangeToTransfer.Direction,
                                    uploadDownload.RelativePathForStatus,
                                    0,
                                    (long)uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size,
                                    false);
                            }
                            catch
                            {
                            }
                        }

                        try
                        {
                            // grab the upload request stream asynchronously since it can take longer than the provided timeout milliseconds
                            httpRequestStream = AsyncGetUploadRequestStreamOrDownloadResponse(uploadDownload.ShutdownToken, httpRequest, upload: true) as Stream;
                        }
                        catch (WebException ex)
                        {
                            if (ex.Status == WebExceptionStatus.ConnectFailure)
                            {
                                status = CLHttpRestStatus.ConnectionFailed;
                            }

                            throw ex;
                        }

                        // if there was no request stream retrieved, then the request was cancelled so return cancelled
                        if (httpRequestStream == null)
                        {
                            status = CLHttpRestStatus.Cancelled;
                            return null;
                        }

                        // define a transfer buffer between the file and the upload stream
                        byte[] uploadBuffer = new byte[FileConstants.BufferSize];

                        // declare a count of the bytes read in each buffer read from the file
                        int bytesRead;
                        // define a count for the total amount of bytes uploaded so far
                        long totalBytesUploaded = 0;

                        if (uploadDownload.ProgressHolder != null)
                        {
                            lock (uploadDownload.ProgressHolder)
                            {
                                uploadDownload.ProgressHolder.Value = new TransferProgress(
                                    0,
                                    storeSizeForStatus);
                            }
                        }

                        if (uploadDownload.ACallback != null)
                        {
                            uploadDownload.ACallback(uploadDownload.AResult);
                        }

                        Helpers.Md5Hasher hasher;

                        if (((uploadParams)uploadDownload).UploadStreamContext == null)
                        {
                            hasher = null; // won't check intermediate hashes
                        }
                        else
                        {
                            hasher = new Helpers.Md5Hasher(FileConstants.MaxUploadIntermediateHashBytesSize);
                            hasher.OnIntermediateHash += (byte[] hash, int index) =>
                            {
                                byte[][] intermediateHashes = ((uploadParams)uploadDownload).UploadStreamContext.IntermediateHashes;
                                bool intermediateHashesMismatch = (intermediateHashes.Length <= index);
                                if (!intermediateHashesMismatch)
                                {
                                    intermediateHashesMismatch = !Helpers.IsEqualHashes(hash, intermediateHashes[index]);
                                }
                                if (intermediateHashesMismatch)
                                {
                                    throw new HashMismatchException("intermediate hash does not match; file has been edited;");
                                }
                            };
                        }

                        try
                        {
                            // loop till there are no more bytes to read, on the loop condition perform the buffer transfer from the file and store the read byte count
                            while ((bytesRead = ((uploadParams)uploadDownload).Stream.Read(uploadBuffer, 0, uploadBuffer.Length)) != 0)
                            {
                                if (hasher != null)
                                {
                                    hasher.Update(uploadBuffer, bytesRead);
                                }

                                // write the buffer from the file to the upload stream
                                httpRequestStream.Write(uploadBuffer, 0, bytesRead);
                                // add the number of bytes read on the current buffer transfer to the total bytes uploaded
                                totalBytesUploaded += bytesRead;

                                // check for sync shutdown
                                if (uploadDownload.ShutdownToken != null)
                                {
                                    Monitor.Enter(uploadDownload.ShutdownToken);
                                    try
                                    {
                                        if (uploadDownload.ShutdownToken.Token.IsCancellationRequested)
                                        {
                                            status = CLHttpRestStatus.Cancelled;
                                            return null;
                                        }
                                    }
                                    finally
                                    {
                                        Monitor.Exit(uploadDownload.ShutdownToken);
                                    }
                                }

                                if (uploadDownload.ProgressHolder != null)
                                {
                                    lock (uploadDownload.ProgressHolder)
                                    {
                                        uploadDownload.ProgressHolder.Value = new TransferProgress(
                                            totalBytesUploaded,
                                            storeSizeForStatus);
                                    }
                                }

                                if (uploadDownload.ACallback != null)
                                {
                                    uploadDownload.ACallback(uploadDownload.AResult);
                                }

                                // fire event callbacks for status change on uploading
                                uploadDownload.StatusCallback(new CLStatusFileTransferUpdateParameters(
                                        transferStartTime, // time of upload start
                                        storeSizeForStatus, // total size of file
                                        uploadDownload.RelativePathForStatus, // relative path of file
                                        totalBytesUploaded), // bytes uploaded so far
                                    uploadDownload.ChangeToTransfer, // the source of the event (the event itself)
                                    SyncBoxId, // pass in sync box id to allow filtering
                                    CopiedSettings.DeviceId); // pass in device id to allow filtering

                                if (uploadDownload.StatusUpdate != null
                                    && uploadDownload.StatusUpdateId != null)
                                {
                                    try
                                    {
                                        uploadDownload.StatusUpdate((Guid)uploadDownload.StatusUpdateId,
                                            uploadDownload.ChangeToTransfer.EventId,
                                            uploadDownload.ChangeToTransfer.Direction,
                                            uploadDownload.RelativePathForStatus,
                                            totalBytesUploaded,
                                            (long)uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size,
                                            false);
                                    }
                                    catch
                                    {
                                    }
                                }
                            }

                            if (hasher != null)
                            {
                                // check the final hash
                                hasher.FinalizeHashes();
                                if (!Helpers.IsEqualHashes(hasher.Hash, ((uploadParams)uploadDownload).UploadStreamContext.Hash))
                                {
                                    throw new HashMismatchException("final hash does not match; file has been edited;");
                                }
                            }

                        }
                        finally
                        {
                            // upload is finished so stream can be disposed
                            ((uploadParams)uploadDownload).DisposeStreamContext();
                        }

                    }
                    // else if the communication is a file download, write the request stream content from the serialized download request object
                    else
                    {
                        try
                        {
                            // grab the request stream for writing
                            httpRequestStream = httpRequest.GetRequestStream();
                        }
                        catch (WebException ex)
                        {
                            if (ex.Status == WebExceptionStatus.ConnectFailure)
                            {
                                status = CLHttpRestStatus.ConnectionFailed;
                            }

                            throw ex;
                        }

                        // write the request for the download
                        httpRequestStream.Write(requestContentBytes, 0, requestContentBytes.Length);
                    }
                }
                finally
                {
                    // dispose the request stream if it was set
                    if (httpRequestStream != null)
                    {
                        try
                        {
                            httpRequestStream.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }
            }
            // else if the communication is neither an upload nor download and there is a serialized request object to write, then get the request stream and write to it
            else if (requestContentBytes != null)
            {
                // create a function to get the request Stream which can be used inline in a using statement which disposes the returned stream,
                // the additional functionality is to check for a specific error getting the request for connection failure
                Func<HttpWebRequest, GenericHolder<bool>, Stream> wrapGetRequest = (innerRequest, connectionFailedHolder) =>
                {
                    if (innerRequest == null)
                    {
                        throw new NullReferenceException("innerRequest cannot be null");
                    }
                    if (connectionFailedHolder == null)
                    {
                        throw new NullReferenceException("connectionFailedHolder cannot be null");
                    }

                    try
                    {
                        return innerRequest.GetRequestStream();
                    }
                    catch (WebException ex)
                    {
                        if (ex.Status == WebExceptionStatus.ConnectFailure)
                        {
                            connectionFailedHolder.Value = true;
                        }

                        throw ex;
                    }
                };

                // define a holder for whether the connection attempt failed
                GenericHolder<bool> connectionFailed = new GenericHolder<bool>(false);

                // try/finally to get the request stream and write the request content, finally check if it failed on connection attempt to mark the appropriate output status
                try
                {
                    using (Stream httpRequestStream = wrapGetRequest(httpRequest, connectionFailed))
                    {
                        httpRequestStream.Write(requestContentBytes, 0, requestContentBytes.Length);
                    }
                }
                finally
                {
                    if (connectionFailed.Value)
                    {
                        status = CLHttpRestStatus.ConnectionFailed;
                    }
                }
            }
            #endregion

            // define the web response outside the regions "get response" and "process response stream" so it can finally be closed (if it ever gets set); also for trace
            HttpWebResponse httpResponse = null; // communication response
            string responseBody = null; // string body content of response (for a string output is used instead of the response stream itself)
            Stream responseStream = null; // response stream (when the communication output is a deserialized object instead of a simple string representation)
            Stream serializationStream = null; // a possible copy of the response stream for when the stream has to be used both for trace and for deserializing a return object

            // try/catch/finally get the response and process its stream for output,
            // on error send a final status event if communication is for upload or download,
            // finally possibly trace if a string response was used and dispose any response/response streams
            try
            {
                #region get response
                // if the communication is a download, then grab the download response asynchronously so its time is not limited to the timeout milliseconds
                if (uploadDownload != null
                    && uploadDownload is downloadParams)
                {
                    // grab the download response asynchronously so its time is not limited to the timeout milliseconds
                    httpResponse = AsyncGetUploadRequestStreamOrDownloadResponse(uploadDownload.ShutdownToken, httpRequest, false) as HttpWebResponse;

                    // if there was no download response, then it was cancelled so return as such
                    if (httpRequest == null)
                    {
                        status = CLHttpRestStatus.Cancelled;
                        return null;
                    }
                }
                // else if the communication is not a download, then grab the response
                else
                {
                    // try/catch grab the communication response, on catch try to pull the response from the exception otherwise rethrow the exception
                    try
                    {
                        httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                    }
                    catch (WebException ex)
                    {
                        if (ex.Response == null)
                        {
                            throw new NullReferenceException(
                                String.Format("httpResponse GetResponse at URL {0}, MethodPath {1}",
                                        (serverUrl ?? "{missing serverUrl}"),
                                        (serverMethodPath ?? "{missing serverMethodPath}"))
                                    + " threw a WebException without a WebResponse",
                                ex);
                        }

                        httpResponse = (HttpWebResponse)ex.Response;
                    }
                }

                // if the status code of the response is not in the provided HashSet of those which represent success,
                // then try to provide a more specific return status and try to pull the content from the response as a string and throw an exception for invalid status code
                if (!validStatusCodes.Contains(httpResponse.StatusCode))
                {
                    // if response status code is a not found, then set the output status accordingly
                    if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        status = CLHttpRestStatus.NotFound;
                    }
                    // else if response status was not a not found and is a no content, then set the output status accordingly
                    else if (httpResponse.StatusCode == HttpStatusCode.NoContent
                        || ((int)httpResponse.StatusCode) == CLDefinitions.CustomNoContentCode) // alternative to no content, so the server can include an error message
                    {
                        status = CLHttpRestStatus.NoContent;
                    }
                    // else if the response status was neither a not found nor a no content and is an unauthorized, then set the output state accordingly
                    else if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        status = CLHttpRestStatus.NotAuthorized;
                    }
                    // else if response status was neither a not found nor a no content and is within the range of a server error (5XX), then set the output status accordingly
                    else
                    {
                        // define the cast int for the status code from the enumeration
                        int statusCodeInt = (int)httpResponse.StatusCode;

                        // if storage quota exceeded then use that status
                        if (statusCodeInt == CLDefinitions.CustomQuotaExceededCode /* Storage quota exceeded code, not in the HttpStatusCode enumeration */)
                        {
                            status = CLHttpRestStatus.QuotaExceeded;
                        }
                        // else if storage quota not exceeded, perform the (5XX) code check for other server error
                        else if (((HttpStatusCode)(statusCodeInt - (statusCodeInt % 100))) == HttpStatusCode.InternalServerError)
                        {
                            status = CLHttpRestStatus.ServerError;
                        }
                    }

                    // try/catch to set the response body from the content of the response, on catch silence the error
                    try
                    {
                        // grab the response stream
                        using (Stream downloadResponseStream = httpResponse.GetResponseStream())
                        {
                            // read the response as UTF8 text
                            using (StreamReader downloadResponseStreamReader = new StreamReader(downloadResponseStream, Encoding.UTF8))
                            {
                                // set the response text
                                responseBody = downloadResponseStreamReader.ReadToEnd();

                                if (!string.IsNullOrEmpty(responseBody)
                                    && status == CLHttpRestStatus.NotAuthorized)
                                {
                                    using (MemoryStream notAuthorizedStream = new MemoryStream())
                                    {
                                        byte[] notAuthorizedMessageBytes = Encoding.Default.GetBytes(responseBody);

                                        notAuthorizedStream.Write(notAuthorizedMessageBytes, 0, notAuthorizedMessageBytes.Length);
                                        notAuthorizedStream.Flush();
                                        notAuthorizedStream.Seek(0, SeekOrigin.Begin);

                                        DataContractJsonSerializer notAuthorizedSerializer;
                                        if (!SerializableResponseTypes.TryGetValue(typeof(JsonContracts.AuthenticationErrorResponse), out notAuthorizedSerializer))
                                        {
                                            throw new KeyNotFoundException("Unable to find serializer for JsonContracts.AuthenticationErrorResponse in SerializableResponseTypes");
                                        }

                                        #region AuthenticationErrorMessage deprecated, replaced by AuthenticationError below
                                        //DataContractJsonSerializer notAuthorizedMessageSerializer;
                                        //if (!SerializableResponseTypes.TryGetValue(typeof(JsonContracts.AuthenticationErrorMessage), out notAuthorizedMessageSerializer))
                                        //{
                                        //    throw new KeyNotFoundException("Unable to find serializer for JsonContracts.AuthenticationErrorMessage in SerializableResponseTypes");
                                        //}

                                        //JsonContracts.AuthenticationErrorResponse parsedErrorResponse = (JsonContracts.AuthenticationErrorResponse)notAuthorizedSerializer.ReadObject(notAuthorizedStream);

                                        //if (parsedErrorResponse.SerializedMessages != null)
                                        //{
                                        //    foreach (string serializedInnerMessage in parsedErrorResponse.SerializedMessages)
                                        //    {
                                        //        try
                                        //        {
                                        //            using (MemoryStream notAuthorizedMessageStream = new MemoryStream())
                                        //            {
                                        //                byte[] notAuthorizedInnerMessageBytes = Encoding.Default.GetBytes(serializedInnerMessage);

                                        //                notAuthorizedMessageStream.Write(notAuthorizedInnerMessageBytes, 0, notAuthorizedInnerMessageBytes.Length);
                                        //                notAuthorizedMessageStream.Flush();
                                        //                notAuthorizedMessageStream.Seek(0, SeekOrigin.Begin);

                                        //                JsonContracts.AuthenticationErrorMessage parsedErrorMessage = (JsonContracts.AuthenticationErrorMessage)notAuthorizedMessageSerializer.ReadObject(notAuthorizedMessageStream);

                                        //                if (parsedErrorMessage.Message == CLDefinitions.MessageTextExpiredCredentials)
                                        //                {
                                        //                    status = CLHttpRestStatus.NotAuthorizedExpiredCredentials;
                                        //                    break;
                                        //                }
                                        //            }
                                        //        }
                                        //        catch
                                        //        {
                                        //        }
                                        //    }
                                        //}
                                        #endregion

                                        JsonContracts.AuthenticationErrorResponse parsedErrorResponse = (JsonContracts.AuthenticationErrorResponse)notAuthorizedSerializer.ReadObject(notAuthorizedStream);

                                        if (parsedErrorResponse.AuthenticationErrors != null
                                            && parsedErrorResponse.AuthenticationErrors.Any(authenticationError => authenticationError.CodeAsEnum == AuthenticationErrorType.SessionExpired))
                                        {
                                            status = CLHttpRestStatus.NotAuthorizedExpiredCredentials;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                    }

                    // throw the exception for an invalid response
                    throw new Exception(String.Format("Invalid HTTP response status code at URL {0}, MethodPath {1}",
                        (serverUrl ?? "{missing serverUrl"),
                        (serverMethodPath ?? "{missing serverMethodPath")) +
                            ": " + ((int)httpResponse.StatusCode).ToString() +
                            (responseBody == null
                                ? string.Empty
                                : Environment.NewLine + "Response:" + Environment.NewLine +
                                    responseBody)); // either the default "incomplete" body or the body retrieved from the response content
                }
                #endregion

                #region process response stream
                // define an object for the communication return, defaulting to null
                T toReturn = null;

                // if the communication was an upload or a download, then process the response stream for a download (which is the download itself) or use a predefined return for an upload
                if (uploadDownload != null)
                {
                    // if communication is an upload, then use a predefined return
                    if (uploadDownload is uploadParams)
                    {
                        // set body as successful value
                        responseBody = "---File upload complete---";

                        try
                        {
                            // grab the stream from the response content
                            responseStream = httpResponse.GetResponseStream();

                            // create a reader for the response content
                            using (TextReader uploadCompleteStream = new StreamReader(responseStream, Encoding.UTF8))
                            {
                                // set the body as successful value
                                responseBody = ((object)uploadCompleteStream.ReadToEnd()).ToString();
                            }
                        }
                        catch
                        {
                        }

                        // if we can use a string output for the return, then use it
                        if (typeof(T) == typeof(string)
                            || typeof(T) == typeof(object))
                        {
                            toReturn = (T)((object)responseBody);
                        }
                    }
                    // else if communication is a download, then process the actual download itself
                    else
                    {
                        // set the response body to a value that will be displayed if the actual response fails to process
                        responseBody = "---Incomplete file download---";

                        try
                        {
                            if (uploadDownload.StatusUpdate != null
                                && uploadDownload.StatusUpdateId != null)
                            {
                                if (uploadDownload.RelativePathForStatus != null)
                                {
                                    try
                                    {
                                        uploadDownload.StatusUpdate((Guid)uploadDownload.StatusUpdateId,
                                            uploadDownload.ChangeToTransfer.EventId,
                                            uploadDownload.ChangeToTransfer.Direction,
                                            uploadDownload.RelativePathForStatus,
                                            0,
                                            (long)uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size,
                                            false);
                                    }
                                    catch
                                    {
                                    }
                                }
                            }

                            // create a new unique id for the download
                            Guid newTempFile = Guid.NewGuid();

                            // if a callback was provided to fire before a download starts, then fire it
                            if (((downloadParams)uploadDownload).BeforeDownloadCallback != null)
                            {
                                ((downloadParams)uploadDownload).BeforeDownloadCallback(newTempFile, ((downloadParams)uploadDownload).BeforeDownloadUserState);
                            }

                            // calculate location for downloading the file
                            string newTempFileString = ((downloadParams)uploadDownload).TempDownloadFolderPath + "\\" + ((Guid)newTempFile).ToString("N");

                            if (uploadDownload.ProgressHolder != null)
                            {
                                lock (uploadDownload.ProgressHolder)
                                {
                                    uploadDownload.ProgressHolder.Value = new TransferProgress(
                                        0,
                                        storeSizeForStatus);
                                }
                            }

                            if (uploadDownload.ACallback != null)
                            {
                                uploadDownload.ACallback(uploadDownload.AResult);
                            }

                            // get the stream of the download
                            using (Stream downloadResponseStream = httpResponse.GetResponseStream())
                            {
                                // create a stream by creating a non-shared writable file at the file path
                                using (FileStream tempFileStream = new FileStream(newTempFileString, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    // define a count for the total bytes downloaded
                                    long totalBytesDownloaded = 0;
                                    // create the buffer for transferring bytes from the download stream to the file stream
                                    byte[] data = new byte[CLDefinitions.SyncConstantsResponseBufferSize];
                                    // declare an int for the amount of bytes read in each buffer transfer
                                    int read;

                                    // loop till there are no more bytes to read, on the loop condition perform the buffer transfer from the download stream and store the read byte count
                                    while ((read = downloadResponseStream.Read(data, 0, data.Length)) > 0)
                                    {
                                        // write the current buffer to the file
                                        tempFileStream.Write(data, 0, read);
                                        // append the count of the read bytes on this buffer transfer to the total downloaded
                                        totalBytesDownloaded += read;

                                        GenericHolder<string> storeReturnBody = new GenericHolder<string>(null);
                                        Func<GenericHolder<string>, string, string> fillAndReturnBody =
                                            (innerStoreReturnBody, innerResponseBody) =>
                                            {
                                                if (innerStoreReturnBody.Value == null)
                                                {
                                                    innerStoreReturnBody.Value = (innerResponseBody ?? "---responseBody set to null---").TrimEnd('-') + ": cancelled---";
                                                }
                                                return innerStoreReturnBody.Value;
                                            };

                                        if (uploadDownload.ChangeToTransfer.NewPath == null)
                                        {
                                            status = CLHttpRestStatus.Cancelled;

                                            responseBody = fillAndReturnBody(storeReturnBody, responseBody);

                                            return null;
                                        }

                                        // check for sync shutdown
                                        if (uploadDownload.ShutdownToken != null)
                                        {
                                            Monitor.Enter(uploadDownload.ShutdownToken);
                                            try
                                            {
                                                if (uploadDownload.ShutdownToken.Token.IsCancellationRequested)
                                                {
                                                    status = CLHttpRestStatus.Cancelled;

                                                    responseBody = fillAndReturnBody(storeReturnBody, responseBody);

                                                    return null;
                                                }
                                            }
                                            finally
                                            {
                                                Monitor.Exit(uploadDownload.ShutdownToken);
                                            }
                                        }

                                        if (uploadDownload.ProgressHolder != null)
                                        {
                                            lock (uploadDownload.ProgressHolder)
                                            {
                                                uploadDownload.ProgressHolder.Value = new TransferProgress(
                                                    totalBytesDownloaded,
                                                    storeSizeForStatus);
                                            }
                                        }

                                        if (uploadDownload.ACallback != null)
                                        {
                                            uploadDownload.ACallback(uploadDownload.AResult);
                                        }

                                        if (uploadDownload.StatusUpdate != null
                                            && uploadDownload.StatusUpdateId != null)
                                        {
                                            try
                                            {
                                                uploadDownload.StatusUpdate((Guid)uploadDownload.StatusUpdateId,
                                                    uploadDownload.ChangeToTransfer.EventId,
                                                    uploadDownload.ChangeToTransfer.Direction,
                                                    uploadDownload.RelativePathForStatus,
                                                    totalBytesDownloaded,
                                                    (long)uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size,
                                                    false);
                                            }
                                            catch
                                            {
                                            }
                                        }

                                        // fire event callbacks for status change on uploading
                                        uploadDownload.StatusCallback(
                                            new CLStatusFileTransferUpdateParameters(
                                                    transferStartTime, // start time for download
                                                    storeSizeForStatus, // total file size
                                                    uploadDownload.RelativePathForStatus, // relative path of file
                                                    totalBytesDownloaded), // current count of completed download bytes
                                            uploadDownload.ChangeToTransfer, // the source of the event, the event itself
                                            SyncBoxId, // pass in sync box id for filtering
                                            CopiedSettings.DeviceId); // pass in device id for filtering
                                    }
                                    // flush file stream to finish the file
                                    tempFileStream.Flush();
                                }
                            }

                            // set the file attributes so when the file move triggers a change in the event source its metadata should match the current event;
                            // also, perform each attribute change with up to 4 retries since it seems to throw errors under normal conditions (if it still fails then it rethrows the exception);
                            // attributes to set: creation time, last modified time, and last access time

                            Helpers.RunActionWithRetries(actionState => System.IO.File.SetCreationTimeUtc(actionState.Key, actionState.Value),
                                new KeyValuePair<string, DateTime>(newTempFileString, uploadDownload.ChangeToTransfer.Metadata.HashableProperties.CreationTime),
                                true);
                            Helpers.RunActionWithRetries(actionState => System.IO.File.SetLastAccessTimeUtc(actionState.Key, actionState.Value),
                                new KeyValuePair<string, DateTime>(newTempFileString, uploadDownload.ChangeToTransfer.Metadata.HashableProperties.LastTime),
                                true);
                            Helpers.RunActionWithRetries(actionState => System.IO.File.SetLastWriteTimeUtc(actionState.Key, actionState.Value),
                                new KeyValuePair<string, DateTime>(newTempFileString, uploadDownload.ChangeToTransfer.Metadata.HashableProperties.LastTime),
                                true);


                            // fire callback to perform the actual move of the temp file to the final destination
                            ((downloadParams)uploadDownload).AfterDownloadCallback(newTempFileString, // location of temp file
                                uploadDownload.ChangeToTransfer,
                                ref responseBody, // reference to response string (sets to "---Completed file download---" on success)
                                ((downloadParams)uploadDownload).AfterDownloadUserState, // timer for failure queue
                                newTempFile); // id for the downloaded file

                            // if the after downloading callback set the response to null, then replace it saying it was null
                            if (responseBody == null)
                            {
                                responseBody = "---responseBody set to null---";
                            }

                            // if a string can be output as the return type, then return the response (which is not the actual download, but a simple string status representation)
                            if (typeof(T) == typeof(string)
                                || typeof(T) == typeof(object))
                            {
                                toReturn = (T)((object)responseBody);
                            }
                        }
                        catch (Exception ex)
                        {
                            responseBody = (responseBody ?? "---responseBody set to null---").TrimEnd('-') + ": " + ex.Message + "---";

                            throw ex;
                        }
                    }
                }
                // else if the communication was neither an upload nor a download, then process the response stream for return
                else
                {
                    // declare the serializer which will be used to deserialize the response content for output
                    DataContractJsonSerializer outSerializer;
                    // try to get the serializer for the output by the type of output from dictionary and if successful, process response content as stream to deserialize
                    if (SerializableResponseTypes.TryGetValue(typeof(T), out outSerializer))
                    {
                        // grab the stream for response content
                        responseStream = httpResponse.GetResponseStream();

                        // set the stream for processing the response by a copy of the communication stream (if trace enabled) or the communication stream itself (if trace is not enabled)
                        serializationStream = (((CopiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                            ? Helpers.CopyHttpWebResponseStreamAndClose(responseStream) // if trace is enabled, then copy the communications stream to a memory stream
                            : responseStream); // if trace is not enabled, use the communication stream

                        // if tracing communication, then trace communication
                        if ((CopiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                        {
                            // log communication for stream body
                            ComTrace.LogCommunication(CopiedSettings.TraceLocation, // trace file location
                                CopiedSettings.DeviceId, // device id
                                SyncBoxId, // user id
                                CommunicationEntryDirection.Response, // communication direction is response
                                serverUrl + serverMethodPath, // input parameter method path
                                true, // trace is enabled
                                httpResponse.Headers, // response headers
                                serializationStream, // copied response stream
                                (int)httpResponse.StatusCode, // status code of the response
                                CopiedSettings.TraceExcludeAuthorization); // whether to include authorization in the trace (such as the authentication key)
                        }

                        if (typeof(T).GetCustomAttributes(typeof(JsonContracts.ContainsMetadataDictionaryAttribute), false).Length == 1)
                        {
                            string responseString;
                            using (StreamReader responseReader = new StreamReader(serializationStream, Encoding.UTF8))
                            {
                                responseString = responseReader.ReadToEnd();
                            }

                            SimpleJsonBase.SimpleJson.JsonObject deserializedResponse = SimpleJsonBase.SimpleJson.SimpleJson.DeserializeObject(responseString) as SimpleJsonBase.SimpleJson.JsonObject;

                            if (deserializedResponse == null)
                            {
                                throw new NullReferenceException("Deserialized JsonObject null from responseString");
                            }

                            foreach (KeyValuePair<string, object> myDeserializedPair in deserializedResponse)
                            {
                                AddBackMetadataType(myDeserializedPair, typeof(T));
                            }

                            string appendedString = SimpleJsonBase.SimpleJson.SimpleJson.SerializeObject(deserializedResponse);
                            using (MemoryStream appendedStream = new MemoryStream())
                            {
                                byte[] appendedBytes = Encoding.Default.GetBytes(appendedString);
                                appendedStream.Write(appendedBytes, 0, appendedBytes.Length);
                                appendedStream.Flush();
                                appendedStream.Seek(0, SeekOrigin.Begin);

                                toReturn = (T)outSerializer.ReadObject(appendedStream);
                            }
                        }
                        else
                        {
                            // deserialize the response content into the appropriate json contract object
                            toReturn = (T)outSerializer.ReadObject(serializationStream);
                        }
                    }
                    // else if the output type is not in the dictionary of those serializable and if the output type is either object or string,
                    // then process the response content as a string to output directly
                    else if (typeof(T) == typeof(string)
                        || (typeof(T) == typeof(object)))
                    {
                        // grab the stream from the response content
                        responseStream = httpResponse.GetResponseStream();

                        // create a reader for the response content
                        using (TextReader purgeResponseStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            // set the error string from the response
                            toReturn = (T)((object)purgeResponseStreamReader.ReadToEnd());
                        }
                    }
                    // else if the output type is not in the dictionary of those serializable and if the output type is also neither object nor string,
                    // then throw an argument exception
                    else
                    {
                        throw new ArgumentException("T is not a serializable output type nor object/string");
                    }
                }

                // if the code has not thrown an exception by now then it was successful so mark it so in the output
                status = CLHttpRestStatus.Success;
                // return any object set to return for the response, if any
                return toReturn;
                #endregion
            }
            catch
            {
                // if there was an event for the upload or download, then fire the event callback for a final transfer status
                if (uploadDownload != null
                    && (uploadDownload is uploadParams
                        || uploadDownload is downloadParams))
                {
                    // try/catch fire the event callback for final transfer status, silencing errors
                    try
                    {
                        if (uploadDownload.RelativePathForStatus != null)
                        {
                            uploadDownload.StatusCallback(
                                new CLStatusFileTransferUpdateParameters(
                                    transferStartTime, // retrieve the upload start time

                                    // need to send a file size which matches the total uploaded bytes so they are equal to cancel the status
                                    uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0,

                                    // try to build the same relative path that would be used in the normal status, falling back first to the full path then to an empty string
                                    uploadDownload.RelativePathForStatus,

                                    // need to send a total uploaded bytes which matches the file size so they are equal to cancel the status
                                    uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0),
                                uploadDownload.ChangeToTransfer, // sender of event (the event itself)
                                SyncBoxId, // pass in sync box id for filtering
                                CopiedSettings.DeviceId); // pass in device id for filtering
                        }
                    }
                    catch
                    {
                    }

                    if (uploadDownload.StatusUpdate != null
                        && uploadDownload.StatusUpdateId != null)
                    {
                        try
                        {
                            if (uploadDownload.RelativePathForStatus != null)
                            {
                                uploadDownload.StatusUpdate((Guid)uploadDownload.StatusUpdateId,
                                    uploadDownload.ChangeToTransfer.EventId,
                                    uploadDownload.ChangeToTransfer.Direction,
                                    uploadDownload.RelativePathForStatus,
                                    uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0,
                                    uploadDownload.ChangeToTransfer.Metadata.HashableProperties.Size ?? 0,
                                    false);
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                // rethrow
                throw;
            }
            finally
            {
                // for communication logging, log communication if it hasn't already been logged in stream deserialization or dispose the serialization stream
                if ((CopiedSettings.TraceType & TraceType.Communication) == TraceType.Communication)
                {
                    // if there was no stream set for deserialization, then the response was handled as a string and needs to be logged here as such
                    if (serializationStream == null)
                    {
                        if (httpResponse != null)
                        {
                            // log communication for string body
                            ComTrace.LogCommunication(CopiedSettings.TraceLocation, // trace file location
                                CopiedSettings.DeviceId, // device id
                                SyncBoxId, // user id
                                CommunicationEntryDirection.Response, // communication direction is response
                                serverUrl + serverMethodPath, // input parameter method path
                                true, // trace is enabled
                                httpResponse.Headers, // response headers
                                responseBody, // response body (either an overridden string that says "complete" or "incomplete" or an error message from the actual response)
                                (int)httpResponse.StatusCode, // status code of the response
                                CopiedSettings.TraceExcludeAuthorization); // whether to include authorization in the trace (such as the authentication key)
                        }
                    }
                    // else if there was a stream set for deserialization then the response was already logged, but it still needs to be disposed here
                    else if (serializationStream != null)
                    {
                        try
                        {
                            serializationStream.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }

                // if there was a response stream retrieved then try to dispose it
                if (responseStream != null)
                {
                    try
                    {
                        responseStream.Dispose();
                    }
                    catch
                    {
                    }
                }

                // if there was a response retrieved then try to close it
                if (httpResponse != null)
                {
                    try
                    {
                        httpResponse.Close();
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static void CleanTypeKeys(object valueToClean)
        {
            SimpleJsonBase.SimpleJson.JsonObject recurseJson;
            SimpleJsonBase.SimpleJson.JsonArray recurseArray;
            if (valueToClean != null)
            {
                if ((recurseJson = valueToClean as SimpleJsonBase.SimpleJson.JsonObject) != null)
                {
                    recurseJson.Remove(MetadataDictionaryJsonTypePair.Key);
                    foreach (object recurseJsonValue in recurseJson.Values)
                    {
                        CleanTypeKeys(recurseJsonValue);
                    }
                }
                else if ((recurseArray = valueToClean as SimpleJsonBase.SimpleJson.JsonArray) != null)
                {
                    recurseArray.ForEach(recurseJsonValue => CleanTypeKeys(recurseJsonValue));
                }
            }
        }

        private static void AddBackMetadataType(KeyValuePair<string, object> valueToAppend, Type parentType)
        {
            SimpleJsonBase.SimpleJson.JsonObject recurseJson;
            SimpleJsonBase.SimpleJson.JsonArray recurseArray;
            if (valueToAppend.Value != null)
            {
                if ((recurseJson = valueToAppend.Value as SimpleJsonBase.SimpleJson.JsonObject) != null)
                {
                    DataMemberAttribute[] innerMemberAttributes;
                    PropertyInfo innerProperty = parentType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .SingleOrDefault(currentInnerProperty => (innerMemberAttributes = currentInnerProperty.GetCustomAttributes(typeof(DataMemberAttribute), false).Cast<DataMemberAttribute>().ToArray()).Length >= 1
                            && (string.IsNullOrEmpty(innerMemberAttributes[0].Name)
                                ? currentInnerProperty.Name == valueToAppend.Key
                                : innerMemberAttributes[0].Name == valueToAppend.Key));

                    bool passedFirstTypeValue;
                    if (innerProperty == null
                        || innerProperty.PropertyType == typeof(MetadataDictionary))
                    {
                        KeyValuePair<string, object>[] backupArray = new KeyValuePair<string, object>[recurseJson.Count];
                        recurseJson.CopyTo(backupArray, 0);

                        recurseJson.Clear();
                        recurseJson.Add(MetadataDictionaryJsonTypePair);
                        Array.ForEach(backupArray, currentBackup => recurseJson.Add(currentBackup));

                        passedFirstTypeValue = false;
                    }
                    else
                    {
                        passedFirstTypeValue = true;
                    }

                    foreach (KeyValuePair<string, object> recurseJsonPair in recurseJson)
                    {
                        if (passedFirstTypeValue)
                        {
                            AddBackMetadataType(recurseJsonPair,
                                (innerProperty == null
                                    ? typeof(MetadataDictionary)
                                    : innerProperty.PropertyType));
                        }
                        else
                        {
                            passedFirstTypeValue = true;
                        }
                    }
                }
                else if ((recurseArray = valueToAppend.Value as SimpleJsonBase.SimpleJson.JsonArray) != null)
                {
                    recurseArray.ForEach(recurseJsonValue => 
                        AddBackMetadataType(new KeyValuePair<string, object>(GenericHolderValuePropertyName, recurseJsonValue), typeof(GenericHolder<object>)));
                }
            }
        }

        private static string GenericHolderValuePropertyName
        {
            get
            {
                lock (_genericHolderValuePropertyName)
                {
                    if (_genericHolderValuePropertyName.Value == null)
                    {
                        _genericHolderValuePropertyName.Value = ((MemberExpression)((Expression<Func<GenericHolder<object>, object>>)(member => member.Value)).Body).Member.Name;
                    }

                    return _genericHolderValuePropertyName.Value;
                }
            }
        }
        private static readonly GenericHolder<string> _genericHolderValuePropertyName = new GenericHolder<string>(null);

        private static KeyValuePair<string, object> MetadataDictionaryJsonTypePair
        {
            get
            {
                lock (_metadataDictionaryJsonTypePair)
                {
                    if (_metadataDictionaryJsonTypePair.Value == null)
                    {
                        string serialized;
                        using (MemoryStream memStream = new MemoryStream())
                        {
                            (new DataContractJsonSerializer(typeof(GenericHolder<object>)))
                                .WriteObject(memStream, new GenericHolder<object>(new MetadataDictionary()));
                            serialized = Encoding.Default.GetString(memStream.ToArray());
                        }

                        Dictionary<string, object> jsonDeserialized = SimpleJsonBase.SimpleJson.SimpleJson.DeserializeObject<Dictionary<string, object>>(serialized);
                        _metadataDictionaryJsonTypePair.Value = ((SimpleJsonBase.SimpleJson.JsonObject)jsonDeserialized.Single().Value).Single();
                    }
                    return (KeyValuePair<string, object>)_metadataDictionaryJsonTypePair.Value;
                }
            }
        }
        private static readonly GenericHolder<Nullable<KeyValuePair<string, object>>> _metadataDictionaryJsonTypePair = new GenericHolder<Nullable<KeyValuePair<string, object>>>(null);

        /// <summary>
        /// a dual-function wrapper for making asynchronous calls for either retrieving an upload request stream or retrieving a download response
        /// </summary>
        internal static object AsyncGetUploadRequestStreamOrDownloadResponse(CancellationTokenSource shutdownToken, HttpWebRequest httpRequest, bool upload)
        {
            // declare the output object which would be either a Stream for upload request or an HttpWebResponse for a download response
            object toReturn;

            // create new async holder used to make async http calls synchronous
            AsyncRequestHolder requestOrResponseHolder = new AsyncRequestHolder(shutdownToken);

            // declare result from async http call
            IAsyncResult requestOrResponseAsyncResult;

            // lock on async holder for modification
            lock (requestOrResponseHolder)
            {
                // create a callback which handles the IAsyncResult style used in wrapping an asyncronous method to make it synchronous
                AsyncCallback requestOrResponseCallback = new AsyncCallback(MakeAsyncRequestSynchronous);

                // if this helper was called for an upload, then the action is for the request stream
                if (upload)
                {
                    // begin getting the upload request stream asynchronously, using callback which will take the async holder and make the request synchronous again, storing the result
                    requestOrResponseAsyncResult = httpRequest.BeginGetRequestStream(requestOrResponseCallback, requestOrResponseHolder);
                }
                // else if this helper was called for a download, then the action is for the response
                else
                {
                    // begin getting the download response asynchronously, using callback which will take the async holder and make the request synchronous again, storing the result
                    requestOrResponseAsyncResult = httpRequest.BeginGetResponse(requestOrResponseCallback, requestOrResponseHolder);
                }

                // if the request was not already completed synchronously, wait on it to complete
                if (!requestOrResponseHolder.CompletedSynchronously)
                {
                    // wait on the request to become synchronous again
                    Monitor.Wait(requestOrResponseHolder);
                }
            }

            // if there was an error that occurred on the async http call, then rethrow the error
            if (requestOrResponseHolder.Error != null)
            {
                throw requestOrResponseHolder.Error;
            }

            // if the http call was cancelled, then return immediately with default
            if (requestOrResponseHolder.IsCanceled)
            {
                return null;
            }

            // if this helper was called for an upload, then the action is for the request stream
            if (upload)
            {
                toReturn = httpRequest.EndGetRequestStream(requestOrResponseAsyncResult);
            }
            // else if this helper was called for a download, then the action is for the response
            else
            {
                // try/catch to retrieve the response and on catch try to pull the response from the exception otherwise rethrow the exception
                try
                {
                    toReturn = httpRequest.EndGetResponse(requestOrResponseAsyncResult);
                }
                catch (WebException ex)
                {
                    if (ex.Response == null)
                    {
                        throw new NullReferenceException("Download httpRequest EndGetResponse threw a WebException without a WebResponse", ex);
                    }

                    toReturn = ex.Response;
                }
            }

            // output the retrieved request stream or the retrieved response
            return toReturn;
        }

        /// <summary>
        /// Async HTTP operation holder used to help make async calls synchronous
        /// </summary>
        internal sealed class AsyncRequestHolder
        {
            /// <summary>
            /// Whether IAsyncResult was found to be CompletedSynchronously: if so, do not Monitor.Wait
            /// </summary>
            public bool CompletedSynchronously
            {
                get
                {
                    return _completedSynchronously;
                }
            }
            /// <summary>
            /// Mark this when IAsyncResult was found to be CompletedSynchronously
            /// </summary>
            public void MarkCompletedSynchronously()
            {
                _completedSynchronously = true;
            }
            // storage for CompletedSynchronously, only marked when true so default to false
            private bool _completedSynchronously = false;

            /// <summary>
            /// cancelation token to check between async calls to cancel out of the operation
            /// </summary>
            public CancellationTokenSource FullShutdownToken
            {
                get
                {
                    return _fullShutdownToken;
                }
            }
            private readonly CancellationTokenSource _fullShutdownToken;

            /// <summary>
            /// Constructor for the async HTTP operation holder
            /// </summary>
            /// <param name="FullShutdownToken">Token to check for cancelation upon async calls</param>
            public AsyncRequestHolder(CancellationTokenSource FullShutdownToken)
            {
                // store the cancellation token
                this._fullShutdownToken = FullShutdownToken;
            }

            /// <summary>
            /// Whether the current async HTTP operation holder detected cancellation
            /// </summary>
            public bool IsCanceled
            {
                get
                {
                    return _isCanceled;
                }
            }
            // storage for cancellation
            private bool _isCanceled = false;

            /// <summary>
            /// Marks the current async HTTP operation holder as cancelled
            /// </summary>
            public void Cancel()
            {
                _isCanceled = true;
            }

            /// <summary>
            /// Any error that happened during current async HTTP operation
            /// </summary>
            public Exception Error
            {
                get
                {
                    return _error;
                }
            }
            // storage for any error that occurs
            private Exception _error = null;

            /// <summary>
            /// Marks the current async HTTP operation holder with any error that occurs
            /// </summary>
            /// <param name="toMark"></param>
            public void MarkException(Exception toMark)
            {
                // null coallesce the exception with a new exception that the exception was null
                _error = toMark ?? new NullReferenceException("toMark is null");
                // lock on this current async HTTP operation holder for pulsing waiters
                lock (this)
                {
                    Monitor.Pulse(this);
                }
            }
        }

        /// <summary>
        /// Method to make async HTTP operations synchronous which can be ; requires passing an AsyncRequestHolder as the userstate
        /// </summary>
        internal static void MakeAsyncRequestSynchronous(IAsyncResult makeSynchronous)
        {
            // try cast userstate as AsyncRequestHolder
            AsyncRequestHolder castHolder = makeSynchronous.AsyncState as AsyncRequestHolder;

            // ensure the cast userstate was successful
            if (castHolder == null)
            {
                throw new NullReferenceException("makeSynchronous AsyncState must be castable as AsyncRequestHolder");
            }

            // try/catch check for completion or cancellation to pulse the AsyncRequestHolder, on catch mark the exception in the AsyncRequestHolder (which will also pulse out)
            try
            {
                // if marked as completed synchronously pass through to the userstate which is used within the callstack to prevent blocking on Monitor.Wait
                if (makeSynchronous.CompletedSynchronously)
                {
                    lock (castHolder)
                    {
                        castHolder.MarkCompletedSynchronously();
                    }
                }

                // if asynchronous task completed, then pulse the AsyncRequestHolder
                if (makeSynchronous.IsCompleted)
                {
                    if (!makeSynchronous.CompletedSynchronously)
                    {
                        lock (castHolder)
                        {
                            Monitor.Pulse(castHolder);
                        }
                    }
                }
                // else if asychronous task is not completed, then check for cancellation
                else if (castHolder.FullShutdownToken != null)
                {
                    // check for cancellation
                    Monitor.Enter(castHolder.FullShutdownToken);
                    try
                    {
                        // if cancelled, then mark the AsyncRequestHolder as cancelled and pulse out
                        if (castHolder.FullShutdownToken.Token.IsCancellationRequested)
                        {
                            castHolder.Cancel();

                            if (!makeSynchronous.CompletedSynchronously)
                            {
                                lock (castHolder)
                                {
                                    Monitor.Pulse(castHolder);
                                }
                            }
                        }
                    }
                    finally
                    {
                        Monitor.Exit(castHolder.FullShutdownToken);
                    }
                }
            }
            catch (Exception ex)
            {
                // mark AsyncRequestHolder with error (which will also pulse out)
                castHolder.MarkException(ex);
            }
        }

        /// <summary>
        /// </summary>
        internal enum requestMethod : byte
        {
            put,
            get,
            post
        }

        /// <summary>
        /// class which is inherited by both the class for storing upload parameters and the class for storing download parameters, with the common properties between them
        /// </summary>
        internal abstract class uploadDownloadParams
        {
            /// <summary>
            /// Path for the file where it would look on disk after truncating the location of the sync directory from the beginning
            /// </summary>
            public string RelativePathForStatus
            {
                get
                {
                    return _relativePathForStatus;
                }
            }
            private readonly string _relativePathForStatus;

            /// <summary>
            /// A handler delegate to be fired whenever there is new status information for an upload or download (the progress of the upload/download or completion)
            /// </summary>
            public SendUploadDownloadStatus StatusCallback
            {
                get
                {
                    return _statusCallback;
                }
            }
            private readonly SendUploadDownloadStatus _statusCallback;

            /// <summary>
            /// UserState object which is required for calling the StatusCallback for sending status information events
            /// </summary>
            public FileChange ChangeToTransfer
            {
                get
                {
                    return _changeToTransfer;
                }
            }
            private readonly FileChange _changeToTransfer;

            /// <summary>
            /// A non-required (possibly null) user-provided token source which is checked through an upload or download in order to cancel it
            /// </summary>
            public CancellationTokenSource ShutdownToken
            {
                get
                {
                    return _shutdownToken;
                }
            }
            private readonly CancellationTokenSource _shutdownToken;

            /// <summary>
            /// Callback which may be provided by a user to fire for status updates
            /// </summary>
            public AsyncCallback ACallback
            {
                get
                {
                    return _aCallback;
                }
            }
            private readonly AsyncCallback _aCallback;

            /// <summary>
            /// Asynchronous result to be passed upon firing the asynchronous callback
            /// </summary>
            public IAsyncResult AResult
            {
                get
                {
                    return _aResult;
                }
            }
            private readonly IAsyncResult _aResult;

            /// <summary>
            /// Holder for the progress state which can be queried by the user
            /// </summary>
            public GenericHolder<TransferProgress> ProgressHolder
            {
                get
                {
                    return _progressHolder;
                }
            }
            private readonly GenericHolder<TransferProgress> _progressHolder;

            /// <summary>
            /// Callback to fire upon status updates, used internally for getting status from CLSync
            /// </summary>
            public FileTransferStatusUpdateDelegate StatusUpdate
            {
                get
                {
                    return _statusUpdate;
                }
            }
            private readonly FileTransferStatusUpdateDelegate _statusUpdate;


            public Nullable<Guid> StatusUpdateId
            {
                get
                {
                    return _statusUpdateId;
                }
            }
            private readonly Nullable<Guid> _statusUpdateId;

            /// <summary>
            /// The constructor for this abstract base object with all parameters corresponding to all properties
            /// </summary>
            /// <param name="StatusCallback">A handler delegate to be fired whenever there is new status information for an upload or download (the progress of the upload/download or completion)</param>
            /// <param name="ChangeToTransfer">UserState object which is required for calling the StatusCallback for sending status information events</param>
            /// <param name="ShutdownToken">A non-required (possibly null) user-provided token source which is checked through an upload or download in order to cancel it</param>
            /// <param name="SyncRootFullPath">Full path to the root directory being synced</param>
            /// <param name="ACallback">User-provided callback to fire upon asynchronous operation</param>
            /// <param name="AResult">Asynchronous result for firing async callbacks</param>
            /// <param name="ProgressHolder">Holder for a progress state which can be queried by the user</param>
            public uploadDownloadParams(SendUploadDownloadStatus StatusCallback, FileChange ChangeToTransfer, CancellationTokenSource ShutdownToken, string SyncRootFullPath, AsyncCallback ACallback, IAsyncResult AResult, GenericHolder<TransferProgress> ProgressHolder, FileTransferStatusUpdateDelegate StatusUpdate, Nullable<Guid> StatusUpdateId)
            {
                // check for required parameters and error out if not set

                if (ChangeToTransfer == null)
                {
                    throw new NullReferenceException("ChangeToTransfer cannot be null");
                }
                if (ChangeToTransfer.Metadata == null)
                {
                    throw new NullReferenceException("ChangeToTransfer Metadata cannot be null");
                }
                if (ChangeToTransfer.Metadata.HashableProperties.Size == null)
                {
                    throw new NullReferenceException("ChangeToTransfer Metadata HashableProperties Size cannot be null");
                }
                if (((long)ChangeToTransfer.Metadata.HashableProperties.Size) < 0)
                {
                    throw new ArgumentException("ChangeToTransfer Metadata HashableProperties Size must be greater than or equal to zero");
                }
                if (ChangeToTransfer.Metadata.StorageKey == null)
                {
                    throw new ArgumentException("ChangeToTransfer Metadata StorageKey cannot be null");
                }

                //// new path can be null if event was cancelled on an alternate thread, but that only happens for downloads
                if (ChangeToTransfer.Direction == SyncDirection.To
                    && ChangeToTransfer.NewPath == null)
                {
                    throw new NullReferenceException("ChangeToTransfer NewPath cannot be null");
                }

                if (StatusCallback == null)
                {
                    throw new NullReferenceException("StatusCallback cannot be null");
                }

                // set the readonly properties for this instance from the construction parameters

                this._statusCallback = StatusCallback;
                this._changeToTransfer = ChangeToTransfer;
                FilePath retainNewPath = this.ChangeToTransfer.NewPath;
                if (retainNewPath == null)
                {
                    this._relativePathForStatus = null;
                }
                else
                {
                    this._relativePathForStatus = retainNewPath.GetRelativePath((SyncRootFullPath ?? string.Empty), false); // relative path is calculated from full path to file minus full path to sync directory
                }
                this._shutdownToken = ShutdownToken;
                this._aCallback = ACallback;
                this._aResult = AResult;
                this._progressHolder = ProgressHolder;
                this._statusUpdate = StatusUpdate;
                this._statusUpdateId = StatusUpdateId;
            }
        }

        /// <summary>
        /// class for storing download properties which inherits abstract base uploadDownloadParams which stores more necessary properties
        /// </summary>
        internal sealed class downloadParams : uploadDownloadParams
        {
            /// <summary>
            /// A non-required (possibly null) event handler for before a download starts
            /// </summary>
            public BeforeDownloadToTempFile BeforeDownloadCallback
            {
                get
                {
                    return _beforeDownloadCallback;
                }
            }
            private readonly BeforeDownloadToTempFile _beforeDownloadCallback;

            /// <summary>
            /// UserState object passed through as-is when the BeforeDownloadCallback handler is fired
            /// </summary>
            public object BeforeDownloadUserState
            {
                get
                {
                    return _beforeDownloadUserState;
                }
            }
            private readonly object _beforeDownloadUserState;

            /// <summary>
            /// Event handler for after a download completes which needs to move the file from the temp location to its final location and set the response body to "---Completed file download---"
            /// </summary>
            public AfterDownloadToTempFile AfterDownloadCallback
            {
                get
                {
                    return _afterDownloadCallback;
                }
            }
            private readonly AfterDownloadToTempFile _afterDownloadCallback;

            /// <summary>
            /// UserState object passed through as-is when the AfterDownloadCallback handler is fired
            /// </summary>
            public object AfterDownloadUserState
            {
                get
                {
                    return _afterDownloadUserState;
                }
            }
            private readonly object _afterDownloadUserState;

            /// <summary>
            /// Full path location to the directory where temporary download files will be stored
            /// </summary>
            public string TempDownloadFolderPath
            {
                get
                {
                    return _tempDownloadFolderPath;
                }
            }
            private readonly string _tempDownloadFolderPath;

            /// <summary>
            /// The sole constructor for this class with all parameters corresponding to all properties in this class and within its base class uploadDownloadParams
            /// </summary>
            /// <param name="StatusCallback">A handler delegate to be fired whenever there is new status information for an upload or download (the progress of the upload/download or completion)</param>
            /// <param name="ChangeToTransfer">UserState object which is required for calling the StatusCallback for sending status information events</param>
            /// <param name="ShutdownToken">A non-required (possibly null) user-provided token source which is checked through an upload or download in order to cancel it</param>
            /// <param name="SyncRootFullPath">Full path to the root directory being synced</param>
            /// <param name="AfterDownloadCallback">Event handler for after a download completes which needs to move the file from the temp location to its final location and set the response body to "---Completed file download---"</param>
            /// <param name="AfterDownloadUserState">UserState object passed through as-is when the AfterDownloadCallback handler is fired</param>
            /// <param name="TempDownloadFolderPath">Full path location to the directory where temporary download files will be stored</param>
            /// <param name="ACallback">User-provided callback to fire upon asynchronous operation</param>
            /// <param name="AResult">Asynchronous result for firing async callbacks</param>
            /// <param name="ProgressHolder">Holder for a progress state which can be queried by the user</param>
            /// <param name="BeforeDownloadCallback">A non-required (possibly null) event handler for before a download starts</param>
            /// <param name="BeforeDownloadUserState">UserState object passed through as-is when the BeforeDownloadCallback handler is fired</param>
            public downloadParams(AfterDownloadToTempFile AfterDownloadCallback, object AfterDownloadUserState, string TempDownloadFolderPath, SendUploadDownloadStatus StatusCallback, FileChange ChangeToTransfer, CancellationTokenSource ShutdownToken, string SyncRootFullPath, AsyncCallback ACallback, IAsyncResult AResult, GenericHolder<TransferProgress> ProgressHolder, FileTransferStatusUpdateDelegate StatusUpdate, Nullable<Guid> StatusUpdateId, BeforeDownloadToTempFile BeforeDownloadCallback = null, object BeforeDownloadUserState = null)
                : base(StatusCallback, ChangeToTransfer, ShutdownToken, SyncRootFullPath, ACallback, AResult, ProgressHolder, StatusUpdate, StatusUpdateId)
            {
                // additional checks for parameters which were not already checked via the abstract base constructor

                if (base.ChangeToTransfer.Direction != SyncDirection.From)
                {
                    throw new ArgumentException("Invalid ChangeToTransfer Direction for a download: " + base.ChangeToTransfer.Direction.ToString());
                }
                //// I changed my mind about this one. We can allow the before download callback to be null.
                //// But, the after download callback is still required since that needs to perform the actual file move operation from temp directory to final location.
                //if (BeforeDownloadCallback == null)
                //{
                //    throw new NullReferenceException("BeforeDownloadCallback cannot be null");
                //}
                if (AfterDownloadCallback == null)
                {
                    throw new NullReferenceException("AfterDownloadCallback cannot be null");
                }

                // set all the readonly fields for public properties by all the parameters which were not passed to the abstract base class

                this._beforeDownloadCallback = BeforeDownloadCallback;
                this._beforeDownloadUserState = BeforeDownloadUserState;
                this._afterDownloadCallback = AfterDownloadCallback;
                this._afterDownloadUserState = AfterDownloadUserState;
                this._tempDownloadFolderPath = TempDownloadFolderPath;
            }
        }

        /// <summary>
        /// class for storing download properties which inherits abstract base uploadDownloadParams which stores more necessary properties
        /// </summary>
        internal sealed class uploadParams : uploadDownloadParams
        {
            /// <summary>
            /// Stream which will be read from to buffer to write into the upload stream, or null if already disposed
            /// </summary>
            public Stream Stream
            {
                get
                {
                    return (_streamContextDisposed
                        ? null
                        : (_streamContext == null ? null : _streamContext.Stream));
                }
            }

            public StreamContext StreamContext
            {
                get
                {
                    return _streamContext;
                }
            }
            private readonly StreamContext _streamContext;

            public UploadStreamContext UploadStreamContext
            {
                get
                {
                    return _streamContext as UploadStreamContext;
                }
            }

            /// <summary>
            /// Disposes Stream for the upload if it was not already disposed and marks that it was disposed; not thread-safe disposal checking
            /// </summary>
            public void DisposeStreamContext()
            {
                if (!_streamContextDisposed)
                {
                    try
                    {
                        _streamContext.Dispose();
                    }
                    catch
                    {
                    }
                    _streamContextDisposed = true;
                }
            }
            private bool _streamContextDisposed = false;

            /// <summary>
            /// MD5 hash lowercase hexadecimal string for the entire upload content
            /// </summary>
            public string Hash
            {
                get
                {
                    return _hash;
                }
            }
            private readonly string _hash;

            /// <summary>
            /// The sole constructor for this class with all parameters corresponding to all properties in this class and within its base class uploadDownloadParams
            /// </summary>
            /// <param name="StatusCallback">A handler delegate to be fired whenever there is new status information for an upload or download (the progress of the upload/download or completion)</param>
            /// <param name="ChangeToTransfer">UserState object which is required for calling the StatusCallback for sending status information events; also used to retrieve the StorageKey and MD5 hash for upload</param>
            /// <param name="ShutdownToken">A non-required (possibly null) user-provided token source which is checked through an upload or download in order to cancel it</param>
            /// <param name="SyncRootFullPath">Full path to the root directory being synced</param>
            /// <param name="Stream">Stream which will be read from to buffer to write into the upload stream, or null if already disposed</param>
            /// <param name="ACallback">User-provided callback to fire upon asynchronous operation</param>
            /// <param name="AResult">Asynchronous result for firing async callbacks</param>
            /// <param name="ProgressHolder">Holder for a progress state which can be queried by the user</param>
            public uploadParams(StreamContext StreamContext, SendUploadDownloadStatus StatusCallback, FileChange ChangeToTransfer, CancellationTokenSource ShutdownToken, string SyncRootFullPath, AsyncCallback ACallback, IAsyncResult AResult, GenericHolder<TransferProgress> ProgressHolder, FileTransferStatusUpdateDelegate StatusUpdate, Nullable<Guid> StatusUpdateId)
                : base(StatusCallback, ChangeToTransfer, ShutdownToken, SyncRootFullPath, ACallback, AResult, ProgressHolder, StatusUpdate, StatusUpdateId)
            {
                // additional checks for parameters which were not already checked via the abstract base constructor

                if (StreamContext == null)
                {
                    throw new Exception("Stream cannot be null");
                }
                if (base.ChangeToTransfer.Metadata.StorageKey == null)
                {
                    throw new Exception("ChangeToTransfer Metadata StorageKey cannot be null");
                }
                if (base.ChangeToTransfer.Direction != SyncDirection.To)
                {
                    throw new ArgumentException("Invalid ChangeToTransfer Direction for an upload: " + base.ChangeToTransfer.Direction.ToString());
                }

                // hash is used in http header for MD5 validation of content stream
                CLError retrieveHashError = this.ChangeToTransfer.GetMD5LowercaseString(out this._hash);
                if (retrieveHashError != null)
                {
                    throw new AggregateException("Unable to retrieve MD5 from ChangeToTransfer", retrieveHashError.GrabExceptions());
                }
                if (this._hash == null)
                {
                    throw new NullReferenceException("ChangeToTransfer must have an MD5 hash");
                }

                // set the readonly field for the public property by all the parameters which were not passed to the abstract base class

                this._streamContext = StreamContext;
            }
        }

        /// <summary>
        /// Handler called whenever progress has been made uploading or downloading a file or if the file was cancelled or completed
        /// </summary>
        /// <param name="status">The parameters which describe the progress itself</param>
        /// <param name="eventSource">The FileChange describing the change to upload or download</param>
        /// <param name="syncBoxId">The unique id of the sync box on the server</param>
        /// <param name="deviceId">The id of this device</param>
        internal delegate void SendUploadDownloadStatus(CLStatusFileTransferUpdateParameters status, FileChange eventSource, Nullable<long> syncBoxId, string deviceId);

        /// <summary>
        /// Handler called before a download starts with the temporary file id (used as filename for the download in the temp download folder) and passes through UserState
        /// </summary>
        /// <param name="tempId">Unique ID created for the file and used as the file's name in the temp download directory</param>
        /// <param name="UserState">Object passed through from the download method call specific to before download</param>
        public delegate void BeforeDownloadToTempFile(Guid tempId, object UserState);

        /// <summary>
        /// ¡¡ Action required: move the completed download file from the temp directory to the final destination !!
        /// Handler called after a file download completes with the id used as the file name in the originally provided temporary download folder,
        /// passes through UserState, passes the download change itself, gives a constructed full path where the downloaded file can be found in the temp folder,
        /// and references a string which should be set to something useful for communications trace to denote a completed file such as "---Completed file download---" (but only set after the file was succesfully moved)
        /// </summary>
        /// <param name="tempFileFullPath">Full path to where the downloaded file can be found in the temp folder (which needs to be moved)</param>
        /// <param name="downloadChange">The download change itself</param>
        /// <param name="responseBody">Reference to string used to trace communication, should be set to something useful to read in communications trace such as "---Completed file download---" (but only after the file was successfully moved)</param>
        /// <param name="UserState">Object passed through from the download method call specific to after download</param>
        /// <param name="tempId">Unique ID created for the file and used as the file's name in the temp download directory</param>
        public delegate void AfterDownloadToTempFile(string tempFileFullPath, FileChange downloadChange, ref string responseBody, object UserState, Guid tempId);
        #endregion

        #region IsAdministrator
        
        /// <summary>
        /// Determine whether the current process has administrative privileges.
        /// </summary>
        /// <returns>bool: true: Is in the Administrator group.</returns>
        public static bool IsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                if (identity == null)
                {
                    throw new InvalidOperationException("Couldn't get the current user identity");
                }
                var principal = new WindowsPrincipal(identity);

                // Check if this user has the Administrator role. If they do, return immediately.
                // If UAC is on, and the process is not elevated, then this will actually return false.
                if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    _trace.writeToLog(9, "Helpers: IsAdministrator: IsInRole adminstrator.  Return true.");
                    return true;
                }

                // If we're not running in Vista onwards, we don't have to worry about checking for UAC.
                if (Environment.OSVersion.Platform != PlatformID.Win32NT || Environment.OSVersion.Version.Major < 6)
                {
                    // Operating system does not support UAC; skipping elevation check.
                    _trace.writeToLog(9, "Helpers: IsAdministrator: OS does not support UAC.  Return falsee.");
                    return false;
                }

                int tokenInfLength = Marshal.SizeOf(typeof(int));
                IntPtr tokenInformation = Marshal.AllocHGlobal(tokenInfLength);

                try
                {
                    var token = identity.Token;
                    var result = Cloud.Static.NativeMethods.GetTokenInformation(token, Cloud.Static.NativeMethods.TokenInformationClass.TokenElevationType, tokenInformation, tokenInfLength, out tokenInfLength);

                    if (!result)
                    {
                        var exception = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                        throw new InvalidOperationException("Couldn't get token information", exception);
                    }

                    var elevationType = (Cloud.Static.NativeMethods.TokenElevationType)Marshal.ReadInt32(tokenInformation);

                    switch (elevationType)
                    {
                        case Cloud.Static.NativeMethods.TokenElevationType.TokenElevationTypeDefault:
                            // TokenElevationTypeDefault - User is not using a split token, so they cannot elevate.
                            _trace.writeToLog(9, "Helpers: IsAdministrator: User is not using a split token, so they cannot elevate.  Return false.");
                            return false;
                        case Cloud.Static.NativeMethods.TokenElevationType.TokenElevationTypeFull:
                            // TokenElevationTypeFull - User has a split token, and the process is running elevated. Assuming they're an administrator.
                            _trace.writeToLog(9, "Helpers: IsAdministrator: User has a split token, and the process is running elevated. Assuming they're an administrator. Return true.");
                            return true;
                        case Cloud.Static.NativeMethods.TokenElevationType.TokenElevationTypeLimited:
                            // TokenElevationTypeLimited - User has a split token, but the process is not running elevated. Assuming they're an administrator.
                            _trace.writeToLog(9, "Helpers: IsAdministrator: IsInRole User has a split token, but the process is not running elevated. Return false.");
                            return false;
                        default:
                            // Unknown token elevation type.
                            _trace.writeToLog(9, "Helpers: IsAdministrator: Unknown token elevation type.  Return false.");
                            return false;
                    }
                }
                finally
                {
                    if (tokenInformation != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(tokenInformation);
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "Helpers: IsAdministrator: ERROR: Exception: Msg: <{0}>. Return false.", ex.Message);
                return false;
            }
        }
        #endregion
    }
}
