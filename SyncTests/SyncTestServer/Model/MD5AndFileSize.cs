using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncTestServer.Model
{
    public struct MD5AndFileSize
    {
        public byte[] MD5
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid MD5AndFileSize");
                }
                return _md5;
            }
        }
        private byte[] _md5;

        public long FileSize
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid MD5AndFileSize");
                }
                return _fileSize;
            }
        }
        private long _fileSize;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public MD5AndFileSize(byte[] MD5, long FileSize)
        {
            if (MD5 == null
                || MD5.Length != 16)
            {
                throw new ArgumentException("MD5 must be a 16-length byte array");
            }
            if (FileSize < 0)
            {
                throw new ArgumentException("FileSize cannot be negative");
            }

            this._md5 = MD5;
            this._fileSize = FileSize;
            this._isValid = true;
            this._toString = null;
            this.ToStringLocker = new object();
        }

        public static IEqualityComparer<MD5AndFileSize> Comparer
        {
            get
            {
                return MD5AndFileSizeComparer.Instance;
            }
        }

        public override string ToString()
        {
            if (!_isValid)
            {
                throw new ArgumentException("Cannot retrieve property values on an invalid MD5AndFileSize");
            }

            lock (ToStringLocker)
            {
                return _toString
                    ?? (_toString = (this._isValid
                            ? this._md5
                                    .Select(md5Byte => string.Format("{0:x2}", md5Byte))
                                    .Aggregate((previousBytes, newByte) => previousBytes + newByte) +
                                _fileSize.ToString()
                            : string.Empty));
            }
        }
        private string _toString;
        private readonly object ToStringLocker;

        private sealed class MD5AndFileSizeComparer : EqualityComparer<MD5AndFileSize>
        {
            /// <summary>
            /// Overridden Equals for comparing MD5AndFileSizes by deep compare
            /// </summary>
            /// <param name="x">First MD5AndFileSize to compare</param>
            /// <param name="y">Second MD5AndFileSize to compare</param>
            /// <returns>Returns true for equality, otherwise false</returns>
            public override bool Equals(MD5AndFileSize x, MD5AndFileSize y)
            {
                return (!x._isValid && !y._isValid)
                    || (x._isValid && y._isValid
                        && x._fileSize == y._fileSize
                        && SyncTestServer.Static.NativeMethods.memcmp(x._md5, y._md5, new UIntPtr((uint)16)) == 0);
            }
            /// <summary>
            /// Overridden GetHashCode that gets a hash from the underlying MD5AndFileSize string,
            /// could be improved for efficiency
            /// </summary>
            /// <param name="obj">MD5AndFileSize to hash</param>
            /// <returns>Returns hashcode of underlying MD5AndFileSize string</returns>
            public override int GetHashCode(MD5AndFileSize obj)
            {
                return obj.ToString().GetHashCode();
            }
            /// <summary>
            /// Public static instance to be used everywhere the MD5AndFileSizeComparer is needed
            /// </summary>
            public static MD5AndFileSizeComparer Instance
            {
                get
                {
                    lock (InstanceLocker)
                    {
                        if (_instance == null)
                        {
                            _instance = new MD5AndFileSizeComparer();
                        }
                        return _instance;
                    }
                }
            }
            private static MD5AndFileSizeComparer _instance = null;
            private static object InstanceLocker = new object();
            /// <summary>
            /// Private constructor to ensure other classes only use the public static Instance
            /// </summary>
            private MD5AndFileSizeComparer() { }
        }
    }
}