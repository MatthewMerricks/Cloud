// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Types
{
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using System.Collections.Generic;

    [DataContract]
    [KnownType(typeof(DerivedDataContractType))]
    [XmlInclude(typeof(DerivedDataContractType))]
    public class DataContractType : INameAndIdContainer
    {
        private int id;
        private string name;

        public DataContractType()
        {
        }

        public DataContractType(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        [DataMember]
        public int Id
        {
            get
            {
                return this.id;
            }

            set
            {
                this.IdSet = true;
                this.id = value;
            }
        }

        [DataMember]
        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                this.NameSet = true;
                this.name = value;
            }
        }

        [XmlIgnore]
        public bool IdSet { get; private set; }

        [XmlIgnore]
        public bool NameSet { get; private set; }

        public static IEnumerable<DataContractType> GetTestData()
        {
            return new DataContractType[] { new DataContractType(), new DataContractType(1, "SomeName") };
        }

        public static IEnumerable<DerivedDataContractType> GetDerivedTypeTestData()
        {
            return new DerivedDataContractType[] { 
                new DerivedDataContractType(), 
                new DerivedDataContractType(1, "SomeName", null), 
                new DerivedDataContractType(1, "SomeName", new PocoType(2, "SomeOtherName"))};
        }
    }
}
