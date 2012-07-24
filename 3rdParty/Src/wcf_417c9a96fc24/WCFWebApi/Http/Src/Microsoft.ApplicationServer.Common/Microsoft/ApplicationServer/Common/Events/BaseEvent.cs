//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Events
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Text;

    [DataContract]
    [KnownType(typeof(MetricEvent))]
    class BaseEvent
    {
        const char Delimiter = ',';
        const char DimensionFieldSeparator = '=';

        public BaseEvent()
        {
            this.TimeCreated = DateTime.UtcNow;
            this.EventSource = string.Empty;
            this.InstanceId = string.Empty;
            this.TenantId = string.Empty;            
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime TimeCreated { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string EventSource { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string InstanceId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string TenantId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> Dimensions { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string AdditionalData { get; set; }

        public string GroupByKey
        {
            get
            {
                return this.GroupBySourceKey + this.GroupByDimensionsOnlyKey;
            }
        }

        public string GroupBySourceKey
        {
            get
            {
                return this.EventSource + Delimiter + this.InstanceId + Delimiter + this.TenantId + Delimiter;
            }
        }

        public string GroupByDimensionsOnlyKey
        {
            get
            {
                return EncodeDimensions(this.Dimensions);
            }
        }

        public static string EncodeDimensions(Dictionary<string, string> dimensions)
        {
            string dimensionsKey = string.Empty;
            if (dimensions != null && dimensions.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, string> pair in dimensions)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(Delimiter);
                    }

                    sb.Append(pair.Key);
                    sb.Append(DimensionFieldSeparator);
                    sb.Append(pair.Value);
                }

                dimensionsKey = sb.ToString();
            }

            return dimensionsKey;
        }

        public static Dictionary<string, string> DecodeDimensions(string groupByDimensionsOnlyKey)
        {
            Dictionary<string, string> dimensions = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(groupByDimensionsOnlyKey))
            {
                string[] dimensionPairs = groupByDimensionsOnlyKey.Split(Delimiter);
                if (dimensionPairs != null && dimensionPairs.Length > 0)
                {
                    foreach (string dimensionPair in dimensionPairs)
                    {
                        string[] dimensionFields = dimensionPair.Split(DimensionFieldSeparator);
                        if (dimensionFields != null && dimensionFields.Length == 2)
                        {
                            dimensions.Add(dimensionFields[0], dimensionFields[1]);
                        }
                    }
                }
            }

            return dimensions;
        }
    }
}
