// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.IO;
    using Microsoft.ApplicationServer.Http;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Common.Test;

    [TestClass]
    public class ActionOfStreamContentTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("ActionOfStreamContent.ReadAsString calls the inner action of stream.")]
        public void ReadAsString_Calls_The_ActionOfStream()
        {
            bool actionCalled = false;
            Action<Stream> actionOfStream = (stream) =>
                {
                    StreamWriter writer = new StreamWriter(stream);
                    writer.Write("Hello");
                    writer.Flush();
                    actionCalled = true;
                };

            ActionOfStreamContent content = new ActionOfStreamContent(actionOfStream);
            Assert.IsFalse(actionCalled, "The actionOfStream should not have been called yet.");
            Assert.AreEqual("Hello", content.ReadAsString(), "The content should have been 'Hello'.");
            Assert.IsTrue(actionCalled, "The actionOfStream should have been called.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("ActionOfStreamContent.Headers.ContentLength returns null.")]
        public void ContentLength_Returns_Null()
        {
            bool actionCalled = false;
            Action<Stream> actionOfStream = (stream) =>
            {
                StreamWriter writer = new StreamWriter(stream);
                writer.Write("Hello");
                writer.Flush();
                actionCalled = true;
            };

            ActionOfStreamContent content = new ActionOfStreamContent(actionOfStream);
            Assert.IsNull(content.Headers.ContentLength, "The content length should have been null.");
            Assert.IsFalse(actionCalled, "The actionOfStream should not have been called yet.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("ActionOfStreamContent.ContentReadStream calls the inner action of stream.")]
        public void ContentReadStream_Calls_The_ActionOfStream()
        {
            bool actionCalled = false;
            Action<Stream> actionOfStream = (stream) =>
            {
                StreamWriter writer = new StreamWriter(stream);
                writer.Write("Hello");
                writer.Flush();
                actionCalled = true;
            };

            ActionOfStreamContent content = new ActionOfStreamContent(actionOfStream);
            Stream contentStream = content.ContentReadStream;
            contentStream.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(contentStream);
            Assert.IsTrue(actionCalled, "The actionOfStream should have been called.");
            Assert.AreEqual("Hello", reader.ReadToEnd(), "The content should have been 'Hello'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("ActionOfStreamContent.CopyTo calls the inner action of stream.")]
        public void CopyTo_Calls_The_ActionOfStream()
        {
            bool actionCalled = false;
            Stream writtenToStream = null;
            Action<Stream> actionOfStream = (stream) =>
            {
                StreamWriter writer = new StreamWriter(stream);
                writer.Write("Hello");
                writer.Flush();
                actionCalled = true;
                writtenToStream = stream;
            };

            ActionOfStreamContent content = new ActionOfStreamContent(actionOfStream);
            Assert.IsFalse(actionCalled, "The actionOfStream should not have been called yet.");

            MemoryStream memoryStream = new MemoryStream();
            content.CopyTo(memoryStream);
            Assert.IsTrue(actionCalled, "The actionOfStream should have been called.");

            memoryStream.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(memoryStream);
            Assert.AreEqual("Hello", reader.ReadToEnd(), "The content should have been 'Hello'.");

            Assert.AreSame(writtenToStream, memoryStream, "The ActionOfStream should have written to the memory stream.");
        }
    }
}
