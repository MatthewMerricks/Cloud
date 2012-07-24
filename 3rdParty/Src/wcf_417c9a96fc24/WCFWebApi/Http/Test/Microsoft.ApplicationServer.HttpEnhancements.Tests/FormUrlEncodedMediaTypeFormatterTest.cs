using System;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ApplicationServer.HttpEnhancements.Tests
{
    using System.Json;

    using Microsoft.ApplicationServer.Http;
    using System.IO;

    [TestClass]
    public class FormUrlEncodedMediaTypeFormatterTest
    {
        public void WhenCanWriteTypeIsCalledReturnsFalse()
        {
            var formatter = new FormUrlEncodedMediaTypeFormatter();
            Assert.IsFalse(formatter.CanWriteType(typeof(object)));
        }
        
        [TestMethod]
        public void WhenOnReadFromStreamIsCalledAndFormUrlEncodedIsPassedAndTypeIsJsonValueThenReturnsJsonValue()
        {
            var formatter = new FormUrlEncodedMediaTypeFormatter();
            var jsonValue = (JsonValue) formatter.OnReadFromStream(typeof(JsonValue), this.GetStream(), null);
            Assert.IsNotNull(jsonValue);
            Assert.AreEqual("Test Value", (string)jsonValue["Value"]);
        }

        [TestMethod]
        public void WhenOnReadFromStreamIsCalledAndFormUrlEncodedIsPassedAndTypeIsDataContractThenReturnsContractInstance()
        {
            var formatter = new FormUrlEncodedMediaTypeFormatter();
            var value = (TestFormUrlEncodedValueClass)formatter.OnReadFromStream(typeof(TestFormUrlEncodedValueClass), this.GetStream(), null);
            Assert.IsNotNull(value);
            Assert.AreEqual("Test Value", value.Value);
        }

        public class TestFormUrlEncodedValueClass
        {
            public string Value { get; set; }
        }

        private Stream GetStream()
        {
            var content = "Value=Test+Value";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
