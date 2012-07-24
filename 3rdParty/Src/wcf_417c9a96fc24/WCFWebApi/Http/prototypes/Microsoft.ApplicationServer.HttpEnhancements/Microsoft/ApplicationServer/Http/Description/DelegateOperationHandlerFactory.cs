using System.Linq;

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ServiceModel.Description;

    using Microsoft.ApplicationServer.Http.Dispatcher;

    internal class DelegateOperationHandlerFactory : HttpOperationHandlerFactory
    {
        public IEnumerable<OperationHandlerItem> createRequestHandlers;
        public IEnumerable<OperationHandlerItem> createResponseHandlers;

        public DelegateOperationHandlerFactory()
        {
            
        }

        private static MediaTypeFormatterCollection GetFormatters(IEnumerable<MediaTypeFormatter> formatters)
        {
            var mediaTypeFormatters = new MediaTypeFormatterCollection();
            foreach(var formatter in formatters)
                mediaTypeFormatters.Add(formatter);
            return mediaTypeFormatters;
        }

        public DelegateOperationHandlerFactory(IEnumerable<MediaTypeFormatter> defaultMediaTypeFormatters, IEnumerable<OperationHandlerItem> requestHandlers, IEnumerable<OperationHandlerItem> responseHandlers)
            : base(GetFormatters(defaultMediaTypeFormatters))
        {
            this.createRequestHandlers = requestHandlers ?? Enumerable.Empty<OperationHandlerItem>();
            this.createResponseHandlers = responseHandlers ?? Enumerable.Empty<OperationHandlerItem>();
        }

        protected override System.Collections.ObjectModel.Collection<HttpOperationHandler> OnCreateRequestHandlers(ServiceEndpoint endpoint, HttpOperationDescription operation)
        {
            var handlers = new Collection<HttpOperationHandler>();
            handlers = base.OnCreateRequestHandlers(endpoint, operation);
            foreach(var handlerItem in this.createRequestHandlers)
            {
                if (handlerItem.Condition==null || handlerItem.Condition(endpoint,operation))
                {
                    handlerItem.Handlers(handlers);
                }
            }

            return handlers;
        }

        protected override System.Collections.ObjectModel.Collection<HttpOperationHandler> OnCreateResponseHandlers(ServiceEndpoint endpoint, HttpOperationDescription operation)
        {
            var handlers = new Collection<HttpOperationHandler>();
            handlers = base.OnCreateResponseHandlers(endpoint, operation);
            foreach (var handlerItem in this.createResponseHandlers)
            {
                if (handlerItem.Condition(endpoint, operation))
                {
                    handlerItem.Handlers(handlers);
                }
            }

            return handlers;
        }
    }
}