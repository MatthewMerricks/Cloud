namespace System.Json.Test
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Json;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Xml;
    using Microsoft.ServiceModel.Web.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// JsonValue unit tests
    /// </summary>
    [TestClass]
    public class JsonValueTests
    {
        /// <summary>
        /// Tests for <see cref="JsonValue.Load(Stream)"/>.
        /// </summary>
        [TestMethod]
        public void StreamLoading()
        {
            string jsonString = "[1, 2, null, false, {\"foo\": 1, \"bar\":true, \"baz\":null}, 1.23e+56]";
            Dictionary<string, Encoding> allEncodings = new Dictionary<string, Encoding>
            {
                { "UTF8, no BOM", new UTF8Encoding(false) },
                { "Unicode, no BOM", new UnicodeEncoding(false, false) },
                { "BigEndianUnicode, no BOM", new UnicodeEncoding(true, false) },
            };
            foreach (bool useSeekableStream in new bool[] { true, false })
            {
                foreach (string key in allEncodings.Keys)
                {
                    Encoding encoding = allEncodings[key];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        StreamWriter sw = new StreamWriter(ms, encoding);
                        sw.Write(jsonString);
                        sw.Flush();
                        Log.Info("[{0}] {1}: size of the json stream: {2}", useSeekableStream ? "seekable" : "non-seekable", key, ms.Position);
                        ms.Position = 0;
                        JsonValue parsed = JsonValue.Parse(jsonString);
                        JsonValue loaded = useSeekableStream ? JsonValue.Load(ms) : JsonValue.Load(new NonSeekableStream(ms));
                        using (StringReader sr = new StringReader(jsonString))
                        {
                            JsonValue loadedFromTextReader = JsonValue.Load(sr);
                            Assert.AreEqual(parsed.ToString(), loaded.ToString());
                            Assert.AreEqual(parsed.ToString(), loadedFromTextReader.ToString());
                        }
                    }
                }
            }

            jsonString = "4";
            foreach (bool useSeekableStream in new bool[] { true, false })
            {
                foreach (string key in allEncodings.Keys)
                {
                    Encoding encoding = allEncodings[key];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        StreamWriter sw = new StreamWriter(ms, encoding);
                        sw.Write(jsonString);
                        sw.Flush();
                        Log.Info("[{0}] {1}: size of the json stream: {2}", useSeekableStream ? "seekable" : "non-seekable", key, ms.Position);
                        ms.Position = 0;
                        JsonValue parsed = JsonValue.Parse(jsonString);
                        JsonValue loaded = useSeekableStream ? JsonValue.Load(ms) : JsonValue.Load(new NonSeekableStream(ms));
                        using (StringReader sr = new StringReader(jsonString))
                        {
                            JsonValue loadedFromTextReader = JsonValue.Load(sr);
                            Assert.AreEqual(parsed.ToString(), loaded.ToString());
                            Assert.AreEqual(parsed.ToString(), loadedFromTextReader.ToString());
                        }
                    }
                }
            }

            ExpectException<FormatException>(delegate
            {
                using (MemoryStream ms = new MemoryStream(new byte[10]))
                {
                    JsonValue.Load(ms);
                }
            });
        }

        /// <summary>
        /// Tests for handling with escaped characters.
        /// </summary>
        [TestMethod]
        public void EscapedCharacters()
        {
            string str = null;
            JsonValue value = null;
            str = (string)value;
            Assert.IsNull(str);
            value = "abc\b\t\r\u1234\uDC80\uDB11def\\\0ghi";
            str = (string)value;
            Assert.AreEqual("\"abc\\u0008\\u0009\\u000d\u1234\\udc80\\udb11def\\\\\\u0000ghi\"", value.ToString());
            value = '\u0000';
            str = (string)value;
            Assert.AreEqual("\u0000", str);
        }

        /// <summary>
        /// Tests for JSON objects with the special '__type' object member.
        /// </summary>
        [TestMethod]
        public void TypeHintAttributeTests()
        {
            string json = "{\"__type\":\"TypeHint\",\"a\":123}";
            JsonValue jv = JsonValue.Parse(json);
            string newJson = jv.ToString();
            Assert.AreEqual(json, newJson);

            json = "{\"b\":567,\"__type\":\"TypeHint\",\"a\":123}";
            jv = JsonValue.Parse(json);
            newJson = jv.ToString();
            Assert.AreEqual(json, newJson);

            json = "[12,{\"__type\":\"TypeHint\",\"a\":123,\"obj\":{\"__type\":\"hint2\",\"b\":333}},null]";
            jv = JsonValue.Parse(json);
            newJson = jv.ToString();
            Assert.AreEqual(json, newJson);
        }

        /// <summary>
        /// Tests for reading JSON with different member names.
        /// </summary>
        [TestMethod]
        public void ObjectNameTests()
        {
            string[] objectNames = new string[]
            {
                "simple",
                "with spaces",
                "with<>brackets",
                "",
            };

            foreach (string objectName in objectNames)
            {
                string json = string.Format(CultureInfo.InvariantCulture, "{{\"{0}\":123}}", objectName);
                JsonValue jv = JsonValue.Parse(json);
                Assert.AreEqual(123, jv[objectName].ReadAs<int>());
                string newJson = jv.ToString();
                Assert.AreEqual(json, newJson);

                JsonObject jo = new JsonObject { { objectName, 123 } };
                Assert.AreEqual(123, jo[objectName].ReadAs<int>());
                newJson = jo.ToString();
                Assert.AreEqual(json, newJson);
            }

            ExpectException<FormatException>(() => JsonValue.Parse("{\"nonXmlChar\u0000\":123}"));
        }

        /// <summary>
        /// Miscellaneous tests for parsing JSON.
        /// </summary>
        [TestMethod]
        public void ParseMiscellaneousTest()
        {
            string[] jsonValues =
            {
                "[]",
                "[1]",
                "[1,2,3,[4.1,4.2],5]",
                "{}",
                "{\"a\":1}",
                "{\"a\":1,\"b\":2,\"c\":3,\"d\":4}",
                "{\"a\":1,\"b\":[2,3],\"c\":3}",
                "{\"a\":1,\"b\":2,\"c\":[1,2,3,[4.1,4.2],5],\"d\":4}",
                "{\"a\":1,\"b\":[2.1,2.2],\"c\":3,\"d\":4,\"e\":[4.1,4.2,4.3,[4.41,4.42],4.4],\"f\":5}",
                "{\"a\":1,\"b\":[2.1,2.2,[[[{\"b1\":2.21}]]],2.3],\"c\":{\"d\":4,\"e\":[4.1,4.2,4.3,[4.41,4.42],4.4],\"f\":5}}"
            };

            foreach (string json in jsonValues)
            {
                JsonValue jv = JsonValue.Parse(json);
                Log.Info("{0}", jv.ToString());

                string jvstr = jv.ToString();
                Assert.AreEqual<string>(json, jvstr);
            }
        }

        /// <summary>
        /// Negative tests for parsing "unbalanced" JSON (i.e., JSON documents which aren't properly closed).
        /// </summary>
        [TestMethod]
        public void ParseUnbalancedJsonTest()
        {
            string[] jsonValues =
            {
                "[",
                "[1,{]",
                "[1,2,3,{{}}",
                "}",
                "{\"a\":}",
                "{\"a\":1,\"b\":[,\"c\":3,\"d\":4}",
                "{\"a\":1,\"b\":[2,\"c\":3}",
                "{\"a\":1,\"b\":[2.1,2.2,\"c\":3,\"d\":4,\"e\":[4.1,4.2,4.3,[4.41,4.42],4.4],\"f\":5}",
                "{\"a\":1,\"b\":[2.1,2.2,[[[[{\"b1\":2.21}]]],\"c\":{\"d\":4,\"e\":[4.1,4.2,4.3,[4.41,4.42],4.4],\"f\":5}}"
            };

            foreach (string json in jsonValues)
            {
                Log.Info("Testing unbalanced JSON: {0}", json);
                ExpectException<FormatException>(() => JsonValue.Parse(json));
            }
        }

        /// <summary>
        /// Test for parsing a deeply nested JSON object.
        /// </summary>
        [TestMethod]
        public void ParseDeeplyNestedJsonObjectString()
        {
            StringBuilder builderExpected = new StringBuilder();
            builderExpected.Append('{');
            int depth = 10000;
            for (int i = 0; i < depth; i++)
            {
                string key = i.ToString(CultureInfo.InvariantCulture);
                builderExpected.AppendFormat("\"{0}\":{{", key);
            }

            for (int i = 0; i < depth + 1; i++)
            {
                builderExpected.Append('}');
            }

            string json = builderExpected.ToString();
            JsonValue jsonValue = JsonValue.Parse(json);
            string jvstr = jsonValue.ToString();

            Assert.AreEqual(json, jvstr);
        }

        /// <summary>
        /// Test for parsing a deeply nested JSON array.
        /// </summary>
        [TestMethod]
        public void ParseDeeplyNestedJsonArrayString()
        {
            StringBuilder builderExpected = new StringBuilder();
            builderExpected.Append('[');
            int depth = 10000;
            for (int i = 0; i < depth; i++)
            {
                builderExpected.Append('[');
            }

            for (int i = 0; i < depth + 1; i++)
            {
                builderExpected.Append(']');
            }

            string json = builderExpected.ToString();
            JsonValue jsonValue = JsonValue.Parse(json);
            string jvstr = jsonValue.ToString();

            Assert.AreEqual(json, jvstr);
        }

        /// <summary>
        /// Test for parsing a deeply nested JSON graph, containing both objects and arrays.
        /// </summary>
        [TestMethod]
        public void ParseDeeplyNestedJsonString()
        {
            StringBuilder builderExpected = new StringBuilder();
            builderExpected.Append('{');
            int depth = 10000;
            for (int i = 0; i < depth; i++)
            {
                string key = i.ToString(CultureInfo.InvariantCulture);
                builderExpected.AppendFormat("\"{0}\":[{{", key);
            }

            for (int i = 0; i < depth; i++)
            {
                builderExpected.Append("}]");
            }

            builderExpected.Append('}');

            string json = builderExpected.ToString();
            JsonValue jsonValue = JsonValue.Parse(json);
            string jvstr = jsonValue.ToString();

            Assert.AreEqual(json, jvstr);
        }

        /// <summary>
        /// Tests for the <see cref="JsonValueExtensions.Load(XmlDictionaryReader)"/>, with XML documents
        /// complying with the JSON-to-XML mapping.
        /// </summary>
        [TestMethod]
        public void TestJXMLMapping()
        {
            string completeJson = "{\"a\":[123,\"hello\",true,null,{}]}";
            string completeJxml = @"<root type='object'>
  <a type='array'>
    <item type='number'>123</item>
    <item type='string'>hello</item>
    <item type='boolean'>true</item>
    <item type='null'></item>
    <item type='object'/>
  </a>
</root>";
            List<Tuple<string, string>> jsonAndJxmlPairs = new List<Tuple<string, string>>
            {
                new Tuple<string, string>(completeJson, completeJxml),
                new Tuple<string, string>("[]", "<root type='array'/>"),
                new Tuple<string, string>("[]", "<root type='array'></root>"),
                new Tuple<string, string>("[]", "<?xml version='1.0'?>   <root type='array'/>"),
                new Tuple<string, string>("{}", "<root type='object'/>"),
                new Tuple<string, string>("{}", "<root type='object'></root>"),
                new Tuple<string, string>("\"hello\"", "<root type='string'>hello</root>"),
                new Tuple<string, string>("\"hello\"", "<root>hello</root>"),
                new Tuple<string, string>("\"\"", "<root></root>"),
                new Tuple<string, string>("\"\"", "<root type='string'/>"),
                new Tuple<string, string>("\"\"", "<root/>"),
                new Tuple<string, string>("[1,\"1\"]", "<root type='array'><item type='number'>1</item><item type='string'>1</item></root>"),
                new Tuple<string, string>("[null,null]", "<root type='array'><item type='null'></item><item type='null'/></root>"),
            };

            foreach (var pair in jsonAndJxmlPairs)
            {
                string json = pair.Item1;
                string jxml = pair.Item2;
                Log.Info("Testing with JSON '{0}' and JXML '{1}'", json, jxml);
                byte[] jxmlBytes = Encoding.UTF8.GetBytes(jxml);
                using (XmlDictionaryReader xdr = XmlDictionaryReader.CreateTextReader(jxmlBytes, XmlDictionaryReaderQuotas.Max))
                {
                    JsonValue jv = JsonValueExtensions.Load(xdr);
                    Assert.AreEqual(json, jv.ToString());
                }
            }
        }

        /// <summary>
        /// Negative tests for the <see cref="JsonValueExtensions.Load(XmlDictionaryReader)"/>, with XML documents
        /// which do not comply with the JSON-to-XML mapping.
        /// </summary>
        [TestMethod]
        public void TestBadJXMLMapping()
        {
            List<string> badJXMLs = new List<string>
            {
                "<item type='array'/>",
                "<?xml version='1.0'?>",
                "<root type='array'><notItem type='string'>hello</notItem></root>",
                "<root type='array'><item type='unknown'>hello</item></root>",
                "<root type='object'><item:item xmlns:item='item' type='string'>foo</item:item></root>",
            };

            foreach (string badJXML in badJXMLs)
            {
                Log.Info("Bad JXML: {0}", badJXML);
                byte[] xmlBytes = Encoding.UTF8.GetBytes(badJXML);
                ExpectException<FormatException>(() => JsonValueExtensions.Load(XmlDictionaryReader.CreateTextReader(xmlBytes, XmlDictionaryReaderQuotas.Max)));
            }
        }

        /// <summary>
        /// Negative test to make sure that one cannot serialize or deserialize <see cref="JsonValue"/> instances.
        /// </summary>
        [TestMethod]
        public void NonSerializableTests()
        {
            foreach (bool useJsonSerializer in new bool[] { false, true })
            {
                foreach (JsonValue jv in new JsonValue[] { "hello", new JsonArray(1, 2), new JsonObject { { "key", "value" } }, new JsonPrimitive(1).ValueOrDefault("default") })
                {
                    Log.Info("Testing with {0} serializer for JsonType.{1}", useJsonSerializer ? "DCJS" : "DCS", jv.JsonType);
                    XmlObjectSerializer serializer;
                    if (useJsonSerializer)
                    {
                        serializer = new DataContractJsonSerializer(jv.GetType());
                    }
                    else
                    {
                        serializer = new DataContractSerializer(jv.GetType());
                    }

                    if (jv.JsonType == JsonType.Default)
                    {
                        ExpectException<NotSupportedException>(() => serializer.WriteObject(Stream.Null, jv));
                        if (useJsonSerializer)
                        {
                            ExpectException<NotSupportedException>(() => serializer.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes("{}"))));
                        }
                    }
                    else
                    {
                        ExpectException<InvalidDataContractException>(() => serializer.WriteObject(Stream.Null, jv));
                        if (useJsonSerializer)
                        {
                            string json = jv.ToString();
                            ExpectException<InvalidDataContractException>(() => serializer.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(json))));
                        }
                    }
                }
            }
        }

        internal static void ExpectException<T>(Action action) where T : Exception
        {
            ExpectException<T>(action, null);
        }

        internal static void ExpectException<T>(Action action, string partOfExceptionString) where T : Exception
        {
            try
            {
                action();
                Assert.Fail("This should have thrown");
            }
            catch (T e)
            {
                if (partOfExceptionString != null)
                {
                    Assert.IsTrue(e.Message.Contains(partOfExceptionString));
                }
            }
        }

        internal class NonSeekableStream : Stream
        {
            Stream innerStream;

            public NonSeekableStream(Stream innerStream)
            {
                this.innerStream = innerStream;
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override long Position
            {
                get
                {
                    throw new NotSupportedException();
                }

                set
                {
                    throw new NotSupportedException();
                }
            }

            public override long Length
            {
                get
                {
                    throw new NotSupportedException();
                }
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return this.innerStream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}