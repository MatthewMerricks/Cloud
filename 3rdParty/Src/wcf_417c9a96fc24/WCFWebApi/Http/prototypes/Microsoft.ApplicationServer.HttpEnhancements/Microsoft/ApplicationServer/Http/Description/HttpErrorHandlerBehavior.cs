using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using Microsoft.ApplicationServer.Http.Dispatcher;

namespace Microsoft.ApplicationServer.Http.Description
{
    public class HttpErrorHandlerBehavior : IServiceBehavior
    {
        private HttpErrorHandler errorHandler;

        public HttpErrorHandlerBehavior(HttpErrorHandler errorHandler)
        {
            this.errorHandler = errorHandler;
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher dispatcher in serviceHostBase.ChannelDispatchers)
            {
                dispatcher.ErrorHandlers.Add(this.errorHandler);
            }
        }
    }
}
