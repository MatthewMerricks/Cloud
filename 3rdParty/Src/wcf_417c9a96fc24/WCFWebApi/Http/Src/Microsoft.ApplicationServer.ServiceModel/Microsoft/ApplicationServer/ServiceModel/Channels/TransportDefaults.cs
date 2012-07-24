// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel.Channels
{
    internal static class TransportDefaults
    {
        internal const long MaxReceivedMessageSize = 65536;
        internal const long MaxBufferPoolSize = 512 * 1024;
        internal const int MaxBufferSize = (int)MaxReceivedMessageSize;
        internal const int MaxFaultSize = MaxBufferSize;
    }
}