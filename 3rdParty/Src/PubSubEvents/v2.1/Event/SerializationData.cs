using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Resources;
using System.Reflection;

namespace Microsoft.WebSolutionsPlatform.Event
{
    /// <summary>
    /// Defines the property types supported by the base event class.
    /// </summary>
    public enum PropertyType : byte
    {
        /// <summary>
        /// This property type will be ignored.
        /// </summary>
        None = 0,
        /// <summary>
        /// Boolean type
        /// </summary>
        Boolean = 1,
        /// <summary>
        /// byte type
        /// </summary>
        Byte = 2,
        /// <summary>
        /// byte[] type
        /// </summary>
        ByteArray = 3,
        /// <summary>
        /// char type
        /// </summary>
        Char = 4,
        /// <summary>
        /// char[] type
        /// </summary>
        CharArray = 5,
        /// <summary>
        /// Decimal type
        /// </summary>
        Decimal = 6,
        /// <summary>
        /// Double type
        /// </summary>
        Double = 7,
        /// <summary>
        /// Int16 type
        /// </summary>
        Int16 = 8,
        /// <summary>
        /// Int32 type
        /// </summary>
        Int32 = 9,
        /// <summary>
        /// Int64 type
        /// </summary>
        Int64 = 10,
        /// <summary>
        /// SByte type
        /// </summary>
        SByte = 11,
        /// <summary>
        /// Single type
        /// </summary>
        Single = 12,
        /// <summary>
        /// String type
        /// </summary>
        String = 13,
        /// <summary>
        /// UInt16 type
        /// </summary>
        UInt16 = 14,
        /// <summary>
        /// UInt32 type
        /// </summary>
        UInt32 = 15,
        /// <summary>
        /// UInt64 type
        /// </summary>
        UInt64 = 16,
        /// <summary>
        /// Version type
        /// </summary>
        Version = 17,
        /// <summary>
        /// DateTime type
        /// </summary>
        DateTime = 18,
        /// <summary>
        /// Guid type
        /// </summary>
        Guid = 19,
        /// <summary>
        /// Uri type
        /// </summary>
        Uri = 20,
        /// <summary>
        /// IPAddress type
        /// </summary>
        IPAddress = 21,
        /// <summary>
        /// Generic StringDictionary type
        /// </summary>
        StringDictionary = 22,
        /// <summary>
        /// Generic KeyValue List as ObjectDictionary
        /// </summary>
        ObjectDictionary = 23,
        /// <summary>
        /// Generic list of strings
        /// </summary>
        StringList = 24,
        /// <summary>
        /// Generic list of objects
        /// </summary>
        ObjectList = 25
    }
}
