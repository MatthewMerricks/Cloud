// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    internal static class TrailingSlashModeHelper
    {
        internal static bool IsDefined(TrailingSlashMode trailingSlashMode)
        {
            return trailingSlashMode == TrailingSlashMode.AutoRedirect ||
                   trailingSlashMode == TrailingSlashMode.Ignore;
        }
    }
}
