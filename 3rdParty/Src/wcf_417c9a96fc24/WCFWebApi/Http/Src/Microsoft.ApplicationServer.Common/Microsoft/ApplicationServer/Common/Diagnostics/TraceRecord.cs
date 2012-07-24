//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Diagnostics
{
    using System.Xml;
    using System;

    [Serializable]
    class TraceRecord
    {
        protected const string EventIdBase = "http://schemas.microsoft.com/2006/08/ServiceModel/";
        protected const string NamespaceSuffix = "TraceRecord";

        internal virtual string EventId { get { return BuildEventId("Empty"); } }

        internal virtual void WriteTo(XmlWriter writer) 
        {
        }

        protected static string BuildEventId(string eventId)
        {
            return TraceRecord.EventIdBase + eventId + TraceRecord.NamespaceSuffix;
        }

        protected static string XmlEncode(string text)
        {
            return DiagnosticTrace.XmlEncode(text);
        }
    }
}