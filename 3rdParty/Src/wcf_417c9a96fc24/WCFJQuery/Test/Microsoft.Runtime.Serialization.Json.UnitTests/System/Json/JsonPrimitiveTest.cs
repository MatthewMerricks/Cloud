namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Json;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class JsonPrimitiveTest
    {
        const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffK";

        [TestMethod]
        public void JsonPrimitiveConstructorTest()
        {
            Assert.AreEqual(AnyInstance.AnyString, (string)(new JsonPrimitive(AnyInstance.AnyString)));
            Assert.AreEqual(AnyInstance.AnyChar, (char)(new JsonPrimitive(AnyInstance.AnyChar)));
            Assert.AreEqual(AnyInstance.AnyUri, (Uri)(new JsonPrimitive(AnyInstance.AnyUri)));
            Assert.AreEqual(AnyInstance.AnyGuid, (Guid)(new JsonPrimitive(AnyInstance.AnyGuid)));
            Assert.AreEqual(AnyInstance.AnyDateTime, (DateTime)(new JsonPrimitive(AnyInstance.AnyDateTime)));
            Assert.AreEqual(AnyInstance.AnyDateTimeOffset, (DateTimeOffset)(new JsonPrimitive(AnyInstance.AnyDateTimeOffset)));
            Assert.AreEqual(AnyInstance.AnyBool, (bool)(new JsonPrimitive(AnyInstance.AnyBool)));
            Assert.AreEqual(AnyInstance.AnyByte, (byte)(new JsonPrimitive(AnyInstance.AnyByte)));
            Assert.AreEqual(AnyInstance.AnyShort, (short)(new JsonPrimitive(AnyInstance.AnyShort)));
            Assert.AreEqual(AnyInstance.AnyInt, (int)(new JsonPrimitive(AnyInstance.AnyInt)));
            Assert.AreEqual(AnyInstance.AnyLong, (long)(new JsonPrimitive(AnyInstance.AnyLong)));
            Assert.AreEqual(AnyInstance.AnySByte, (sbyte)(new JsonPrimitive(AnyInstance.AnySByte)));
            Assert.AreEqual(AnyInstance.AnyUShort, (ushort)(new JsonPrimitive(AnyInstance.AnyUShort)));
            Assert.AreEqual(AnyInstance.AnyUInt, (uint)(new JsonPrimitive(AnyInstance.AnyUInt)));
            Assert.AreEqual(AnyInstance.AnyULong, (ulong)(new JsonPrimitive(AnyInstance.AnyULong)));
            Assert.AreEqual(AnyInstance.AnyDecimal, (decimal)(new JsonPrimitive(AnyInstance.AnyDecimal)));
            Assert.AreEqual(AnyInstance.AnyFloat, (float)(new JsonPrimitive(AnyInstance.AnyFloat)));
            Assert.AreEqual(AnyInstance.AnyDouble, (double)(new JsonPrimitive(AnyInstance.AnyDouble)));
        }

        [TestMethod]
        public void ValueTest()
        {
            object[] values = 
            {
                AnyInstance.AnyInt, AnyInstance.AnyString, AnyInstance.AnyGuid, AnyInstance.AnyDecimal, AnyInstance.AnyBool, AnyInstance.AnyDateTime
            };

            foreach (object value in values)
            {
                JsonPrimitive jp;
                bool success = JsonPrimitive.TryCreate(value, out jp);
                Assert.IsTrue(success);
                Assert.AreEqual(value, jp.Value);
            }
        }

        [TestMethod]
        public void TryCreateTest()
        {
            object[] numberValues =
            {
                AnyInstance.AnyByte, AnyInstance.AnySByte, AnyInstance.AnyShort, AnyInstance.AnyDecimal, 
                AnyInstance.AnyDouble, AnyInstance.AnyShort, AnyInstance.AnyInt, AnyInstance.AnyLong, 
                AnyInstance.AnyUShort, AnyInstance.AnyUInt, AnyInstance.AnyULong, AnyInstance.AnyFloat
            };

            object[] booleanValues =
            {
                true, false
            };


            object[] stringValues =
            {
                AnyInstance.AnyString, AnyInstance.AnyChar, 
                AnyInstance.AnyDateTime, AnyInstance.AnyDateTimeOffset,
                AnyInstance.AnyGuid, AnyInstance.AnyUri
            };

            CheckValues(numberValues, JsonType.Number);
            CheckValues(booleanValues, JsonType.Boolean);
            CheckValues(stringValues, JsonType.String);
        }

        [TestMethod]
        public void TryCreateInvalidTest()
        {
            bool success;
            JsonPrimitive target;

            object[] values =
            {
                AnyInstance.AnyJsonArray, AnyInstance.AnyJsonObject, AnyInstance.AnyJsonPrimitive, 
                null, AnyInstance.DefaultJsonValue, AnyInstance.AnyDynamic, AnyInstance.AnyAddress,
                AnyInstance.AnyPerson
            };

            foreach (object value in values)
            {
                success = JsonPrimitive.TryCreate(value, out target);
                Assert.IsFalse(success);
                Assert.IsNull(target);
            }
        }

        [TestMethod]
        public void NumberToNumberConversionTest()
        {
            long longValue;
            Assert.AreEqual((long)AnyInstance.AnyInt, (long)(new JsonPrimitive(AnyInstance.AnyInt)));
            Assert.AreEqual((long)AnyInstance.AnyUInt, (long)(new JsonPrimitive(AnyInstance.AnyUInt)));
            Assert.IsTrue(new JsonPrimitive(AnyInstance.AnyInt).TryReadAs<long>(out longValue));
            Assert.AreEqual((long)AnyInstance.AnyInt, longValue);

            int intValue;
            Assert.AreEqual((int)AnyInstance.AnyShort, (int)(new JsonPrimitive(AnyInstance.AnyShort)));
            Assert.AreEqual((int)AnyInstance.AnyUShort, (int)(new JsonPrimitive(AnyInstance.AnyUShort)));
            Assert.IsTrue(new JsonPrimitive(AnyInstance.AnyUShort).TryReadAs<int>(out intValue));
            Assert.AreEqual((int)AnyInstance.AnyUShort, intValue);

            short shortValue;
            Assert.AreEqual((short)AnyInstance.AnyByte, (short)(new JsonPrimitive(AnyInstance.AnyByte)));
            Assert.AreEqual((short)AnyInstance.AnySByte, (short)(new JsonPrimitive(AnyInstance.AnySByte)));
            Assert.IsTrue(new JsonPrimitive(AnyInstance.AnyByte).TryReadAs<short>(out shortValue));
            Assert.AreEqual((short)AnyInstance.AnyByte, shortValue);

            double dblValue;
            Assert.AreEqual((double)AnyInstance.AnyFloat, (double)(new JsonPrimitive(AnyInstance.AnyFloat)));
            Assert.AreEqual((double)AnyInstance.AnyDecimal, (double)(new JsonPrimitive(AnyInstance.AnyDecimal)));
            Assert.IsTrue(new JsonPrimitive(AnyInstance.AnyFloat).TryReadAs<double>(out dblValue));
            Assert.AreEqual((double)AnyInstance.AnyFloat, dblValue);
            ExceptionTestHelper.ExpectException<OverflowException>(delegate { int i = (int)(new JsonPrimitive(1L << 32)); });
            Assert.IsFalse(new JsonPrimitive(1L << 32).TryReadAs<int>(out intValue));
            Assert.AreEqual(default(int), intValue);

            byte byteValue;
            ExceptionTestHelper.ExpectException<OverflowException>(delegate { byte b = (byte)(new JsonPrimitive(1L << 32)); }); 
            ExceptionTestHelper.ExpectException<OverflowException>(delegate { byte b = (byte)(new JsonPrimitive(sbyte.MinValue)); });
            Assert.IsFalse(new JsonPrimitive(sbyte.MinValue).TryReadAs<byte>(out byteValue));
            Assert.AreEqual(default(byte), byteValue);
        }

        [TestMethod]
        public void NumberToStringConverstionTest()
        {
            Dictionary<string, JsonPrimitive> allNumbers = new Dictionary<string, JsonPrimitive>
            {
                { AnyInstance.AnyByte.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyByte) },
                { AnyInstance.AnySByte.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnySByte) },
                { AnyInstance.AnyShort.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyShort) },
                { AnyInstance.AnyUShort.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyUShort) },
                { AnyInstance.AnyInt.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyInt) },
                { AnyInstance.AnyUInt.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyUInt) },
                { AnyInstance.AnyLong.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyLong) },
                { AnyInstance.AnyULong.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyULong) },
                { AnyInstance.AnyDecimal.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyDecimal) },
                { AnyInstance.AnyDouble.ToString("R", CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyDouble) },
                { AnyInstance.AnyFloat.ToString("R", CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyFloat) },
            };

            foreach (string stringRepresentation in allNumbers.Keys)
            {
                JsonPrimitive jp = allNumbers[stringRepresentation];
                Assert.AreEqual(stringRepresentation, (string)jp);
                Assert.AreEqual(stringRepresentation, jp.ReadAs<string>());
            }
        }

        [TestMethod]
        public void NonNumberToStringConversionTest()
        {
            Dictionary<string, JsonPrimitive> allValues = new Dictionary<string, JsonPrimitive>
            {
                { new string(AnyInstance.AnyChar, 1), new JsonPrimitive(AnyInstance.AnyChar) },
                { AnyInstance.AnyBool.ToString(CultureInfo.InvariantCulture).ToLowerInvariant(), new JsonPrimitive(AnyInstance.AnyBool) },
                { AnyInstance.AnyGuid.ToString("D", CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyGuid) },
                { AnyInstance.AnyDateTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyDateTime) },
                { AnyInstance.AnyDateTimeOffset.ToString(DateTimeFormat, CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyDateTimeOffset) },
            };

            foreach (char escapedChar in "\r\n\t\u0000\uffff\u001f\\\"")
            {
                allValues.Add(new string(escapedChar, 1), new JsonPrimitive(escapedChar));
            }

            foreach (string stringRepresentation in allValues.Keys)
            {
                JsonPrimitive jp = allValues[stringRepresentation];
                Assert.AreEqual(stringRepresentation, (string)jp);
                Assert.AreEqual(stringRepresentation, jp.ReadAs<string>());
            }
        }

        [TestMethod]
        public void NonNumberToNumberConversionTest()
        {
            Assert.AreEqual(1, new JsonPrimitive('1').ReadAs<int>());
            Assert.AreEqual<byte>(AnyInstance.AnyByte, new JsonPrimitive(AnyInstance.AnyByte.ToString(CultureInfo.InvariantCulture)).ReadAs<byte>());
            Assert.AreEqual<sbyte>(AnyInstance.AnySByte, (sbyte)(new JsonPrimitive(AnyInstance.AnySByte.ToString(CultureInfo.InvariantCulture))));
            Assert.AreEqual<short>(AnyInstance.AnyShort, (short)(new JsonPrimitive(AnyInstance.AnyShort.ToString(CultureInfo.InvariantCulture))));
            Assert.AreEqual<ushort>(AnyInstance.AnyUShort, new JsonPrimitive(AnyInstance.AnyUShort.ToString(CultureInfo.InvariantCulture)).ReadAs<ushort>());
            Assert.AreEqual<int>(AnyInstance.AnyInt, new JsonPrimitive(AnyInstance.AnyInt.ToString(CultureInfo.InvariantCulture)).ReadAs<int>());
            Assert.AreEqual<uint>(AnyInstance.AnyUInt, (uint)(new JsonPrimitive(AnyInstance.AnyUInt.ToString(CultureInfo.InvariantCulture))));
            Assert.AreEqual<long>(AnyInstance.AnyLong, (long)(new JsonPrimitive(AnyInstance.AnyLong.ToString(CultureInfo.InvariantCulture))));
            Assert.AreEqual<ulong>(AnyInstance.AnyULong, new JsonPrimitive(AnyInstance.AnyULong.ToString(CultureInfo.InvariantCulture)).ReadAs<ulong>());

            Assert.AreEqual<decimal>(AnyInstance.AnyDecimal, (decimal)(new JsonPrimitive(AnyInstance.AnyDecimal.ToString(CultureInfo.InvariantCulture))));
            Assert.AreEqual<float>(AnyInstance.AnyFloat, new JsonPrimitive(AnyInstance.AnyFloat.ToString(CultureInfo.InvariantCulture)).ReadAs<float>());
            Assert.AreEqual<double>(AnyInstance.AnyDouble, (double)(new JsonPrimitive(AnyInstance.AnyDouble.ToString(CultureInfo.InvariantCulture))));

            Assert.AreEqual<byte>(Convert.ToByte(1.23, CultureInfo.InvariantCulture), new JsonPrimitive("1.23").ReadAs<byte>());
            Assert.AreEqual<int>(Convert.ToInt32(12345.6789, CultureInfo.InvariantCulture), new JsonPrimitive("12345.6789").ReadAs<int>());
            Assert.AreEqual<short>(Convert.ToInt16(1.23e2), (short)new JsonPrimitive("1.23e2"));
            Assert.AreEqual<float>(Convert.ToSingle(1.23e40), (float)new JsonPrimitive("1.23e40"));
            Assert.AreEqual<float>(Convert.ToSingle(1.23e-38), (float)new JsonPrimitive("1.23e-38"));

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var n = new JsonPrimitive(AnyInstance.AnyBool).ReadAs<sbyte>(); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var n = new JsonPrimitive(AnyInstance.AnyBool).ReadAs<short>(); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var n = new JsonPrimitive(AnyInstance.AnyBool).ReadAs<uint>(); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var n = new JsonPrimitive(AnyInstance.AnyBool).ReadAs<long>(); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var n = new JsonPrimitive(AnyInstance.AnyBool).ReadAs<double>(); });

            ExceptionTestHelper.ExpectException<FormatException>(delegate { var n = new JsonPrimitive(AnyInstance.AnyUri).ReadAs<int>(); });
            ExceptionTestHelper.ExpectException<FormatException>(delegate { var n = new JsonPrimitive(AnyInstance.AnyDateTime).ReadAs<float>(); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var n = (decimal)(new JsonPrimitive('c')); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var n = (byte)(new JsonPrimitive("0xFF")); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var n = (sbyte)(new JsonPrimitive(AnyInstance.AnyDateTimeOffset)); });
            ExceptionTestHelper.ExpectException<FormatException>(delegate { var n = new JsonPrimitive(AnyInstance.AnyUri).ReadAs<uint>(); });
            ExceptionTestHelper.ExpectException<FormatException>(delegate { var n = new JsonPrimitive(AnyInstance.AnyDateTime).ReadAs<double>(); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var n = (long)(new JsonPrimitive('c')); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var n = (ulong)(new JsonPrimitive("0xFF")); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var n = (short)(new JsonPrimitive(AnyInstance.AnyDateTimeOffset)); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var n = (ushort)(new JsonPrimitive('c')); });

            ExceptionTestHelper.ExpectException<OverflowException>(delegate { int i = (int)new JsonPrimitive((1L << 32).ToString(CultureInfo.InvariantCulture)); });
            ExceptionTestHelper.ExpectException<OverflowException>(delegate { byte b = (byte)new JsonPrimitive("-1"); });
        }

        [TestMethod]
        public void StringToNonNumberConversionTest()
        {
            const string DateTimeWithOffsetFormat = "yyyy-MM-ddTHH:mm:sszzz";
            const string DateTimeWithOffsetFormat2 = "yyy-MM-ddTHH:mm:ss.fffK";
            const string DateTimeWithoutOffsetWithoutTimeFormat = "yyy-MM-dd";
            const string DateTimeWithoutOffsetFormat = "yyy-MM-ddTHH:mm:ss";
            const string DateTimeWithoutOffsetFormat2 = "yyy-MM-ddTHH:mm:ss.fff";
            const string TimeWithoutOffsetFormat = "HH:mm:ss";
            const string TimeWithoutOffsetFormat2 = "HH:mm";

            Assert.AreEqual(false, new JsonPrimitive("false").ReadAs<bool>());
            Assert.AreEqual(false, (bool)(new JsonPrimitive("False")));
            Assert.AreEqual(true, (bool)(new JsonPrimitive("true")));
            Assert.AreEqual(true, new JsonPrimitive("True").ReadAs<bool>());

            Assert.AreEqual<Uri>(AnyInstance.AnyUri, new JsonPrimitive(AnyInstance.AnyUri.ToString()).ReadAs<Uri>());
            Assert.AreEqual<char>(AnyInstance.AnyChar, (char)(new JsonPrimitive(new string(AnyInstance.AnyChar, 1))));
            Assert.AreEqual<Guid>(AnyInstance.AnyGuid, (Guid)(new JsonPrimitive(AnyInstance.AnyGuid.ToString("D", CultureInfo.InvariantCulture))));

            DateTime anyLocalDateTime = AnyInstance.AnyDateTime.ToLocalTime();
            DateTime anyUtcDateTime = AnyInstance.AnyDateTime.ToUniversalTime();

            Assert.AreEqual<DateTime>(anyUtcDateTime, (DateTime)(new JsonPrimitive(anyUtcDateTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture))));
            Assert.AreEqual<DateTime>(anyLocalDateTime, new JsonPrimitive(anyLocalDateTime.ToString(DateTimeWithOffsetFormat2, CultureInfo.InvariantCulture)).ReadAs<DateTime>());
            Assert.AreEqual<DateTime>(anyUtcDateTime, new JsonPrimitive(anyUtcDateTime.ToString(DateTimeWithOffsetFormat2, CultureInfo.InvariantCulture)).ReadAs<DateTime>());
            Assert.AreEqual<DateTime>(anyLocalDateTime.Date, (DateTime)(new JsonPrimitive(anyLocalDateTime.ToString(DateTimeWithoutOffsetWithoutTimeFormat, CultureInfo.InvariantCulture))));
            Assert.AreEqual<DateTime>(anyLocalDateTime, new JsonPrimitive(anyLocalDateTime.ToString(DateTimeWithoutOffsetFormat, CultureInfo.InvariantCulture)).ReadAs<DateTime>());
            Assert.AreEqual<DateTime>(anyLocalDateTime, new JsonPrimitive(anyLocalDateTime.ToString(DateTimeWithoutOffsetFormat2, CultureInfo.InvariantCulture)).ReadAs<DateTime>());

            DateTime dt = new JsonPrimitive(anyLocalDateTime.ToString(TimeWithoutOffsetFormat, CultureInfo.InvariantCulture)).ReadAs<DateTime>();
            Assert.AreEqual(anyLocalDateTime.Hour, dt.Hour);
            Assert.AreEqual(anyLocalDateTime.Minute, dt.Minute);
            Assert.AreEqual(anyLocalDateTime.Second, dt.Second);

            dt = new JsonPrimitive(anyLocalDateTime.ToString(TimeWithoutOffsetFormat2, CultureInfo.InvariantCulture)).ReadAs<DateTime>();
            Assert.AreEqual(anyLocalDateTime.Hour, dt.Hour);
            Assert.AreEqual(anyLocalDateTime.Minute, dt.Minute);
            Assert.AreEqual(0, dt.Second);

            Assert.AreEqual<DateTimeOffset>(AnyInstance.AnyDateTimeOffset, new JsonPrimitive(AnyInstance.AnyDateTimeOffset.ToString(DateTimeFormat, CultureInfo.InvariantCulture)).ReadAs<DateTimeOffset>());
            Assert.AreEqual<DateTimeOffset>(AnyInstance.AnyDateTimeOffset, new JsonPrimitive(AnyInstance.AnyDateTimeOffset.ToString(DateTimeWithOffsetFormat, CultureInfo.InvariantCulture)).ReadAs<DateTimeOffset>());
            Assert.AreEqual<DateTimeOffset>(AnyInstance.AnyDateTimeOffset, new JsonPrimitive(AnyInstance.AnyDateTimeOffset.ToString(DateTimeWithOffsetFormat2, CultureInfo.InvariantCulture)).ReadAs<DateTimeOffset>());
            Assert.AreEqual<DateTimeOffset>(AnyInstance.AnyDateTimeOffset.ToLocalTime(), (DateTimeOffset)(new JsonPrimitive(AnyInstance.AnyDateTimeOffset.ToLocalTime().ToString(DateTimeWithoutOffsetFormat, CultureInfo.InvariantCulture))));
            Assert.AreEqual<DateTimeOffset>(AnyInstance.AnyDateTimeOffset.ToLocalTime(), (DateTimeOffset)(new JsonPrimitive(AnyInstance.AnyDateTimeOffset.ToLocalTime().ToString(DateTimeWithoutOffsetFormat2, CultureInfo.InvariantCulture))));

            DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(DateTime));
            MemoryStream ms = new MemoryStream();
            dcjs.WriteObject(ms, AnyInstance.AnyDateTime);
            string dcjsSerializedDateTime = Encoding.UTF8.GetString(ms.ToArray());
            Assert.AreEqual(AnyInstance.AnyDateTime, JsonValue.Parse(dcjsSerializedDateTime).ReadAs<DateTime>());

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var b = (bool)(new JsonPrimitive("notBool")); });
            ExceptionTestHelper.ExpectException<UriFormatException>(delegate { var u = new JsonPrimitive("not an uri - " + new string('r', 100000)).ReadAs<Uri>(); });
            ExceptionTestHelper.ExpectException<FormatException>(delegate { var date = new JsonPrimitive("not a date time").ReadAs<DateTime>(); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var dto = (DateTimeOffset)(new JsonPrimitive("not a date time offset")); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var c = (char)new JsonPrimitive(""); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var c = (char)new JsonPrimitive("cc"); });
            ExceptionTestHelper.ExpectException<FormatException>(delegate { var g = new JsonPrimitive("not a guid").ReadAs<Guid>(); });
        }

        [TestMethod]
        public void AspNetDateTimeFormatConversionTest()
        {
            DateTime unixEpochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime unixEpochLocal = unixEpochUtc.ToLocalTime();
            Assert.AreEqual(unixEpochUtc, new JsonPrimitive("/Date(0)/").ReadAs<DateTime>());
            Assert.AreEqual(unixEpochLocal, new JsonPrimitive("/Date(0-0900)/").ReadAs<DateTime>());
            Assert.AreEqual(unixEpochLocal, new JsonPrimitive("/Date(0+1000)/").ReadAs<DateTime>());
        }

        [TestMethod]
        public void ToStringTest()
        {
            char anyUnescapedChar = 'c';
            string anyUnescapedString = "hello";

            Dictionary<string, JsonPrimitive> toStringResults = new Dictionary<string, JsonPrimitive>
            {
                // Boolean types
                { AnyInstance.AnyBool.ToString(CultureInfo.InvariantCulture).ToLowerInvariant(), new JsonPrimitive(AnyInstance.AnyBool) },

                // Numeric types
                { AnyInstance.AnyByte.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyByte) },
                { AnyInstance.AnySByte.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnySByte) },
                { AnyInstance.AnyShort.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyShort) },
                { AnyInstance.AnyUShort.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyUShort) },
                { AnyInstance.AnyInt.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyInt) },
                { AnyInstance.AnyUInt.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyUInt) },
                { AnyInstance.AnyLong.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyLong) },
                { AnyInstance.AnyULong.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyULong) },
                { AnyInstance.AnyFloat.ToString("R", CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyFloat) },
                { AnyInstance.AnyDouble.ToString("R", CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyDouble) },
                { AnyInstance.AnyDecimal.ToString(CultureInfo.InvariantCulture), new JsonPrimitive(AnyInstance.AnyDecimal) },

                // String types
                { "\"" + new string(anyUnescapedChar, 1) + "\"", new JsonPrimitive(anyUnescapedChar) },
                { "\"" + anyUnescapedString + "\"", new JsonPrimitive(anyUnescapedString) },
                { "\"" + AnyInstance.AnyDateTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture) + "\"", new JsonPrimitive(AnyInstance.AnyDateTime) },
                { "\"" + AnyInstance.AnyDateTimeOffset.ToString(DateTimeFormat, CultureInfo.InvariantCulture) + "\"", new JsonPrimitive(AnyInstance.AnyDateTimeOffset) },
                { "\"" + AnyInstance.AnyUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped).Replace("/", "\\/") + "\"", new JsonPrimitive(AnyInstance.AnyUri) },
                { "\"" + AnyInstance.AnyGuid.ToString("D", CultureInfo.InvariantCulture) + "\"", new JsonPrimitive(AnyInstance.AnyGuid) },
            };

            foreach (string stringRepresentation in toStringResults.Keys)
            {
                string actualResult = toStringResults[stringRepresentation].ToString();
                Assert.AreEqual(stringRepresentation, actualResult);
            }

            Dictionary<string, JsonPrimitive> escapedValues = new Dictionary<string, JsonPrimitive>
            {
                { "\"\\u000d\"", new JsonPrimitive('\r') },
                { "\"\\u000a\"", new JsonPrimitive('\n') },
                { "\"\\\\\"", new JsonPrimitive('\\') },
                { "\"\\/\"", new JsonPrimitive('/') },
                { "\"\\u000b\"", new JsonPrimitive('\u000b') },
                { "\"\\\"\"", new JsonPrimitive('\"') },
                { "\"slash-r-\\u000d-fffe-\\ufffe-ffff-\\uffff-tab-\\u0009\"", new JsonPrimitive("slash-r-\r-fffe-\ufffe-ffff-\uffff-tab-\t") },
            };

            foreach (string stringRepresentation in escapedValues.Keys)
            {
                string actualResult = escapedValues[stringRepresentation].ToString();
                Assert.AreEqual(stringRepresentation, actualResult);
            }
        }

        [TestMethod]
        public void JsonTypeTest()
        {
            Assert.AreEqual(JsonType.Boolean, new JsonPrimitive(AnyInstance.AnyBool).JsonType);
            Assert.AreEqual(JsonType.Number, new JsonPrimitive(AnyInstance.AnyByte).JsonType);
            Assert.AreEqual(JsonType.Number, new JsonPrimitive(AnyInstance.AnySByte).JsonType);
            Assert.AreEqual(JsonType.Number, new JsonPrimitive(AnyInstance.AnyShort).JsonType);
            Assert.AreEqual(JsonType.Number, new JsonPrimitive(AnyInstance.AnyUShort).JsonType);
            Assert.AreEqual(JsonType.Number, new JsonPrimitive(AnyInstance.AnyInt).JsonType);
            Assert.AreEqual(JsonType.Number, new JsonPrimitive(AnyInstance.AnyUInt).JsonType);
            Assert.AreEqual(JsonType.Number, new JsonPrimitive(AnyInstance.AnyLong).JsonType);
            Assert.AreEqual(JsonType.Number, new JsonPrimitive(AnyInstance.AnyULong).JsonType);
            Assert.AreEqual(JsonType.Number, new JsonPrimitive(AnyInstance.AnyDecimal).JsonType);
            Assert.AreEqual(JsonType.Number, new JsonPrimitive(AnyInstance.AnyDouble).JsonType);
            Assert.AreEqual(JsonType.Number, new JsonPrimitive(AnyInstance.AnyFloat).JsonType);
            Assert.AreEqual(JsonType.String, new JsonPrimitive(AnyInstance.AnyChar).JsonType);
            Assert.AreEqual(JsonType.String, new JsonPrimitive(AnyInstance.AnyString).JsonType);
            Assert.AreEqual(JsonType.String, new JsonPrimitive(AnyInstance.AnyUri).JsonType);
            Assert.AreEqual(JsonType.String, new JsonPrimitive(AnyInstance.AnyGuid).JsonType);
            Assert.AreEqual(JsonType.String, new JsonPrimitive(AnyInstance.AnyDateTime).JsonType);
            Assert.AreEqual(JsonType.String, new JsonPrimitive(AnyInstance.AnyDateTimeOffset).JsonType);
        }

        [TestMethod]
        public void InvalidPropertyTest()
        {
            JsonValue target  = AnyInstance.AnyJsonPrimitive;
            Assert.IsTrue(target.Count == 0);
            Assert.IsFalse(target.ContainsKey(string.Empty));
            Assert.IsFalse(target.ContainsKey(AnyInstance.AnyString));
        }

        private void CheckValues(object[] values, JsonType expectedType)
        {
            JsonPrimitive target;
            bool success;

            foreach (object value in values)
            {
                success = JsonPrimitive.TryCreate(value, out target);
                Assert.IsTrue(success);
                Assert.IsNotNull(target);
                Assert.AreEqual(expectedType, target.JsonType);
            }
        }
    }
}
