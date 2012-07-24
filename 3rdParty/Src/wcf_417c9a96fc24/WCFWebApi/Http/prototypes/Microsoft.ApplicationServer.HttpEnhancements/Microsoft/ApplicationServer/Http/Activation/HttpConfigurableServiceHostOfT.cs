// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using Microsoft.ApplicationServer.Http.Description;

namespace Microsoft.ApplicationServer.Http.Activation
{
    using System;

    public class HttpConfigurableServiceHost<TService> : HttpConfigurableServiceHost
    {
        public HttpConfigurableServiceHost(IHttpHostConfigurationBuilder builder, params Uri[] baseAddresses)
            : base(typeof(TService), builder,  baseAddresses)
        {
        }
    }
}