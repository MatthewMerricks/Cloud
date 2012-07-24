// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>


using Microsoft.ServiceModel.Web;

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using System.ServiceModel.Description;

    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.ApplicationServer.Http.Channels;
    using System.ServiceModel;
    using System.Json;

    public class FormUrlEncodedMediaTypeFormatter : MediaTypeFormatter
    {
        public FormUrlEncodedMediaTypeFormatter()
        {
            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/x-www-form-urlencoded"));
        }

        public override object OnReadFromStream(Type type, Stream stream, HttpContentHeaders contentHeaders)
        {
            var reader = new StreamReader(stream);
            var jsonContent = reader.ReadToEnd();
            var jsonObject = FormUrlEncodedExtensions.ParseFormUrlEncoded(jsonContent);

            if (typeof(JsonValue) == type)
                return jsonObject;

            var value = jsonObject.ReadAsType(type);
            return value;
        }

        public override void OnWriteToStream(Type type, object value, Stream stream, HttpContentHeaders contentHeaders, System.Net.TransportContext context)
        {
            throw new NotImplementedException();
        }

        //form url encoding is only for the sending in the request
        protected override bool OnCanWriteType(Type type)
        {
            return false;
        }

        protected override bool OnCanReadType(Type type)
        {
            return true;
        }
    }
}