// <copyright file="WebSphereReliableSession.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.WebSphere
{
    using System.ServiceModel;

    /// <summary>
    /// Reliable session binding element for WebSphere
    /// </summary>
    public class WebSphereReliableSession : InteropReliableSession
    {
        /// <summary>
        /// Initializes a new instance of the WebSphereReliableSession class
        /// </summary>
        /// <param name="reliableSession">Reliable session binding element</param>
        public WebSphereReliableSession(OptionalReliableSession reliableSession)
            : base(reliableSession)
        {
            this.InnerReliableSession.Ordered = false;
        }
    }
}

