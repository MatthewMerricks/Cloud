// <copyright file="JsonValueFormatter.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Web
{
    using System;
    using System.IO;
    using System.Json;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using System.ServiceModel.Web;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    internal class JsonValueFormatter : IDispatchMessageFormatter
    {
        internal const string BinaryElementName = "Binary";
        internal const string ApplicationJsonContentType = "application/json";
        private const string FormUrlEncodedContentType = "application/x-www-form-urlencoded";
        private static readonly Regex contentTypeCharset = new Regex(@"[^;]+\s*\;\s*charset\s*=\s*(?<charset>\S+)", RegexOptions.IgnoreCase);
        private OperationDescription operationDescription;
        private QueryStringConverter queryStringConverter;
        private int jsonValuePosition;
        private string charset;
        private XmlDictionaryReaderQuotas readerQuotas;

        public JsonValueFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint, QueryStringConverter queryStringConverter, int jsonValuePosition)
        {
            this.operationDescription = operationDescription;
            this.queryStringConverter = queryStringConverter;
            this.jsonValuePosition = jsonValuePosition;

            BindingElementCollection bindingElements = endpoint.Binding.CreateBindingElements();
            WebMessageEncodingBindingElement webEncoding = bindingElements.Find<WebMessageEncodingBindingElement>();

            this.charset = CharsetFromEncoding(webEncoding);
            this.readerQuotas = new XmlDictionaryReaderQuotas();
            XmlDictionaryReaderQuotas.Max.CopyTo(this.readerQuotas);
            if (webEncoding != null)
            {
                webEncoding.ReaderQuotas.CopyTo(this.readerQuotas);
            }
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            JsonValue jsonValue = null;
            bool isJsonInput = false;

            if (message != null)
            {
                if (message.IsEmpty)
                {
                    Encoding contentEncoding;
                    if (IsContentTypeSupported(WebOperationContext.Current.IncomingRequest.ContentType, ApplicationJsonContentType, out contentEncoding))
                    {
                        isJsonInput = true;
                        jsonValue = null;
                    }
                }
                else if (message.Properties.ContainsKey(WebBodyFormatMessageProperty.Name))
                {
                    WebBodyFormatMessageProperty bodyFormatProperty =
                        (WebBodyFormatMessageProperty)message.Properties[WebBodyFormatMessageProperty.Name];
                    if (bodyFormatProperty.Format == WebContentFormat.Json)
                    {
                        isJsonInput = true;
                        jsonValue = DeserializeFromJXML(message);
                    }
                }
            }

            if (!isJsonInput)
            {
                Encoding contentEncoding;
                if (!IsContentTypeSupported(WebOperationContext.Current.IncomingRequest.ContentType, FormUrlEncodedContentType, out contentEncoding))
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.ExpectUrlEncodedOrJson));
                }

                string formData = string.Empty;
                if (message != null && !message.IsEmpty)
                {
                    XmlDictionaryReader bodyReader = message.GetReaderAtBodyContents();
                    bodyReader.ReadStartElement(BinaryElementName);

                    using (Stream s = new MemoryStream(bodyReader.ReadContentAsBase64()))
                    {
                        if (contentEncoding == null)
                        {
                            formData = new StreamReader(s).ReadToEnd();
                        }
                        else
                        {
                            formData = new StreamReader(s, contentEncoding).ReadToEnd();
                        }
                    }
                }

                jsonValue = FormUrlEncodedExtensions.ParseFormUrlEncoded(formData, this.readerQuotas.MaxDepth);
            }

            UriTemplateMatch match = WebOperationContext.Current.IncomingRequest.UriTemplateMatch;

            MessagePartDescriptionCollection messageParts = this.operationDescription.Messages[0].Body.Parts;

            Func<MessagePartDescription, object> binder = this.CreateParameterBinder(match);
            object[] values = messageParts.Select(p => binder(p)).ToArray();

            values[this.jsonValuePosition] = jsonValue;
            values.CopyTo(parameters, 0);
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            CheckMessageVersion(messageVersion);
            string contentType = string.Empty;
            if (result != null)
            {
                contentType = ApplicationJsonContentType + "; charset=" + this.charset;
            }

            Message reply = StreamMessageHelper.CreateMessage(
                messageVersion,
                this.operationDescription.Messages[1].Action,
                contentType,
                (JsonValue)result);
            return reply;
        }

        private static void CheckMessageVersion(MessageVersion messageVersion)
        {
            if (messageVersion != MessageVersion.None)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.MessageVersionMustBeNone));
            }
        }

        private static bool IsContentTypeSupported(string contentType, string expectedContentType, out Encoding contentEncoding)
        {
            contentEncoding = null;
            if (string.IsNullOrEmpty(contentType))
            {
                // body is empty
                return true;
            }
            else if (expectedContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
            {
                // no charset
                return true;
            }
            else if (contentType.StartsWith(expectedContentType, StringComparison.OrdinalIgnoreCase))
            {
                // possibly charset parameter
                Match match = contentTypeCharset.Match(contentType);
                if (match.Success)
                {
                    contentEncoding = Encoding.GetEncoding(match.Groups["charset"].Value);
                }

                return true;
            }

            return false;
        }

        private static JsonValue DeserializeFromJXML(Message message)
        {
            using (XmlDictionaryReader bodyReader = message.GetReaderAtBodyContents())
            {
                return JsonValueExtensions.Load(bodyReader);
            }
        }

        private static string CharsetFromEncoding(WebMessageEncodingBindingElement webEncoding)
        {
            const string UTF8Charset = "utf-8";
            const string UnicodeLECharset = "utf-16LE";
            const string UnicodeBECharset = "utf-16BE";
            string result = UTF8Charset;
            if (webEncoding != null)
            {
                if (webEncoding.WriteEncoding.WebName == Encoding.Unicode.WebName)
                {
                    result = UnicodeLECharset;
                }
                else if (webEncoding.WriteEncoding.WebName == Encoding.BigEndianUnicode.WebName)
                {
                    result = UnicodeBECharset;
                }
            }

            return result;
        }

        private Func<MessagePartDescription, object> CreateParameterBinder(UriTemplateMatch match)
        {
            return delegate(MessagePartDescription pi)
            {
                string value = match.BoundVariables[pi.Name];

                if (this.queryStringConverter.CanConvert(pi.Type) && value != null)
                {
                    return this.queryStringConverter.ConvertStringToValue(value, pi.Type);
                }
                else
                {
                    return value;
                }
            };
        }
    }
}