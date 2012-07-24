// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels.Mocks
{
    using System.Net.Http;

    public class MockInvalidMessageHandler : DelegatingChannel
    {
        public MockInvalidMessageHandler(HttpMessageChannel innerChannel, bool test) : base(innerChannel) 
        { 
        }
    }
}
