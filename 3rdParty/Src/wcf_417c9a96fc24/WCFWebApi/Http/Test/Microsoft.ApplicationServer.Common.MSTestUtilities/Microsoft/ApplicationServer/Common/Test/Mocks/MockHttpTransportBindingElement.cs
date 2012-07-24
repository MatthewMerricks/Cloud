// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Mocks
{
    using System;
    using System.ServiceModel.Channels;

    public class MockHttpTransportBindingElement : TransportBindingElement
    {
        public BindingContext BindingContext { get; private set; }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            this.BindingContext = context;
            return null;
        }

        public override string Scheme
        {
            get { throw new NotImplementedException(); }
        }

        public override BindingElement Clone()
        {
            return null;
        }
    }
}
