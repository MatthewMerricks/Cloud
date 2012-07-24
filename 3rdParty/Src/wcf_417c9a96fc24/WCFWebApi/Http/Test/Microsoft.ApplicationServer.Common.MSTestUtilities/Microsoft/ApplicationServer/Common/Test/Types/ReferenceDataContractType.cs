// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Types
{
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using System.Collections.Generic;

    [DataContract(IsReference = true)]
    public class ReferenceDataContractType : INotJsonSerializable
    {
        private ReferenceDataContractType other;

        public ReferenceDataContractType()
        {
        }

        public ReferenceDataContractType(ReferenceDataContractType other)
        {
            this.other = other;
        }

        [DataMember]
        public ReferenceDataContractType Other
        {
            get
            {
                return this.other;
            }

            set
            {
                this.OtherSet = true;
                this.other = value;
            }
        }

        [XmlIgnore]
        public bool OtherSet { get; private set; }

        public static IEnumerable<ReferenceDataContractType> GetTestData()
        {
            ReferenceDataContractType first = new ReferenceDataContractType();
            ReferenceDataContractType second = new ReferenceDataContractType(first);
            first.Other = second;

            return new ReferenceDataContractType[] { 
                new ReferenceDataContractType(), 
                new ReferenceDataContractType(new ReferenceDataContractType()),
                first};
        }
    }
}
