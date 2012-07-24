// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Provides an <see cref="HttpMessageEncoderFactory"/> that returns a <see cref="MessageEncoder"/> 
    /// that is able to produce and consume <see cref="HttpMessage"/> instances.
    /// </summary>
    public sealed class HttpMessageEncodingBindingElement : MessageEncodingBindingElement
    {
        private static readonly Type IReplyChannelType = typeof(IReplyChannel);
        private static readonly Type httpMessageEncodingBindingElementType = typeof(HttpMessageEncodingBindingElement);

        /// <summary>
        /// Gets or sets the message version that can be handled by the message encoders produced by the message encoder factory.
        /// </summary>
        /// <returns>The <see cref="MessageVersion"/> used by the encoders produced by the message encoder factory.</returns>
        public override MessageVersion MessageVersion
        {
            get
            {
                return MessageVersion.None;
            }

            set
            {
                if (value == null)
                {
                    throw Fx.Exception.ArgumentNull("value");
                }

                if (value != MessageVersion.None)
                {
                    throw Fx.Exception.AsError(
                        new NotSupportedException(
                                SR.OnlyMessageVersionNoneSupportedOnHttpMessageEncodingBindingElement(
                                    typeof(HttpMessageEncodingBindingElement))));
                }
            }
        }

        /// <summary>
        /// Returns a value that indicates whether the binding element can build a listener for a specific type of channel.
        /// </summary>
        /// <typeparam name="TChannel">The type of channel the listener accepts.</typeparam>
        /// <param name="context">The <see cref="BindingContext"/> that provides context for the binding element</param>
        /// <returns>true if the <see cref="IChannelListener&lt;TChannel&gt;"/> of type <see cref="IChannel"/> can be built by the binding element; otherwise, false.</returns>
        [SuppressMessage(FxCop.Category.Design, FxCop.Rule.GenericMethodsShouldProvideTypeParameter, Justification = "Existing public API")]
        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            return false;
        }

        /// <summary>
        /// Returns a value that indicates whether the binding element can build a channel factory for a specific type of channel.
        /// </summary>
        /// <typeparam name="TChannel">The type of channel the channel factory produces.</typeparam>
        /// <param name="context">The <see cref="BindingContext"/> that provides context for the binding element</param>
        /// <returns>ALways false.</returns>
        [SuppressMessage(FxCop.Category.Design, FxCop.Rule.GenericMethodsShouldProvideTypeParameter, Justification = "Existing public API")]
        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            throw Fx.Exception.AsError(
                new NotSupportedException(
                    SR.ChannelFactoryNotSupportedByHttpMessageEncodingBindingElement(httpMessageEncodingBindingElementType.Name)));
        }

        /// <summary>
        /// Returns a value that indicates whether the binding element can build a channel factory for a specific type of channel.
        /// </summary>
        /// <typeparam name="TChannel">The type of channel the channel factory produces.</typeparam>
        /// <param name="context">The <see cref="BindingContext"/> that provides context for the binding element</param>
        /// <returns>ALways false.</returns>
        [SuppressMessage(FxCop.Category.Design, FxCop.Rule.GenericMethodsShouldProvideTypeParameter, Justification = "Existing public API")]
        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw Fx.Exception.ArgumentNull("context");
            }

            context.BindingParameters.Add(this);

            return IsChannelShapeSupported<TChannel>() && context.CanBuildInnerChannelListener<TChannel>();
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

            if (!IsChannelShapeSupported<TChannel>())
            {
                throw Fx.Exception.AsError(
                    new NotSupportedException(
                        SR.ChannelShapeNotSupportedByHttpMessageEncodingBindingElement(typeof(TChannel).Name)));
            }

            context.BindingParameters.Add(this);

            IChannelListener<IReplyChannel> innerListener = context.BuildInnerChannelListener<IReplyChannel>();

            if (innerListener == null)
            {
                return null;
            }
            
            return (IChannelListener<TChannel>)new HttpMessageEncodingChannelListener(context.Binding, innerListener);
        }

        /// <summary>
        /// Returns a copy of the binding element object.
        /// </summary>
        /// <returns>A <see cref="BindingElement"/> object that is a deep clone of the original.</returns>
        public override BindingElement Clone()
        {
            return new HttpMessageEncodingBindingElement();
        }

        /// <summary>
        /// Creates a factory for producing message encoders that are able to 
        /// produce and consume <see cref="HttpMessage"/> instances.
        /// </summary>
        /// <returns>
        /// The <see cref=".MessageEncoderFactory"/> used to produce message encoders that are able to 
        /// produce and consume <see cref="HttpMessage"/> instances.
        /// </returns>
        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            return new HttpMessageEncoderFactory();
        }

        private static bool IsChannelShapeSupported<TChannel>()
        {
            return typeof(TChannel) == IReplyChannelType;
        }
    }
}
