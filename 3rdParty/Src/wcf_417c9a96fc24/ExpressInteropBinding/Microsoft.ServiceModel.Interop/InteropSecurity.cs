// <copyright file="InteropSecurity.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Security;

    /// <summary>
    /// Base class for the security binding element
    /// </summary>
    public abstract class InteropSecurity
    {
        private WSHttpSecurity innerSecurity;

        /// <summary>
        /// Initializes a new instance of the InteropSecurity class
        /// </summary>
        /// <param name="security">Inner security binding element</param>
        protected InteropSecurity(WSHttpSecurity security)
        {
            if (security == null)
            {
                throw new ArgumentNullException("security");
            }

            this.innerSecurity = security;
        }

        /// <summary>
        /// Gets or sets the security algorithm suite
        /// </summary>
        public SecurityAlgorithmSuite AlgorithmSuite
        {
            get
            {
                return this.innerSecurity.Message.AlgorithmSuite;
            }

            set
            {
                this.innerSecurity.Message.AlgorithmSuite = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether secure conversation is enabled
        /// </summary>
        public bool EstablishSecurityContext
        {
            get
            {
                return this.innerSecurity.Message.EstablishSecurityContext;
            }

            set
            {
                this.innerSecurity.Message.EstablishSecurityContext = value;
            }
        }

        /// <summary>
        /// Gets the security element used to initialize this binding element
        /// </summary>
        protected WSHttpSecurity InnerSecurity
        {
            get { return this.innerSecurity; }
        }
    }
}

