// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System;
    using System.Linq;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Web;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Http.Description;

    public static class TestServiceHost
    {
        public static ServiceHost CreateWebHost<T>(bool asynchronousSendEnabled, bool faultDetail, bool streamed, HttpMessageHandlerFactory httpMessageHandlerFactory) where T : TestWebServiceBase
        {
            var webHost = new HttpServiceHost(typeof(T), TestServiceCommon.ServiceAddress);
            webHost.AddDefaultEndpoints();

            if (faultDetail && webHost.Description.Behaviors.Contains(typeof(ServiceDebugBehavior)))
            {
                var debug = webHost.Description.Behaviors[typeof(ServiceDebugBehavior)] as ServiceDebugBehavior;
                debug.IncludeExceptionDetailInFaults = true;
            }

            foreach (HttpEndpoint endpoint in webHost.Description.Endpoints.OfType<HttpEndpoint>())
            {
                endpoint.HelpEnabled = true;
                endpoint.MessageHandlerFactory = httpMessageHandlerFactory;
                endpoint.Behaviors.Add(new DispatcherSynchronizationBehavior { AsynchronousSendEnabled = asynchronousSendEnabled });

                if (streamed)
                {
                    endpoint.TransferMode = TransferMode.Streamed;
                }
            }

            webHost.Open();
            return webHost;

        }
        public static ServiceHost CreateHost<T>(bool asynchronousSendEnabled, bool customBinding, HttpMessageHandlerFactory httpMessageHandlerFactory) where T : TestServiceBase
        {
            return TestServiceHost.CreateHost<T>(asynchronousSendEnabled, customBinding, true, httpMessageHandlerFactory);
        }

        public static ServiceHost CreateHost<T>(bool asynchronousSendEnabled, bool customBinding, bool faultDetail, HttpMessageHandlerFactory httpMessageHandlerFactory) where T : TestServiceBase
        {
            return TestServiceHost.CreateHost<T>(asynchronousSendEnabled, customBinding, faultDetail, false, httpMessageHandlerFactory);
        }

        public static ServiceHost CreateHost<T>(bool asynchronousSendEnabled, bool customBinding, bool faultDetail, bool streamed, HttpMessageHandlerFactory httpMessageHandlerFactory) where T : TestServiceBase
        {
            var webHost = new WebServiceHost(typeof(T), TestServiceCommon.ServiceAddress);

            if (faultDetail && webHost.Description.Behaviors.Contains(typeof(ServiceDebugBehavior)))
            {
                var debug = webHost.Description.Behaviors[typeof(ServiceDebugBehavior)] as ServiceDebugBehavior;
                debug.IncludeExceptionDetailInFaults = true;
            }

            Binding httpBinding = null;
            if (customBinding)
            {
                var bindingElement = new HttpMessageHandlerBindingElement();
                bindingElement.MessageHandlerFactory = httpMessageHandlerFactory;

                var httpMsgBinding = new HttpBinding();
                if (streamed)
                {
                    httpMsgBinding.TransferMode = TransferMode.Streamed;
                }

                var bindingElements = httpMsgBinding.CreateBindingElements();
                bindingElements.Insert(0, bindingElement);

                httpBinding = new CustomBinding(bindingElements);
            }
            else
            {
                var httpMsgBinding = new HttpBinding() { MessageHandlerFactory = httpMessageHandlerFactory };

                if (streamed)
                {
                    httpMsgBinding.TransferMode = TransferMode.Streamed;
                }

                httpBinding = httpMsgBinding;
            }

            var endpoint = webHost.AddServiceEndpoint(typeof(ITestServiceContract), httpBinding, "");

            ServiceDebugBehavior debugBehavior = webHost.Description.Behaviors.Find<ServiceDebugBehavior>();
            DispatcherSynchronizationBehavior synchronizationBehavior = new DispatcherSynchronizationBehavior() { AsynchronousSendEnabled = asynchronousSendEnabled };
            endpoint.Behaviors.Add(synchronizationBehavior);
            endpoint.Behaviors.Add(new TestHttpBindingParameterBehavior(debugBehavior, synchronizationBehavior));

            webHost.Open();
            return webHost;
        }

        public static Binding CreateBinding(bool customBinding, HttpMessageHandlerFactory httpMessageHandlerFactory)
        {
            if (customBinding)
            {
                var bindingElement = new HttpMessageHandlerBindingElement();
                bindingElement.MessageHandlerFactory = httpMessageHandlerFactory;

                var httpBinding = new HttpBinding();
                var bindingElements = httpBinding.CreateBindingElements();
                bindingElements.Insert(0, bindingElement);
                return new CustomBinding(bindingElements);
            }
            else
            {
                return new HttpBinding() { MessageHandlerFactory = httpMessageHandlerFactory };
            }
        }
    }
}
