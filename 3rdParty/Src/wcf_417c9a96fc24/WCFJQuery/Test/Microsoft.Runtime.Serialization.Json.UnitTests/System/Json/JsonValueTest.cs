namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Json;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Xml;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class JsonValueTest
    {
        const string IndexerNotSupportedOnJsonType = "'{0}' type indexer is not supported on JsonValue of 'JsonType.{1}' type.";
        const string InvalidIndexType = "Invalid '{0}' index type; only 'System.String' and non-negative 'System.Int32' types are supported.\r\nParameter name: indexes";

        [TestMethod]
        public void ContainsKeyTest()
        {
            JsonObject target = new JsonObject { { AnyInstance.AnyString, AnyInstance.AnyString } };
            Assert.IsTrue(target.ContainsKey(AnyInstance.AnyString));
        }

        [TestMethod]
        public void LoadTest()
        {
            string json = "{\"a\":123,\"b\":[false,null,12.34]}";
            foreach (bool useLoadTextReader in new bool[] { false, true })
            {
                JsonValue jv;
                if (useLoadTextReader)
                {
                    using (StringReader sr = new StringReader(json))
                    {
                        jv = JsonValue.Load(sr);
                    }
                }
                else
                {
                    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    {
                        jv = JsonValue.Load(ms);
                    }
                }

                Assert.AreEqual(json, jv.ToString());
            }

            ExceptionTestHelper.ExpectException<ArgumentNullException>(() => JsonValue.Load((Stream)null));
            ExceptionTestHelper.ExpectException<ArgumentNullException>(() => JsonValue.Load((TextReader)null));
        }

        [TestMethod]
        public void LoadFromXmlJsonReaderTest()
        {
            string json = "{\"a\":123,\"b\":[false,null,12.34]}";
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            JsonValue jv;
            using (XmlDictionaryReader xdr = JsonReaderWriterFactory.CreateJsonReader(jsonBytes, XmlDictionaryReaderQuotas.Max))
            {
                jv = JsonValueExtensions.Load(xdr);
            }

            Assert.AreEqual(json, jv.ToString());

            ExceptionTestHelper.ExpectException<ArgumentNullException>(() => JsonValueExtensions.Load((XmlDictionaryReader)null));
        }

        [TestMethod]
        public void SaveToXmlJsonWriterTest()
        {
            string json = "{\"a\":123,\"b\":[false,null,12.34]}";
            JsonValue jv = JsonValue.Parse(json);
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlDictionaryWriter xdw = JsonReaderWriterFactory.CreateJsonWriter(ms))
                {
                    jv.Save(xdw);
                    xdw.Flush();
                    string saved = Encoding.UTF8.GetString(ms.ToArray());
                    Assert.AreEqual(json, saved);
                }
            }

            ExceptionTestHelper.ExpectException<ArgumentNullException>(() => jv.Save((XmlDictionaryWriter)null));
        }

        [TestMethod]
        public void SaveToXmlWriterTest()
        {
            string json = "{\"a\":123,\"b\":[false,null,12.34]}";
            string expectedJxml = "<root type=\"object\"><a type=\"number\">123</a><b type=\"array\"><item type=\"boolean\">false</item><item type=\"null\"/><item type=\"number\">12.34</item></b></root>";
            JsonValue jv = JsonValue.Parse(json);
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlDictionaryWriter xdw = XmlDictionaryWriter.CreateTextWriter(ms))
                {
                    jv.Save(xdw);
                    xdw.Flush();
                    string saved = Encoding.UTF8.GetString(ms.ToArray());
                    Assert.AreEqual(expectedJxml, saved);
                }
            }

            ExceptionTestHelper.ExpectException<ArgumentNullException>(() => jv.Save((XmlDictionaryWriter)null));
        }

        [TestMethod]
        public void LoadFromXmlTest()
        {
            string json = "{\"a\":123,\"b\":[false,null,12.34]}";
            string xml = "<root type=\"object\"><a type=\"number\">123</a><b type=\"array\"><item type=\"boolean\">false</item><item type=\"null\"/><item type=\"number\">12.34</item></b></root>";
            using (XmlDictionaryReader xdr = XmlDictionaryReader.CreateTextReader(Encoding.UTF8.GetBytes(xml), XmlDictionaryReaderQuotas.Max))
            {
                JsonValue jv = JsonValueExtensions.Load(xdr);
                Assert.AreEqual(json, jv.ToString());
            }
        }

        [TestMethod]
        public void ParseTest()
        {
            string json = "{\"a\":123,\"b\":[false,null,12.34],\"with space\":\"hello\",\"\":\"empty key\",\"withTypeHint\":{\"__type\":\"typeHint\"}}";
            JsonValue jv = JsonValue.Parse(json);
            Assert.AreEqual(json, jv.ToString());

            ExceptionTestHelper.ExpectException<ArgumentNullException>(() => JsonValue.Parse(null));
            ExceptionTestHelper.ExpectException<ArgumentException>(() => JsonValue.Parse(""));
        }

        [TestMethod]
        public void ParseNumbersTest()
        {
            string json = "{\"long\":12345678901234,\"zero\":0.0,\"double\":1.23e+200}";
            string expectedJson = "{\"long\":12345678901234,\"zero\":0,\"double\":1.23E+200}";
            JsonValue jv = JsonValue.Parse(json);

            Assert.AreEqual(expectedJson, jv.ToString());
            Assert.AreEqual(12345678901234L, (long)jv["long"]);
            Assert.AreEqual<double>(0, jv["zero"].ReadAs<double>());
            Assert.AreEqual<double>(1.23e200, jv["double"].ReadAs<double>());

            ExceptionTestHelper.ExpectException<ArgumentException>(() => JsonValue.Parse("[1.2e+400]"));
        }

        [TestMethod]
        public void ReadAsTest()
        {
            JsonValue target = new JsonPrimitive(AnyInstance.AnyInt);
            Assert.AreEqual(AnyInstance.AnyInt.ToString(CultureInfo.InvariantCulture), target.ReadAs(typeof(string)));
            Assert.AreEqual(AnyInstance.AnyInt.ToString(CultureInfo.InvariantCulture), target.ReadAs<string>());
            
            object value;
            double dblValue;

            Assert.IsTrue(target.TryReadAs(typeof(double), out value));
            Assert.IsTrue(target.TryReadAs<double>(out dblValue));
            Assert.AreEqual(Convert.ToDouble(AnyInstance.AnyInt, CultureInfo.InvariantCulture), (double) value);
            Assert.AreEqual(Convert.ToDouble(AnyInstance.AnyInt, CultureInfo.InvariantCulture), dblValue);

            Assert.IsFalse(target.TryReadAs(typeof(Guid), out value), "TryReadAs should have failed to read a double as a Guid");
            Assert.IsNull(value, "expected from failed TryReadAs should be null!");
        }

        [TestMethod]
        public void SaveTest()
        {
            JsonObject jo = new JsonObject
            {
                { "first", 1 },
                { "second", 2 },
            };
            JsonValue jv = new JsonArray(123, null, jo);
            string expectedJson = "[123,null,{\"first\":1,\"second\":2}]";

            foreach (bool useStream in new bool[] { false, true })
            {
                string json;
                if (useStream)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        jv.Save(ms);
                        json = Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    using (TextWriter writer = new StringWriter(sb))
                    {
                        jv.Save(writer);
                        json = sb.ToString();
                    }
                }

                Assert.AreEqual(expectedJson, json);
            }

            JsonValue target = AnyInstance.DefaultJsonValue;
            using (MemoryStream ms = new MemoryStream())
            {
                ExceptionTestHelper.ExpectException<InvalidOperationException>(() => target.Save(ms));
            }
        }

        [TestMethod]
        public void GetEnumeratorTest()
        {
            IEnumerable target = new JsonArray(AnyInstance.AnyGuid);
            IEnumerator enumerator = target.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(AnyInstance.AnyGuid, (Guid)(JsonValue)enumerator.Current);
            Assert.IsFalse(enumerator.MoveNext());

            target = new JsonObject();
            enumerator = target.GetEnumerator();
            Assert.IsFalse(enumerator.MoveNext());
        }

        [TestMethod]
        public void IEnumerableTest()
        {
            JsonValue target = AnyInstance.AnyJsonArray;

            // Test IEnumerable<JsonValue> on JsonArray
            int count = 0;

            foreach (JsonValue value in ((JsonArray)target))
            {
                Assert.AreSame(target[count], value);
                count++;
            }

            Assert.AreEqual<int>(target.Count, count);

            // Test IEnumerable<KeyValuePair<string, JsonValue>> on JsonValue
            count = 0;
            foreach (KeyValuePair<string, JsonValue> pair in target)
            {
                int index = Int32.Parse(pair.Key);
                Assert.AreEqual(count, index);
                Assert.AreSame(target[index], pair.Value);
                count++;
            }

            Assert.AreEqual<int>(target.Count, count);

            target = AnyInstance.AnyJsonObject;
            count = 0;
            foreach (KeyValuePair<string, JsonValue> pair in target)
            {
                count++;
                Assert.AreSame(AnyInstance.AnyJsonObject[pair.Key], pair.Value);
            }

            Assert.AreEqual<int>(AnyInstance.AnyJsonObject.Count, count);
        }

        [TestMethod]
        public void GetJsonPrimitiveEnumeratorTest()
        {
            JsonValue target = AnyInstance.AnyJsonPrimitive;
            IEnumerator<KeyValuePair<string, JsonValue>> enumerator = target.GetEnumerator();
            Assert.IsFalse(enumerator.MoveNext());
        }

        [TestMethod]
        public void GetJsonUndefinedEnumeratorTest()
        {
            JsonValue target = AnyInstance.AnyJsonPrimitive.AsDynamic().IDontExist;
            IEnumerator<KeyValuePair<string, JsonValue>> enumerator = target.GetEnumerator();
            Assert.IsFalse(enumerator.MoveNext());
        }

        [TestMethod]
        public void ToStringTest()
        {
            JsonObject jo = new JsonObject
            {
                { "first", 1 },
                { "second", 2 },
                { "third", new JsonObject { { "inner_one", 4 }, { "", null }, { "inner_3", "" } } },
                { "fourth", new JsonArray { "Item1", 2, false } },
                { "fifth", null }
            };
            JsonValue jv = new JsonArray(123, null, jo);
            string expectedJson = "[123,null,{\"first\":1,\"second\":2,\"third\":{\"inner_one\":4,\"\":null,\"inner_3\":\"\"},\"fourth\":[\"Item1\",2,false],\"fifth\":null}]";
            Assert.AreEqual(expectedJson, jv.ToString());
        }

        [TestMethod]
        public void CastTests()
        {
            int value = 10;
            JsonValue target = new JsonPrimitive(value);

            int v1 = JsonValue.CastValue<int>(target);
            Assert.AreEqual<int>(value, v1);
            v1 = (int)target;
            Assert.AreEqual<int>(value, v1);

            long v2 = JsonValue.CastValue<long>(target);
            Assert.AreEqual<long>(value, v2);
            v2 = (long)target;
            Assert.AreEqual<long>(value, v2);

            string s = JsonValue.CastValue<string>(target);
            Assert.AreEqual<string>(value.ToString(), s);
            s = (string)target;
            Assert.AreEqual<string>(value.ToString(), s);

            object obj = JsonValue.CastValue<object>(target);
            Assert.AreEqual(target, obj);
            obj = (object)target;
            Assert.AreEqual(target, obj);

            object nill = JsonValue.CastValue<object>(null);
            Assert.IsNull(nill);
            
            dynamic dyn = target;
            JsonValue defaultJv = dyn.IamDefault;
            nill = JsonValue.CastValue<string>(defaultJv);
            Assert.IsNull(nill);
            nill = (string)defaultJv;
            Assert.IsNull(nill);

            obj = JsonValue.CastValue<object>(defaultJv);
            Assert.AreSame(defaultJv, obj);
            obj = (object)defaultJv;
            Assert.AreSame(defaultJv, obj);

            JsonValue jv = JsonValue.CastValue<JsonValue>(target);
            Assert.AreEqual<JsonValue>(target, jv);

            jv = JsonValue.CastValue<JsonValue>(defaultJv);
            Assert.AreEqual<JsonValue>(defaultJv, jv);

            jv = JsonValue.CastValue<JsonPrimitive>(target);
            Assert.AreEqual<JsonValue>(target, jv);

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { int i = JsonValue.CastValue<int>(null); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { int i = JsonValue.CastValue<int>(defaultJv); });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { int i = JsonValue.CastValue<char>(target); });
        }

        [TestMethod]
        public void CastingTests()
        {
            JsonValue target = new JsonPrimitive(AnyInstance.AnyInt);

            Assert.AreEqual(AnyInstance.AnyInt.ToString(CultureInfo.InvariantCulture), (string)target);
            Assert.AreEqual(Convert.ToDouble(AnyInstance.AnyInt, CultureInfo.InvariantCulture), (double)target);

            Assert.AreEqual(AnyInstance.AnyString, (string)(JsonValue)AnyInstance.AnyString);
            Assert.AreEqual(AnyInstance.AnyChar, (char)(JsonValue)AnyInstance.AnyChar);
            Assert.AreEqual(AnyInstance.AnyUri, (Uri)(JsonValue)AnyInstance.AnyUri);
            Assert.AreEqual(AnyInstance.AnyGuid, (Guid)(JsonValue)AnyInstance.AnyGuid);
            Assert.AreEqual(AnyInstance.AnyDateTime, (DateTime)(JsonValue)AnyInstance.AnyDateTime);
            Assert.AreEqual(AnyInstance.AnyDateTimeOffset, (DateTimeOffset)(JsonValue)AnyInstance.AnyDateTimeOffset);
            Assert.AreEqual(AnyInstance.AnyBool, (bool)(JsonValue)AnyInstance.AnyBool);
            Assert.AreEqual(AnyInstance.AnyByte, (byte)(JsonValue)AnyInstance.AnyByte);
            Assert.AreEqual(AnyInstance.AnyShort, (short)(JsonValue)AnyInstance.AnyShort);
            Assert.AreEqual(AnyInstance.AnyInt, (int)(JsonValue)AnyInstance.AnyInt);
            Assert.AreEqual(AnyInstance.AnyLong, (long)(JsonValue)AnyInstance.AnyLong);
            Assert.AreEqual(AnyInstance.AnySByte, (sbyte)(JsonValue)AnyInstance.AnySByte);
            Assert.AreEqual(AnyInstance.AnyUShort, (ushort)(JsonValue)AnyInstance.AnyUShort);
            Assert.AreEqual(AnyInstance.AnyUInt, (uint)(JsonValue)AnyInstance.AnyUInt);
            Assert.AreEqual(AnyInstance.AnyULong, (ulong)(JsonValue)AnyInstance.AnyULong);
            Assert.AreEqual(AnyInstance.AnyDecimal, (decimal)(JsonValue)AnyInstance.AnyDecimal);
            Assert.AreEqual(AnyInstance.AnyFloat, (float)(JsonValue)AnyInstance.AnyFloat);
            Assert.AreEqual(AnyInstance.AnyDouble, (double)(JsonValue)AnyInstance.AnyDouble);

            Uri uri = null;
            string str = null;

            JsonValue jv = uri;
            Assert.IsNull(jv);
            uri = (Uri)jv;
            Assert.IsNull(uri);

            jv = str;
            Assert.IsNull(jv);
            str = (string)jv;
            Assert.IsNull(str);

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var s = (string)AnyInstance.AnyJsonArray; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var s = (string)AnyInstance.AnyJsonObject; });
        }

        [TestMethod]
        public void InvalidCastTest()
        {
            JsonValue nullValue = (JsonValue)null;
            JsonValue strValue = new JsonPrimitive(AnyInstance.AnyString);
            JsonValue boolValue = new JsonPrimitive(AnyInstance.AnyBool);
            JsonValue intValue = new JsonPrimitive(AnyInstance.AnyInt);

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (double)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (double)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (double)boolValue; });
            Assert.AreEqual<double>(AnyInstance.AnyInt, (double)intValue);

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (float)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (float)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (float)boolValue; });
            Assert.AreEqual<float>(AnyInstance.AnyInt, (float)intValue);

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (decimal)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (decimal)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (decimal)boolValue; });
            Assert.AreEqual<decimal>(AnyInstance.AnyInt, (decimal)intValue);

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (long)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (long)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (long)boolValue; });
            Assert.AreEqual<long>(AnyInstance.AnyInt, (long)intValue);

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (ulong)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (ulong)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (ulong)boolValue; });
            Assert.AreEqual<ulong>(AnyInstance.AnyInt, (ulong)intValue);

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (int)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (int)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (int)boolValue; });
            Assert.AreEqual<int>(AnyInstance.AnyInt, (int)intValue);

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (uint)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (uint)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (uint)boolValue; });
            Assert.AreEqual<uint>(AnyInstance.AnyInt, (uint)intValue);

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (short)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (short)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (short)boolValue; });

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (ushort)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (ushort)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (ushort)boolValue; });

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (sbyte)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (sbyte)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (sbyte)boolValue; });

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (byte)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (byte)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (byte)boolValue; });

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (Guid)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (Guid)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (Guid)boolValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (Guid)intValue; });

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (DateTime)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (DateTime)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (DateTime)boolValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (DateTime)intValue; });

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (char)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (char)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (char)boolValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (char)intValue; });

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (DateTimeOffset)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (DateTimeOffset)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (DateTimeOffset)boolValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (DateTimeOffset)intValue; });

            Assert.IsNull((Uri)nullValue);
            Assert.AreEqual(((Uri)strValue).ToString(), (string)strValue);
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (Uri)boolValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (Uri)intValue; });

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (bool)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (bool)strValue; });
            Assert.AreEqual(AnyInstance.AnyBool, (bool)boolValue);
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (bool)intValue; });

            Assert.AreEqual(null, (string)nullValue);
            Assert.AreEqual(AnyInstance.AnyString, (string)strValue);
            Assert.AreEqual(AnyInstance.AnyBool.ToString().ToLowerInvariant(), ((string)boolValue).ToLowerInvariant());
            Assert.AreEqual(AnyInstance.AnyInt.ToString(CultureInfo.InvariantCulture), (string)intValue);

            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (int)nullValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (int)strValue; });
            ExceptionTestHelper.ExpectException<InvalidCastException>(delegate { var v = (int)boolValue; });
            Assert.AreEqual(AnyInstance.AnyInt, (int)intValue);
        }

        [TestMethod]
        public void CountTest()
        {
            JsonArray ja = new JsonArray(1, 2);
            Assert.AreEqual(2, ja.Count);

            JsonObject jo = new JsonObject
            {
                { "key1", 123 },
                { "key2", null },
                { "key3", "hello" },
            };
            Assert.AreEqual(3, jo.Count);
        }

        [TestMethod]
        public void ItemTest()
        {
            //// Positive tests for Item on JsonArray and JsonObject are on JsonArrayTest and JsonObjectTest, respectively.
            JsonValue target;
            target = AnyInstance.AnyJsonPrimitive;
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var c = target[1]; }, string.Format(IndexerNotSupportedOnJsonType, typeof(int), target.JsonType));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { target[0] = 123; }, string.Format(IndexerNotSupportedOnJsonType, typeof(int), target.JsonType));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var c = target["key"]; }, string.Format(IndexerNotSupportedOnJsonType, typeof(string), target.JsonType));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { target["here"] = 123; }, string.Format(IndexerNotSupportedOnJsonType, typeof(string), target.JsonType));

            target = AnyInstance.AnyJsonObject;
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var c = target[0]; }, string.Format(IndexerNotSupportedOnJsonType, typeof(int), target.JsonType));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { target[0] = 123; }, string.Format(IndexerNotSupportedOnJsonType, typeof(int), target.JsonType));

            target = AnyInstance.AnyJsonArray;
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var c = target["key"]; }, string.Format(IndexerNotSupportedOnJsonType, typeof(string), target.JsonType));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { target["here"] = 123; }, string.Format(IndexerNotSupportedOnJsonType, typeof(string), target.JsonType));
        }

        [TestMethod]
        public void NonSerializableTest()
        {
            DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(JsonValue));
            ExceptionTestHelper.ExpectException<NotSupportedException>(() => dcjs.WriteObject(Stream.Null, AnyInstance.DefaultJsonValue));
        }

        [TestMethod()]
        public void DefaultConcatTest()
        {
            JsonValue jv = JsonValueExtensions.CreateFrom(AnyInstance.AnyPerson);
            dynamic target = JsonValueExtensions.CreateFrom(AnyInstance.AnyPerson);
            Person person = AnyInstance.AnyPerson;

            Assert.AreEqual(person.Address.City, target.Address.City.ReadAs<string>());
            Assert.AreEqual(person.Friends[0].Age, target.Friends[0].Age.ReadAs<int>());

            Assert.AreEqual(target.ValueOrDefault("Address").ValueOrDefault("City"), target.Address.City);
            Assert.AreEqual(target.ValueOrDefault("Address", "City"), target.Address.City);

            Assert.AreEqual(target.ValueOrDefault("Friends").ValueOrDefault(0).ValueOrDefault("Age"), target.Friends[0].Age);
            Assert.AreEqual(target.ValueOrDefault("Friends", 0, "Age"), target.Friends[0].Age);

            Assert.AreEqual(JsonType.Default, AnyInstance.AnyJsonValue1.ValueOrDefault((object[])null).JsonType);
            Assert.AreEqual(JsonType.Default, jv.ValueOrDefault("Friends", null).JsonType);
            Assert.AreEqual(JsonType.Default, AnyInstance.AnyJsonValue1.ValueOrDefault((string)null).JsonType);
            Assert.AreEqual(JsonType.Default, AnyInstance.AnyJsonPrimitive.ValueOrDefault(AnyInstance.AnyString, AnyInstance.AnyShort).JsonType);
            Assert.AreEqual(JsonType.Default, AnyInstance.AnyJsonArray.ValueOrDefault((string)null).JsonType);
            Assert.AreEqual(JsonType.Default, AnyInstance.AnyJsonObject.ValueOrDefault(AnyInstance.AnyString, null).JsonType);
            Assert.AreEqual(JsonType.Default, AnyInstance.AnyJsonArray.ValueOrDefault(-1).JsonType);

            Assert.AreSame(AnyInstance.AnyJsonValue1, AnyInstance.AnyJsonValue1.ValueOrDefault());

            Assert.AreSame(AnyInstance.AnyJsonArray.ValueOrDefault(0), AnyInstance.AnyJsonArray.ValueOrDefault((short)0));
            Assert.AreSame(AnyInstance.AnyJsonArray.ValueOrDefault(0), AnyInstance.AnyJsonArray.ValueOrDefault((ushort)0));
            Assert.AreSame(AnyInstance.AnyJsonArray.ValueOrDefault(0), AnyInstance.AnyJsonArray.ValueOrDefault((byte)0));
            Assert.AreSame(AnyInstance.AnyJsonArray.ValueOrDefault(0), AnyInstance.AnyJsonArray.ValueOrDefault((sbyte)0));
            Assert.AreSame(AnyInstance.AnyJsonArray.ValueOrDefault(0), AnyInstance.AnyJsonArray.ValueOrDefault((char)0));

            jv = new JsonObject();
            jv[AnyInstance.AnyString] = AnyInstance.AnyJsonArray;

            Assert.AreSame(jv.ValueOrDefault(AnyInstance.AnyString, 0), jv.ValueOrDefault(AnyInstance.AnyString, (short)0));
            Assert.AreSame(jv.ValueOrDefault(AnyInstance.AnyString, 0), jv.ValueOrDefault(AnyInstance.AnyString, (ushort)0));
            Assert.AreSame(jv.ValueOrDefault(AnyInstance.AnyString, 0), jv.ValueOrDefault(AnyInstance.AnyString, (byte)0));
            Assert.AreSame(jv.ValueOrDefault(AnyInstance.AnyString, 0), jv.ValueOrDefault(AnyInstance.AnyString, (sbyte)0));
            Assert.AreSame(jv.ValueOrDefault(AnyInstance.AnyString, 0), jv.ValueOrDefault(AnyInstance.AnyString, (char)0));

            jv = AnyInstance.AnyJsonObject;

            ExceptionTestHelper.ExpectException<ArgumentException>(delegate { var c = jv.ValueOrDefault(AnyInstance.AnyString, AnyInstance.AnyLong); }, string.Format(InvalidIndexType, typeof(long)));
            ExceptionTestHelper.ExpectException<ArgumentException>(delegate { var c = jv.ValueOrDefault(AnyInstance.AnyString, AnyInstance.AnyUInt); }, string.Format(InvalidIndexType, typeof(uint)));
            ExceptionTestHelper.ExpectException<ArgumentException>(delegate { var c = jv.ValueOrDefault(AnyInstance.AnyString, AnyInstance.AnyBool); }, string.Format(InvalidIndexType, typeof(bool)));
        }
    }
}
