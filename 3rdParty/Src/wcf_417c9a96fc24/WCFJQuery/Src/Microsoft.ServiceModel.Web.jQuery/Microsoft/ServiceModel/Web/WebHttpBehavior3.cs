// <copyright file="WebHttpBehavior3.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Web
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.IO;
    using System.Json;
    using System.Net;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using System.ServiceModel.Web;
    using System.Xml.Linq;

    /// <summary>
    /// Enables the System.Json extensions for the Web programming model for a service.
    /// </summary>
    public class WebHttpBehavior3 : WebHttpBehavior
    {
        private bool automaticFormatSelectionEnabled = true;
        private bool faultExceptionEnabled = false;
        private bool helpEnabled = true;
        private bool validationEnabled = false;

        /// <summary>
        /// Gets or sets a value that determines if automatic format selection is enabled.
        /// </summary>
        public override bool AutomaticFormatSelectionEnabled
        {
            get
            {
                return this.automaticFormatSelectionEnabled;
            }

            set
            {
                this.automaticFormatSelectionEnabled = value;
            }
        }

        /// <summary>
        /// Gets or sets the flag that specifies whether a FaultException is generated when
        /// an internal server error (HTTP status code: 500) occurs.
        /// </summary>
        public override bool FaultExceptionEnabled
        {
            get
            {
                return this.faultExceptionEnabled;
            }

            set
            {
                this.faultExceptionEnabled = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that determines if the Help page is enabled.
        /// </summary>
        /// <remarks>For more information about the REST Help page, see
        /// <a href="http://msdn.microsoft.com/en-us/library/ee230442.aspx">WCF REST Service Help Page</a>.</remarks>
        public override bool HelpEnabled
        {
            get
            {
                return this.helpEnabled;
            }

            set
            {
                this.helpEnabled = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether validation is to be performed for typed
        /// arguments.
        /// </summary>
        public bool ValidationEnabled
        {
            get
            {
                return this.validationEnabled;
            }

            set
            {
                this.validationEnabled = value;
            }
        }

        /// <summary>
        /// Implements the Implements the <see cref="System.ServiceModel.Description.IEndpointBehavior.ApplyDispatchBehavior">ApplyDispatchBehavior(ServiceEndpoint,
        /// EndpointDispatcher)</see> method to support modification or extension of the service across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that exposes the contract.</param>
        /// <param name="endpointDispatcher">The endpoint dispatcher to which the behavior is applied.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1",
            Justification = "This is called by WCF and will not be null.")]
        public override void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            if (this.ValidationEnabled)
            {
                foreach (DispatchOperation operation in endpointDispatcher.DispatchRuntime.Operations)
                {
                    operation.ParameterInspectors.Add(new ValidationParameterInspector());
                }
            }

            base.ApplyDispatchBehavior(endpoint, endpointDispatcher);
        }

        /// <summary>
        /// Gets the reply formatter on the service for the specified endpoint and service operation.
        /// </summary>
        /// <param name="operationDescription">The service operation.</param>
        /// <param name="endpoint">The service endpoint.</param>
        /// <returns>An <see cref="System.ServiceModel.Dispatcher.IDispatchMessageFormatter"/> reference to the reply formatter on the service for the specified operation and endpoint.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0",
            Justification = "This is called by WCF and will not be null.")]
        protected override IDispatchMessageFormatter GetReplyDispatchFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
        {
            if (operationDescription.Messages.Count > 1)
            {
                if (typeof(JsonValue).IsAssignableFrom(operationDescription.Messages[1].Body.ReturnValue.Type) && operationDescription.Messages[1].Body.Parts.Count == 0)
                {
                    this.CheckBodyStyle(operationDescription, false);
                    CheckResponseFormat(operationDescription);
                    return new JsonValueFormatter(operationDescription, endpoint, null, 0);
                }
            }

            return base.GetReplyDispatchFormatter(operationDescription, endpoint);
        }

        /// <summary>
        /// Gets the request formatter on the service for the specified endpoint and service operation.
        /// </summary>
        /// <param name="operationDescription">The service operation.</param>
        /// <param name="endpoint">The service endpoint.</param>
        /// <returns>An <see cref="System.ServiceModel.Dispatcher.IDispatchMessageFormatter"/> reference to the request formatter on the service for the specified operation and endpoint.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0",
            Justification = "This is called by WCF and will not be null.")]
        protected override IDispatchMessageFormatter GetRequestDispatchFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
        {
            MessagePartDescriptionCollection parts = operationDescription.Messages[0].Body.Parts;
            int jsonValuePosition = 0;
            string requestMethod = GetHttpRequestMethod(operationDescription);
            if (requestMethod != "GET" && requestMethod != "HEAD" &&
                parts.Count > 0 &&
                HasExactlyOneJsonValue(parts, out jsonValuePosition))
            {
                this.CheckBodyStyle(operationDescription, true);
                this.CheckNoUnmappedParameters(operationDescription, jsonValuePosition);
                return new JsonValueFormatter(operationDescription, endpoint, this.GetQueryStringConverter(operationDescription), jsonValuePosition);
            }

            return base.GetRequestDispatchFormatter(operationDescription, endpoint);
        }

        /// <summary>
        /// Overrides the base method to add an <see cref="System.ServiceModel.Dispatcher.IErrorHandler"/> which can
        /// return correctly formatted faults for <see cref="System.Json.JsonValue"/> and <see cref="System.ComponentModel.DataAnnotations.ValidationException"/>.
        /// </summary>
        /// <param name="endpoint">The service endpoint.</param>
        /// <param name="endpointDispatcher">The endpoint dispatcher to which the error handler is applied.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1",
            Justification = "This is called by WCF and will not be null.")]
        protected override void AddServerErrorHandlers(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            int errorHandlerCount = endpointDispatcher.DispatchRuntime.ChannelDispatcher.ErrorHandlers.Count;
            base.AddServerErrorHandlers(endpoint, endpointDispatcher);
            IErrorHandler baseHandler = endpointDispatcher.DispatchRuntime.ChannelDispatcher.ErrorHandlers[errorHandlerCount];
            endpointDispatcher.DispatchRuntime.ChannelDispatcher.ErrorHandlers.RemoveAt(errorHandlerCount);
            JsonValueAwareValidationErrorHandler newHandler = new JsonValueAwareValidationErrorHandler(baseHandler);
            endpointDispatcher.DispatchRuntime.ChannelDispatcher.ErrorHandlers.Add(newHandler);
        }

        private static bool HasExactlyOneJsonValue(MessagePartDescriptionCollection parts, out int jsonValuePosition)
        {
            int count = 0;
            jsonValuePosition = -1;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].Type == typeof(JsonValue) || parts[i].Type == typeof(JsonObject))
                {
                    count++;
                    jsonValuePosition = i;
                }

                if (count > 1)
                {
                    break;
                }
            }

            return count == 1;
        }

        private static void CheckWebInvokeAndWebGet(WebInvokeAttribute wia, WebGetAttribute wga, OperationDescription operationDescription)
        {
            if (wga != null && wia != null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(DiagnosticUtility.GetString(SR.OperationContractConflictsWebGetOrWebInvoke, operationDescription.Name)));
            }
        }

        private static string GetHttpRequestMethod(OperationDescription operationDescription)
        {
            WebGetAttribute wga = operationDescription.Behaviors.Find<WebGetAttribute>();
            WebInvokeAttribute wia = operationDescription.Behaviors.Find<WebInvokeAttribute>();
            CheckWebInvokeAndWebGet(wia, wga, operationDescription);

            if (wga != null)
            {
                return "GET";
            }

            if (wia != null && wia.Method != null)
            {
                return wia.Method;
            }

            return "POST";
        }

        private static void CheckResponseFormat(OperationDescription operationDescription)
        {
            WebInvokeAttribute wia = operationDescription.Behaviors.Find<WebInvokeAttribute>();
            WebGetAttribute wga = operationDescription.Behaviors.Find<WebGetAttribute>();
            CheckWebInvokeAndWebGet(wia, wga, operationDescription);

            WebMessageFormat? outgoingResponseFormat = null;
            if (wia != null && wia.IsResponseFormatSetExplicitly)
            {
                outgoingResponseFormat = wia.ResponseFormat;
            }
            else if (wga != null && wga.IsResponseFormatSetExplicitly)
            {
                outgoingResponseFormat = wga.ResponseFormat;
            }

            if (outgoingResponseFormat.HasValue && outgoingResponseFormat.Value != WebMessageFormat.Json)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(DiagnosticUtility.GetString(SR.ResponseFormatMustBeJson, operationDescription.Name)));
            }
        }

        private void CheckNoUnmappedParameters(OperationDescription operationDescription, int jsonValuePosition)
        {
            string uriTemplate = null;
            WebInvokeAttribute wia = operationDescription.Behaviors.Find<WebInvokeAttribute>();
            MessagePartDescriptionCollection inputBodyParts = operationDescription.Messages[0].Body.Parts;
            if (wia != null)
            {
                uriTemplate = wia.UriTemplate;
            }

            // if uriTemplate == null, no parameters are bound to the template; JsonValue must be the only parameter in the operation
            if (uriTemplate != null)
            {
                UriTemplate template = new UriTemplate(uriTemplate);
                Dictionary<string, Type> operationParameters = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, bool> mappedOperationParameters = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < inputBodyParts.Count; i++)
                {
                    if (i != jsonValuePosition)
                    {
                        operationParameters.Add(inputBodyParts[i].Name, inputBodyParts[i].Type);
                        mappedOperationParameters[inputBodyParts[i].Name] = false;
                    }
                }

                foreach (string pathVar in template.PathSegmentVariableNames)
                {
                    if (operationParameters.ContainsKey(pathVar))
                    {
                        mappedOperationParameters[pathVar] = true;
                    }
                    else
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new InvalidOperationException(
                                DiagnosticUtility.GetString(
                                    SR.UriTemplateParameterNotInOperation,
                                    operationDescription.Name,
                                    operationDescription.DeclaringContract.Name,
                                    pathVar)));
                    }
                }

                QueryStringConverter qsc = this.GetQueryStringConverter(operationDescription);
                foreach (string queryVar in template.QueryValueVariableNames)
                {
                    if (operationParameters.ContainsKey(queryVar))
                    {
                        mappedOperationParameters[queryVar] = true;
                        if (!qsc.CanConvert(operationParameters[queryVar]))
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                new InvalidOperationException(
                                    DiagnosticUtility.GetString(
                                        SR.QueryVariableCannotBeConverted,
                                        operationDescription.Name,
                                        operationDescription.DeclaringContract.Name,
                                        queryVar,
                                        operationParameters[queryVar].FullName,
                                        qsc.GetType().Name)));
                        }
                    }
                    else
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new InvalidOperationException(
                                DiagnosticUtility.GetString(
                                    SR.UriTemplateParameterNotInOperation,
                                    operationDescription.Name,
                                    operationDescription.DeclaringContract.Name,
                                    queryVar)));
                    }
                }

                foreach (string paramName in mappedOperationParameters.Keys)
                {
                    if (!mappedOperationParameters[paramName])
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new InvalidOperationException(
                                DiagnosticUtility.GetString(
                                    SR.ParameterUnmappedInUriTemplate,
                                    operationDescription.Name,
                                    operationDescription.DeclaringContract.Name)));
                    }
                }
            }
        }

        private void CheckBodyStyle(OperationDescription operationDescription, bool isRequest)
        {
            WebInvokeAttribute wia = operationDescription.Behaviors.Find<WebInvokeAttribute>();
            WebGetAttribute wga = operationDescription.Behaviors.Find<WebGetAttribute>();
            CheckWebInvokeAndWebGet(wia, wga, operationDescription);

            WebMessageBodyStyle bodyStyle = this.DefaultBodyStyle;
            if (wia != null && wia.IsBodyStyleSetExplicitly)
            {
                bodyStyle = wia.BodyStyle;
            }
            else if (wga != null && wga.IsBodyStyleSetExplicitly)
            {
                bodyStyle = wga.BodyStyle;
            }

            if (isRequest)
            {
                if (wga == null && (bodyStyle == WebMessageBodyStyle.Wrapped || bodyStyle == WebMessageBodyStyle.WrappedRequest))
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.FormEncodedMustBeBare));
                }
            }
            else
            {
                if (bodyStyle == WebMessageBodyStyle.Wrapped || bodyStyle == WebMessageBodyStyle.WrappedResponse)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.FormEncodedMustBeBare));
                }
            }
        }

        private class JsonValueAwareValidationErrorHandler : IErrorHandler
        {
            private IErrorHandler baseHandler;

            public JsonValueAwareValidationErrorHandler(IErrorHandler baseHandler)
            {
                this.baseHandler = baseHandler;
            }

            public bool HandleError(Exception error)
            {
                return true;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0",
            Justification = "This is called by WCF and will not be null.")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily",
                Justification = "For code simplicity, casted value is not cached.")]
            public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
            {
                Type errorType = error.GetType();
                bool handled = false;
                WebFaultException<XElement> webFaultOfXElement = error as WebFaultException<XElement>;
                if (webFaultOfXElement != null)
                {
                    handled = true;
                    WebOperationContext.Current.OutgoingResponse.StatusCode = webFaultOfXElement.StatusCode;
                    fault = WebOperationContext.Current.CreateXmlResponse(webFaultOfXElement.Detail);
                }
                else if (errorType.IsGenericType && errorType.GetGenericTypeDefinition() == typeof(WebFaultException<>))
                {
                    Type genericParameter = errorType.GetGenericArguments()[0];
                    if (typeof(JsonValue).IsAssignableFrom(genericParameter))
                    {
                        handled = true;
                        JsonValue detail = (JsonValue)errorType.GetProperty("Detail").GetValue(error, null);
                        HttpStatusCode statusCode = (HttpStatusCode)errorType.GetProperty("StatusCode").GetValue(error, null);
                        WebOperationContext.Current.OutgoingResponse.StatusCode = statusCode;
                        if (detail == null)
                        {
                            WebOperationContext.Current.OutgoingResponse.SuppressEntityBody = true;
                            fault = WebOperationContext.Current.CreateStreamResponse(Stream.Null, string.Empty);
                        }
                        else
                        {
                            fault = CreateJsonObjectMessage(version, detail);
                        }
                    }
                }
                else if (error is ValidationException)
                {
                    ValidationException validationError = error as ValidationException;
                    if (validationError != null)
                    {
                        handled = true;
                        JsonObject jsonError = new JsonObject();
                        foreach (string name in validationError.ValidationResult.MemberNames)
                        {
                            jsonError.Add(name, validationError.ValidationResult.ErrorMessage);
                        }

                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
                        fault = CreateJsonObjectMessage(version, jsonError);
                    }
                }

                if (!handled)
                {
                    this.baseHandler.ProvideFault(error, version, ref fault);
                }
            }

            private static Message CreateJsonObjectMessage(MessageVersion version, JsonValue detail)
            {
                string contentType = JsonValueFormatter.ApplicationJsonContentType + "; charset=utf-8";
                WebOperationContext.Current.OutgoingResponse.Headers.Remove(HttpResponseHeader.ContentType); // we need to force it to be JSON, even if it was set in the operation
                Message fault = StreamMessageHelper.CreateMessage(version, null, contentType, detail);
                return fault;
            }
        }

        private class ValidationParameterInspector : IParameterInspector
        {
            public void AfterCall(string operationName, object[] outputs, object returnValue, object correlationState)
            {
                // No validation after call 
            }

            public object BeforeCall(string operationName, object[] inputs)
            {
                if (inputs != null)
                {
                    foreach (object input in inputs)
                    {
                        if (input != null)
                        {
                            Validator.ValidateObject(input, new ValidationContext(input, null, null), /*validateAllProperties*/ true);
                        }
                    }
                }

                return null;
            }
        }
    }
}
