// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Mocks
{
    using System;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;

    public class MockBindingContext : BindingContext
    {
        public MockBindingContext(CustomBinding binding)
            : base(binding, new BindingParameterCollection(), new Uri("http://somehost"), string.Empty, ListenUriMode.Unique)
        {
        }

        public HttpTransportBindingElement TransportBindingElement { get; private set; }

        public MockHttpTransportBindingElement MockTransportBindingElement { get; private set; }

        public static MockBindingContext Create()
        {
            HttpTransportBindingElement transport = new HttpTransportBindingElement();
            CustomBinding binding = new CustomBinding(transport);
            return new MockBindingContext(binding) { TransportBindingElement = transport };
        }

        public static MockBindingContext CreateWithMockTransport()
        {
            MockHttpTransportBindingElement transport = new MockHttpTransportBindingElement();
            CustomBinding binding = new CustomBinding(transport);
            return new MockBindingContext(binding) { MockTransportBindingElement = transport };
        }
    }
}
