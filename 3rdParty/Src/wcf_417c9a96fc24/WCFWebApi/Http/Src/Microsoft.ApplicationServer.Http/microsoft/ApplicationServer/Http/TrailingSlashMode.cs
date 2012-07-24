// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    /// <summary>
    /// TrailingSlashMode lists the possible ways in which the trailing slash will be handled in URI's.
    /// </summary>
    public enum TrailingSlashMode
    {
        /// <summary>
        /// AutoRedirects in the case of request uri differing by trailing slash
        /// </summary>
        AutoRedirect,
        
        /// <summary>
        /// Ignores the difference in trailing slash in the request uri
        /// </summary>
        Ignore,
    }
}