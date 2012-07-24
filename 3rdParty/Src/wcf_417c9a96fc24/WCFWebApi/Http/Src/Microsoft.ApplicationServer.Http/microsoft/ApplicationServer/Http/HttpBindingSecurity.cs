// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System.ComponentModel;
    using System.ServiceModel;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Specifies the types of security available to a service endpoint configured to use an
    /// <see cref="HttpBinding"/> binding.
    /// </summary>
    public sealed class HttpBindingSecurity
    {
        internal const HttpBindingSecurityMode DefaultMode = HttpBindingSecurityMode.None;
        
        private HttpBindingSecurityMode mode;
        private HttpTransportSecurity transportSecurity;

        /// <summary>
        /// Creates a new instance of the <see cref="HttpBindingSecurity"/> class.
        /// </summary>
        public HttpBindingSecurity()
        {
            this.mode = DefaultMode;
            this.transportSecurity = new HttpTransportSecurity();
        }

        /// <summary>
        /// Gets or sets the mode of security that is used by an endpoint configured to use an
        /// <see cref="HttpBinding"/> binding.
        /// </summary>
        public HttpBindingSecurityMode Mode
        {
            get 
            { 
                return this.mode; 
            }

            set
            {
                if (!HttpBindingSecurityModeHelper.IsDefined(value))
                {
                    throw Fx.Exception.AsError(
                        new InvalidEnumArgumentException("value", (int)value, typeof(HttpBindingSecurityMode)));
                }

                this.IsModeSet = true;
                this.mode = value;
            }
        }

        /// <summary>
        /// Gets or sets an object that contains the transport-level security settings for the 
        /// <see cref="HttpBinding"/> binding.
        /// </summary>
        public HttpTransportSecurity Transport
        {
            get 
            { 
                return this.transportSecurity; 
            }

            set
            {
                this.transportSecurity = value ?? new HttpTransportSecurity();
            }
        }

        internal bool IsModeSet { get; private set; }
    }
}
