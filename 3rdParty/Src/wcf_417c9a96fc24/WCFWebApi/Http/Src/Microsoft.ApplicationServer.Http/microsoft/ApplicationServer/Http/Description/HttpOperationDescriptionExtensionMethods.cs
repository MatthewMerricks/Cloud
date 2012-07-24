// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.ServiceModel.Description;
    using System.ServiceModel.Web;
    using System.Text;
    using System.Xml;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Dispatcher;
    using Microsoft.ApplicationServer.ServiceModel;

    /// <summary>
    /// Provides extension methods for <see cref="OperationDescription"/> instances.
    /// </summary>
    public static class HttpOperationDescriptionExtensionMethods
    {
        private static readonly Type webGetAttributeType = typeof(WebGetAttribute);
        private static readonly Type webInvokeAttributeType = typeof(WebInvokeAttribute);
        private static readonly Type descriptionAttributeType = typeof(DescriptionAttribute);

        /// <summary>
        /// Creates an <see cref="HttpOperationDescription"/> instance based on the given
        /// <see cref="OperationDescription"/> instance.
        /// </summary>
        /// <param name="operation">The <see cref="OperationDescription"/> instance.</param>
        /// <returns>A new <see cref="HttpOperationDescription"/> instance.</returns>
        public static HttpOperationDescription ToHttpOperationDescription(this OperationDescription operation)
        {
            if (operation == null)
            {
                throw Fx.Exception.ArgumentNull("operation");
            }

            return new HttpOperationDescription(operation);
        }

        /// <summary>
        /// Gets the <see cref="HttpMethod"/> for the given <paramref name="operation"/>.
        /// </summary>
        /// <param name="operation">The <see cref="HttpOperationDescription"/> instance.</param>
        /// <returns>The <see cref="HttpMethod"/>.</returns>
        public static HttpMethod GetHttpMethod(this HttpOperationDescription operation)
        {
            if (operation == null)
            {
                throw Fx.Exception.ArgumentNull("operation");
            }

            return new HttpMethod(GetWebMethod(operation));
        }

        /// <summary>
        /// Gets the <see cref="UriTemplate"/> associated with the given <paramref name="operation"/>.
        /// </summary>
        /// <param name="operation">The <see cref="HttpOperationDescription"/> instance.</param>
        /// <returns>The <see cref="UriTemplate"/>.</returns>
        public static UriTemplate GetUriTemplate(this HttpOperationDescription operation)
        {
            if (operation == null)
            {
                throw Fx.Exception.ArgumentNull("operation");
            }

            return new UriTemplate(operation.GetUriTemplateStringOrDefault());
        }

        internal static string GetDescription(this HttpOperationDescription operation)
        {
            Fx.Assert(operation != null, "The 'operation' parameter should not be null.");

            OperationDescription operationDescription = operation.ToOperationDescription();

            object[] attributes = null;
            if (operationDescription.SyncMethod != null)
            {
                attributes = operationDescription.SyncMethod.GetCustomAttributes(descriptionAttributeType, true);
            }
            else if (operationDescription.BeginMethod != null)
            {
                attributes = operationDescription.BeginMethod.GetCustomAttributes(descriptionAttributeType, true);
            }

            if (attributes != null && attributes.Length > 0)
            {
                return ((DescriptionAttribute)attributes[0]).Description;
            }
            else
            {
                return String.Empty;
            }
        }

        private static string GetWebMethod(this HttpOperationDescription operation)
        {
            Fx.Assert(operation != null, "The 'operation' parameter should not be null.");

            WebGetAttribute webGet = operation.Behaviors.Find<WebGetAttribute>();
            WebInvokeAttribute webInvoke = operation.Behaviors.Find<WebInvokeAttribute>();
            EnsureOneOneWebAttribute(webGet, webInvoke, operation);
            if (webGet != null)
            {
                return HttpMethod.Get.ToString();
            }

            if (webInvoke == null)
            {
                return HttpMethod.Post.ToString();
            }

            return webInvoke.Method ?? HttpMethod.Post.ToString();
        }

        private static string GetWebUriTemplate(this HttpOperationDescription operation)
        {
            Fx.Assert(operation != null, "The 'operation' parameter should not be null.");

            WebGetAttribute webGet = operation.Behaviors.Find<WebGetAttribute>();
            WebInvokeAttribute webInvoke = operation.Behaviors.Find<WebInvokeAttribute>();
            EnsureOneOneWebAttribute(webGet, webInvoke, operation);
            if (webGet != null)
            {
                return webGet.UriTemplate;
            }

            if (webInvoke != null)
            {
                return webInvoke.UriTemplate;
            }

            return null;
        }

        private static string GetUriTemplateStringOrDefault(this HttpOperationDescription operation)
        {
            Fx.Assert(operation != null, "The 'operation' parameter should not be null.");

            string webUriTemplate = GetWebUriTemplate(operation);
            if ((webUriTemplate == null) && (GetWebMethod(operation) == HttpMethod.Get.ToString()))
            {
                webUriTemplate = MakeDefaultGetUriTemplateString(operation);
            }

            if (webUriTemplate == null)
            {
                webUriTemplate = operation.Name;
            }

            return webUriTemplate;
        }

        private static string MakeDefaultGetUriTemplateString(this HttpOperationDescription operation)
        {
            Fx.Assert(operation != null, "The 'operation' parameter should not be null.");

            StringBuilder builder = new StringBuilder(XmlConvert.DecodeName(operation.Name));
            builder.Append("?");

            foreach (HttpParameter parameterDescription in operation.InputParameters)
            {
                string decodedName = XmlConvert.DecodeName(parameterDescription.Name);

                builder.Append(decodedName);
                builder.Append("={");
                builder.Append(decodedName);
                builder.Append("}&");
            }

            builder.Remove(builder.Length - 1, 1);
            return builder.ToString();
        }

        private static void EnsureOneOneWebAttribute(WebGetAttribute webGet, WebInvokeAttribute webInvoke, HttpOperationDescription operation)
        {
            if (webGet != null && webInvoke != null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.MultipleWebAttributes(
                            operation.Name,
                            operation.DeclaringContract.Name,
                            webGetAttributeType.Name,
                            webInvokeAttributeType.Name)));
            }
        }
    }
}
