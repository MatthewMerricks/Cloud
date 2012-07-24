// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Http.Dispatcher;
    using System.Collections.ObjectModel;


    public class HttpHostConfiguration : IHttpHostConfigurationBuilder
    {
        public HttpHostConfiguration()
        {
            this.configuration = this;
        }

        public static IHttpHostConfigurationBuilder Create()
        {
            return new HttpHostConfiguration();
        }

        private HttpHostConfiguration configuration;
        public IResourceFactory InstanceFactory { get; internal set; }

        private HttpOperationHandlerFactory operationHandlerFactory;
        public HttpOperationHandlerFactory OperationHandlerFactory
        {
            get
            {
                if (operationHandlerFactory == null)
                {
                    this.operationHandlerFactory = new DelegateOperationHandlerFactory(this.formatters, this.requestHandlers, this.responseHandlers);
                }
                return this.operationHandlerFactory;
            }   
            set
            {
                this.operationHandlerFactory = value;
            }
        }

        public HttpMessageHandlerFactory MessageHandlerFactory { get; internal set;}
        
        public HttpErrorHandler ErrorHandler { get; internal set; }
        internal Collection<MediaTypeFormatter> formatters = new Collection<MediaTypeFormatter>();
        internal Collection<OperationHandlerItem> requestHandlers = new Collection<OperationHandlerItem>();
        internal Collection<OperationHandlerItem> responseHandlers = new Collection<OperationHandlerItem>();

        IHttpHostConfigurationBuilder IHttpHostConfigurationBuilder.SetResourceFactory(IResourceFactory factory)
        {
            this.InstanceFactory = factory;
            return this;
        }

        IHttpHostConfigurationBuilder IHttpHostConfigurationBuilder.SetOperationHandlerFactory(HttpOperationHandlerFactory factory)
        {
            this.OperationHandlerFactory = factory;
            return this;
        }

        IHttpHostConfigurationBuilder IHttpHostConfigurationBuilder.SetMessageHandlerFactory(HttpMessageHandlerFactory factory)
        {
            this.MessageHandlerFactory = factory;
            return this;
        }

        IHttpHostConfigurationBuilder IHttpHostConfigurationBuilder.SetMessageHandlerFactory(Func<HttpMessageChannel, HttpMessageChannel> factory)
        {
            var builder = (IHttpHostConfigurationBuilder)this;
            builder.SetMessageHandlerFactory(new DelegateMessageHandlerFactory(factory));
            return this;
        }

        IHttpHostConfigurationBuilder IHttpHostConfigurationBuilder.SetResourceFactory(Func<Type, InstanceContext, HttpRequestMessage, object> getInstance, Action<InstanceContext, object> releaseInstance)
        {
            this.InstanceFactory = new DelegateInstanceFactory(getInstance, releaseInstance);
            return this;
        }

        IHttpHostConfigurationBuilder IHttpHostConfigurationBuilder.SetErrorHandler(HttpErrorHandler handler)
        {
            this.ErrorHandler = handler;
            return this;
        }

        IHttpHostConfigurationBuilder IHttpHostConfigurationBuilder.SetErrorHandler(Func<Exception, bool> handleError, Func<Exception, HttpResponseMessage> provideResponse)
        {
            this.ErrorHandler = new DelegateErrorHandler(handleError, provideResponse);
            return this;
        }

        HttpHostConfiguration IHttpHostConfigurationBuilder.Configuration
        {
            get { return this; }
        }

        protected IHttpHostConfigurationBuilder Configure
        {
            get { return this; }
        }

        public IHttpHostConfigurationBuilder SetOperationHandlerFactory<T>() where T : HttpOperationHandlerFactory, new()
        {
            return Configure.SetOperationHandlerFactory(new T());
        }

        public IHttpHostConfigurationBuilder AddMessageHandlers(params Type[] handlers)
        {
            return Configure.SetMessageHandlerFactory(new HttpMessageHandlerFactory(handlers));
        }

        public IHttpHostConfigurationBuilder SetResourceFactory<T>() where T : IResourceFactory, new()
        {
            return Configure.SetResourceFactory(new T());
        }

        public IHttpHostConfigurationBuilder SetErrorHandler<T>() where T : HttpErrorHandler, new()
        {
            return Configure.SetErrorHandler(new T());
        }

        public IHttpHostConfigurationBuilder AddFormatters(params MediaTypeFormatter[] formatters)
        {
            foreach(var formatter in formatters)
            {
                this.formatters.Add(formatter);
            }
            return this; 
        }

        public IHttpHostConfigurationBuilder AddRequestHandlers(Action<Collection<HttpOperationHandler>> handlers, Func<ServiceEndpoint, HttpOperationDescription,bool> condition = null)
        {
            this.requestHandlers.Add(new OperationHandlerItem(handlers, condition));
            return this;
        }

        public IHttpHostConfigurationBuilder AddResponseHandlers(Action<Collection<HttpOperationHandler>> handlers, Func<ServiceEndpoint, HttpOperationDescription,bool> condition = null)
        {
            this.responseHandlers.Add(new OperationHandlerItem(handlers, condition));
            return this;
        }
    }

    internal class OperationHandlerItem
    {
        public OperationHandlerItem(Action<Collection<HttpOperationHandler>> handlers, Func<ServiceEndpoint, HttpOperationDescription, bool> condition)
        {
            this.Handlers = handlers;
            this.Condition  = condition;
        }

        public Action<Collection<HttpOperationHandler>> Handlers { get; private set; }
        public Func<ServiceEndpoint, HttpOperationDescription,bool> Condition { get; private set; }
    }
}