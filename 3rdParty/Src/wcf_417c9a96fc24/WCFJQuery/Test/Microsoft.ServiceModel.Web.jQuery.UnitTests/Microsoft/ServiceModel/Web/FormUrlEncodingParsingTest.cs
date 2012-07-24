namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System;
    using System.Json;
    using System.Linq;
    using Microsoft.ServiceModel.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FormUrlEncodingParsingTest
    {
        [TestMethod]
        public void SimpleStringsTest()
        {
            string anyString = "abc";
            JsonObject jv = FormUrlEncodedExtensions.ParseFormUrlEncoded(anyString);
            Assert.AreEqual(1, jv.Count);
            Assert.IsTrue(jv.ContainsKey(anyString));
            Assert.AreEqual(anyString, jv.Keys.First());
            Assert.IsNull(jv[anyString]);

            string escaped = "%2eabc%2e";
            ValidateFormsEncodingParsing(escaped, "{\".abc.\":null}");

            ValidateFormsEncodingParsing("", "{}");
            ValidateFormsEncodingParsing("?", "{}");

            string anyFormUrlEncoded = "a=1";
            Assert.IsNotNull(FormUrlEncodedExtensions.ParseFormUrlEncoded(anyFormUrlEncoded));

            ExceptionTestHelper.ExpectException<ArgumentNullException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded((string)null));
        }

        [TestMethod]
        public void SimpleObjectsTest()
        {
            ValidateFormsEncodingParsing("a=2", "{\"a\":\"2\"}");
            ValidateFormsEncodingParsing("b=true", "{\"b\":\"true\"}");
            ValidateFormsEncodingParsing("c=hello", "{\"c\":\"hello\"}");
            ValidateFormsEncodingParsing("d=", "{\"d\":\"\"}");
            ValidateFormsEncodingParsing("e=null", "{\"e\":\"null\"}");
        }

        [TestMethod]
        public void LegacyArraysTest()
        {
            ValidateFormsEncodingParsing("a=1&a=hello&a=333", "{\"a\":[\"1\",\"hello\",\"333\"]}");

            // Only valid in shallow serialization
            ExceptionTestHelper.ExpectException<ArgumentException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded("a[z]=2&a[z]=3"));
        }

        [TestMethod]
        public void ArraysTest()
        {
            ValidateFormsEncodingParsing("a[]=1&a[]=hello&a[]=333", "{\"a\":[\"1\",\"hello\",\"333\"]}");
            ValidateFormsEncodingParsing("a[b][]=1&a[b][]=hello&a[b][]=333", "{\"a\":{\"b\":[\"1\",\"hello\",\"333\"]}}");
            ValidateFormsEncodingParsing("a[]=", "{\"a\":[\"\"]}");
            ValidateFormsEncodingParsing("a%5B%5D=2", @"{""a"":[""2""]}");
            ValidateFormsEncodingParsing("a[x][0]=1&a[x][]=2", @"{""a"":{""x"":[""1"",""2""]}}");
        }

        [TestMethod]
        public void MultidimensionalArraysTest()
        {
            ValidateFormsEncodingParsing("a[0][]=1&a[0][]=hello&a[1][]=333", "{\"a\":[[\"1\",\"hello\"],[\"333\"]]}");
            ValidateFormsEncodingParsing("a[b][0][]=1&a[b][1][]=hello&a[b][1][]=333", "{\"a\":{\"b\":[[\"1\"],[\"hello\",\"333\"]]}}");
            ValidateFormsEncodingParsing("a[0][0][0][]=1", "{\"a\":[[[[\"1\"]]]]}");
        }

        [TestMethod]
        public void SparseArraysTest()
        {
            ValidateFormsEncodingParsing("a[0][]=hello&a[2][]=333", "{\"a\":{\"0\":[\"hello\"],\"2\":[\"333\"]}}");
            ValidateFormsEncodingParsing("a[0]=hello", "{\"a\":[\"hello\"]}");
            ValidateFormsEncodingParsing("a[1][]=hello", "{\"a\":{\"1\":[\"hello\"]}}");
            ValidateFormsEncodingParsing("a[1][0]=hello", "{\"a\":{\"1\":[\"hello\"]}}");
        }

        [TestMethod]
        public void ArraysWithMixedMembers()
        {
            ValidateFormsEncodingParsing("b[]=2&b[1][c]=d", "{\"b\":[\"2\",{\"c\":\"d\"}]}");
        }

        [TestMethod]
        public void EmptyKeyTest()
        {
            ValidateFormsEncodingParsing("=3", "{\"\":\"3\"}");
            ValidateFormsEncodingParsing("a=1&=3", "{\"a\":\"1\",\"\":\"3\"}");
            ValidateFormsEncodingParsing("=3&b=2", "{\"\":\"3\",\"b\":\"2\"}");
        }

        [TestMethod]
        public void InvalidObjectGraphsTest()
        {
            ExceptionTestHelper.ExpectException<ArgumentException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded("a[b]=1&a=2"));
            ExceptionTestHelper.ExpectException<ArgumentException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded("a[b]=1&a[b][]=2"));
            ExceptionTestHelper.ExpectException<ArgumentException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded("a[x][]=1&a[x][0]=2"));
            ExceptionTestHelper.ExpectException<ArgumentException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded("a=2&a[b]=1"));
            ExceptionTestHelper.ExpectException<ArgumentException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded("[]=1"));
            ExceptionTestHelper.ExpectException<ArgumentException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded("a[][]=0"));
            ExceptionTestHelper.ExpectException<ArgumentException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded("a[][x]=0"));
            ExceptionTestHelper.ExpectException<ArgumentException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded("a&a=1"));
            ExceptionTestHelper.ExpectException<ArgumentException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded("a=1&a"));
            ExceptionTestHelper.ExpectException<ArgumentException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded("a&a[b]=1"));
            ExceptionTestHelper.ExpectException<ArgumentException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded("a[]=1&a"));
        }

        [TestMethod]
        public void InvalidFormUrlEncodingTest()
        {
            ExceptionTestHelper.ExpectException<ArgumentException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded("a[b=2"));
            ExceptionTestHelper.ExpectException<ArgumentException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded("a[[b]=2"));
            ExceptionTestHelper.ExpectException<ArgumentException>(() => FormUrlEncodedExtensions.ParseFormUrlEncoded("a[b]]=2"));
        }

        static void ValidateFormsEncodingParsing(string formUrlEncoded, string expectedJson)
        {
            JsonValue jv = FormUrlEncodedExtensions.ParseFormUrlEncoded(formUrlEncoded);
            Assert.IsNotNull(jv);
            Assert.AreEqual(expectedJson, jv.ToString());
        }
    }
}