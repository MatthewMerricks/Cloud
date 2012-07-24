//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]    
    sealed class MetricEvent : BaseEvent
    {
        public MetricEvent()
            : base()
        {
            this.Dimensions = new Dictionary<string, string>();
        }

        [DataMember(EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TimeSpan TimeWindow { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int Count { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public double Average { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public double Total { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public double Minimum { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public double Maximum { get; set; }

        public MetricEvent Clone()
        {
            MetricEvent metricEvent = new MetricEvent()
            {
                TimeCreated = this.TimeCreated,
                EventSource = this.EventSource,
                TenantId = this.TenantId,
                InstanceId = this.InstanceId,
                AdditionalData = this.AdditionalData,
                Name = this.Name,
                TimeWindow = this.TimeWindow,
                Count = this.Count,
                Total = this.Total,
                Average = this.Average,
                Minimum = this.Minimum,
                Maximum = this.Maximum
            };

            foreach (KeyValuePair<string, string> pair in this.Dimensions)
            {
                metricEvent.Dimensions.Add(pair.Key, pair.Value);
            }

            return metricEvent;
        }
    }
}
