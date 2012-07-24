using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using Microsoft.ApplicationServer.Http.Channels;
using Microsoft.ApplicationServer.Http.Description;
using Microsoft.ApplicationServer.Http.Dispatcher;

namespace Microsoft.ApplicationServer.Http.Description
{
    public interface IHttpHostConfigurationBuilder
    {
        IHttpHostConfigurationBuilder SetResourceFactory(IResourceFactory factory);
        IHttpHostConfigurationBuilder SetOperationHandlerFactory(HttpOperationHandlerFactory factory);
        IHttpHostConfigurationBuilder SetOperationHandlerFactory<T>() where T:HttpOperationHandlerFactory, new();
        IHttpHostConfigurationBuilder AddFormatters(params MediaTypeFormatter[] formatters);
        IHttpHostConfigurationBuilder AddRequestHandlers(Action<Collection<HttpOperationHandler>> handlers, Func<ServiceEndpoint, HttpOperationDescription, bool> condition = null);
        IHttpHostConfigurationBuilder AddResponseHandlers(Action<Collection<HttpOperationHandler>> handlers, Func<ServiceEndpoint, HttpOperationDescription, bool> condition = null);
        IHttpHostConfigurationBuilder SetMessageHandlerFactory(HttpMessageHandlerFactory factory);
        IHttpHostConfigurationBuilder SetMessageHandlerFactory(Func<HttpMessageChannel, HttpMessageChannel> factory);
        IHttpHostConfigurationBuilder AddMessageHandlers(params Type[] handlers);
        IHttpHostConfigurationBuilder SetResourceFactory(Func<Type, InstanceContext, HttpRequestMessage, object> getInstance, Action<InstanceContext, object> releaseInstance);
        IHttpHostConfigurationBuilder SetResourceFactory<T>() where T:IResourceFactory, new();
        IHttpHostConfigurationBuilder SetErrorHandler(HttpErrorHandler handler);
        IHttpHostConfigurationBuilder SetErrorHandler(Func<Exception, bool> handleError, Func<Exception, HttpResponseMessage> provideResponse);
        IHttpHostConfigurationBuilder SetErrorHandler<T>() where T:HttpErrorHandler, new();
        HttpHostConfiguration Configuration { get; }
    }
}