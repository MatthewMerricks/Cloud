//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Diagnostics
{
    // Order is important here. The order must match the order of strings in src\ndp\cdf\src\WCF\EventLog\EventLog.mc
    enum EventLogEventId : uint
    {
        // EventIDs from shared Diagnostics and Reliability code
        FailedToSetupTracing = 0xC0010064,
        FailedToInitializeTraceSource,
        FailFast,
        FailFastException,
        FailedToTraceEvent,
        FailedToTraceEventWithException        
    }
}