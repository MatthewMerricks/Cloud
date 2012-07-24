// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel.Channels
{
    using System.ServiceModel;

    internal static class HttpTransportDefaults
    {
        internal const HostNameComparisonMode HostNameComparisonMode = System.ServiceModel.HostNameComparisonMode.StrongWildcard;
        internal const TransferMode TransferMode = System.ServiceModel.TransferMode.Buffered;
    }
}