namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Json;
    using System.Runtime.Serialization.Json;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class JsonValueDynamicTest
    {
        const string InvalidIndexType = "Invalid '{0}' index type; only 'System.String' and non-negative 'System.Int32' types are supported.";
        const string NonSingleNonNullIndexNotSupported = "Null index or multidimensional indexing is not supported by this indexer; use 'System.Int32' or 'System.String' for array and object indexing respectively.";

        [TestMethod]
        public void SettingDifferentValueTypes()
        {
            dynamic dyn = new JsonObject();
            dyn.boolean = AnyInstance.AnyBool;
            dyn.int16 = AnyInstance.AnyShort;
            dyn.int32 = AnyInstance.AnyInt;
            dyn.int64 = AnyInstance.AnyLong;
            dyn.uint16 = AnyInstance.AnyUShort;
            dyn.uint32 = AnyInstance.AnyUInt;
            dyn.uint64 = AnyInstance.AnyULong;
            dyn.@char = AnyInstance.AnyChar;
            dyn.dbl = AnyInstance.AnyDouble;
            dyn.flt = AnyInstance.AnyFloat;
            dyn.dec = AnyInstance.AnyDecimal;
            dyn.str = AnyInstance.AnyString;
            dyn.uri = AnyInstance.AnyUri;
            dyn.@byte = AnyInstance.AnyByte;
            dyn.@sbyte = AnyInstance.AnySByte;
            dyn.guid = AnyInstance.AnyGuid;
            dyn.dateTime = AnyInstance.AnyDateTime;
            dyn.dateTimeOffset = AnyInstance.AnyDateTimeOffset;
            dyn.JsonArray = AnyInstance.AnyJsonArray;
            dyn.JsonPrimitive = AnyInstance.AnyJsonPrimitive;
            dyn.JsonObject = AnyInstance.AnyJsonObject;

            JsonObject jo = (JsonObject)dyn;
            Assert.AreEqual(AnyInstance.AnyBool, (bool)jo["boolean"]);
            Assert.AreEqual(AnyInstance.AnyShort, (short)jo["int16"]);
            Assert.AreEqual(AnyInstance.AnyUShort, (ushort)jo["uint16"]);
            Assert.AreEqual(AnyInstance.AnyInt, (int)jo["int32"]);
            Assert.AreEqual(AnyInstance.AnyUInt, (uint)jo["uint32"]);
            Assert.AreEqual(AnyInstance.AnyLong, (long)jo["int64"]);
            Assert.AreEqual(AnyInstance.AnyULong, (ulong)jo["uint64"]);
            Assert.AreEqual(AnyInstance.AnySByte, (sbyte)jo["sbyte"]);
            Assert.AreEqual(AnyInstance.AnyByte, (byte)jo["byte"]);
            Assert.AreEqual(AnyInstance.AnyChar, (char)jo["char"]);
            Assert.AreEqual(AnyInstance.AnyDouble, (double)jo["dbl"]);
            Assert.AreEqual(AnyInstance.AnyFloat, (float)jo["flt"]);
            Assert.AreEqual(AnyInstance.AnyDecimal, (decimal)jo["dec"]);
            Assert.AreEqual(AnyInstance.AnyString, (string)jo["str"]);
            Assert.AreEqual(AnyInstance.AnyUri, (Uri)jo["uri"]);
            Assert.AreEqual(AnyInstance.AnyGuid, (Guid)jo["guid"]);
            Assert.AreEqual(AnyInstance.AnyDateTime, (DateTime)jo["dateTime"]);
            Assert.AreEqual(AnyInstance.AnyDateTimeOffset, (DateTimeOffset)jo["dateTimeOffset"]);
            Assert.AreSame(AnyInstance.AnyJsonArray, jo["JsonArray"]);
            Assert.AreEqual(AnyInstance.AnyJsonPrimitive, jo["JsonPrimitive"]);
            Assert.AreSame(AnyInstance.AnyJsonObject, jo["JsonObject"]);

            Assert.AreEqual(AnyInstance.AnyBool, (bool)dyn.boolean);
            Assert.AreEqual(AnyInstance.AnyShort, (short)dyn.int16);
            Assert.AreEqual(AnyInstance.AnyUShort, (ushort)dyn.uint16);
            Assert.AreEqual(AnyInstance.AnyInt, (int)dyn.int32);
            Assert.AreEqual(AnyInstance.AnyUInt, (uint)dyn.uint32);
            Assert.AreEqual(AnyInstance.AnyLong, (long)dyn.int64);
            Assert.AreEqual(AnyInstance.AnyULong, (ulong)dyn.uint64);
            Assert.AreEqual(AnyInstance.AnySByte, (sbyte)dyn.@sbyte);
            Assert.AreEqual(AnyInstance.AnyByte, (byte)dyn.@byte);
            Assert.AreEqual(AnyInstance.AnyChar, (char)dyn.@char);
            Assert.AreEqual(AnyInstance.AnyDouble, (double)dyn.dbl);
            Assert.AreEqual(AnyInstance.AnyFloat, (float)dyn.flt);
            Assert.AreEqual(AnyInstance.AnyDecimal, (decimal)dyn.dec);
            Assert.AreEqual(AnyInstance.AnyString, (string)dyn.str);
            Assert.AreEqual(AnyInstance.AnyUri, (Uri)dyn.uri);
            Assert.AreEqual(AnyInstance.AnyGuid, (Guid)dyn.guid);
            Assert.AreEqual(AnyInstance.AnyDateTime, (DateTime)dyn.dateTime);
            Assert.AreEqual(AnyInstance.AnyDateTimeOffset, (DateTimeOffset)dyn.dateTimeOffset);
            Assert.AreSame(AnyInstance.AnyJsonArray, dyn.JsonArray);
            Assert.AreEqual(AnyInstance.AnyJsonPrimitive, dyn.JsonPrimitive);
            Assert.AreSame(AnyInstance.AnyJsonObject, dyn.JsonObject);

            ExceptionTestHelper.ExpectException<ArgumentException>(delegate { dyn.other = Console.Out; });
            ExceptionTestHelper.ExpectException<ArgumentException>(delegate { dyn.other = dyn.NonExistentProp; });
        }

        [TestMethod]
        public void NullTests()
        {
            dynamic dyn = new JsonObject();
            JsonObject jo = (JsonObject)dyn;

            dyn.@null = null;
            Assert.AreSame(dyn.@null, AnyInstance.DefaultJsonValue);

            jo["@null"] = null;
            Assert.IsNull(jo["@null"]);
        }

        [TestMethod]
        public void DynamicNotationTest()
        {
            bool boolValue;
            JsonValue jsonValue;

            Person person = Person.CreateSample();
            dynamic jo = JsonValueExtensions.CreateFrom(person);

            dynamic target = jo;
            Assert.AreEqual<int>(person.Age, target.Age.ReadAs<int>()); // JsonPrimitive
            Assert.AreEqual<string>(person.Address.ToString(), ((JsonObject)target.Address).ReadAsType<Address>().ToString()); // JsonObject

            target = jo.Address.City;  // JsonPrimitive
            Assert.IsNotNull(target);
            Assert.AreEqual<string>(target.ReadAs<string>(), person.Address.City);

            target = jo.Friends;  // JsonArray
            Assert.IsNotNull(target);
            jsonValue = target as JsonValue;
            Assert.AreEqual<int>(person.Friends.Count, jsonValue.ReadAsType<List<Person>>().Count);

            target = jo.Friends[1].Address.City;
            Assert.IsNotNull(target);
            Assert.AreEqual<string>(target.ReadAs<string>(), person.Address.City);

            target = jo.Address.NonExistentProp.NonExistentProp2; // JsonObject (default)
            Assert.IsNotNull(target);
            Assert.IsTrue(jo is JsonObject);
            Assert.IsFalse(target.TryReadAs<bool>(out boolValue));
            Assert.IsTrue(target.TryReadAs<JsonValue>(out jsonValue));
            Assert.AreSame(target, jsonValue);

            Assert.AreSame(jo.Address.NonExistent, AnyInstance.DefaultJsonValue);
            Assert.AreSame(jo.Friends[1000], AnyInstance.DefaultJsonValue);
            Assert.AreSame(jo.Age.NonExistentProp, AnyInstance.DefaultJsonValue);
            Assert.AreSame(jo.Friends.NonExistentProp, AnyInstance.DefaultJsonValue);
        }

        [TestMethod]
        public void PropertyAccessTest()
        {
            Person p = AnyInstance.AnyPerson;
            JsonObject jo = JsonValueExtensions.CreateFrom(p) as JsonObject;
            JsonArray ja = JsonValueExtensions.CreateFrom(p.Friends) as JsonArray;
            JsonPrimitive jp = AnyInstance.AnyJsonPrimitive;
            JsonValue jv = AnyInstance.DefaultJsonValue;

            dynamic jod = jo;
            dynamic jad = ja;
            dynamic jpd = jp;
            dynamic jvd = jv;

            Assert.AreEqual(jo.Count, jod.Count);
            Assert.AreEqual(jo.JsonType, jod.JsonType);
            Assert.AreEqual(jo.Keys.Count, jod.Keys.Count);
            Assert.AreEqual(jo.Values.Count, jod.Values.Count);
            Assert.AreEqual(p.Age, (int)jod.Age);
            Assert.AreEqual(p.Age, (int)jod["Age"]);
            Assert.AreEqual(p.Age, (int)jo["Age"]);
            Assert.AreEqual(p.Address.City, (string)jo["Address"]["City"]);
            Assert.AreEqual(p.Address.City, (string)jod["Address"]["City"]);
            Assert.AreEqual(p.Address.City, (string)jod.Address.City);

            Assert.AreEqual(p.Friends.Count, ja.Count);
            Assert.AreEqual(ja.Count, jad.Count);
            Assert.AreEqual(ja.IsReadOnly, jad.IsReadOnly);
            Assert.AreEqual(ja.JsonType, jad.JsonType);
            Assert.AreEqual(p.Friends[0].Age, (int)ja[0]["Age"]);
            Assert.AreEqual(p.Friends[0].Age, (int)jad[0].Age);

            Assert.AreEqual(jp.JsonType, jpd.JsonType);
        }

        [TestMethod]
        public void ConcatDynamicAssignmentTest()
        {
            string value = "MyValue";
            dynamic dynArray = JsonValue.Parse(AnyInstance.AnyJsonArray.ToString());
            dynamic dynObj = JsonValue.Parse(AnyInstance.AnyJsonObject.ToString());

            JsonValue target;
            
            target = dynArray[0] = dynArray[1] = dynArray[2] = value;
            Assert.AreEqual((string)target, value);
            Assert.AreEqual((string)dynArray[0], value);
            Assert.AreEqual((string)dynArray[1], value);
            Assert.AreEqual((string)dynArray[2], value);

            target = dynObj["key0"] = dynObj["key1"] = dynObj["key2"] = value;
            Assert.AreEqual((string)target, value);
            Assert.AreEqual((string)dynObj["key0"], value);
            Assert.AreEqual((string)dynObj["key1"], value);
            Assert.AreEqual((string)dynObj["key2"], value);

            foreach (KeyValuePair<string, JsonValue> pair in AnyInstance.AnyJsonObject)
            { 
                Assert.AreEqual<string>(AnyInstance.AnyJsonObject[pair.Key].ToString(), dynObj[pair.Key].ToString());
            }
        }

        [TestMethod]
        public void IndexConversionTest()
        {
            dynamic target = AnyInstance.AnyJsonArray;
            dynamic expected = AnyInstance.AnyJsonArray[0];
            dynamic result;

            dynamic[] zero_indexes = 
            {
                (short)0,
                (ushort)0,
                (byte)0,
                (sbyte)0,
                (char)0,
                (int)0
            };


            result = target[(short)0];
            Assert.AreSame(expected, result);
            result = target[(ushort)0];
            Assert.AreSame(expected, result);
            result = target[(byte)0];
            Assert.AreSame(expected, result);
            result = target[(sbyte)0];
            Assert.AreSame(expected, result);
            result = target[(char)0];
            Assert.AreSame(expected, result);

            foreach (dynamic zero_index in zero_indexes)
            {
                result = target[zero_index];
                Assert.AreSame(expected, result);
            }
        }

        [TestMethod]
        public void InvalidIndexTest()
        {
            object index1 = new object();
            bool index2 = true;
            Person index3 = AnyInstance.AnyPerson;
            JsonObject jo = AnyInstance.AnyJsonObject;

            dynamic target;
            object ret;

            JsonValue[] values = { AnyInstance.AnyJsonObject, AnyInstance.AnyJsonArray };

            foreach (JsonValue value in values)
            {
                target = value;

                ExceptionTestHelper.ExpectException<ArgumentException>(delegate { ret = target[index1]; }, string.Format(InvalidIndexType, index1.GetType().FullName));
                ExceptionTestHelper.ExpectException<ArgumentException>(delegate { ret = target[index2]; }, string.Format(InvalidIndexType, index2.GetType().FullName));
                ExceptionTestHelper.ExpectException<ArgumentException>(delegate { ret = target[index3]; }, string.Format(InvalidIndexType, index3.GetType().FullName));
                ExceptionTestHelper.ExpectException<ArgumentException>(delegate { ret = target[null]; }, NonSingleNonNullIndexNotSupported);

                ExceptionTestHelper.ExpectException<ArgumentException>(delegate { ret = target[0, 1]; }, NonSingleNonNullIndexNotSupported);
                ExceptionTestHelper.ExpectException<ArgumentException>(delegate { ret = target["key1", "key2"]; }, NonSingleNonNullIndexNotSupported);

                ExceptionTestHelper.ExpectException<ArgumentException>(delegate { ret = target[true]; }, string.Format(InvalidIndexType, true.GetType().FullName));

                ExceptionTestHelper.ExpectException<ArgumentException>(delegate { target[index1] = jo; }, string.Format(InvalidIndexType, index1.GetType().FullName));
                ExceptionTestHelper.ExpectException<ArgumentException>(delegate { target[index2] = jo; }, string.Format(InvalidIndexType, index2.GetType().FullName));
                ExceptionTestHelper.ExpectException<ArgumentException>(delegate { target[index3] = jo; }, string.Format(InvalidIndexType, index3.GetType().FullName));
                ExceptionTestHelper.ExpectException<ArgumentException>(delegate { target[null] = jo; }, NonSingleNonNullIndexNotSupported);

                ExceptionTestHelper.ExpectException<ArgumentException>(delegate { target[0, 1] = jo; }, NonSingleNonNullIndexNotSupported);
                ExceptionTestHelper.ExpectException<ArgumentException>(delegate { target["key1", "key2"] = jo; }, NonSingleNonNullIndexNotSupported);

                ExceptionTestHelper.ExpectException<ArgumentException>(delegate { target[true] = jo; }, string.Format(InvalidIndexType, true.GetType().FullName));
            }
        }

        [TestMethod]
        public void ImplicitDataConvertionTest()
        {
            dynamic target;
            
            target = (JsonValue)"true";
            Assert.IsTrue(target == true);

            if (!target)
            {
                Assert.Fail("bool conversion faild");
            }

            target = (JsonValue)"false";
            Assert.IsFalse(target != false);

            if (target)
            {
                Assert.Fail("bool conversion faild");
            }

            target = (JsonValue)AnyInstance.AnyGuid;
            Assert.IsTrue(target == AnyInstance.AnyGuid);
            Assert.IsTrue(target == AnyInstance.AnyGuid.ToString());

            target = (JsonValue)AnyInstance.AnyChar;
            Assert.IsTrue(target == AnyInstance.AnyChar);
            Assert.IsTrue(target == AnyInstance.AnyChar.ToString());

            target = (JsonValue)AnyInstance.AnyInt;
            Assert.IsTrue(target == AnyInstance.AnyInt);
            Assert.IsTrue(target == AnyInstance.AnyInt.ToString());
        }

        [TestMethod]
        public void DynamicCoerceOperationsSimpleTest()
        {
            JsonValue jv = new JsonObject { { "one", 1 }, { "one_point_two", 1.2 } };
            dynamic actual, expected;

            char charValue = 'A';
            byte byteValue = 0x02;
            sbyte sbyteValue = 0x10;
            ushort ushortValue = 100;
            short shortValue = -10;
            int intValue = -120;
            uint uintValue = 200;
            long longValue = -1000;
            ulong ulongValue = 2000;
            float floatValue = -20.10f;
            double doubleValue = -1200.50;

            double[] values = 
            {
                charValue, byteValue, sbyteValue, ushortValue, shortValue, intValue, uintValue, longValue, ulongValue, floatValue, doubleValue
            };

            dynamic dyn = jv;

            Assert.IsTrue(dyn.one != charValue);
            Assert.IsTrue(dyn.one < byteValue);
            Assert.IsFalse(dyn.one_point_two > sbyteValue);
            Assert.IsFalse(dyn.one_point_two >= ushortValue);
            Assert.IsTrue(dyn.one > shortValue);
            Assert.IsFalse(dyn.one_point_two < intValue);
            Assert.IsFalse(dyn.one_point_two == uintValue);
            Assert.IsFalse(dyn.one_point_two == longValue);
            Assert.IsFalse(dyn.one_point_two == ulongValue);
            Assert.IsTrue(dyn.one > floatValue);
            Assert.IsFalse(dyn.one <= doubleValue);

            foreach (double value in values)
            {
                Assert.IsTrue(dyn.one != value);
                Assert.IsFalse(dyn.one == value);

                expected = dyn.one.ReadAs(value.GetType()) + value;
                actual = dyn.one + value;
                Assert.AreEqual(expected, actual);

                expected = dyn.one.ReadAs(value.GetType()) - value;
                actual = dyn.one - value;
                Assert.AreEqual(expected, actual);

                expected = dyn.one.ReadAs(value.GetType()) * value;
                actual = dyn.one * value;
                Assert.AreEqual(expected, actual);

                expected = dyn.one.ReadAs(value.GetType()) / value;
                actual = dyn.one / value;
                Assert.AreEqual(expected, actual);

                expected = dyn.one.ReadAs(value.GetType()) % value;
                actual = dyn.one % value;
                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void DynamicCoerceOperationsTest()
        {
            object ret = null;
            Dictionary<Type, JsonValue> jsonValueTypeTable;
            Dictionary<Type, List<Type>> convertionTable;

            InitializeNumericConverionTables(out jsonValueTypeTable, out convertionTable);

            foreach (KeyValuePair<Type, JsonValue> jsonValueType in jsonValueTypeTable)
            {
                Type type = jsonValueType.Key;
                dynamic target = jsonValueType.Value;

                foreach (KeyValuePair<Type, List<Type>> typeInfo in convertionTable)
                {
                    Type convertType = typeInfo.Key;

                    // DISABLED: 197545 - remove this condition when the bug is fixed.
                    if (type == typeof(char))
                    {
                        continue;
                    }

                    if (type != convertType && (convertionTable[type].Contains(convertType) || convertionTable[convertType].Contains(type)))
                    {
                        try
                        {
                            switch (Type.GetTypeCode(convertType))
                            {
                                case TypeCode.SByte:
                                    ret = target + AnyInstance.AnySByte;
                                    break;
                                case TypeCode.Byte:
                                    ret = target - AnyInstance.AnyByte;
                                    break;
                                case TypeCode.Int16:
                                    ret = target / AnyInstance.AnyShort;
                                    break;
                                case TypeCode.UInt16:
                                    ret = target * AnyInstance.AnyUShort;
                                    break;
                                case TypeCode.Int32:
                                    ret = target + AnyInstance.AnyInt;
                                    break;
                                case TypeCode.UInt32:
                                    ret = target - AnyInstance.AnyUInt;
                                    break;
                                case TypeCode.Int64:
                                    ret = target * AnyInstance.AnyLong;
                                    break;
                                case TypeCode.UInt64:
                                    ret = target / AnyInstance.AnyULong;
                                    break;
                                case TypeCode.Char:
                                    ret = target + AnyInstance.AnyChar;
                                    break;
                                case TypeCode.Single:
                                    ret = target - AnyInstance.AnyFloat;
                                    break;
                                case TypeCode.Double:
                                    ret = target * AnyInstance.AnyDouble;
                                    break;
                                case TypeCode.Decimal:
                                    ret = target / AnyInstance.AnyDecimal;
                                    break;
                            }
                        }
                        catch (OverflowException ex)
                        {
                            Console.WriteLine("Caught overflow exception with message: '{0}' - this is ok for this test.", ex.Message);
                        }
                        catch (Exception ex)
                        {
                            Assert.Fail("{0}: {1}", ex.GetType().FullName, ex.Message);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void InvalidDynamicCoerceOperations()
        {
            object ret;
            Dictionary<Type, JsonValue> jsonValueTypeTable;
            Dictionary<Type, List<Type>> convertionTable;

            InitializeNumericConverionTables(out jsonValueTypeTable, out convertionTable);

            foreach (KeyValuePair<Type, JsonValue> jsonValueType in jsonValueTypeTable)
            {
                Type type = jsonValueType.Key;
                dynamic target = jsonValueType.Value;

                foreach (KeyValuePair<Type, List<Type>> typeInfo in convertionTable)
                {
                    Type convertType = typeInfo.Key;

                    if (type != convertType && !convertionTable[type].Contains(convertType) && !convertionTable[convertType].Contains(type))
                    {
                        switch (Type.GetTypeCode(convertType))
                        {
                            case TypeCode.SByte:
                                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { ret = target + AnyInstance.AnySByte; });
                                break;
                            case TypeCode.Byte:
                                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { ret = target - AnyInstance.AnyByte; });
                                break;
                            case TypeCode.Int16:
                                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { ret = target * AnyInstance.AnyShort; });
                                break;
                            case TypeCode.UInt16:
                                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { ret = target / AnyInstance.AnyUShort; });
                                break;
                            case TypeCode.Int32:
                                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { ret = target + AnyInstance.AnyInt; });
                                break;
                            case TypeCode.UInt32:
                                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { ret = target - AnyInstance.AnyUInt; });
                                break;
                            case TypeCode.Int64:
                                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { ret = target * AnyInstance.AnyLong; });
                                break;
                            case TypeCode.UInt64:
                                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { ret = target / AnyInstance.AnyULong; });
                                break;
                            case TypeCode.Char:
                                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { ret = target + AnyInstance.AnyChar; });
                                break;
                            case TypeCode.Single:
                                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { ret = target - AnyInstance.AnyFloat; });
                                break;
                            case TypeCode.Double:
                                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { ret = target * AnyInstance.AnyDouble; });
                                break;
                            case TypeCode.Decimal:
                                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { ret = target / AnyInstance.AnyDecimal; });
                                break;
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void InvalidCastingTests()
        {
            dynamic dyn;
            string value = "NameValue";

            dyn = AnyInstance.AnyJsonPrimitive;
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { dyn.name = value; });

            dyn = AnyInstance.AnyJsonArray;
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { dyn.name = value; });

            dyn = new JsonObject(AnyInstance.AnyJsonObject);
            dyn.name = value;
            Assert.AreEqual((string)dyn.name, value);

            dyn = AnyInstance.DefaultJsonValue;
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { dyn.name = value; });
        }

        [TestMethod]
        public void CastTests()
        {
            dynamic dyn = JsonValueExtensions.CreateFrom(AnyInstance.AnyPerson) as JsonObject;
            string city = dyn.Address.City;

            Assert.AreEqual<string>(AnyInstance.AnyPerson.Address.City, dyn.Address.City.ReadAs<string>());
            Assert.AreEqual<string>(AnyInstance.AnyPerson.Address.City, city);

            JsonValue[] values = 
            {
                AnyInstance.AnyInt,
                AnyInstance.AnyString,
                AnyInstance.AnyDateTime,
                AnyInstance.AnyJsonObject,
                AnyInstance.AnyJsonArray,
                AnyInstance.DefaultJsonValue 
            };

            int loopCount = 2;
            bool explicitCast = true;

            while (loopCount > 0)
            {
                loopCount--;

                foreach (JsonValue jv in values)
                {
                    EvaluateNoExceptions<JsonValue>(null, explicitCast);
                    EvaluateNoExceptions<JsonValue>(jv, explicitCast);
                    EvaluateNoExceptions<object>(jv, explicitCast);
                    EvaluateNoExceptions<IDynamicMetaObjectProvider>(jv, explicitCast);
                    EvaluateNoExceptions<IEnumerable<KeyValuePair<string, JsonValue>>>(jv, explicitCast);
                    EvaluateNoExceptions<string>(null, explicitCast);
                    
                    EvaluateExpectExceptions<int>(null, explicitCast);
                    EvaluateExpectExceptions<Person>(jv, explicitCast);
                    EvaluateExpectExceptions<Exception>(jv, explicitCast);

                    EvaluateIgnoreExceptions<JsonObject>(jv, explicitCast);
                    EvaluateIgnoreExceptions<int>(jv, explicitCast);
                    EvaluateIgnoreExceptions<string>(jv, explicitCast);
                    EvaluateIgnoreExceptions<DateTime>(jv, explicitCast);
                    EvaluateIgnoreExceptions<JsonArray>(jv, explicitCast);
                    EvaluateIgnoreExceptions<JsonPrimitive>(jv, explicitCast);
                }

                explicitCast = false;
            }

            EvaluateNoExceptions<IDictionary<string, JsonValue>>(AnyInstance.AnyJsonObject, false);
            EvaluateNoExceptions<IList<JsonValue>>(AnyInstance.AnyJsonArray, false);
        }

        static void EvaluateNoExceptions<T>(JsonValue value, bool cast)
        {
            Evaluate<T>(value, cast, false, true);
        }

        static void EvaluateExpectExceptions<T>(JsonValue value, bool cast)
        {
            Evaluate<T>(value, cast, true, true);
        }

        static void EvaluateIgnoreExceptions<T>(JsonValue value, bool cast)
        {
            Evaluate<T>(value, cast, true, false);
        }

        static void Evaluate<T>(JsonValue value, bool cast, bool throwExpected, bool assertExceptions)
        {
            T ret2;
            object obj = null;
            bool exceptionThrown = false;
            string retstr2, retstr1;

            Console.WriteLine("Test info: expected:[{0}], explicitCast type:[{1}]", value, typeof(T));

            try
            {
                if (typeof(int) == typeof(T))
                {
                    obj = ((int)value);
                }
                else if (typeof(string) == typeof(T))
                {
                    obj = ((string)value);
                }
                else if (typeof(DateTime) == typeof(T))
                {
                    obj = ((DateTime)value);
                }
                else if (typeof(IList<JsonValue>) == typeof(T))
                {
                    obj = (IList<JsonValue>)value;
                }
                else if (typeof(IDictionary<string, JsonValue>) == typeof(T))
                {
                    obj = (IDictionary<string, JsonValue>)value;
                }
                else if (typeof(JsonValue) == typeof(T))
                {
                    obj = (JsonValue)value;
                }
                else if (typeof(JsonObject) == typeof(T))
                {
                    obj = (JsonObject)value;
                }
                else if (typeof(JsonArray) == typeof(T))
                {
                    obj = (JsonArray)value;
                }
                else if (typeof(JsonPrimitive) == typeof(T))
                {
                    obj = (JsonPrimitive)value;
                }
                else
                {
                    obj = (T)(object)value;
                }

                retstr1 = obj == null ? "null" : obj.ToString();
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                retstr1 = ex.Message;
            }

            if (assertExceptions)
            {
                Assert.AreEqual<bool>(throwExpected, exceptionThrown, "Exception thrown: " + retstr1);
            }

            exceptionThrown = false;

            try
            {
                dynamic dyn = value as dynamic;
                if (cast)
                {
                    ret2 = (T)dyn;
                }
                else
                {
                    ret2 = dyn;
                }
                retstr2 = ret2 != null ? ret2.ToString() : "null";
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                retstr2 = ex.Message;
            }

            if (assertExceptions)
            {
                Assert.AreEqual<bool>(throwExpected, exceptionThrown, "Exception thrown: " + retstr2);
            }

            // fixup string
            retstr1 = retstr1.Replace("\'Person\'", string.Format("\'{0}\'", typeof(Person).FullName));
            if (retstr1.EndsWith(".")) retstr1 = retstr1.Substring(0, retstr1.Length - 1);

            // fixup string
            retstr2 = retstr2.Replace("\'string\'", string.Format("\'{0}\'", typeof(string).FullName));
            retstr2 = retstr2.Replace("\'int\'", string.Format("\'{0}\'", typeof(int).FullName));
            if (retstr2.EndsWith(".")) retstr2 = retstr2.Substring(0, retstr2.Length - 1);

            Assert.AreEqual<string>(retstr1, retstr2);
        }

        static void InitializeNumericConverionTables(out Dictionary<Type, JsonValue> jsonValueTypeTable, out Dictionary<Type, List<Type>> convertionTable)
        {
            Type typeofSbyte = typeof(sbyte);
            Type typeofByte = typeof(byte);
            Type typeofShort = typeof(short);
            Type typeofUShort = typeof(ushort);
            Type typeofInt = typeof(int);
            Type typeofUInt = typeof(uint);
            Type typeofLong = typeof(long);
            Type typeofULong = typeof(ulong);
            Type typeofChar = typeof(char);
            Type typeofFloat = typeof(float);
            Type typeofDouble = typeof(double);
            Type typeofDecimal = typeof(decimal);

            jsonValueTypeTable = new Dictionary<Type, JsonValue>()
            {
                { typeofSbyte, AnyInstance.AnySByte },
                { typeofByte, AnyInstance.AnyByte },
                { typeofShort, AnyInstance.AnyShort },
                { typeofUShort, AnyInstance.AnyUShort },
                { typeofInt, AnyInstance.AnyInt }, 
                { typeofUInt, AnyInstance.AnyUInt },
                { typeofLong, AnyInstance.AnyLong },
                { typeofULong, AnyInstance.AnyULong },
                { typeofChar, AnyInstance.AnyChar },
                { typeofFloat, AnyInstance.AnyFloat },
                { typeofDouble, AnyInstance.AnyDouble },
                { typeofDecimal, AnyInstance.AnyDecimal }
            };

            convertionTable = new Dictionary<Type, List<Type>>()
            {
                ///     sbyte   -> short, int, long, float, double, or decimal
                ///     byte    -> short, ushort, int, uint, long, ulong, float, double, or decimal
                ///     short   -> int, long, float, double, or decimal
                ///     ushort  -> int, uint, long, ulong, float, double, or decimal
                ///     int     -> long, float, double, or decimal
                ///     uint    -> long, ulong, float, double, or decimal
                ///     long    -> float, double, or decimal
                ///     char    -> ushort, int, uint, long, ulong, float, double, or decimal
                ///     float   -> double
                ///     ulong   -> float, double, or decimal
                ///     double  ->
                ///     decimal ->
                ///     
                { typeofSbyte, new List<Type>() { typeofShort, typeofInt, typeofLong, typeofFloat, typeofDouble, typeofDecimal } },
                { typeofByte, new List<Type>() { typeofShort, typeofUShort, typeofInt, typeofUInt, typeofLong, typeofULong, typeofFloat, typeofDouble, typeofDecimal }},
                { typeofShort, new List<Type>() { typeofInt, typeofLong, typeofFloat, typeofDouble, typeofDecimal }},
                { typeofUShort, new List<Type>() { typeofInt, typeofUInt, typeofLong, typeofULong, typeofFloat, typeofDouble, typeofDecimal }},
                { typeofInt  , new List<Type>() { typeofLong, typeofFloat, typeofDouble, typeofDecimal }},
                { typeofUInt , new List<Type>() { typeofLong, typeofULong, typeofFloat, typeofDouble, typeofDecimal }},
                { typeofLong , new List<Type>() { typeofFloat, typeofDouble, typeofDecimal }},
                { typeofULong, new List<Type>() { typeofFloat, typeofDouble, typeofDecimal }},
                { typeofChar , new List<Type>() { typeofUShort, typeofInt, typeofUInt, typeofLong, typeofULong, typeofFloat, typeofDouble, typeofDecimal }},
                { typeofFloat, new List<Type>() { typeofDouble }},
                { typeofDouble, new List<Type>() { }}, 
                { typeofDecimal, new List<Type>() { }}
            };
        }
    }
}
