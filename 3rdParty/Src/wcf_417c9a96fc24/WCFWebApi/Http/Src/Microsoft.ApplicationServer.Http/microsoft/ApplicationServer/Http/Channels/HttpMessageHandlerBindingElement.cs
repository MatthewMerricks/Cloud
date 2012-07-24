// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Provides a <see cref="BindingElement"/> implementation that provides <see cref="HttpMessageHandlerChannelListener"/> instances. 
    /// </summary>
    public class HttpMessageHandlerBindingElement : BindingElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpMessageHandlerBindingElement"/> class. 
        /// </summary>
        public HttpMessageHandlerBindingElement()
        {
        }

        private HttpMessageHandlerBindingElement(HttpMessageHandlerBindingElement elementToBeCloned)
        {
            this.MessageHandlerFactory = elementToBeCloned.MessageHandlerFactory;
        }

        /// <summary>
        /// Gets or sets a value representing the <see cref="HttpMessageHandlerFactory"/> to use for
        /// instantiating an <see cref="HttpMessageHandlerChannel"/>.
        /// </summary>
        public HttpMessageHandlerFactory MessageHandlerFactory { get; set; }

        /// <summary>
        /// Returns a value that indicates whether the binding element can build a channel factory for a specific type of channel.
        /// </summary>
        /// <typeparam name="TChannel">The type of channel the channel factory produces.</typeparam>
        /// <param name="context">The <see cref="BindingContext"/> that provides context for the binding element</param>
        /// <returns>ALways false.</returns>
        [SuppressMessage(FxCop.Category.Design, FxCop.Rule.GenericMethodsShouldProvideTypeParameter, Justification = "Existing public API")]
        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            return false;
        }

        /// <summary>
        /// Initializes a channel factory for producing channels of a specified type from the binding context.
        /// </summary>
        /// <typeparam name="TChannel">The type of channel the factory builds.</typeparam>
        /// <param name="context">The <see cref="BindingContext"/> that provides context for the binding element</param>
        /// <returns>Throws <see cref="NotSupportedException"/>.</returns>
        [SuppressMessage(FxCop.Category.Design, FxCop.Rule.GenericMethodsShouldProvideTypeParameter, Justification = "Existing public API")]
        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            throw Fx.Exception.AsError(
                new NotSupportedException(
                    SR.HttpMessageHandlerChannelFactoryNotSupported(
                        typeof(HttpMessageHandlerBindingElement).Name,
                        typeof(IChannelFactory<TChannel>).Name)));
        }

        /// <summary>
        /// Returns a value that indicates whether the binding element can build a listener for a specific type of channel.
        /// </summary>
        /// <typeparam name="TChannel">The type of channel the listener accepts.</typeparam>
        /// <param name="context">The <see cref="BindingContext"/> that provides context for the binding element</param>
        /// <returns>true if the <see cref="IChannelListener&lt;TChannel&gt;"/> of type <see cref="IChannel"/> can be built by the binding element; otherwise, false.</returns>
        [SuppressMessage(FxCop.Category.Design, FxCop.Rule.GenericMethodsShouldProvideTypeParameter, Justification = "Existing public API")]
        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw Fx.Exception.ArgumentNull("context");
            }

            return HttpMessageHandlerBindingElement.IsChannelShapeSupported<TChannel>() && context.CanBuildInnerChannelListener<TChannel>();
        }

        /// <summary>
        /// Initializes a channel listener to accept channels of a specified type from the binding context.
        /// </summary>
        /// <typeparam name="TChannel">The type of channel the listener is built to accept.</typeparam>
        /// <param name="context">The <see cref="BindingContext"/> that provides context for the binding element</param>
        /// <returns>The <see cref="IChannelListener&lt;TChannel&gt;"/> of type <see cref="IChannel"/> initialized from the context.</returns>
        [SuppressMessage(FxCop.Category.Design, FxCop.Rule.GenericMethodsShouldProvideTypeParameter, Justification = "Existing public API")]
        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw Fx.Exception.ArgumentNull("context");
            }

            if (!this.CanBuildChannelListener<TChannel>(context))
            {
                throw Fx.Exception.AsError(
                    new NotSupportedException(
                        SR.HttpMessageHandlerChannelInvalidChannelShape(typeof(TChannel).Name, typeof(HttpMessageHandlerBindingElement).Name, typeof(IReplyChannel).Name)));
            }

            // Check whether we should use asynchronous reply path and whether we should include exceptions details in error responses
            bool asynchronousSendEnabled = false;
            bool includeExceptionDetailInFaults = false;
            if (context.BindingParameters != null)
            {
                DispatcherSynchronizationBehavior dispatcherSynchronizationBehavior = context.BindingParameters.Find<DispatcherSynchronizationBehavior>();
                if (dispatcherSynchronizationBehavior != null)
                {
                    asynchronousSendEnabled = dispatcherSynchronizationBehavior.AsynchronousSendEnabled;
                }

                ServiceDebugBehavior serviceDebugBehavior = context.BindingParameters.Find<ServiceDebugBehavior>();
                if (serviceDebugBehavior != null)
                {
                    includeExceptionDetailInFaults = serviceDebugBehavior.IncludeExceptionDetailInFaults;
                }
            }

            IChannelListener<IReplyChannel> innerListener = context.BuildInnerChannelListener<IReplyChannel>();
            if (innerListener == null)
            {
                return null;
            }

            return (IChannelListener<TChannel>)new HttpMessageHandlerChannelListener(context.Binding, innerListener, this.MessageHandlerFactory, asynchronousSendEnabled, includeExceptionDetailInFaults);
        }

        /// <summary>
        /// Returns a copy of the binding element object.
        /// </summary>
        /// <returns>A <see cref="BindingElement"/> object that is a deep clone of the original.</returns>
        public override BindingElement Clone()
        {
            return new HttpMessageHandlerBindingElement(this);
        }

        /// <summary>
        /// Teturns a typed object requested, if present, from the appropriate layer in the binding stack
        /// </summary>
        /// <typeparam name="T">e typed object for which the method is querying.</typeparam>
        /// <param name="context">The <see cref="BindingContext"/> that provides context for the binding element</param>
        /// <returns>The typed object T requested if it is present or null if it is not present.</returns>
        [SuppressMessage(FxCop.Category.Design, FxCop.Rule.GenericMethodsShouldProvideTypeParameter, Justification = "Existing public API")]
        public override T GetProperty<T>(BindingContext context)
        {
            if (context == null)
            {
                throw Fx.Exception.ArgumentNull("context");
            }

            return context.GetInnerProperty<T>();
        }

        private static bool IsChannelShapeSupported<TChannel>()
        {
            return typeof(IReplyChannel).IsAssignableFrom(typeof(TChannel));
        }
    }
}