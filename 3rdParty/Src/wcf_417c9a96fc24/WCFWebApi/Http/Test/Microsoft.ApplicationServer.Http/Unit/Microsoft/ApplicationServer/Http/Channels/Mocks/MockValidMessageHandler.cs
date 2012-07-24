// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels.Mocks
{
    using System.Net.Http;

    public class MockValidMessageHandler : DelegatingChannel
    {
        public MockValidMessageHandler(HttpMessageChannel innerChannel) : base(innerChannel)
        { 
        }
    }
}
