// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.ApplicationServer.Common.Test
{
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// MSTest utility for testing a self-hosted WebHttp service.
    /// </summary>
    public static class WebHttpServiceHostAssert
    {
        /// <summary>
        /// Asserts that an <see cref="HttpRequestMessage"/> sent to the self-hosted WebHttp <paramref name="serviceSingleton"/> 
        /// results in a response that is equivalent to the expected <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="serviceSingleton">The singleton for the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> instance to send to the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="expectedResponse">The expected <see cref="HttpResponseMessage"/>. Should not be <c>null</c>.</param>
        public static void Execute(object serviceSingleton, HttpRequestMessage request, HttpResponseMessage expectedResponse)
        {
            Execute(serviceSingleton, null, null, request, expectedResponse);
        }


        /// <summary>
        /// Asserts that an <see cref="HttpRequestMessage"/> sent to the self-hosted WebHttp <paramref name="serviceSingleton"/> 
        /// results in a response that is equivalent to the expected <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="serviceSingleton">The singleton for the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="behavior">The <see cref="WebHttpBehavior"/> to use with the WebHttp service.</param>
        /// <param name="binding">The <see cref="WebHttpBinding"/> to use with the WebHttp service.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> instance to send to the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="expectedResponse">The expected <see cref="HttpResponseMessage"/>. Should not be <c>null</c>.</param>
        public static void Execute(object serviceSingleton, WebHttpBehavior behavior, WebHttpBinding binding, HttpRequestMessage request, HttpResponseMessage expectedResponse)
        {
            Assert.IsNotNull(serviceSingleton, "The 'serviceSingleton' parameter should not be null.");
            Assert.IsNotNull(request, "The 'request' parameter should not be null.");
            Assert.IsNotNull(expectedResponse, "The 'expectedResponse' parameter should not be null.");

            ServiceHostAssert.Execute((baseAddress) => GetServiceHost(serviceSingleton, behavior, binding, baseAddress), request, expectedResponse);
        }

        /// <summary>
        /// Asserts that an <see cref="HttpRequestMessage"/> sent to the self-hosted WebHttp <paramref name="serviceSingleton"/> 
        /// results in the actual <see cref="HttpResponseMessage"/> given by the <paramref name="onGetActualResponse"/> <see cref="Action<>"/>.
        /// </summary>
        /// <param name="serviceSingleton">The singleton for the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> instance to send to the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="onGetActualResponse">The actual <see cref="HttpResponseMessage"/> instance provided as an <see cref="Action<>"/>. Should not be <c>null</c>.</param>
        public static void Execute(object serviceSingleton, HttpRequestMessage request, Action<HttpResponseMessage> onGetActualResponse)
        {
            Execute(serviceSingleton, null, null, request, onGetActualResponse);
        }

        /// <summary>
        /// Asserts that an <see cref="HttpRequestMessage"/> sent to the self-hosted WebHttp <paramref name="serviceSingleton"/> 
        /// results in the actual <see cref="HttpResponseMessage"/> given by the <paramref name="onGetActualResponse"/> <see cref="Action<>"/>.
        /// </summary>
        /// <param name="serviceSingleton">The singleton for the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="behavior">The <see cref="WebHttpBehavior"/> to use with the WebHttp service.</param>
        /// <param name="binding">The <see cref="WebHttpBinding"/> to use with the WebHttp service.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> instance to send to the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="onGetActualResponse">The actual <see cref="HttpResponseMessage"/> instance provided as an <see cref="Action<>"/>. Should not be <c>null</c>.</param>
        public static void Execute(object serviceSingleton, WebHttpBehavior behavior, WebHttpBinding binding, HttpRequestMessage request, Action<HttpResponseMessage> onGetActualResponse)
        {
            Assert.IsNotNull(serviceSingleton, "The 'serviceSingleton' parameter should not be null.");
            Assert.IsNotNull(request, "The 'request' parameter should not be null.");
            Assert.IsNotNull(onGetActualResponse, "The 'onGetActualResponse' parameter should not be null.");

            ServiceHostAssert.Execute((baseAddress) => GetServiceHost(serviceSingleton, behavior, binding, baseAddress), request, onGetActualResponse);
        }

        /// <summary>
        /// Asserts that an <see cref="HttpRequestMessage"/> sent to the self-hosted WebHttp <paramref name="serviceType"/> 
        /// results in a response that is equivalent to the expected <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="serviceType">The service type to use for the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> instance to send to the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="expectedResponse">The expected <see cref="HttpResponseMessage"/>. Should not be <c>null</c>.</param>
        public static void Execute(Type serviceType, HttpRequestMessage request, HttpResponseMessage expectedResponse)
        {
            Execute(serviceType, null, null, request, expectedResponse);
        }

        /// <summary>
        /// Asserts that an <see cref="HttpRequestMessage"/> sent to the self-hosted WebHttp <paramref name="serviceType"/> 
        /// results in a response that is equivalent to the expected <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="serviceType">The service type to use for the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="behavior">The <see cref="WebHttpBehavior"/> to use with the WebHttp service.</param>
        /// <param name="binding">The <see cref="WebHttpBinding"/> to use with the WebHttp service.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> instance to send to the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="expectedResponse">The expected <see cref="HttpResponseMessage"/>. Should not be <c>null</c>.</param>
        public static void Execute(Type serviceType, WebHttpBehavior behavior, WebHttpBinding binding, HttpRequestMessage request, HttpResponseMessage expectedResponse)
        {
            Assert.IsNotNull(serviceType, "The 'serviceType' parameter should not be null.");
            Assert.IsNotNull(request, "The 'request' parameter should not be null.");
            Assert.IsNotNull(expectedResponse, "The 'expectedResponse' parameter should not be null.");

            ServiceHostAssert.Execute((baseAddress) => GetServiceHost(serviceType, behavior, binding, baseAddress), request, expectedResponse);
        }

        /// <summary>
        /// Asserts that an <see cref="HttpRequestMessage"/> sent to the self-hosted WebHttp <paramref name="serviceType"/> 
        /// results in a response that is equivalent to the expected <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="serviceType">The service type to use for the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> instance to send to the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="onGetActualResponse">The actual <see cref="HttpResponseMessage"/> instance provided as an <see cref="Action<>"/>. Should not be <c>null</c>.</param>
        public static void Execute(Type serviceType, HttpRequestMessage request, Action<HttpResponseMessage> onGetActualResponse)
        {
            Execute(serviceType, null, null, request, onGetActualResponse);
        }

        /// <summary>
        /// Asserts that an <see cref="HttpRequestMessage"/> sent to the self-hosted WebHttp <paramref name="serviceType"/> 
        /// results in a response that is equivalent to the expected <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="serviceType">The service type to use for the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="behavior">The <see cref="WebHttpBehavior"/> to use with the WebHttp service.</param>
        /// <param name="binding">The <see cref="WebHttpBinding"/> to use with the WebHttp service.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> instance to send to the self-hosted WebHttp service. Should not be <c>null</c>.</param>
        /// <param name="onGetActualResponse">The actual <see cref="HttpResponseMessage"/> instance provided as an <see cref="Action<>"/>. Should not be <c>null</c>.</param>
        public static void Execute(Type serviceType, WebHttpBehavior behavior, WebHttpBinding binding, HttpRequestMessage request, Action<HttpResponseMessage> onGetActualResponse)
        {
            Assert.IsNotNull(serviceType, "The 'serviceType' parameter should not be null.");
            Assert.IsNotNull(request, "The 'request' parameter should not be null.");
            Assert.IsNotNull(onGetActualResponse, "The 'onGetActualResponse' parameter should not be null.");

            ServiceHostAssert.Execute((baseAddress) => GetServiceHost(serviceType, behavior, binding, baseAddress), request, onGetActualResponse);
        }

        private static ServiceHost GetServiceHost(object serviceSingleton, WebHttpBehavior behavior, WebHttpBinding binding, Uri baseAddress)
        {
            ServiceHost host = new ServiceHost(serviceSingleton, baseAddress);
            ConfigureInstanceContextMode(host, InstanceContextMode.Single);
            AddEndpoint(host, serviceSingleton.GetType(), behavior, binding);
            return host;
        }

        private static ServiceHost GetServiceHost(Type serviceType, WebHttpBehavior behavior, WebHttpBinding binding, Uri baseAddress)
        {
            ServiceHost host = new ServiceHost(serviceType, baseAddress);
            ConfigureInstanceContextMode(host, InstanceContextMode.PerCall);
            AddEndpoint(host, serviceType, behavior, binding);
            return host;
        }

        private static void ConfigureInstanceContextMode(ServiceHost host, InstanceContextMode mode)
        {
            ServiceBehaviorAttribute serviceBehaviorAttribute = host.Description.Behaviors.Find<ServiceBehaviorAttribute>();
            if (serviceBehaviorAttribute == null)
            {
                serviceBehaviorAttribute = new ServiceBehaviorAttribute();
                host.Description.Behaviors.Add(serviceBehaviorAttribute);
            }

            serviceBehaviorAttribute.InstanceContextMode = mode;
        }

        private static void AddEndpoint(ServiceHost host, Type serviceType, WebHttpBehavior behavior, WebHttpBinding binding)
        {
            if (behavior == null)
            {
                behavior = new WebHttpBehavior();
                behavior.HelpEnabled = true;
                behavior.AutomaticFormatSelectionEnabled = true;
            }

            if (binding == null)
            {
                binding = new WebHttpBinding();
            }

            ServiceEndpoint endpoint = host.AddServiceEndpoint(serviceType, binding, string.Empty);
            endpoint.Behaviors.Add(behavior);
        }
    }
}
