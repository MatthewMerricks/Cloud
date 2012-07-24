//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
namespace Microsoft.ApplicationServer.Common
{
    //using Microsoft.AppFabric.Tracing;

    //For every provider that wants to trace these events, add the events to the provider and add additional if clauses here.
    class ProviderEventEmitter
    {
        public static void EventWriteUnhandledException(object eventProvider, string message)
        {
            /*
            ManagementEtwProvider managementProvider = eventProvider as ManagementEtwProvider;
            if (managementProvider != null)
            {
                managementProvider.EventWriteUnhandledException(message);
                return;
            }
             */
        }
        public static void EventWriteThrowingException(object eventProvider, string message)
        {
            /*
            ManagementEtwProvider managementProvider = eventProvider as ManagementEtwProvider;
            if (managementProvider != null)
            {
                managementProvider.EventWriteThrowingException(message);
                return;
            } 
             */
        }

        public static void EventWriteShipAssertException(object eventProvider, string message)
        {
            /*
            ManagementEtwProvider managementProvider = eventProvider as ManagementEtwProvider;
            if (managementProvider != null)
            {
                managementProvider.EventWriteShipAssertException(message);
                return;
            } 
             */
        }
    }
}
