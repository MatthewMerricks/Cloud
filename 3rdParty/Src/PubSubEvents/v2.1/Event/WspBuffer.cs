using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Microsoft.WebSolutionsPlatform.Event
{
    /// <summary>
    /// WspBuffer is a byte array buffer
    /// </summary>
    public class WspBuffer
    {
        private static Dictionary<Type, PropertyType> propTypes = null;
        private static object lockObj = new object();

        private byte[] buffer;

        private int position = 0;
        /// <summary>
        /// Current position in the buffer.
        /// </summary>
        public int Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }

        /// <summary>
        /// Current size of the buffer.
        /// </summary>
        public int Size
        {
            get
            {
                return buffer.Length;
            }
        }

        /// <summary>
        /// Constructor for WspBuffer
        /// </summary>
        public WspBuffer()
        {
            buffer = new byte[51200];
        }

        /// <summary>
        /// Constructor for WspBuffer
        /// </summary>
        public WspBuffer(byte[] bufferIn)
        {
            buffer = (byte[]) bufferIn.Clone();
        }

        /// <summary>
        /// Resets the position of the buffer to zero.
        /// </summary>
        public void Reset()
        {
            position = 0;
        }

        /// <summary>
        /// AppendBytes a byte array to the buffer.
        /// </summary>
        /// <param name="src">The byte array to be appended</param>
        /// <param name="offset">Offset to begin copying bytes from</param>
        /// <param name="length">The number of bytes to append</param>
        private void AppendBytes(byte[] src, int offset, int length)
        {
            if ((position + length) > buffer.Length)
            {
                int newLength = buffer.Length;

                while( newLength < (position + length) )
                {
                    if ((int.MaxValue / 2) > buffer.Length)
                    {
                        newLength = buffer.Length * 2;
                    }
                    else
                    {
                        if (buffer.Length == int.MaxValue)
                        {
                            throw new OutOfMemoryException("WspBuffer has reached maximum size.");
                        }
                        else
                        {
                            newLength = int.MaxValue;
                        }
                    }
                }

                byte[] newBuffer = new byte[newLength];

                Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
                buffer = newBuffer;

                if ((position + length) > buffer.Length)
                {
                    throw new OutOfMemoryException("WspBuffer has reached maximum size.");
                }
            }

            Buffer.BlockCopy(src, offset, buffer, position, length);

            position = position + length;
        }

        /// <summary>
        /// AppendBytes a byte array to the buffer.
        /// </summary>
        /// <param name="src">The byte array to be appended</param>
        private void AppendByte(byte src)
        {
            if ((position + 1) > buffer.Length)
            {
                int newLength;

                if ((int.MaxValue / 2) > buffer.Length)
                {
                    newLength = buffer.Length * 2;
                }
                else
                {
                    if (buffer.Length == int.MaxValue)
                    {
                        throw new OutOfMemoryException("WspBuffer has reached maximum size.");
                    }
                    else
                    {
                        newLength = int.MaxValue;
                    }
                }

                byte[] newBuffer = new byte[newLength];

                Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
                buffer = newBuffer;

                if ((position + 1) > buffer.Length)
                {
                    throw new OutOfMemoryException("WspBuffer has reached maximum size.");
                }
            }

            buffer[position] = src;

            position++;
        }

        /// <summary>
        /// Gets the next byte in the WspBuffer specified by Position
        /// </summary>
        /// <param name="outByte">Byte being read</param>
        /// <returns>True if read successful else False if read failed</returns>
        public bool GetByte(out Byte outByte)
        {
            if (position < buffer.Length)
            {
                outByte = buffer[position];
                position++;
                return true;
            }
            else
            {
                outByte = 0;
                return false;
            }
        }

        /// <summary>
        /// Gets the next byte in the WspBuffer specified by Position
        /// </summary>
        /// <param name="length">Number of bytes to be read</param>
        /// <param name="outArray">Byte array being returned</param>
        /// <returns>True if read successful else False if read failed</returns>
        public bool GetBytes(Int32 length, out Byte[] outArray)
        {
            outArray = new Byte[length];

            if (position + length <= buffer.Length)
            {
                Buffer.BlockCopy(buffer, position, outArray, 0, length);
                position = position + length;
                return true;
            }
            else
            {
                outArray = null;
                return false;
            }
        }
        /// <summary>
        /// Method returns the event's header properties.
        /// </summary>
        /// <param name="originatingRouterName">Machine were event originated from</param>
        /// <param name="inRouterName">Machine which passed the event to this machine</param>
        /// <param name="eventType">Event type</param>
        /// <returns>Number of bytes of the buffer which was the header</returns>
        public bool GetHeader(out string originatingRouterName, out string inRouterName, out Guid eventType)
        {
            bool rc;

            Reset();

            originatingRouterName = string.Empty;
            inRouterName = string.Empty;
            eventType = Guid.Empty;

            rc = Read(out originatingRouterName);

            if (rc == false)
            {
                return false;
            }

            rc = Read(out inRouterName);

            if (rc == false)
            {
                return false;
            }

            rc = Read(out eventType);

            if (rc == false)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a byte array copy of the buffer for the size of the byte array
        /// </summary>
        /// <returns>Byte array copy of buffer</returns>
        public byte[] ToByteArray()
        {
            byte[] bufferOut = new byte[position];

            Buffer.BlockCopy(buffer, 0, bufferOut, 0, position);

            return bufferOut;
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(bool value)
        {
            AppendByte(BitConverter.GetBytes(value)[0]);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(byte value)
        {
            AppendByte(value);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(byte[] value)
        {
            byte outByte;
            Int32 arrayLength = value.Length;

            while (arrayLength >= 0x80)
            {
                outByte = (byte)(arrayLength | 0x80);

                Write(outByte);

                arrayLength >>= 7;
            }

            outByte = (byte)arrayLength;

            Write(outByte);

            AppendBytes(value, 0, value.Length);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(Char value)
        {
            Char[] array = new Char[1];
            byte[] encodedArray;

            array[0] = value;

            encodedArray = Encoding.UTF8.GetBytes(array);

            Write((byte)encodedArray.Length);

            AppendBytes(encodedArray, 0, encodedArray.Length);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(Char[] value)
        {
            byte outByte;
            byte[] encodedArray = Encoding.UTF8.GetBytes(value);
            Int32 arrayLength = encodedArray.Length;

            while (arrayLength >= 0x80)
            {
                outByte = (byte)(arrayLength | 0x80);

                Write(outByte);

                arrayLength >>= 7;
            }

            outByte = (byte)arrayLength;

            Write(outByte);

            AppendBytes(encodedArray, 0, encodedArray.Length);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(Decimal value)
        {
            Int32[] intArray = Decimal.GetBits(value);

            AppendBytes(BitConverter.GetBytes(intArray[0]), 0, 4);
            AppendBytes(BitConverter.GetBytes(intArray[1]), 0, 4);
            AppendBytes(BitConverter.GetBytes(intArray[2]), 0, 4);
            AppendBytes(BitConverter.GetBytes(intArray[3]), 0, 4);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(Double value)
        {
            AppendBytes(BitConverter.GetBytes(value), 0, 8);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(Guid value)
        {
            Write(value.ToString());
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(IPAddress value)
        {
            Write(value.ToString());
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(Uri value)
        {
            Write(value.ToString());
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(Int16 value)
        {
            AppendBytes(BitConverter.GetBytes(value), 0, 2);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(Int32 value)
        {
            AppendBytes(BitConverter.GetBytes(value), 0, 4);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(Int64 value)
        {
            AppendBytes(BitConverter.GetBytes(value), 0, 8);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(Single value)
        {
            AppendBytes(BitConverter.GetBytes(value), 0, 4);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        [CLSCompliantAttribute(false)]
        public void Write(SByte value)
        {
            AppendByte((byte) value);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(DateTime value)
        {
            Write(value.Ticks);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(Version value)
        {
            Write(value.ToString());
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(String value)
        {
            byte outByte;
            byte[] encodedArray;

            encodedArray = Encoding.UTF8.GetBytes(value);

            Int32 stringLength = encodedArray.Length;

            while (stringLength >= 0x80)
            {
                outByte = (byte)(stringLength | 0x80);

                Write(outByte);

                stringLength >>= 7;
            }

            outByte = (byte)stringLength;

            Write(outByte);

            AppendBytes(encodedArray, 0, encodedArray.Length);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(object value)
        {
            PropertyType propType;
            Type valueType = value.GetType();

            if (propTypes == null)
            {
                LoadPropTypes();
            }

            if (propTypes.TryGetValue(valueType, out propType) == true)
            {
                Write((byte)propType);

                switch (propType)
                {
                    case PropertyType.String:
                        Write((string)value);
                        break;

                    case PropertyType.Boolean:
                        Write((bool)value);
                        break;

                    case PropertyType.Int32:
                        Write((Int32)value);
                        break;

                    case PropertyType.Int64:
                        Write((Int64)value);
                        break;

                    case PropertyType.Double:
                        Write((double)value);
                        break;

                    case PropertyType.Decimal:
                        Write((decimal)value);
                        break;

                    case PropertyType.Byte:
                        Write((byte)value);
                        break;

                    case PropertyType.Char:
                        Write((char)value);
                        break;

                    case PropertyType.Version:
                        Write((Version)value);
                        break;

                    case PropertyType.DateTime:
                        Write((DateTime)value);
                        break;

                    case PropertyType.Guid:
                        Write((Guid)value);
                        break;

                    case PropertyType.Uri:
                        Write((Uri)value);
                        break;

                    case PropertyType.Int16:
                        Write((Int16)value);
                        break;

                    case PropertyType.Single:
                        Write((Single)value);
                        break;

                    case PropertyType.UInt16:
                        Write((UInt16)value);
                        break;

                    case PropertyType.UInt32:
                        Write((UInt32)value);
                        break;

                    case PropertyType.UInt64:
                        Write((UInt64)value);
                        break;

                    case PropertyType.IPAddress:
                        Write((IPAddress)value);
                        break;

                    case PropertyType.ByteArray:
                        Write((byte[])value);
                        break;

                    case PropertyType.CharArray:
                        Write((char[])value);
                        break;

                    case PropertyType.StringDictionary:
                        Write((Dictionary<string, string>)value);
                        break;

                    case PropertyType.ObjectDictionary:
                        Write((Dictionary<string, object>)value);
                        break;

                    case PropertyType.StringList:
                        Write((List<string>)value);
                        break;

                    case PropertyType.ObjectList:
                        Write((List<object>)value);
                        break;
                }
            }
        }

        /// <summary>
        /// Load property types into dictionary
        /// </summary>
        private void LoadPropTypes()
        {
            bool boolType = true;
            byte byteType = 0;
            byte[] byteArrayType = new byte[1];
            char charType = ' ';
            char[] charArrayType = new char[1];
            decimal decimalType = 0;
            double doubleType = 0;
            Int16 int16Type = 0;
            Int32 int32Type = 0;
            Int64 int64Type = 0;
            SByte sByteType = 0;
            Single singleType = 0;
            string stringType = string.Empty;
            UInt16 uInt16Type = 0;
            UInt32 uInt32Type = 0;
            UInt64 uInt64Type = 0;
            Version versionType = new Version();
            DateTime dateTimeType = DateTime.Now;
            Guid guidType = Guid.Empty;
            Uri uriType = new Uri("http://localhost");
            IPAddress ipAddressType = new IPAddress(0);
            Dictionary<string, string> stringDictionaryType = new Dictionary<string,string>();
            Dictionary<string, object> objectDictionaryType = new Dictionary<string,object>();
            List<string> stringListType = new List<string>();
            List<object> objectListType = new List<object>();

           lock(lockObj)
            {
                if (propTypes == null)
                {
                    propTypes = new Dictionary<Type, PropertyType>();

                    propTypes[boolType.GetType()] = PropertyType.Boolean;
                    propTypes[byteType.GetType()] = PropertyType.Byte;
                    propTypes[byteArrayType.GetType()] = PropertyType.ByteArray;
                    propTypes[charType.GetType()] = PropertyType.Char;
                    propTypes[charArrayType.GetType()] = PropertyType.CharArray;
                    propTypes[decimalType.GetType()] = PropertyType.Decimal;
                    propTypes[doubleType.GetType()] = PropertyType.Double;
                    propTypes[int16Type.GetType()] = PropertyType.Int16;
                    propTypes[int32Type.GetType()] = PropertyType.Int32;
                    propTypes[int64Type.GetType()] = PropertyType.Int64;
                    propTypes[sByteType.GetType()] = PropertyType.SByte;
                    propTypes[singleType.GetType()] = PropertyType.Single;
                    propTypes[stringType.GetType()] = PropertyType.String;
                    propTypes[uInt16Type.GetType()] = PropertyType.UInt16;
                    propTypes[uInt32Type.GetType()] = PropertyType.UInt32;
                    propTypes[uInt64Type.GetType()] = PropertyType.UInt64;
                    propTypes[versionType.GetType()] = PropertyType.Version;
                    propTypes[dateTimeType.GetType()] = PropertyType.DateTime;
                    propTypes[guidType.GetType()] = PropertyType.Guid;
                    propTypes[uriType.GetType()] = PropertyType.Uri;
                    propTypes[ipAddressType.GetType()] = PropertyType.IPAddress;
                    propTypes[stringDictionaryType.GetType()] = PropertyType.StringDictionary;
                    propTypes[objectDictionaryType.GetType()] = PropertyType.ObjectDictionary;
                    propTypes[stringListType.GetType()] = PropertyType.StringList;
                    propTypes[objectListType.GetType()] = PropertyType.ObjectList;
                }
            }
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        [CLSCompliantAttribute(false)]
        public void Write(UInt16 value)
        {
            AppendBytes(BitConverter.GetBytes(value), 0, 2);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        [CLSCompliantAttribute(false)]
        public void Write(UInt32 value)
        {
            AppendBytes(BitConverter.GetBytes(value), 0, 4);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        [CLSCompliantAttribute(false)]
        public void Write(UInt64 value)
        {
            AppendBytes(BitConverter.GetBytes(value), 0, 8);
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(Dictionary<string, string> value)
        {
            Write(value.Count);

            foreach (string key in value.Keys)
            {
                Write(key);
                Write(value[key]);
            }
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(Dictionary<string, object> value)
        {
            Write(value.Count);

            foreach (string key in value.Keys)
            {
                Write(key);
                Write(value[key]);
            }
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(List<string> value)
        {
            Write(value.Count);

            for (int i = 0; i < value.Count; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>
        /// Write buffer to the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be written</param>
        public void Write(List<object> value)
        {
            Write(value.Count);

            for (int i = 0; i < value.Count; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out bool value)
        {
            byte outByte;
            bool rc;

            rc = GetByte(out outByte);

            value = Convert.ToBoolean(outByte);

            return rc;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out byte value)
        {
            return GetByte(out value);
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out byte[] value)
        {
            byte byteOut;
            Int32 arrayLength = 0;
            Int32 shiftBits = 0;

            do
            {
                if(GetByte(out byteOut) == false)
                {
                    value = null;
                    return false;
                }

                arrayLength |= (byteOut & 0x7f) << shiftBits;

                shiftBits += 7;
            } while ((byteOut & 0x80) != 0);

            if (arrayLength == 0)
            {
                value = new byte[0];
                return true;
            }

            if (GetBytes(arrayLength, out value) == false)
            {
                value = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out Char value)
        {
            byte byteOut;
            byte[] arrayOut;
            Char[] decodedArray;

            if (GetByte(out byteOut) == false || byteOut == 0)
            {
                value = Char.MinValue;
                return false;
            }

            if (GetBytes((Int32)byteOut, out arrayOut) == false)
            {
                value = Char.MinValue;
                return false;
            }

            decodedArray = Encoding.UTF8.GetChars(arrayOut);

            value = decodedArray[0];

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out Char[] value)
        {
            byte byteOut;
            byte[] arrayOut;
            Int32 arrayLength = 0;
            Int32 shiftBits = 0;

            do
            {
                if (GetByte(out byteOut) == false)
                {
                    value = null;
                    return false;
                }

                arrayLength |= (byteOut & 0x7f) << shiftBits;

                shiftBits += 7;
            } while ((byteOut & 0x80) != 0);

            if (arrayLength == 0)
            {
                value = new Char[0];
                return true;
            }

            if (GetBytes(arrayLength, out arrayOut) == false)
            {
                value = null;
                return false;
            }

            value = Encoding.UTF8.GetChars(arrayOut);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out Decimal value)
        {
            Int32[] intArray = new Int32[4];

            if (Read(out intArray[0]) == false)
            {
                value = Decimal.MinValue;
                return false;
            }

            if (Read(out intArray[1]) == false)
            {
                value = Decimal.MinValue;
                return false;
            }

            if (Read(out intArray[2]) == false)
            {
                value = Decimal.MinValue;
                return false;
            }

            if (Read(out intArray[3]) == false)
            {
                value = Decimal.MinValue;
                return false;
            }

            value = new Decimal(intArray);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out Guid value)
        {
            string guidOut;

            if (Read(out guidOut) == false)
            {
                value = Guid.Empty;
                return false;
            }

            value = new Guid(guidOut);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out DateTime value)
        {
            Int64 dateTimeOut;

            if (Read(out dateTimeOut) == false)
            {
                value = DateTime.MinValue;
                return false;
            }

            value = new DateTime(dateTimeOut);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out IPAddress value)
        {
            string ipOut;

            if (Read(out ipOut) == false)
            {
                value = IPAddress.None;
                return false;
            }

            value = IPAddress.Parse(ipOut);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out Uri value)
        {
            string uriOut;

            if (Read(out uriOut) == false)
            {
                value = null;
                return false;
            }

            value = new Uri(uriOut);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out Version value)
        {
            string versionOut;

            if (Read(out versionOut) == false)
            {
                value = new Version();
                return false;
            }

            value = new Version(versionOut);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out Double value)
        {
            byte[] arrayOut;

            if (GetBytes(8, out arrayOut) == false)
            {
                value = Double.MinValue;
                return false;
            }
            value = BitConverter.ToDouble(arrayOut, 0);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out Int16 value)
        {
            byte[] arrayOut;

            if (GetBytes(2, out arrayOut) == false)
            {
                value = Int16.MinValue;
                return false;
            }

            value = BitConverter.ToInt16(arrayOut, 0);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out Int32 value)
        {
            byte[] arrayOut;

            if (GetBytes(4, out arrayOut) == false)
            {
                value = Int32.MinValue;
                return false;
            }

            value = BitConverter.ToInt32(arrayOut, 0);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out Int64 value)
        {
            byte[] arrayOut;

            if (GetBytes(8, out arrayOut) == false)
            {
                value = Int64.MinValue;
                return false;
            }

            value = BitConverter.ToInt64(arrayOut, 0);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        [CLSCompliantAttribute(false)]
        public bool Read(out SByte value)
        {
            bool rc;
            byte byteOut = 0;

            rc = GetByte(out byteOut);

            value = (SByte) byteOut;

            return rc;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out Single value)
        {
            byte[] arrayOut;

            if (GetBytes(4, out arrayOut) == false)
            {
                value = Single.MinValue;
                return false;
            }

            value = BitConverter.ToSingle(arrayOut, 0);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out Dictionary<string, string> value)
        {
            int dictCount;
            string stringKey;
            string stringValue;

            if (Read(out dictCount) == false)
            {
                value = new Dictionary<string, string>();
                return false;
            }

            value = new Dictionary<string, string>(dictCount);

            for (int i = 0; i < dictCount; i++)
            {
                if (Read(out stringKey) == false)
                {
                    return false;
                }

                if (Read(out stringValue) == false)
                {
                    return false;
                }

                value[stringKey] = stringValue;
            }

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out Dictionary<string, object> value)
        {
            int dictCount;
            byte propType;
            object objvalue;
            string stringKey;

            if (Read(out dictCount) == false)
            {
                value = new Dictionary<string, object>();
                return false;
            }

            value = new Dictionary<string, object>(dictCount);

            for (int i = 0; i < dictCount; i++)
            {
                if (Read(out stringKey) == false)
                {
                    return false;
                }

                if (Read(out propType) == false)
                {
                    return false;
                }

                if (Read((PropertyType)propType, out objvalue) == false)
                {
                    return false;
                }

                value[stringKey] = objvalue;
            }

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out List<string> value)
        {
            int listCount;
            string stringValue;

            if (Read(out listCount) == false)
            {
                value = new List<string>();
                return false;
            }

            value = new List<string>(listCount);

            for (int i = 0; i < listCount; i++)
            {
                if (Read(out stringValue) == false)
                {
                    return false;
                }

                value.Add(stringValue);
            }

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out List<object> value)
        {
            int listCount;
            byte propType;
            object objvalue;

            if (Read(out listCount) == false)
            {
                value = new List<object>();
                return false;
            }

            value = new List<object>(listCount);

            for (int i = 0; i < listCount; i++)
            {

                if (Read(out propType) == false)
                {
                    return false;
                }

                if (Read((PropertyType)propType, out objvalue) == false)
                {
                    return false;
                }

                value.Add(objvalue);
            }

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        public bool Read(out String value)
        {
            byte byteOut;
            byte[] arrayOut;
            Int32 arrayLength = 0;
            Int32 shiftBits = 0;

            do
            {
                if (GetByte(out byteOut) == false)
                {
                    value = null;
                    return false;
                }

                arrayLength |= (byteOut & 0x7f) << shiftBits;

                shiftBits += 7;
            } while ((byteOut & 0x80) != 0);

            if (arrayLength == 0)
            {
                value = string.Empty;
                return true;
            }

            if (GetBytes(arrayLength, out arrayOut) == false)
            {
                value = null;
                return false;
            }

            value = Encoding.UTF8.GetString(arrayOut);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        [CLSCompliantAttribute(false)]
        public bool Read(out UInt16 value)
        {
            byte[] arrayOut;

            if (GetBytes(2, out arrayOut) == false)
            {
                value = UInt16.MinValue;
                return false;
            }

            value = BitConverter.ToUInt16(arrayOut, 0);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        [CLSCompliantAttribute(false)]
        public bool Read(out UInt32 value)
        {
            byte[] arrayOut;

            if (GetBytes(4, out arrayOut) == false)
            {
                value = UInt32.MinValue;
                return false;
            }

            value = BitConverter.ToUInt32(arrayOut, 0);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="value">Value object to be read</param>
        [CLSCompliantAttribute(false)]
        public bool Read(out UInt64 value)
        {
            byte[] arrayOut;

            if (GetBytes(8, out arrayOut) == false)
            {
                value = UInt64.MinValue;
                return false;
            }

            value = BitConverter.ToUInt64(arrayOut, 0);

            return true;
        }

        /// <summary>
        /// Read buffer from the WspBuffer
        /// </summary>
        /// <param name="propType">Type of property to read</param>
        /// <param name="value">Value object to be read</param>
        public bool Read(PropertyType propType, out object value)
        {
            bool rc;

            string stringValue = string.Empty;
            byte byteValue = 0;
            byte[] byteArrayValue = null;
            char charValue = Char.MinValue;
            char[] charArrayValue = null;
            bool boolValue = false;
            Int16 int16Value = 0;
            Int32 int32Value = 0;
            Int64 int64Value = 0;
            UInt16 uint16Value = 0;
            UInt32 uint32Value = 0;
            UInt64 uint64Value = 0;
            SByte sByteValue = 0;
            Single singleValue = 0;
            Double doubleValue = 0;
            Decimal decimalValue = 0;
            Version versionValue = null;
            DateTime dateTimeValue = DateTime.MinValue;
            Guid guidValue = Guid.Empty;
            IPAddress ipAddressValue = null;
            Uri uriValue = null;
            Dictionary<string, string> stringDictionaryValue = null;
            Dictionary<string, object> objectDictionaryValue = null;
            List<string> stringListValue = null;
            List<object> objectListValue = null;

            value = null;

            switch (propType)
            {
                case PropertyType.String:
                    rc = Read(out stringValue);
                    value = stringValue;
                    break;

                case PropertyType.Boolean:
                    rc = Read(out boolValue);
                    value = boolValue;
                    break;

                case PropertyType.Int32:
                    rc = Read(out int32Value);
                    value = int32Value;
                    break;

                case PropertyType.Int64:
                    rc = Read(out int64Value);
                    value = int64Value;
                    break;

                case PropertyType.Double:
                    rc = Read(out doubleValue);
                    value = doubleValue;
                    break;

                case PropertyType.Decimal:
                    rc = Read(out decimalValue);
                    value = decimalValue;
                    break;

                case PropertyType.Byte:
                    rc = Read(out byteValue);
                    value = byteValue;
                    break;

                case PropertyType.Char:
                    rc = Read(out charValue);
                    value = charValue;
                    break;

                case PropertyType.Version:
                    rc = Read(out versionValue);
                    value = versionValue;
                    break;

                case PropertyType.DateTime:
                    rc = Read(out dateTimeValue);
                    value = dateTimeValue;
                    break;

                case PropertyType.Guid:
                    rc = Read(out guidValue);
                    value = guidValue;
                    break;

                case PropertyType.Uri:
                    rc = Read(out uriValue);
                    value = uriValue;
                    break;

                case PropertyType.Int16:
                    rc = Read(out int16Value);
                    value = int16Value;
                    break;

                case PropertyType.SByte:
                    rc = Read(out sByteValue);
                    value = sByteValue;
                    break;

                case PropertyType.Single:
                    rc = Read(out singleValue);
                    value = singleValue;
                    break;

                case PropertyType.UInt16:
                    rc = Read(out uint16Value);
                    value = uint16Value;
                    break;

                case PropertyType.UInt32:
                    rc = Read(out uint32Value);
                    value = uint32Value;
                    break;

                case PropertyType.UInt64:
                    rc = Read(out uint64Value);
                    value = uint64Value;
                    break;

                case PropertyType.IPAddress:
                    rc = Read(out ipAddressValue);
                    value = ipAddressValue;
                    break;

                case PropertyType.ByteArray:
                    rc = Read(out byteArrayValue);
                    value = byteArrayValue;
                    break;

                case PropertyType.CharArray:
                    rc = Read(out charArrayValue);
                    value = charArrayValue;
                    break;

                case PropertyType.StringDictionary:
                    rc = Read(out stringDictionaryValue);
                    value = stringDictionaryValue;
                    break;

                case PropertyType.ObjectDictionary:
                    rc = Read(out objectDictionaryValue);
                    value = objectDictionaryValue;
                    break;

                case PropertyType.StringList:
                    rc = Read(out stringListValue);
                    value = stringListValue;
                    break;

                case PropertyType.ObjectList:
                    rc = Read(out objectListValue);
                    value = objectListValue;
                    break;

                default:
                    rc = false;
                    break;
            }

            if(rc == false)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(String key, string value)
        {
            Write(key);

            Write((byte)PropertyType.String);

            if (value == null)
            {
                Write(string.Empty);
            }
            else
            {
                Write(value);
            }
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, bool value)
        {
            Write(key);

            Write((byte)PropertyType.Boolean);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, Int16 value)
        {
            Write(key);

            Write((byte)PropertyType.Int16);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, Int32 value)
        {
            Write(key);

            Write((byte)PropertyType.Int32);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, Int64 value)
        {
            Write(key);

            Write((byte)PropertyType.Int64);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        [CLSCompliantAttribute(false)]
        public void AddElement(string key, UInt16 value)
        {
            Write(key);

            Write((byte)PropertyType.UInt16);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        [CLSCompliantAttribute(false)]
        public void AddElement(string key, UInt32 value)
        {
            Write(key);

            Write((byte)PropertyType.UInt32);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        [CLSCompliantAttribute(false)]
        public void AddElement(string key, UInt64 value)
        {
            Write(key);

            Write((byte)PropertyType.UInt64);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, Double value)
        {
            Write(key);

            Write((byte)PropertyType.Double);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, Decimal value)
        {
            Write(key);

            Write((byte)PropertyType.Decimal);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, Byte value)
        {
            Write(key);

            Write((byte)PropertyType.Byte);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, Char value)
        {
            Write(key);

            Write((byte)PropertyType.Char);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, Single value)
        {
            Write(key);

            Write((byte)PropertyType.Single);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        [CLSCompliantAttribute(false)]
        public void AddElement(string key, SByte value)
        {
            Write(key);

            Write((byte)PropertyType.SByte);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, Version value)
        {
            Write(key);

            Write((byte)PropertyType.Version);

            if (value == null)
            {
                Write(string.Empty);
            }
            else
            {
                Write(value);
            }
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, DateTime value)
        {
            Write(key);

            Write((byte)PropertyType.DateTime);

            if (value == null)
            {
                Write(((long)0).ToString());
            }
            else
            {
                Write(value);
            }
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, Guid value)
        {
            Write(key);

            Write((byte)PropertyType.Guid);

            if (value == null)
            {
                Write(Guid.Empty);
            }
            else
            {
                Write(value);
            }
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, Uri value)
        {
            Write(key);

            Write((byte)PropertyType.Uri);

            if (value == null)
            {
                Write(new Uri(@"http://EmptyUri"));
            }
            else
            {
                Write(value);
            }
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, IPAddress value)
        {
            Write(key);

            Write((byte)PropertyType.IPAddress);

            if (value == null)
            {
                Write(@"0.0.0.0");
            }
            else
            {
                Write((string)value.ToString());
            }
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, Byte[] value)
        {
            Write(key);

            Write((byte)PropertyType.ByteArray);

            if (value == null)
            {
                Byte[] emptyByteArray = new byte[] { };
                Write(emptyByteArray);
            }
            else
            {
                Write(value);
            }
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, Char[] value)
        {
            Write(key);

            Write((byte)PropertyType.CharArray);

            if (value == null)
            {
                Char[] emptyArray = new Char[] { };
                Write(emptyArray);
            }
            else
            {
                Write(value);
            }
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, Dictionary<string, string> value)
        {
            Write(key);

            Write((byte)PropertyType.StringDictionary);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, Dictionary<string, object> value)
        {
            Write(key);

            Write((byte)PropertyType.ObjectDictionary);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, List<string> value)
        {
            Write(key);

            Write((byte)PropertyType.StringList);

            Write(value);
        }

        /// <summary>
        /// Method to add element buffer to the SerializationData object.
        /// </summary>
        /// <param name="key">Key of the element being added</param>
        /// <param name="value">Value of the element being added</param>
        public void AddElement(string key, List<object> value)
        {
            Write(key);

            Write((byte)PropertyType.ObjectList);

            Write(value);
        }
    }
}
