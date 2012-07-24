// <copyright file="StreamMessageHelper.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Web
{
    using System;
    using System.Json;
    using System.Net;
    using System.Runtime.Serialization.Json;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.Xml;

    internal static class StreamMessageHelper
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000",
            Justification = "Object 'reply' *cannot* be disposed, as it's the return value of the method.")]
        public static Message CreateMessage(MessageVersion version, string action, string contentType, JsonValue result)
        {
            Message reply = Message.CreateMessage(version, action, new JsonValueBodyWriter(result));
            HttpResponseMessageProperty response;
            if (OperationContext.Current.OutgoingMessageProperties.ContainsKey(HttpResponseMessageProperty.Name))
            {
                response = (HttpResponseMessageProperty)OperationContext.Current.OutgoingMessageProperties[HttpResponseMessageProperty.Name];
            }
            else
            {
                response = new HttpResponseMessageProperty();
            }

            if (response.Headers[HttpResponseHeader.ContentType] == null)
            {
                response.Headers[System.Net.HttpResponseHeader.ContentType] = contentType;
            }

            reply.Properties.Add(HttpResponseMessageProperty.Name, response);
            reply.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Json));

            return reply;
        }

        private class JsonValueBodyWriter : BodyWriter
        {
            private JsonValue json;

            public JsonValueBodyWriter(JsonValue json)
                : base(true)
            {
                this.json = json;
            }

            protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
            {
                if (this.json != null)
                {
                    this.json.Save(writer);
                }
            }
        }
    }
}
