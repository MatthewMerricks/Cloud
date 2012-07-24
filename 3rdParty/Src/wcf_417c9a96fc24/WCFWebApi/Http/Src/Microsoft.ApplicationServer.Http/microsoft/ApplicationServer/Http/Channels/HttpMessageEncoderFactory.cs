// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.ServiceModel.Channels;

    // TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available
    //// using SMTD = System.ServiceModel.Diagnostics.Application.TD;
    //// using WebTD = System.ServiceModel.Web.Diagnostics.Application.TD;

    internal class HttpMessageEncoderFactory : MessageEncoderFactory
    {
        private HttpMessageEncoder encoder;

        public HttpMessageEncoderFactory()
        {
            this.encoder = new HttpMessageEncoder();
        }

        public override MessageEncoder Encoder
        {
            get
            {
                return this.encoder;
            }
        }

        public override MessageVersion MessageVersion
        {
            get
            {
                return MessageVersion.None;
            }
        }

        public override MessageEncoder CreateSessionEncoder()
        {
            throw Fx.Exception.AsError(
                new NotSupportedException(
                    SR.HttpMessageEncoderFactoryDoesNotSupportSessionEncoder(typeof(HttpMessageEncoderFactory))));
        }

        private class HttpMessageEncoder : MessageEncoder
        {
            private const string ContentTypeHeaderName = "Content-Type";
            private const string MaxSentMessageSizeExceededResourceStringName = "MaxSentMessageSizeExceeded";
            private static readonly string httpBindingClassName = typeof(HttpBinding).FullName;
            private static readonly string httpResponseMessageClassName = typeof(HttpResponseMessage).FullName;

            public override string ContentType
            {
                get
                {
                    return string.Empty;
                }
            }

            public override string MediaType
            {
                get
                {
                    return string.Empty;
                }
            }

            public override MessageVersion MessageVersion
            {
                get
                {
                    return MessageVersion.None;
                }
            }

            public override bool IsContentTypeSupported(string contentType)
            {
                if (contentType == null)
                {
                    throw Fx.Exception.ArgumentNull("contentType");
                }

                return true;
            }

            public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
            {
                if (bufferManager == null)
                {
                    throw Fx.Exception.ArgumentNull("bufferManager");
                }

                // TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available:
                //// if (WebTD.HttpMessageDecodingStartIsEnabled())
                //// {
                ////     WebTD.HttpMessageDecodingStart();
                //// }

                HttpRequestMessage request = new HttpRequestMessage();
                request.Content = new ByteArrayBufferManagerContent(bufferManager, buffer.Array, buffer.Offset, buffer.Count);
                if (!string.IsNullOrEmpty(contentType))
                {
                    request.Content.Headers.Add(ContentTypeHeaderName, contentType);
                }

                Message message = request.ToMessage();
                message.Properties.Encoder = this;

                // TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available:
                //// if (TD.MessageReadByEncoderIsEnabled() && buffer != null)
                //// {
                ////     TD.MessageReadByEncoder(
                ////             EventTraceActivityHelper.TryExtractActivity(message, true),
                ////             buffer.Count,
                ////             this);
                //// }

                // TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available:
                //// if (MessageLogger.LogMessagesAtTransportLevel)
                //// {
                ////     MessageLogger.LogMessage(ref message, MessageLoggingSource.TransportReceive);
                //// }

                return message;
            }

            public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
            {
                if (stream == null)
                {
                    throw Fx.Exception.ArgumentNull("stream");
                }

                // TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available:
                //// if (WebTD.HttpMessageDecodingStartIsEnabled())
                //// {
                ////     WebTD.HttpMessageDecodingStart();
                //// }

                HttpRequestMessage request = new HttpRequestMessage();
                request.Content = new StreamContent(stream);
                if (!string.IsNullOrEmpty(contentType))
                {
                    request.Content.Headers.Add(ContentTypeHeaderName, contentType);
                }

                Message message = request.ToMessage();
                message.Properties.Encoder = this;

                // TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available:
                //// if (TD.StreamedMessageReadByEncoderIsEnabled())
                //// {
                ////     TD.StreamedMessageReadByEncoder(EventTraceActivityHelper.TryExtractActivity(message, true));
                //// }

                // TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available:
                //// if (MessageLogger.LogMessagesAtTransportLevel)
                //// {
                ////     MessageLogger.LogMessage(ref message, MessageLoggingSource.TransportReceive);
                //// }

                return message;
            }

            public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
            {
                if (message == null)
                {
                    throw Fx.Exception.ArgumentNull("message");
                }

                if (bufferManager == null)
                {
                    throw Fx.Exception.ArgumentNull("bufferManager");
                }

                if (maxMessageSize < 0)
                {
                    throw Fx.Exception.AsError(new ArgumentOutOfRangeException("maxMessageSize"));
                }

                if (messageOffset < 0)
                {
                    throw Fx.Exception.AsError(new ArgumentOutOfRangeException("messageOffset"));
                }

                if (messageOffset > maxMessageSize)
                {
                    throw Fx.Exception.Argument(
                        string.Empty,
                        SR.ParameterMustBeLessThanOrEqualSecondParameter("messageOffset", "maxMessageSize"));
                }

                // TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available:
                //// EventTraceActivity eventTraceActivity = null;
                //// if (WebTD.HttpMessagEncodingStartIsEnabled())
                //// {
                ////     eventTraceActivity = EventTraceActivityHelper.TryExtractActivity(message);
                ////     WebTD.HttpMessagEncodingStart(eventTraceActivity);
                //// }

                using (BufferManagerOutputStream stream = new BufferManagerOutputStream(MaxSentMessageSizeExceededResourceStringName, 0, maxMessageSize, bufferManager))
                {
                    int num;
                    stream.Skip(messageOffset);
                    this.WriteMessage(message, stream);
                    ArraySegment<byte> messageData = new ArraySegment<byte>(stream.ToArray(out num), 0, num - messageOffset);
                    
                    // TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available:
                    //// if (SMTD.MessageWrittenByEncoderIsEnabled() && messageData != null)
                    //// {
                    ////     SMTD.MessageWrittenByEncoder(
                    ////             eventTraceActivity ?? EventTraceActivityHelper.TryExtractActivity(message),
                    ////             messageData.Count,
                    ////             this);
                    //// }

                    return messageData;
                }
            }

            public override void WriteMessage(Message message, Stream stream)
            {
                if (message == null)
                {
                    throw Fx.Exception.ArgumentNull("message");
                }

                if (stream == null)
                {
                    throw Fx.Exception.ArgumentNull("stream");
                }

                this.ThrowIfMismatchedMessageVersion(message);

                // TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available:
                //// EventTraceActivity eventTraceActivity = null;
                //// if (WebTD.HttpMessagEncodingStartIsEnabled())
                //// {
                ////     eventTraceActivity = EventTraceActivityHelper.TryExtractActivity(message);
                ////     WebTD.HttpMessagEncodingStart(eventTraceActivity);
                //// }

                message.Properties.Encoder = this;

                HttpResponseMessage response = GetHttpResponseMessageOrThrow(message);

                if (response.Content != null)
                {
                    response.Content.CopyTo(stream);
                }

                // TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available:
                //// if (SMTD.StreamedMessageWrittenByEncoderIsEnabled())
                //// {
                ////     SMTD.StreamedMessageWrittenByEncoder(eventTraceActivity ?? EventTraceActivityHelper.TryExtractActivity(message));
                //// }

                // TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available:
                //// if (MessageLogger.LogMessagesAtTransportLevel)
                //// {
                ////     MessageLogger.LogMessage(ref message, MessageLoggingSource.TransportSend);
                //// }
            }

            internal void ThrowIfMismatchedMessageVersion(Message message)
            {
                if (message.Version != MessageVersion)
                {
                    throw Fx.Exception.AsError(
                        new ProtocolException(
                            SR.EncoderMessageVersionMismatch(message.Version, MessageVersion)));
                }
            }

            private static HttpResponseMessage GetHttpResponseMessageOrThrow(Message message)
            {
                HttpResponseMessage response = message.ToHttpResponseMessage();
                if (response == null)
                {
                    throw Fx.Exception.AsError(
                        new InvalidOperationException(
                            SR.MessageInvalidForHttpMessageEncoder(
                                httpBindingClassName,
                                HttpMessageExtensionMethods.ToMessageMethodName,
                                httpResponseMessageClassName)));
                }

                return response;
            }

            private class ByteArrayBufferManagerContent : ByteArrayContent
            {
                private bool disposed;
                private BufferManager bufferManager;
                private byte[] content;
                private object disposingLock;

                public ByteArrayBufferManagerContent(BufferManager bufferManager, byte[] content, int offset, int count)
                    : base(content, offset, count)
                {
                    Fx.Assert(bufferManager != null, "The 'bufferManager' parameter should never be null.");

                    this.bufferManager = bufferManager;
                    this.content = content;
                    this.disposingLock = new object();
                }

                protected override void Dispose(bool disposing)
                {
                    try
                    {
                        if (disposing && !this.disposed)
                        {
                            lock (this.disposingLock)
                            {
                                if (!this.disposed)
                                {
                                    this.disposed = true;
                                    this.bufferManager.ReturnBuffer(this.content);
                                    this.content = null;
                                }
                            }
                        }
                    }
                    finally
                    {
                        base.Dispose(disposing);
                    }
                }
            }
        }
    }
}
