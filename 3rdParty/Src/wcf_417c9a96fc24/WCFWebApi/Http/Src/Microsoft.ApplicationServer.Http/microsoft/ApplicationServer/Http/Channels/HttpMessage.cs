// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.Net.Http;
    using System.ServiceModel.Channels;
    using System.Xml;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http;

    internal class HttpMessage : Message
    {
        internal static readonly string MessageTypeFullName = typeof(Message).FullName;

        private HttpRequestMessage request;
        private HttpResponseMessage response;
        private MessageHeaders headers;
        private MessageProperties properties;

        internal HttpMessage(HttpRequestMessage request)
        {
            Fx.Assert(request != null, "The 'request' parameter should not be null.");
            this.request = request;
            this.Headers.To = request.RequestUri; 
            this.IsRequest = true;
        }

        internal HttpMessage(HttpResponseMessage response)
        {
            Fx.Assert(response != null, "The 'response' parameter should not be null.");
            this.response = response;
            this.IsRequest = false;
        }

        public override MessageVersion Version
        {
            get
            {
                this.EnsureNotDisposed();
                return MessageVersion.None;
            }
        }
        
        public override MessageHeaders Headers
        {
            get
            {
                this.EnsureNotDisposed();
                if (this.headers == null)
                {
                    this.headers = new MessageHeaders(MessageVersion.None);
                }

                return this.headers;
            }
        }

        public override MessageProperties Properties
        {
            get
            {
                this.EnsureNotDisposed();
                if (this.properties == null)
                {
                    this.properties = new MessageProperties();
                    this.properties.AllowOutputBatching = false;
                }

                return this.properties;
            }
        }

        public override bool IsEmpty
        {
            get
            {
                long? contentLength = this.GetHttpContentLength();
                return contentLength.HasValue && contentLength.Value == 0;
            }
        }

        public override bool IsFault
        {
            get
            {
                return false;
            }
        }

        internal bool IsRequest { get; private set; }

        internal HttpRequestMessage GetHttpRequestMessage(bool extract)
        {
            this.EnsureNotDisposed();
            Fx.Assert(this.IsRequest, "This method should only be called when IsRequest is true.");
            if (extract)
            {
                HttpRequestMessage req = this.request;
                this.request = null;
                return req;
            }

            return this.request;
        }

        internal HttpResponseMessage GetHttpResponseMessage(bool extract)
        {
            this.EnsureNotDisposed();
            Fx.Assert(!this.IsRequest, "This method should only be called when IsRequest is false.");
            if (extract)
            {
                HttpResponseMessage res = this.response;
                this.response = null;
                return res;
            }
            
            return this.response;
        }

        protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
        {
            throw Fx.Exception.AsError(GetNotSupportedException());
        }

        protected override MessageBuffer OnCreateBufferedCopy(int maxBufferSize)
        {
            throw Fx.Exception.AsError(GetNotSupportedException());
        }

        protected override XmlDictionaryReader OnGetReaderAtBodyContents()
        {
            throw Fx.Exception.AsError(GetNotSupportedException());
        }

        protected override string OnGetBodyAttribute(string localName, string ns)
        {
            throw Fx.Exception.AsError(GetNotSupportedException());
        }

        protected override void OnWriteStartBody(XmlDictionaryWriter writer)
        {
            throw Fx.Exception.AsError(GetNotSupportedException());
        }

        protected override void OnWriteStartEnvelope(XmlDictionaryWriter writer)
        {
            throw Fx.Exception.AsError(GetNotSupportedException());
        }

        protected override void OnBodyToString(XmlDictionaryWriter writer)
        {
            long? contentLength = this.GetHttpContentLength();
            string contentString = null;

            if (this.IsRequest)
            {
                contentString = contentLength.HasValue ?
                    SR.MessageBodyIsHttpRequestMessageWithKnownContentLength(contentLength.Value) :
                    SR.MessageBodyIsHttpRequestMessageWithUnknownContentLength;
            }
            else
            {
                contentString = contentLength.HasValue ?
                    SR.MessageBodyIsHttpResponseMessageWithKnownContentLength(contentLength.Value) :
                    SR.MessageBodyIsHttpResponseMessageWithUnknownContentLength;
            }

            writer.WriteString(contentString);
        }

        protected override void OnWriteMessage(XmlDictionaryWriter writer)
        {
            throw Fx.Exception.AsError(GetNotSupportedException());
        }

        protected override void OnWriteStartHeaders(XmlDictionaryWriter writer)
        {
            throw Fx.Exception.AsError(GetNotSupportedException());
        }

        protected override void OnClose()
        {
            base.OnClose();
            if (this.request != null)
            {
                this.request.Dispose();
                this.request = null;
            }
            
            if (this.response != null)
            {
                this.response.Dispose();
                this.response = null;
            }
        }

        private static NotSupportedException GetNotSupportedException()
        {
            return new NotSupportedException(
                                    SR.MessageReadWriteCopyNotSupported(
                                        HttpMessageExtensionMethods.ToHttpRequestMessageMethodName,
                                        HttpMessageExtensionMethods.ToHttpResponseMessageMethodName,
                                        MessageTypeFullName));
        }

        private void EnsureNotDisposed()
        {
            if (this.IsDisposed)
            {
                throw Fx.Exception.AsError(
                    new ObjectDisposedException(
                              string.Empty,
                              SR.MessageClosed));
            }
        }

        private long? GetHttpContentLength()
        {
            HttpContent content = this.IsRequest ?
                this.GetHttpRequestMessage(false).Content :
                this.GetHttpResponseMessage(false).Content;

            if (content == null)
            {
                return 0;
            }

            return content.Headers.ContentLength;
        }
    }
}
