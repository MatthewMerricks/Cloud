// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    /// <summary>
    /// Internal helper class to validate <see cref="HttpBindingSecurityMode"/> enum values.
    /// </summary>
    internal static class HttpBindingSecurityModeHelper
    {
        /// <summary>
        /// Determines whether the given <paramref name="value"/> is a valid
        /// <see cref="HttpBindingSecurityMode"/> value.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is a valid <see cref="HttpBindingSecurityMode"/> value; otherwise<c> false</c>.</returns>
        internal static bool IsDefined(HttpBindingSecurityMode value)
        {
            return value == HttpBindingSecurityMode.None ||
                value == HttpBindingSecurityMode.Transport ||
                value == HttpBindingSecurityMode.TransportCredentialOnly;
        }
    }
}
