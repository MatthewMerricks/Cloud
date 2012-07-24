using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ApplicationServer.HttpEnhancements.Tests
{
    using System.IO;
    using System.Json;
    using System.Net.Http.Headers;

    using Microsoft.ApplicationServer.Http;

    [TestClass]
    public class JsonValueMediaTypeFormatterTest
    {
        [TestMethod]
        public void WhenTypeIsNotJsonValueThenCanReadIsFalse()
        {
            var formatter = new JsonValueMediaTypeFormatter();
            Assert.IsFalse(formatter.CanReadType(typeof(int)));
        }

        [TestMethod]
        public void WhenTypeIsJsonValueThenCanReadIsTrue()
        {
            var formatter = new JsonValueMediaTypeFormatter();
            Assert.IsTrue(formatter.CanReadType(typeof(JsonValue)));
        }

        [TestMethod]
        public void WhenOnReadFromStreamIsCalledAndJsonIsPassedThenJsonValueIsReturned()
        {
            var formatter = new JsonValueMediaTypeFormatter();
            MemoryStream stream = GetStream();
            var jsonValue = (JsonValue) formatter.OnReadFromStream(typeof(JsonValue), this.GetStream(), null);
            Assert.IsNotNull(jsonValue);
            Assert.AreEqual("TestValue", (string)jsonValue["Value"]);
        }

        [TestMethod]
        public void WhenOnWriteToStreamIsCalledAndJsonValueIsPassedThenJsonIsReturned()
        {
            var formatter = new JsonValueMediaTypeFormatter();
            var stream = new MemoryStream();
            var reader = new StreamReader(stream);
            dynamic jsonValue = new JsonObject();
            jsonValue.Value = "TestValue";
            formatter.OnWriteToStream(null, jsonValue, stream, null, null);
            stream.Position = 0;
            var json = reader.ReadToEnd();
            Assert.AreEqual("{\"Value\":\"TestValue\"}", json);
        }

        private MemoryStream GetStream()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.WriteLine("{\"Value\":\"TestValue\"}");
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

    
    }
}        
