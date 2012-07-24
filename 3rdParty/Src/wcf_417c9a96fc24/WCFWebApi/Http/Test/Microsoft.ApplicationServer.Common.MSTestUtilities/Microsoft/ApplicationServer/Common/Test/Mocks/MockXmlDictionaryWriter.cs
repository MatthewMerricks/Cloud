// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Mocks
{
    using System;
    using System.Xml;

    public class MockXmlDictionaryWriter : XmlDictionaryWriter
    {
        public bool WriteCalled { get; private set; }

        public override void Close()
        {

        }

        public override void Flush()
        {

        }

        public override string LookupPrefix(string ns)
        {
            return null;
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            this.WriteCalled = true;
        }

        public override void WriteCData(string text)
        {
            this.WriteCalled = true;
        }

        public override void WriteCharEntity(char ch)
        {
            this.WriteCalled = true;
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            this.WriteCalled = true;
        }

        public override void WriteComment(string text)
        {
            this.WriteCalled = true;
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            this.WriteCalled = true;
        }

        public override void WriteEndAttribute()
        {
            this.WriteCalled = true;
        }

        public override void WriteEndDocument()
        {
            this.WriteCalled = true;
        }

        public override void WriteEndElement()
        {
            this.WriteCalled = true;
        }

        public override void WriteEntityRef(string name)
        {
            this.WriteCalled = true;
        }

        public override void WriteFullEndElement()
        {
            this.WriteCalled = true;
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
            this.WriteCalled = true;
        }

        public override void WriteRaw(string data)
        {
            this.WriteCalled = true;
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            this.WriteCalled = true;
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            this.WriteCalled = true;
        }

        public override void WriteStartDocument(bool standalone)
        {
            this.WriteCalled = true;
        }

        public override void WriteStartDocument()
        {
            this.WriteCalled = true;
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            this.WriteCalled = true;
        }

        public override WriteState WriteState
        {
            get { throw new NotImplementedException(); }
        }

        public override void WriteString(string text)
        {
            this.WriteCalled = true;
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            this.WriteCalled = true;
        }

        public override void WriteWhitespace(string ws)
        {
            this.WriteCalled = true;
        }
    }
}
