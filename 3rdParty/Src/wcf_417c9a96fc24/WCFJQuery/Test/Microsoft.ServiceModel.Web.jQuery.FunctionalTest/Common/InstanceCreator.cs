namespace Microsoft.Silverlight.Cdf.Test.Common.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Settings used by the <see cref="InstanceCreator"/> class.
    /// </summary>
    public static class CreatorSettings
    {
        static CreatorSettings()
        {
            MaxArrayLength = 10;
            MaxListLength = 10;
            MaxStringLength = 100;
            CreateOnlyAsciiChars = false;
            DontCreateSurrogateChars = false;
            CreateDateTimeWithSubMilliseconds = true;
            NullValueProbability = 0.01;
            AvoidStackOverflowDueToTypeCycles = false;
            CreatorSurrogate = null;
        }

        /// <summary>
        /// Gets or sets the maximum length of arrays created by the <see cref="InstanceCreator"/>.
        /// </summary>
        public static int MaxArrayLength { get; set; }

        /// <summary>
        /// Gets or sets the maximum length of lists created by the <see cref="InstanceCreator"/>.
        /// </summary>
        public static int MaxListLength { get; set; }

        /// <summary>
        /// Gets or sets the maximum length of strings created by the <see cref="InstanceCreator"/>.
        /// </summary>
        public static int MaxStringLength { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether only ascii chars should be used when creating strings.
        /// </summary>
        public static bool CreateOnlyAsciiChars { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether chars in the surrogate range can be returned by the
        /// <see cref="InstanceCreator"/> when creating char instances.
        /// </summary>
        public static bool DontCreateSurrogateChars { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether <see cref="DateTime"/> values created by the
        /// <see cref="InstanceCreator"/> can have submillisecond precision.
        /// </summary>
        public static bool CreateDateTimeWithSubMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets a value (0-1) indicating the probability of the <see cref="InstanceCreator"/>
        /// returning a <code>null</code> value when creating instances of class types.
        /// </summary>
        public static double NullValueProbability { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether the protection against stack overflow
        /// for cyclic types is enabled. If this flag is set, whenever a type which has already
        /// been created up in the stack is created again, the <see cref="InstanceCreator"/>
        /// will return the default value for that type.
        /// </summary>
        public static bool AvoidStackOverflowDueToTypeCycles { get; set; }

        /// <summary>
        /// Gets or sets the instance of an <see cref="InstanceCreatorSurrogate"/> which can intercept
        /// requests to create instances on the <see cref="InstanceCreator"/>.
        /// </summary>
        public static InstanceCreatorSurrogate CreatorSurrogate { get; set; }
    }

    /// <summary>
    /// Utility class used to create test instances of primitive types.
    /// </summary>
    public static class PrimitiveCreator
    {
        static readonly Regex RelativeIPv6UriRegex = new Regex(@"^\/\/(.+\@)?\[\:\:\d\]");
        static Dictionary<Type, MethodInfo> creators;

        static PrimitiveCreator()
        {
            Type primitiveCreatorType = typeof(PrimitiveCreator);
            creators = new Dictionary<Type, MethodInfo>();
            creators.Add(typeof(bool), primitiveCreatorType.GetMethod("CreateInstanceOfBoolean", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(byte), primitiveCreatorType.GetMethod("CreateInstanceOfByte", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(char), primitiveCreatorType.GetMethod("CreateInstanceOfChar", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(DateTime), primitiveCreatorType.GetMethod("CreateInstanceOfDateTime", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(DateTimeOffset), primitiveCreatorType.GetMethod("CreateInstanceOfDateTimeOffset", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(decimal), primitiveCreatorType.GetMethod("CreateInstanceOfDecimal", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(double), primitiveCreatorType.GetMethod("CreateInstanceOfDouble", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(Guid), primitiveCreatorType.GetMethod("CreateInstanceOfGuid", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(short), primitiveCreatorType.GetMethod("CreateInstanceOfInt16", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(int), primitiveCreatorType.GetMethod("CreateInstanceOfInt32", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(long), primitiveCreatorType.GetMethod("CreateInstanceOfInt64", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(object), primitiveCreatorType.GetMethod("CreateInstanceOfObject", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(sbyte), primitiveCreatorType.GetMethod("CreateInstanceOfSByte", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(float), primitiveCreatorType.GetMethod("CreateInstanceOfSingle", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(string), primitiveCreatorType.GetMethod("CreateInstanceOfString", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Random) }, null));
            creators.Add(typeof(ushort), primitiveCreatorType.GetMethod("CreateInstanceOfUInt16", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(uint), primitiveCreatorType.GetMethod("CreateInstanceOfUInt32", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(ulong), primitiveCreatorType.GetMethod("CreateInstanceOfUInt64", BindingFlags.Public | BindingFlags.Static));
            creators.Add(typeof(Uri), primitiveCreatorType.GetMethod("CreateInstanceOfUri", BindingFlags.Public | BindingFlags.Static));
        }

        /// <summary>
        /// Creates an instance of the <see cref="Boolean"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="Boolean"/> type.</returns>
        public static bool CreateInstanceOfBoolean(Random rndGen)
        {
            return rndGen.Next(2) == 0;
        }

        /// <summary>
        /// Creates an instance of the <see cref="Byte"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="Byte"/> type.</returns>
        public static byte CreateInstanceOfByte(Random rndGen)
        {
            byte[] rndValue = new byte[1];
            rndGen.NextBytes(rndValue);
            return rndValue[0];
        }

        /// <summary>
        /// Creates an instance of the <see cref="Char"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="Char"/> type.</returns>
        public static char CreateInstanceOfChar(Random rndGen)
        {
            if (CreatorSettings.CreateOnlyAsciiChars)
            {
                return (char)rndGen.Next(0x20, 0x7F);
            }
            else if (CreatorSettings.DontCreateSurrogateChars)
            {
                char c;
                do
                {
                    c = (char)rndGen.Next((int)Char.MinValue, (int)Char.MaxValue);
                }
                while (Char.IsSurrogate(c));
                return c;
            }
            else
            {
                return (char)rndGen.Next((int)Char.MinValue, (int)Char.MaxValue + 1);
            }
        }

        /// <summary>
        /// Creates an instance of the <see cref="DateTime"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="DateTime"/> type.</returns>
        public static System.DateTime CreateInstanceOfDateTime(Random rndGen)
        {
            long temp = CreateInstanceOfInt64(rndGen);
            temp = Math.Abs(temp);
            DateTime result;
            try
            {
                result = new DateTime(temp % (DateTime.MaxValue.Ticks + 1));
            }
            catch (ArgumentOutOfRangeException)
            {
                result = DateTime.Now;
            }

            int kind = rndGen.Next(3);
            switch (kind)
            {
                case 0:
                    result = DateTime.SpecifyKind(result, DateTimeKind.Local);
                    break;
                case 1:
                    result = DateTime.SpecifyKind(result, DateTimeKind.Unspecified);
                    break;
                default:
                    result = DateTime.SpecifyKind(result, DateTimeKind.Utc);
                    break;
            }

            if (!CreatorSettings.CreateDateTimeWithSubMilliseconds)
            {
                result = new DateTime(
                    result.Year,
                    result.Month,
                    result.Day,
                    result.Hour,
                    result.Minute,
                    result.Second,
                    result.Millisecond,
                    result.Kind);
            }

            return result;
        }

        /// <summary>
        /// Creates an instance of the <see cref="DateTimeOffset"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="DateTimeOffset"/> type.</returns>
        public static System.DateTimeOffset CreateInstanceOfDateTimeOffset(Random rndGen)
        {
            DateTime temp = CreateInstanceOfDateTime(rndGen);
            temp = DateTime.SpecifyKind(temp, DateTimeKind.Unspecified);
            int offsetMinutes = rndGen.Next(-14 * 60, 14 * 60);
            DateTimeOffset result = new DateTimeOffset(temp, TimeSpan.FromMinutes(offsetMinutes));
            return result;
        }

        /// <summary>
        /// Creates an instance of the <see cref="Decimal"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="Decimal"/> type.</returns>
        public static decimal CreateInstanceOfDecimal(Random rndGen)
        {
            int low = CreateInstanceOfInt32(rndGen);
            int mid = CreateInstanceOfInt32(rndGen);
            int high = CreateInstanceOfInt32(rndGen);
            bool isNegative = rndGen.Next(2) == 0;
            const int MaxDecimalScale = 28;
            byte scale = (byte)rndGen.Next(0, MaxDecimalScale + 1);
            return new decimal(low, mid, high, isNegative, scale);
        }

        /// <summary>
        /// Creates an instance of the <see cref="Double"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="Double"/> type.</returns>
        public static double CreateInstanceOfDouble(Random rndGen)
        {
            bool negative = rndGen.Next(2) == 0;
            int temp = rndGen.Next(40);
            double result;
            switch (temp)
            {
                case 0: return Double.NaN;
                case 1: return Double.PositiveInfinity;
                case 2: return Double.NegativeInfinity;
                case 3: return Double.MinValue;
                case 4: return Double.MaxValue;
                case 5: return Double.Epsilon;
                default:
                    result = (double)(rndGen.NextDouble() * 100000);
                    if (negative)
                    {
                        result = -result;
                    }

                    return result;
            }
        }

        /// <summary>
        /// Creates an instance of the <see cref="Guid"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="Guid"/> type.</returns>
        public static System.Guid CreateInstanceOfGuid(Random rndGen)
        {
            byte[] temp = new byte[16];
            rndGen.NextBytes(temp);
            return new Guid(temp);
        }

        /// <summary>
        /// Creates an instance of the <see cref="Int16"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="Int16"/> type.</returns>
        public static short CreateInstanceOfInt16(Random rndGen)
        {
            byte[] rndValue = new byte[2];
            rndGen.NextBytes(rndValue);
            short result = 0;
            for (int i = 0; i < rndValue.Length; i++)
            {
                result = (short)(result << 8);
                result = (short)(result | (short)rndValue[i]);
            }

            return result;
        }

        /// <summary>
        /// Creates an instance of the <see cref="Int32"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="Int32"/> type.</returns>
        public static int CreateInstanceOfInt32(Random rndGen)
        {
            byte[] rndValue = new byte[4];
            rndGen.NextBytes(rndValue);
            int result = 0;
            for (int i = 0; i < rndValue.Length; i++)
            {
                result = (int)(result << 8);
                result = (int)(result | (int)rndValue[i]);
            }

            return result;
        }

        /// <summary>
        /// Creates an instance of the <see cref="Int64"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="Int64"/> type.</returns>
        public static long CreateInstanceOfInt64(Random rndGen)
        {
            byte[] rndValue = new byte[8];
            rndGen.NextBytes(rndValue);
            long result = 0;
            for (int i = 0; i < rndValue.Length; i++)
            {
                result = (long)(result << 8);
                result = (long)(result | (long)rndValue[i]);
            }

            return result;
        }

        /// <summary>
        /// Creates an instance of the <see cref="Object"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="Object"/> type.</returns>
        public static object CreateInstanceOfObject(Random rndGen)
        {
            return (rndGen.Next(5) == 0) ? null : new object();
        }

        /// <summary>
        /// Creates an instance of the <see cref="SByte"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="SByte"/> type.</returns>
        [CLSCompliant(false)]
        public static sbyte CreateInstanceOfSByte(Random rndGen)
        {
            byte[] rndValue = new byte[1];
            rndGen.NextBytes(rndValue);
            sbyte result = (sbyte)rndValue[0];
            return result;
        }

        /// <summary>
        /// Creates an instance of the <see cref="Single"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="Single"/> type.</returns>
        public static float CreateInstanceOfSingle(Random rndGen)
        {
            bool negative = rndGen.Next(2) == 0;
            int temp = rndGen.Next(40);
            float result;
            switch (temp)
            {
                case 0: return Single.NaN;
                case 1: return Single.PositiveInfinity;
                case 2: return Single.NegativeInfinity;
                case 3: return Single.MinValue;
                case 4: return Single.MaxValue;
                case 5: return Single.Epsilon;
                default:
                    result = (float)(rndGen.NextDouble() * 100000);
                    if (negative)
                    {
                        result = -result;
                    }

                    return result;
            }
        }

        /// <summary>
        /// Creates an instance of the <see cref="String"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <param name="size">The size of the string to be creted.</param>
        /// <param name="charsToUse">The characters to use when creating the string.</param>
        /// <returns>An instance of the <see cref="String"/> type.</returns>
        public static string CreateRandomString(Random rndGen, int size, string charsToUse)
        {
            int maxSize = CreatorSettings.MaxStringLength;

            // invalid per the XML spec (http://www.w3.org/TR/REC-xml/#charsets), cannot be sent as XML
            string invalidXmlChars = "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u000B\u000C\u000E\u000F\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001A\u001B\u001C\u001D\u001E\u001F\uFFFE\uFFFF";

            const int LowSurrogateMin = 0xDC00;
            const int LowSurrogateMax = 0xDFFF;
            const int HighSurrogateMin = 0xD800;
            const int HighSurrogateMax = 0xDBFF;

            if (size < 0)
            {
                double rndNumber = rndGen.NextDouble();
                if (rndNumber < CreatorSettings.NullValueProbability)
                {
                    return null; // 1% chance of null value
                }

                size = (int)Math.Pow(maxSize, rndNumber); // this will create more small strings than large ones
                size--;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < size; i++)
            {
                char c;
                if (charsToUse != null)
                {
                    c = charsToUse[rndGen.Next(charsToUse.Length)];
                    sb.Append(c);
                }
                else
                {
                    if (CreatorSettings.CreateOnlyAsciiChars || rndGen.Next(2) == 0)
                    {
                        c = (char)rndGen.Next(0x20, 0x7F); // low-ascii chars
                        sb.Append(c);
                    }
                    else
                    {
                        do
                        {
                            c = (char)rndGen.Next((int)char.MinValue, (int)char.MaxValue + 1);
                        }
                        while ((LowSurrogateMin <= c && c <= LowSurrogateMax) || (invalidXmlChars.IndexOf(c) >= 0));

                        sb.Append(c);
                        if (HighSurrogateMin <= c && c <= HighSurrogateMax)
                        {
                            // need to add a low surrogate
                            c = (char)rndGen.Next(LowSurrogateMin, LowSurrogateMax + 1);
                            sb.Append(c);
                        }
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Creates an instance of the <see cref="String"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="String"/> type.</returns>
        public static string CreateInstanceOfString(Random rndGen)
        {
            return CreateInstanceOfString(rndGen, true);
        }

        /// <summary>
        /// Creates an instance of the <see cref="String"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <param name="allowNull">A flag indicating whether null values can be returned.</param>
        /// <returns>An instance of the <see cref="String"/> type.</returns>
        public static string CreateInstanceOfString(Random rndGen, bool allowNull)
        {
            string result;
            do
            {
                result = CreateRandomString(rndGen, -1, null);
            }
            while (result == null && !allowNull);

            return result;
        }

        /// <summary>
        /// Creates an instance of the <see cref="String"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <param name="size">The size of the string to be creted.</param>
        /// <param name="charsToUse">The characters to use when creating the string.</param>
        /// <returns>An instance of the <see cref="String"/> type.</returns>
        public static string CreateInstanceOfString(Random rndGen, int size, string charsToUse)
        {
            return CreateRandomString(rndGen, size, charsToUse);
        }

        /// <summary>
        /// Creates an instance of the <see cref="UInt16"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="UInt16"/> type.</returns>
        [CLSCompliant(false)]
        public static ushort CreateInstanceOfUInt16(Random rndGen)
        {
            byte[] rndValue = new byte[2];
            rndGen.NextBytes(rndValue);
            ushort result = 0;
            for (int i = 0; i < rndValue.Length; i++)
            {
                result = (ushort)(result << 8);
                result = (ushort)(result | (ushort)rndValue[i]);
            }

            return result;
        }

        /// <summary>
        /// Creates an instance of the <see cref="UInt32"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="UInt32"/> type.</returns>
        [CLSCompliant(false)]
        public static uint CreateInstanceOfUInt32(Random rndGen)
        {
            byte[] rndValue = new byte[4];
            rndGen.NextBytes(rndValue);
            uint result = 0;
            for (int i = 0; i < rndValue.Length; i++)
            {
                result = (uint)(result << 8);
                result = (uint)(result | (uint)rndValue[i]);
            }

            return result;
        }

        /// <summary>
        /// Creates an instance of the <see cref="UInt64"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="UInt64"/> type.</returns>
        [CLSCompliant(false)]
        public static ulong CreateInstanceOfUInt64(Random rndGen)
        {
            byte[] rndValue = new byte[8];
            rndGen.NextBytes(rndValue);
            ulong result = 0;
            for (int i = 0; i < rndValue.Length; i++)
            {
                result = (ulong)(result << 8);
                result = (ulong)(result | (ulong)rndValue[i]);
            }

            return result;
        }

        /// <summary>
        /// Creates an instance of the <see cref="Uri"/> type.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the <see cref="Uri"/> type.</returns>
        public static System.Uri CreateInstanceOfUri(Random rndGen)
        {
            Uri result;
            UriKind kind;
            try
            {
                string uriString;
                do
                {
                    uriString = UriCreator.CreateUri(rndGen, out kind);
                }
                while (IsRelativeIPv6Uri(uriString, kind));
                result = new Uri(uriString, kind);
            }
            catch (ArgumentException)
            {
                result = new Uri("my.schema://userName:password@my.domain/path1/path2?query1=123&query2=%22hello%22");
            }

            return result;
        }

        /// <summary>
        /// Creates an instance of the a string which represents an <see cref="Uri"/>.
        /// </summary>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the a string which represents an <see cref="Uri"/>.</returns>
        public static string CreateInstanceOfUriString(Random rndGen)
        {
            UriKind kind;
            return UriCreator.CreateUri(rndGen, out kind);
        }

        /// <summary>
        /// Checks whether this creator can create an instance of the given type.
        /// </summary>
        /// <param name="type">The type to be created.</param>
        /// <returns><code>true</code> if this creator can create an instance of the given type; <code>false</code> otherwise.</returns>
        public static bool CanCreateInstanceOf(Type type)
        {
            return creators.ContainsKey(type);
        }

        /// <summary>
        /// Creates an instance of the given primitive type.
        /// </summary>
        /// <param name="type">The type to create an instance.</param>
        /// <param name="rndGen">A <see cref="Random"/> used to create the instance.</param>
        /// <returns>An instance of the given type.</returns>
        public static object CreatePrimitiveInstance(Type type, Random rndGen)
        {
            if (creators.ContainsKey(type))
            {
                return creators[type].Invoke(null, new object[] { rndGen });
            }
            else
            {
                throw new ArgumentException("Type " + type.FullName + " not supported");
            }
        }

        private static bool IsRelativeIPv6Uri(string uriString, UriKind kind)
        {
            return kind == UriKind.Relative && RelativeIPv6UriRegex.Match(uriString).Success;
        }

        /// <summary>
        /// Creates URI instances based on RFC 2396
        /// </summary>
        internal static class UriCreator
        {
            static readonly string digit;
            static readonly string upalpha;
            static readonly string lowalpha;
            static readonly string alpha;
            static readonly string alphanum;
            static readonly string hex;
            static readonly string mark;
            static readonly string unreserved;
            static readonly string reserved;

            static UriCreator()
            {
                digit = "0123456789";
                upalpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                lowalpha = upalpha.ToLower();
                alpha = upalpha + lowalpha;
                alphanum = alpha + digit;
                hex = digit + "ABCDEFabcdef";
                mark = "-_.!~*'()";
                unreserved = alphanum + mark;
                reserved = ";/?:@&=+$,";
            }

            internal static string CreateUri(Random rndGen, out UriKind kind)
            {
                StringBuilder sb = new StringBuilder();
                kind = UriKind.Relative;
                if (rndGen.Next(3) > 0)
                {
                    // Add URI scheme
                    CreateScheme(sb, rndGen);
                    kind = UriKind.Absolute;
                }

                if (rndGen.Next(3) > 0)
                {
                    // Add URI host
                    sb.Append("//");
                    if (rndGen.Next(10) == 0)
                    {
                        CreateUserInfo(sb, rndGen);
                    }

                    CreateHost(sb, rndGen);
                    if (rndGen.Next(2) > 0)
                    {
                        sb.Append(':');
                        sb.Append(rndGen.Next(65536));
                    }
                }

                if (rndGen.Next(4) > 0)
                {
                    // Add URI path
                    for (int i = 0; i < rndGen.Next(1, 4); i++)
                    {
                        sb.Append('/');
                        AddPathSegment(sb, rndGen);
                    }
                }

                if (rndGen.Next(3) == 0)
                {
                    // Add URI query string
                    sb.Append('?');
                    AddUriC(sb, rndGen);
                }

                return sb.ToString();
            }

            private static void CreateScheme(StringBuilder sb, Random rndGen)
            {
                int size = rndGen.Next(1, 10);
                AddChars(sb, rndGen, alpha, 1);
                string schemeChars = alpha + digit + "+-.";
                AddChars(sb, rndGen, schemeChars, size);
                sb.Append(':');
            }

            private static void CreateIPv4Address(StringBuilder sb, Random rndGen)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (i > 0)
                    {
                        sb.Append('.');
                    }

                    sb.Append(rndGen.Next(1000));
                }
            }

            private static void AddIPv6AddressPart(StringBuilder sb, Random rndGen)
            {
                int size = rndGen.Next(1, 10);
                if (size > 4)
                {
                    size = 4;
                }

                AddChars(sb, rndGen, hex, size);
            }

            private static void CreateIPv6Address(StringBuilder sb, Random rndGen)
            {
                sb.Append('[');
                int temp = rndGen.Next(6);
                int i;
                switch (temp)
                {
                    case 0:
                        sb.Append("::");
                        break;
                    case 1:
                        sb.Append("::1");
                        break;
                    case 2:
                        sb.Append("FF01::101");
                        break;
                    case 3:
                        sb.Append("::1");
                        break;
                    case 4:
                        for (i = 0; i < 3; i++)
                        {
                            AddIPv6AddressPart(sb, rndGen);
                            sb.Append(':');
                        }

                        for (i = 0; i < 3; i++)
                        {
                            sb.Append(':');
                            AddIPv6AddressPart(sb, rndGen);
                        }

                        break;
                    default:
                        for (i = 0; i < 8; i++)
                        {
                            if (i > 0)
                            {
                                sb.Append(':');
                            }

                            AddIPv6AddressPart(sb, rndGen);
                        }

                        break;
                }

                sb.Append(']');
            }

            private static void AddChars(StringBuilder sb, Random rndGen, string validChars, int size)
            {
                for (int i = 0; i < size; i++)
                {
                    sb.Append(validChars[rndGen.Next(validChars.Length)]);
                }
            }

            private static void CreateHostName(StringBuilder sb, Random rndGen)
            {
                int domainLabelCount = rndGen.Next(4);
                int size;
                for (int i = 0; i < domainLabelCount; i++)
                {
                    AddChars(sb, rndGen, alphanum, 1);
                    size = rndGen.Next(10) - 1;
                    if (size > 0)
                    {
                        AddChars(sb, rndGen, alphanum + "-", size);
                        AddChars(sb, rndGen, alphanum, 1);
                    }

                    sb.Append('.');
                }

                AddChars(sb, rndGen, alpha, 1);
                size = rndGen.Next(10) - 1;
                if (size > 0)
                {
                    AddChars(sb, rndGen, alphanum + "-", size);
                    AddChars(sb, rndGen, alphanum, 1);
                }
            }

            private static void CreateHost(StringBuilder sb, Random rndGen)
            {
                int temp = rndGen.Next(3);
                switch (temp)
                {
                    case 0:
                        CreateIPv4Address(sb, rndGen);
                        break;
                    case 1:
                        CreateIPv6Address(sb, rndGen);
                        break;
                    case 2:
                        CreateHostName(sb, rndGen);
                        break;
                }
            }

            private static void CreateUserInfo(StringBuilder sb, Random rndGen)
            {
                AddChars(sb, rndGen, alpha, rndGen.Next(1, 10));
                if (rndGen.Next(3) > 0)
                {
                    sb.Append(':');
                    AddChars(sb, rndGen, alpha, rndGen.Next(1, 10));
                }

                sb.Append('@');
            }

            private static void AddEscapedChar(StringBuilder sb, Random rndGen)
            {
                sb.Append('%');
                AddChars(sb, rndGen, hex, 2);
            }

            private static void AddPathSegment(StringBuilder sb, Random rndGen)
            {
                string pchar = unreserved + ":@&=+$,";
                int size = rndGen.Next(1, 10);
                for (int i = 0; i < size; i++)
                {
                    if (rndGen.Next(pchar.Length + 1) > 0)
                    {
                        AddChars(sb, rndGen, pchar, 1);
                    }
                    else
                    {
                        AddEscapedChar(sb, rndGen);
                    }
                }
            }

            private static void AddUriC(StringBuilder sb, Random rndGen)
            {
                int size = rndGen.Next(20);
                string reservedPlusUnreserved = reserved + unreserved;
                for (int i = 0; i < size; i++)
                {
                    if (rndGen.Next(5) > 0)
                    {
                        AddChars(sb, rndGen, reservedPlusUnreserved, 1);
                    }
                    else
                    {
                        AddEscapedChar(sb, rndGen);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Enables tests to create specific instances of certain types.
    /// </summary>
    public abstract class InstanceCreatorSurrogate
    {
        /// <summary>
        /// Checks whether this surrogate can create instances of a given type.
        /// </summary>
        /// <param name="type">The type which needs to be created.</param>
        /// <returns>A true value if this surrogate can create the given type; a
        /// false value otherwise.</returns>
        public abstract bool CanCreateInstanceOf(Type type);

        /// <summary>
        /// Creates an instance of the given type.
        /// </summary>
        /// <param name="type">The type to create an instance for.</param>
        /// <param name="rndGen">A Random generator to assist in creating the instance.</param>
        /// <returns>An instance of the given type.</returns>
        public abstract object CreateInstanceOf(Type type, Random rndGen);
    }
}