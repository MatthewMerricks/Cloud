// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The interface which takes a list of items and a given Uri, returns a refined list 
    /// </summary>
    public interface IQueryComposer
    {
        /// <summary>
        /// composes the query based on the url to get a refined list
        /// </summary>
        /// <param name="rootedQuery">a list of items</param>
        /// <param name="url">request uri</param>
        /// <returns>A refined list</returns>
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "The url represents the resource string used for query composition and not a network location")]
        IEnumerable ComposeQuery(IEnumerable rootedQuery, string url);
    }
}
