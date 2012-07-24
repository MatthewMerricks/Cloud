namespace Microsoft.ServiceModel.Web.Test
{
    using System;
    using System.Collections.Generic;
    using System.Json;
    using System.Text;
    using System.Web;
    using Microsoft.Silverlight.Cdf.Test.Common.Utility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for parsing form-url-encoded data.
    /// </summary>
    [TestClass]
    public class FormUrlEncodedTests
    {
        /// <summary>
        /// Tests for parsing form-urlencoded data originated from JS primitives.
        /// </summary>
        [TestMethod]
        public void TestJsonPrimitive()
        {
            this.TestFormEncodedParsing("abc", @"{""abc"":null}");
            this.TestFormEncodedParsing("123", @"{""123"":null}");
            this.TestFormEncodedParsing("true", @"{""true"":null}");
            this.TestFormEncodedParsing("", "{}");
            this.TestFormEncodedParsing("%2fabc%2f", @"{""\/abc\/"":null}");
        }

        /// <summary>
        /// Negative tests for parsing form-urlencoded data originated from JS primitives.
        /// </summary>
        [TestMethod]
        public void TestJsonPrimitiveNegative()
        {
            this.ExpectException<ArgumentException>(() => ParseFormUrlEncoded("a[b]=1&a=2"));
            this.ExpectException<ArgumentException>(() => ParseFormUrlEncoded("a=2&a[b]=1"));
            this.ExpectException<ArgumentException>(() => ParseFormUrlEncoded("[]=1"));
            this.ExpectException<ArgumentNullException>(() => ParseFormUrlEncoded((string)null));
        }

        /// <summary>
        /// Tests for parsing form-urlencoded data originated from JS objects.
        /// </summary>
        [TestMethod]
        public void TestObjects()
        {
            this.TestFormEncodedParsing("a=NaN", @"{""a"":""NaN""}");
            this.TestFormEncodedParsing("a=false", @"{""a"":""false""}");
            this.TestFormEncodedParsing("a=foo", @"{""a"":""foo""}");
            this.TestFormEncodedParsing("1=1", "{\"1\":\"1\"}");
        }

        /// <summary>
        /// Tests for parsing form-urlencoded data originated from JS arrays.
        /// </summary>
        [TestMethod]
        public void TestArray()
        {
            this.TestFormEncodedParsing("a[]=2", @"{""a"":[""2""]}");
            this.TestFormEncodedParsing("a[]=", @"{""a"":[""""]}");
            this.TestFormEncodedParsing("a[0][0][]=1", @"{""a"":[[[""1""]]]}");
            this.TestFormEncodedParsing("z[]=9&z[]=true&z[]=undefined&z[]=", @"{""z"":[""9"",""true"",""undefined"",""""]}");
            this.TestFormEncodedParsing("z[]=9&z[]=true&z[]=undefined&z[]=null", @"{""z"":[""9"",""true"",""undefined"",""null""]}");
            this.TestFormEncodedParsing("z[0][]=9&z[0][]=true&z[1][]=undefined&z[1][]=null", @"{""z"":[[""9"",""true""],[""undefined"",""null""]]}");
            this.TestFormEncodedParsing("a[0][x]=2", @"{""a"":[{""x"":""2""}]}");
            this.TestFormEncodedParsing("a%5B%5D=2", @"{""a"":[""2""]}");
            this.TestFormEncodedParsing("a%5B%5D=", @"{""a"":[""""]}");
            this.TestFormEncodedParsing("z%5B%5D=9&z%5B%5D=true&z%5B%5D=undefined&z%5B%5D=", @"{""z"":[""9"",""true"",""undefined"",""""]}");
            this.TestFormEncodedParsing("z%5B%5D=9&z%5B%5D=true&z%5B%5D=undefined&z%5B%5D=null", @"{""z"":[""9"",""true"",""undefined"",""null""]}");
            this.TestFormEncodedParsing("z%5B0%5D%5B%5D=9&z%5B0%5D%5B%5D=true&z%5B1%5D%5B%5D=undefined&z%5B1%5D%5B%5D=null", @"{""z"":[[""9"",""true""],[""undefined"",""null""]]}");
        }

        /// <summary>
        /// Tests for parsing form-urlencoded data originated from JS arrays, using the jQuery 1.3 format (no []'s).
        /// </summary>
        [TestMethod]
        public void TestArrayCompat()
        {
            this.TestFormEncodedParsing("z=9&z=true&z=undefined&z=", @"{""z"":[""9"",""true"",""undefined"",""""]}");
            this.TestFormEncodedParsing("z=9&z=true&z=undefined&z=null", @"{""z"":[""9"",""true"",""undefined"",""null""]}");
            this.TestFormEncodedParsing("z=9&z=true&z=undefined&z=null&a=hello", @"{""z"":[""9"",""true"",""undefined"",""null""],""a"":""hello""}");
        }

        /// <summary>
        /// Negative tests for parsing form-urlencoded data originated from JS arrays.
        /// </summary>
        [TestMethod]
        public void TestArrayCompatNegative()
        {
            this.ExpectException<ArgumentException>(() => ParseFormUrlEncoded("a[z]=2&a[z]=3"), "a[z]");
        }

        /// <summary>
        /// Tests for form-urlencoded data originated from sparse JS arrays.
        /// </summary>
        [TestMethod]
        public void TestArraySparse()
        {
            this.TestFormEncodedParsing("a[2]=hello", @"{""a"":{""2"":""hello""}}");
            this.TestFormEncodedParsing("a[x][0]=2", @"{""a"":{""x"":[""2""]}}");
            this.TestFormEncodedParsing("a[x][1]=2", @"{""a"":{""x"":{""1"":""2""}}}");
            this.TestFormEncodedParsing("a[x][0]=0&a[x][1]=1", @"{""a"":{""x"":[""0"",""1""]}}");
            this.TestFormEncodedParsing("a[0][0][0]=hello&a[1][0][0][0][]=hello", @"{""a"":[[[""hello""]],[[[[""hello""]]]]]}");
            this.TestFormEncodedParsing("a[0][0][0]=hello&a[1][0][0][0]=hello", @"{""a"":[[[""hello""]],[[[""hello""]]]]}");
            this.TestFormEncodedParsing("a[1][0][]=1", @"{""a"":{""1"":[[""1""]]}}");
            this.TestFormEncodedParsing("a[1][1][]=1", @"{""a"":{""1"":{""1"":[""1""]}}}");
            this.TestFormEncodedParsing("a[1][1][0]=1", @"{""a"":{""1"":{""1"":[""1""]}}}");
            this.TestFormEncodedParsing("a[0][]=2&a[0][]=3&a[2][]=1", "{\"a\":{\"0\":[\"2\",\"3\"],\"2\":[\"1\"]}}");
            this.TestFormEncodedParsing("a[x][]=1&a[x][1]=2", @"{""a"":{""x"":[""1"",""2""]}}");
            this.TestFormEncodedParsing("a[x][0]=1&a[x][]=2", @"{""a"":{""x"":[""1"",""2""]}}");
        }

        /// <summary>
        /// Negative tests for parsing form-urlencoded arrays.
        /// </summary>
        [TestMethod]
        public void TestArrayIndexNegative()
        {
            this.ExpectException<ArgumentException>(() => ParseFormUrlEncoded("a[x]=2&a[x][]=3"), "a[x]");
            this.ExpectException<ArgumentException>(() => ParseFormUrlEncoded("a[]=1&a[0][]=2"), "a[0]");
            this.ExpectException<ArgumentException>(() => ParseFormUrlEncoded("a[]=1&a[0][0][]=2"), "a[0]");
            this.ExpectException<ArgumentException>(() => ParseFormUrlEncoded("a[x][]=1&a[x][0]=2"), "a[x][0]");
            this.ExpectException<ArgumentException>(() => ParseFormUrlEncoded("a[][]=0"), "a[]");
            this.ExpectException<ArgumentException>(() => ParseFormUrlEncoded("a[][x]=0"), "a[]");
        }

        /// <summary>
        /// Tests for parsing complex object graphs form-urlencoded.
        /// </summary>
        [TestMethod]
        public void TestObject()
        {
            string encoded = "a[]=4&a[]=5&b[x][]=7&b[y]=8&b[z][]=9&b[z][]=true&b[z][]=undefined&b[z][]=&c=1&f=";
            string resultStr = @"{""a"":[""4"",""5""],""b"":{""x"":[""7""],""y"":""8"",""z"":[""9"",""true"",""undefined"",""""]},""c"":""1"",""f"":""""}";
            this.TestFormEncodedParsing(encoded, resultStr);

            encoded = "customer[Name]=Pete&customer[Address]=Redmond&customer[Age][0][]=23&customer[Age][0][]=24&customer[Age][1][]=25&" +
                "customer[Age][1][]=26&customer[Phones][]=425+888+1111&customer[Phones][]=425+345+7777&customer[Phones][]=425+888+4564&" +
                "customer[EnrolmentDate]=%22%5C%2FDate(1276562539537)%5C%2F%22&role=NewRole&changeDate=3&count=15";
            resultStr = @"{""customer"":{""Name"":""Pete"",""Address"":""Redmond"",""Age"":[[""23"",""24""],[""25"",""26""]]," +
                @"""Phones"":[""425 888 1111"",""425 345 7777"",""425 888 4564""],""EnrolmentDate"":""\""\\\/Date(1276562539537)\\\/\""""},""role"":""NewRole"",""changeDate"":""3"",""count"":""15""}";
            this.TestFormEncodedParsing(encoded, resultStr);

            encoded = "customers[0][Name]=Pete2&customers[0][Address]=Redmond2&customers[0][Age][0][]=23&customers[0][Age][0][]=24&" +
                "customers[0][Age][1][]=25&customers[0][Age][1][]=26&customers[0][Phones][]=425+888+1111&customers[0][Phones][]=425+345+7777&" +
                "customers[0][Phones][]=425+888+4564&customers[0][EnrolmentDate]=%22%5C%2FDate(1276634840700)%5C%2F%22&customers[1][Name]=Pete3&" +
                "customers[1][Address]=Redmond3&customers[1][Age][0][]=23&customers[1][Age][0][]=24&customers[1][Age][1][]=25&customers[1][Age][1][]=26&" +
                "customers[1][Phones][]=425+888+1111&customers[1][Phones][]=425+345+7777&customers[1][Phones][]=425+888+4564&customers[1][EnrolmentDate]=%22%5C%2FDate(1276634840700)%5C%2F%22";
            resultStr = @"{""customers"":[{""Name"":""Pete2"",""Address"":""Redmond2"",""Age"":[[""23"",""24""],[""25"",""26""]]," +
                @"""Phones"":[""425 888 1111"",""425 345 7777"",""425 888 4564""],""EnrolmentDate"":""\""\\\/Date(1276634840700)\\\/\""""}," +
                @"{""Name"":""Pete3"",""Address"":""Redmond3"",""Age"":[[""23"",""24""],[""25"",""26""]],""Phones"":[""425 888 1111"",""425 345 7777"",""425 888 4564""],""EnrolmentDate"":""\""\\\/Date(1276634840700)\\\/\""""}]}";
            this.TestFormEncodedParsing(encoded, resultStr);

            encoded = "ab%5B%5D=hello";
            resultStr = @"{""ab"":[""hello""]}";
            this.TestFormEncodedParsing(encoded, resultStr);

            encoded = "123=hello";
            resultStr = @"{""123"":""hello""}";
            this.TestFormEncodedParsing(encoded, resultStr);
        }

        /// <summary>
        /// Tests for parsing form-urlencoded data with encoded names.
        /// </summary>
        [TestMethod]
        public void TestEncodedName()
        {
            string encoded = "some+thing=10";
            string resultStr = @"{""some thing"":""10""}";
            this.TestFormEncodedParsing(encoded, resultStr);

            encoded = "%E5%B8%A6%E4%B8%89%E4%B8%AA%E8%A1%A8=bar";
            resultStr = @"{""带三个表"":""bar""}";
            this.TestFormEncodedParsing(encoded, resultStr);

            encoded = "some+thing=10&%E5%B8%A6%E4%B8%89%E4%B8%AA%E8%A1%A8=bar";
            resultStr = @"{""some thing"":""10"",""带三个表"":""bar""}";
            this.TestFormEncodedParsing(encoded, resultStr);

            encoded = "a[0\r\n][b]=1";
            resultStr = "{\"a\":{\"0\\u000d\\u000a\":{\"b\":\"1\"}}}";
            this.TestFormEncodedParsing(encoded, resultStr);
            this.TestFormEncodedParsing(encoded.Replace("\r", "%0D").Replace("\n", "%0A"), resultStr);

            this.TestFormEncodedParsing("a[0\0]=1", "{\"a\":{\"0\\u0000\":\"1\"}}");
            this.TestFormEncodedParsing("a[0%00]=1", "{\"a\":{\"0\\u0000\":\"1\"}}");
            this.TestFormEncodedParsing("a[\00]=1", "{\"a\":{\"\\u00000\":\"1\"}}");
            this.TestFormEncodedParsing("a[%000]=1", "{\"a\":{\"\\u00000\":\"1\"}}");
        }

        /// <summary>
        /// Tests for malformed form-urlencoded data.
        /// </summary>
        [TestMethod]
        public void TestNegative()
        {
            this.ExpectException<ArgumentException>(() => ParseFormUrlEncoded("a[b=2"), "1");
            this.ExpectException<ArgumentException>(() => ParseFormUrlEncoded("a[[b]=2"), "2");
            this.ExpectException<ArgumentException>(() => ParseFormUrlEncoded("a[b]]=2"), "4");
            this.ExpectException<ArgumentException>(() => ParseFormUrlEncoded("&some+thing=10&%E5%B8%A6%E4%B8%89%E4%B8%AA%E8%A1%A8=bar"));
            this.ExpectException<ArgumentException>(() => ParseFormUrlEncoded("some+thing=10&%E5%B8%A6%E4%B8%89%E4%B8%AA%E8%A1%A8=bar&"));
        }

        /// <summary>
        /// Tests for parsing generated form-urlencoded data.
        /// </summary>
        [TestMethod]
        public void GeneratedJsonValueTest()
        {
            Random rndGen = new Random(1);
            int oldMaxArray = CreatorSettings.MaxArrayLength;
            int oldMaxList = CreatorSettings.MaxListLength;
            int oldMaxStr = CreatorSettings.MaxStringLength;
            double oldNullProbability = CreatorSettings.NullValueProbability;
            bool oldCreateAscii = CreatorSettings.CreateOnlyAsciiChars;
            CreatorSettings.MaxArrayLength = 5;
            CreatorSettings.MaxListLength = 3;
            CreatorSettings.MaxStringLength = 3;
            CreatorSettings.NullValueProbability = 0;
            CreatorSettings.CreateOnlyAsciiChars = true;
            JsonValueCreatorSurrogate jsonValueCreator = new JsonValueCreatorSurrogate();
            try
            {
                for (int i = 0; i < 1000; i++)
                {
                    JsonValue jv = (JsonValue)jsonValueCreator.CreateInstanceOf(typeof(JsonValue), rndGen);
                    if (jv.JsonType == JsonType.Array || jv.JsonType == JsonType.Object)
                    {
                        string jaStr = FormUrlEncoding(jv);
                        JsonValue deserJv = FormUrlEncodedExtensions.ParseFormUrlEncoded(jaStr);
                        bool compare = true;
                        if (deserJv is JsonObject && ((JsonObject)deserJv).ContainsKey("JV"))
                        {
                            compare = JsonValueRoundTripComparer.Compare(jv, deserJv["JV"]);
                        }
                        else
                        {
                            compare = JsonValueRoundTripComparer.Compare(jv, deserJv);
                        }

                        Assert.IsTrue(compare, "Comparison failed for test instance " + i);
                    }
                }
            }
            finally
            {
                CreatorSettings.MaxArrayLength = oldMaxArray;
                CreatorSettings.MaxListLength = oldMaxList;
                CreatorSettings.MaxStringLength = oldMaxStr;
                CreatorSettings.NullValueProbability = oldNullProbability;
                CreatorSettings.CreateOnlyAsciiChars = oldCreateAscii;
            }
        }

        private static string FormUrlEncoding(JsonValue jsonValue)
        {
            List<string> results = new List<string>();
            if (jsonValue is JsonPrimitive)
            {
                return HttpUtility.UrlEncode(((JsonPrimitive)jsonValue).Value.ToString());
            }

            BuildParams("JV", jsonValue, results);
            StringBuilder strResult = new StringBuilder();
            foreach (var result in results)
            {
                strResult.Append("&" + result);
            }

            if (strResult.Length > 0)
            {
                return strResult.Remove(0, 1).ToString();
            }

            return strResult.ToString();
        }

        private static void BuildParams(string prefix, JsonValue jsonValue, List<string> results)
        {
            if (jsonValue is JsonPrimitive)
            {
                JsonPrimitive jsonPrimitive = jsonValue as JsonPrimitive;
                if (jsonPrimitive != null)
                {
                    if (jsonPrimitive.JsonType == JsonType.String && string.IsNullOrEmpty(jsonPrimitive.Value.ToString()))
                    {
                        results.Add(prefix + "=" + string.Empty);
                    }
                    else
                    {
                        if (jsonPrimitive.Value is DateTime || jsonPrimitive.Value is DateTimeOffset)
                        {
                            string dateStr = jsonPrimitive.ToString();
                            if (!string.IsNullOrEmpty(dateStr) && dateStr.StartsWith("\""))
                            {
                                dateStr = dateStr.Substring(1, dateStr.Length - 2);
                            }
                            results.Add(prefix + "=" + HttpUtility.UrlEncode(dateStr));
                        }
                        else
                        {
                            results.Add(prefix + "=" + HttpUtility.UrlEncode(jsonPrimitive.Value.ToString()));
                        }
                    }
                }
                else
                {
                    results.Add(prefix + "=" + string.Empty);
                }
            }
            else if (jsonValue is JsonArray)
            {
                for (int i = 0; i < jsonValue.Count; i++)
                {
                    if (jsonValue[i] is JsonArray || jsonValue[i] is JsonObject)
                    {
                        BuildParams(prefix + "[" + i + "]", jsonValue[i], results);
                    }
                    else
                    {
                        BuildParams(prefix + "[]", jsonValue[i], results);
                    }
                }
            }
            else //jsonValue is JsonObject
            {
                foreach (KeyValuePair<string, JsonValue> item in jsonValue)
                {
                    BuildParams(prefix + "[" + item.Key + "]", item.Value, results);
                }
            }
        }

        private static JsonObject ParseFormUrlEncoded(string data)
        {
            return FormUrlEncodedExtensions.ParseFormUrlEncoded(data);
        }

        void TestFormEncodedParsing(string encoded, string expectedResult)
        {
            JsonObject result = FormUrlEncodedExtensions.ParseFormUrlEncoded(encoded);
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResult, result.ToString());
        }

        void ExpectException<T>(Action action) where T : Exception
        {
            ExpectException<T>(action, null);
        }

        void ExpectException<T>(Action action, string partOfExceptionString) where T : Exception
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
    }
}