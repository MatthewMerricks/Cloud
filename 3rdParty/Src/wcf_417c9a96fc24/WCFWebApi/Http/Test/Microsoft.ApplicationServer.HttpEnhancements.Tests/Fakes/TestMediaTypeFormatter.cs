namespace Microsoft.ApplicationServer.HttpEnhancements.Tests
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http.Headers;

    using Microsoft.ApplicationServer.Http;

    public class TestMediaTypeFormatter : MediaTypeFormatter
    {
        public override object OnReadFromStream(Type type, Stream stream, HttpContentHeaders contentHeaders)
        {
            throw new NotImplementedException();
        }

        public override void OnWriteToStream(Type type, object value, Stream stream, HttpContentHeaders contentHeaders, TransportContext context)
        {
            throw new NotImplementedException();
        }
    }
}