// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Channels;

    /// <summary>
    /// Declares methods that provide a service object or recycle a service object for a service
    /// based on <see cref="HttpBinding">HttpBinding</see>.
    /// </summary>
    public abstract class HttpInstanceProvider : IInstanceProvider
    {
        /// <summary>
        /// Returns a service object given the specified <see cref="InstanceContext"/> object.
        /// </summary>
        /// <param name="instanceContext">The current <see cref="InstanceContext"/> object.</param>
        /// <returns>The service object.</returns>
        public object GetInstance(InstanceContext instanceContext)
        {
            if (instanceContext == null)
            {
                throw Fx.Exception.ArgumentNull("instanceContext");
            }

            return this.OnGetInstance(instanceContext);
        }

        /// <summary>
        /// Called when an <see cref="InstanceContext"/> object recycles a service object.
        /// </summary>
        /// <param name="instanceContext">The service's instance context.</param>
        /// <param name="instance">The service object to be recycled.</param>
        public void ReleaseInstance(InstanceContext instanceContext, object instance)
        {
            if (instanceContext == null)
            {
                throw Fx.Exception.ArgumentNull("instanceContext");
            }

            if (instance == null)
            {
                throw Fx.Exception.ArgumentNull("instance");
            }

            this.OnReleaseInstance(instanceContext, instance);
        }

        /// <summary>
        /// Returns a service object given the specified <see cref="InstanceContext"/> object
        /// and <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="instanceContext">The current <see cref="InstanceContext"/> object.</param>
        /// <param name="request">The request message that triggered the creation of a service object.</param>
        /// <returns>The service object.</returns>
        public object GetInstance(InstanceContext instanceContext, HttpRequestMessage request)
        {
            if (instanceContext == null)
            {
                throw Fx.Exception.ArgumentNull("instanceContext");
            }

            if (request == null)
            {
                throw Fx.Exception.ArgumentNull("request");
            }

            return this.OnGetInstance(instanceContext, request);
        }

        /// <summary>
        /// Returns a service object given the specified <see cref="InstanceContext"/> object.
        /// </summary>
        /// <param name="instanceContext">The current <see cref="InstanceContext"/> object.</param>
        /// <param name="message">The message that triggered the creation of a service object.</param>
        /// <returns>The service object.</returns>
        object IInstanceProvider.GetInstance(InstanceContext instanceContext, Message message)
        {
            if (message == null)
            {
                throw Fx.Exception.ArgumentNull("message");
            }

            HttpRequestMessage request = message.ToHttpRequestMessage();
            if (request == null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                            SR.HttpInstanceProviderNullRequest(this.GetType().Name, typeof(HttpRequestMessage).Name, "GetInstance")));
            }

            return this.GetInstance(instanceContext, request);
        }

        /// <summary>
        /// Called by <see cref="GetInstance(InstanceContext)"/> to get the instance.
        /// Derived classes must implement this.
        /// </summary>
        /// <param name="instanceContext">The current <see cref="InstanceContext"/> object.</param>
        /// <returns>The service object.</returns>
        protected abstract object OnGetInstance(InstanceContext instanceContext);

        /// <summary>
        /// Called by <see cref="GetInstance(InstanceContext, HttpRequestMessage"/> to get the instance.
        /// Derived classes must implement this.
        /// </summary>
        /// <param name="instanceContext">The current <see cref="InstanceContext"/> object.</param>
        /// <param name="request">The request message that triggered the creation of a service object.</param>
        /// <returns>The service object.</returns>
        protected abstract object OnGetInstance(
                                   InstanceContext instanceContext,
                                   HttpRequestMessage request);

        /// <summary>
        /// Called by <see cref="ReleaseInstance"/> to release the instance.
        /// Derived classes must implement this.
        /// </summary>
        /// <param name="instanceContext">The service's instance context.</param>
        /// <param name="instance">The service object to be recycled.</param>
        protected abstract void OnReleaseInstance(InstanceContext instanceContext, object instance);
    }
}
