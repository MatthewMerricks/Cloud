// <copyright file="MetroReliableSession.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Metro
{
    using System.ServiceModel;

    /// <summary>
    /// Reliable session binding element for Metro
    /// </summary>
    public class MetroReliableSession : InteropReliableSession
    {
        /// <summary>
        /// Initializes a new instance of the MetroReliableSession class
        /// </summary>
        /// <param name="reliableSession">Reliable session binding element</param>
        public MetroReliableSession(OptionalReliableSession reliableSession)
            : base(reliableSession)
        {
            this.InnerReliableSession.Ordered = false;
        }
    }
}

