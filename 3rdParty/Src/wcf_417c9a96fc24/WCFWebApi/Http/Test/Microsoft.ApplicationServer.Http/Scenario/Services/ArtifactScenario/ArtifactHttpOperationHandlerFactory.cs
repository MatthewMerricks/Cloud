// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.ServiceModel.Description;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Http.Dispatcher;

    public class ArtifactHttpOperationHandlerFactory : HttpOperationHandlerFactory
    {
        public ArtifactHttpOperationHandlerFactory(IEnumerable<MediaTypeFormatter> mediaTypeFormatters)
            : base(mediaTypeFormatters)
        {
        }

        protected override Collection<HttpOperationHandler> OnCreateRequestHandlers(ServiceEndpoint endpoint, HttpOperationDescription operation)
        {
            Collection<HttpOperationHandler> handlers = base.OnCreateRequestHandlers(endpoint, operation);
            if (operation.InputParameters.Any(p => p.Type == typeof(GridPosition)))
            {
                handlers.Add(new GridHandler());
            }
            return handlers;
        }
    }
}
