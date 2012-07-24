//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;
    using System.Collections.Generic;

    class AggregateGroupKey
    {
        public AggregateGroupKey(string eventSource, string instanceId, string tenantId, Dictionary<string, string> dimensions, string metric)
        {
            this.EventSource = eventSource;
            this.InstanceId = instanceId;
            this.TenantId = tenantId;
            this.Dimensions = dimensions;
            this.Metric = metric;
            this.DimensionsKey = BaseEvent.EncodeDimensions(this.Dimensions);
        }

        public string Metric { get; private set; }

        public string EventSource { get; private set; }

        public string InstanceId { get; private set; }

        public string TenantId { get; private set; }

        public Dictionary<string, string> Dimensions { get; private set; }

        string DimensionsKey { get; set; }

        public override int GetHashCode()
        {
            return this.EventSource.GetHashCode() ^ this.InstanceId.GetHashCode() ^ this.TenantId.GetHashCode() ^ this.DimensionsKey.GetHashCode() ^ this.Metric.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            AggregateGroupKey otherKey = obj as AggregateGroupKey;
            if (otherKey != null)
            {
                return this.EventSource.Equals(otherKey.EventSource) &&
                    this.InstanceId.Equals(otherKey.InstanceId) &&
                    this.TenantId.Equals(otherKey.TenantId) &&
                    this.DimensionsKey.Equals(otherKey.DimensionsKey) &&
                    this.Metric.Equals(otherKey.Metric);
            }

            return false;
        }
    }
}
