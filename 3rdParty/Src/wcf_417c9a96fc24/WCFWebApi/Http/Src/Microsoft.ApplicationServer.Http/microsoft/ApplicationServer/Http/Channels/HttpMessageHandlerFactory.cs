// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Net.Http;
    using System.Reflection;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Default HTTP message handler factory used by <see cref="HttpMessageHandlerBindingElement"/>
    /// for instantiating a set of HTTP message handler types using their default constructors.
    /// For more complex initialization scenarios, derive from <see cref="HttpMessageHandlerFactory"/>
    /// and override the <see cref="HttpMessageHandlerFactory.OnCreate"/> method.
    /// </summary>
    public class HttpMessageHandlerFactory
    {
        private static readonly string argTypeName = typeof(HttpMessageChannel).Name;
        private static readonly string baseTypeName = typeof(DelegatingChannel).Name;
        private static readonly Type[] constructorArgumentTypes = new Type[] { typeof(HttpMessageChannel) };

        private ReadOnlyCollection<Type> httpMessageHandlers;
        private IEnumerable<ConstructorInfo> handlerCtors;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpMessageHandlerFactory"/> class given
        /// a set of HTTP message handler types to instantiate using their default constructors.
        /// </summary>
        /// <param name="handlers">An ordered list of HTTP message handler types to be invoked as part of an 
        /// <see cref="HttpMessageHandlerBindingElement"/> instance.
        /// HTTP message handler types must derive from <see cref="DelegatingChannel"/> and have a public constructor
        /// taking exactly one argument of type <see cref="HttpMessageChannel"/>. The handlers are invoked in a 
        /// buttom-up fashion. That is, the last entry is called first and the first entry is called last.</param>
        public HttpMessageHandlerFactory(params Type[] handlers)
        {
            if (handlers == null)
            {
                throw Fx.Exception.ArgumentNull("handlers");
            }

            ConstructorInfo[] handlerCtors = new ConstructorInfo[handlers.Length];
            int cnt = 0;
            foreach (var handler in handlers)
            {
                if (handler == null)
                {
                    throw Fx.Exception.Argument(
                        string.Empty,
                        SR.HttpMessageHandlerTypeNotSupported("null", HttpMessageHandlerFactory.baseTypeName, HttpMessageHandlerFactory.argTypeName));
                }

                if (!typeof(DelegatingChannel).IsAssignableFrom(handler) || handler.IsAbstract)
                {
                    throw Fx.Exception.Argument(
                        string.Empty,
                        SR.HttpMessageHandlerTypeNotSupported(handler.Name, HttpMessageHandlerFactory.baseTypeName, HttpMessageHandlerFactory.argTypeName));
                }

                ConstructorInfo ctorInfo = handler.GetConstructor(HttpMessageHandlerFactory.constructorArgumentTypes);
                if (ctorInfo == null)
                {
                    throw Fx.Exception.Argument(
                        string.Empty,
                        SR.HttpMessageHandlerTypeNotSupported(handler.Name, HttpMessageHandlerFactory.baseTypeName, HttpMessageHandlerFactory.argTypeName));
                }

                handlerCtors[cnt++] = ctorInfo;
            }

            this.handlerCtors = handlerCtors;
            this.httpMessageHandlers = new ReadOnlyCollection<Type>(handlers);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpMessageHandlerFactory"/> class.
        /// </summary>
        protected HttpMessageHandlerFactory() 
            : this(Type.EmptyTypes)
        {
        }

        /// <summary>
        /// Gets the set of HTTP message handler types instantiated by this factory.
        /// </summary>
        public ReadOnlyCollection<Type> HttpMessageHandlers
        {
            get
            {
                return this.httpMessageHandlers;
            }
        }

        /// <summary>
        /// Creates an instance of an <see cref="HttpMessageChannel"/> using the HTTP message handlers
        /// provided in the constructor.
        /// </summary>
        /// <param name="innerChannel">The inner channel represents the destination of the HTTP message channel.</param>
        /// <returns>The HTTP message channel.</returns>
        public HttpMessageChannel Create(HttpMessageChannel innerChannel)
        {
            if (innerChannel == null)
            {
                throw Fx.Exception.ArgumentNull("innerChannel");
            }

            return this.OnCreate(innerChannel);
        }

        /// <summary>
        /// Creates an instance of an <see cref="HttpMessageChannel"/> using the HTTP message handlers
        /// provided in the constructor.
        /// </summary>
        /// <param name="innerChannel">The inner channel represents the destination of the HTTP message channel.</param>
        /// <returns>The HTTP message channel.</returns>
        protected virtual HttpMessageChannel OnCreate(HttpMessageChannel innerChannel)
        {
            if (innerChannel == null)
            {
                throw Fx.Exception.ArgumentNull("innerChannel");
            }

            HttpMessageChannel pipeline = innerChannel;
            try
            {
                foreach (var ctor in this.handlerCtors)
                {
                    pipeline = (DelegatingChannel)ctor.Invoke(new object[] { pipeline });
                }
            }
            catch (TargetInvocationException tie)
            {
                Fx.Exception.AsError(tie.InnerException);
                throw;
            }

            return pipeline;
        }
    }
}
