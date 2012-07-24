// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.QueryComposition.Server;
    
    /// <summary>
    /// The default Query Composer implementation
    /// </summary>
    public class UrlQueryComposer : IQueryComposer
    {
        /// <summary>
        /// Default implementation on how to refine a list of itmes based on the incoming url
        /// </summary>
        /// <param name="rootedQuery">The original list of items</param>
        /// <param name="url">The url which contains query</param>
        /// <returns>A refined list of the items</returns>
        public virtual IEnumerable ComposeQuery(IEnumerable rootedQuery, string url) 
        {
            Debug.Assert(rootedQuery != null, "queryableRoot should not be null");
            Debug.Assert(!String.IsNullOrEmpty(url), "url should not be null or empty");

            // cast the queryableRoot to an IQueryable type to further compose the query
            IQueryable queryable = rootedQuery as IQueryable;
            if (queryable == null)
            {
                queryable = rootedQuery.AsQueryable();
            }
            
            if (queryable != null)
            {
                IQueryable finalQueryable = QueryTranslator.Translate(queryable, url);
                return finalQueryable;
            }

            return rootedQuery;
        }
    }
}
