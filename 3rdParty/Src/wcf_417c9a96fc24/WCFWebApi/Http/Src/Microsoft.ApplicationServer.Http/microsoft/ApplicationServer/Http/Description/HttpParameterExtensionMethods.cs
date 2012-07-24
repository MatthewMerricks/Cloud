// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System.ServiceModel.Description;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Provides extension methods for <see cref="MessagePartDescription"/>
    /// to translate to <see cref="HttpParameter"/>.
    /// </summary>
    public static class HttpParameterExtensionMethods
    {
        /// <summary>
        /// Creates a new <see cref="HttpParameter"/> instance from the given
        /// <see cref="MessagePartDescription"/>.
        /// </summary>
        /// <param name="description">The <see cref="MessagePartDescription"/> to use.</param>
        /// <returns>A new <see cref="HttpParameter"/> instance.</returns>
        public static HttpParameter ToHttpParameter(this MessagePartDescription description)
        {
            if (description == null)
            {
                throw Fx.Exception.ArgumentNull("description");
            }

            return new HttpParameter(description);
        }
    }
}
