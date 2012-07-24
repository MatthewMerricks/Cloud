// <copyright file="InteropReliableSession.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop
{
    using System;
    using System.ServiceModel;

    /// <summary>
    /// Base class for the reliable session binding element
    /// </summary>
    public abstract class InteropReliableSession
    {
        private OptionalReliableSession innerReliableSession;

        /// <summary>
        /// Initializes a new instance of the InteropReliableSession class
        /// </summary>
        /// <param name="reliableSession">Inner reliable session binding element</param>
        protected InteropReliableSession(OptionalReliableSession reliableSession)
        {
            if (reliableSession == null)
            {
                throw new ArgumentNullException("reliableSession");
            }

            this.innerReliableSession = reliableSession;
        }

        /// <summary>
        /// Gets or sets a value indicating whether reliable session is enabled
        /// </summary>
        public bool Enabled
        {
            get { return this.innerReliableSession.Enabled; }
            set { this.innerReliableSession.Enabled = value; }
        }

        /// <summary>
        /// Gets or sets the inactivity timeout
        /// </summary>
        public TimeSpan InactivityTimeout
        {
            get { return this.innerReliableSession.InactivityTimeout; }
            set { this.innerReliableSession.InactivityTimeout = value; }
        }

        /// <summary>
        /// Gets the inner reliable session element
        /// </summary>
        protected OptionalReliableSession InnerReliableSession
        {
            get { return this.innerReliableSession; }
        }
    }
}

