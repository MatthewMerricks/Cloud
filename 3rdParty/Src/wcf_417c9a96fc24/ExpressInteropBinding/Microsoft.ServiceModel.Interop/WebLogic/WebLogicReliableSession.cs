// <copyright file="WebLogicReliableSession.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.WebLogic
{
    using System.ServiceModel;

    /// <summary>
    /// Reliable session binding element for WebLogic
    /// </summary>
    public class WebLogicReliableSession : InteropReliableSession
    {
        /// <summary>
        /// Initializes a new instance of the WebLogicReliableSession class
        /// </summary>
        /// <param name="reliableSession">Reliable session binding element</param>
        public WebLogicReliableSession(OptionalReliableSession reliableSession)
            : base(reliableSession)
        {
            this.InnerReliableSession.Ordered = false;
        }
    }
}

