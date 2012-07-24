// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Types
{
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using System.Collections.Generic;

    [DataContract]
    [KnownType(typeof(DerivedPocoType))]
    [KnownType(typeof(DerivedDataContractType))]
    [XmlInclude(typeof(DerivedPocoType))]
    [XmlInclude(typeof(DerivedDataContractType))]
    public class DerivedDataContractType : DataContractType
    {
        private PocoType reference;

        public DerivedDataContractType()
        {
        }

        public DerivedDataContractType(int id, string name, PocoType reference) 
            : base(id, name)
        {
            this.reference = reference;
        }

        [DataMember]
        public PocoType Reference
        {
            get
            {
                return this.reference;
            }

            set
            {
                this.ReferenceSet = true;
                this.reference = value;
            }
        }

        [XmlIgnore]
        public bool ReferenceSet { get; private set; }

        public static new IEnumerable<DerivedDataContractType> GetTestData()
        {
            return new DerivedDataContractType[] { 
                new DerivedDataContractType(), 
                new DerivedDataContractType(1, "SomeName", new PocoType(2, "SomeOtherName")) };
        }

        public static IEnumerable<DerivedDataContractType> GetKnownTypeTestData()
        {
            return new DerivedDataContractType[] { 
                new DerivedDataContractType(), 
                new DerivedDataContractType(1, "SomeName", null), 
                new DerivedDataContractType(1, "SomeName", new DerivedPocoType(2, "SomeOtherName", null))};
        }
    }
}
