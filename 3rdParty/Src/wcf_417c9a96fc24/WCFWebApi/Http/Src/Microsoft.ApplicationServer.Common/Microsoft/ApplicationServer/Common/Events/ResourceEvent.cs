//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;
    using System.Runtime.Serialization;

    sealed class ResourceEvent : BaseEvent
    {
        public string Name { get; set; }

        public double Value { get; set; }
    }
}
