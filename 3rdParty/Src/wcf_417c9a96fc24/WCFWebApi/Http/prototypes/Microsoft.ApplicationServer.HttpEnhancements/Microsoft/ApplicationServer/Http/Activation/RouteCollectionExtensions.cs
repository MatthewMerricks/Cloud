// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using System.Linq;
using Microsoft.ApplicationServer.Http.Description;

namespace Microsoft.ApplicationServer.Http.Activation
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.Web.Routing;

    public static class RouteCollectionExtensions
    {
        public static void MapServiceRoute<TService>(this RouteCollection routes, string routePrefix, IHttpHostConfigurationBuilder builder = null, params object[] constraints)
        {
            MapServiceRoute<TService, HttpConfigurableServiceHostFactory>(routes, routePrefix, builder, constraints);
        }

        public static void MapServiceRoute<TService, TServiceHostFactory>(this RouteCollection routes, string routePrefix, IHttpHostConfigurationBuilder builder = null, params object[] constraints) where TServiceHostFactory : ServiceHostFactoryBase, IConfigurableServiceHostFactory, new()
        {
            if (routes == null)
            {
                throw new ArgumentNullException("routes");
            }
            var route = new ServiceRoute(routePrefix, new TServiceHostFactory() { Builder = (HttpHostConfiguration) builder }, typeof(TService));
            routes.Add(route);
        }

        public static void MapServiceRoute<TService, TServiceHostFactory>(this RouteCollection routes, string routePrefix, params object[] constraints) where TServiceHostFactory : ServiceHostFactoryBase, new()
        {
            if (routes == null)
            {
                throw new ArgumentNullException("routes");
            }

            var route = new ServiceRoute(routePrefix, new TServiceHostFactory(), typeof(TService));
            route.Constraints = new RouteValueDictionary(constraints);
            routes.Add(route);
        }
    }
}
