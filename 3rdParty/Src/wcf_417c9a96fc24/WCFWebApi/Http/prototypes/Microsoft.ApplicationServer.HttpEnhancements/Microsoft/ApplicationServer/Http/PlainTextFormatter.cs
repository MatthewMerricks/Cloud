// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.ServiceModel.Description;

    using System.Net.Http;
    using Microsoft.ApplicationServer.Http;

    public class PlainTextFormatter : MediaTypeFormatter 
    {
        public PlainTextFormatter()
        {
            this.SupportedMediaTypes.Add(new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain"));
        }

        public override void OnWriteToStream(Type type, object value, Stream stream, System.Net.Http.Headers.HttpContentHeaders contentHeaders, System.Net.TransportContext context)
        {
            var output = (string)value;
            var writer = new StreamWriter(stream);
            writer.Write(output);
        }

        public override object OnReadFromStream(Type type, Stream stream, System.Net.Http.Headers.HttpContentHeaders contentHeaders)
        {
            throw new NotImplementedException();
        }
    }
}
