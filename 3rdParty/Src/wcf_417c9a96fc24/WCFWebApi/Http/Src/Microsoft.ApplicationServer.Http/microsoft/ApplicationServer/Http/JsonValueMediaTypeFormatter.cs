// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Json;
    using System.Runtime.Serialization.Json;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using System.Threading;

    using System.Net.Http;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.ApplicationServer.Http.Description;

    public class JsonValueMediaTypeFormatter : MediaTypeFormatter
    {
        public JsonValueMediaTypeFormatter()
        {
            this.SupportedMediaTypes.Add(new System.Net.Http.Headers.MediaTypeHeaderValue("text/json"));
            this.SupportedMediaTypes.Add(new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"));
        }

        public override void OnWriteToStream(Type type, object value, Stream stream, System.Net.Http.Headers.HttpContentHeaders contentHeaders, System.Net.TransportContext context)
        {
            var jsonValue = (JsonValue)value;
            jsonValue.Save(stream);
        }

        public override object OnReadFromStream(Type type, Stream stream, System.Net.Http.Headers.HttpContentHeaders contentHeaders)
        {
            var jsonObject = JsonValue.Load(stream);
            return jsonObject;
        }

        protected override bool OnCanReadType(Type type)
        {
            return typeof(JsonValue) == type;
        }

        protected override bool OnCanWriteType(Type type)
        {
            return typeof(JsonValue) == type;
        }
    }
}