// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Types
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [KnownType(typeof(DerivedPocoType))]
    [XmlInclude(typeof(DerivedPocoType))]
    public class DerivedXmlSerializableType : XmlSerializableType, INotJsonSerializable
    {
        private PocoType reference;

        public DerivedXmlSerializableType()
        {
        }

        public DerivedXmlSerializableType(int id, string name, PocoType reference) 
            : base(id, name)
        {
            this.reference = reference;
        }

        [XmlElement]
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

        public static new IEnumerable<DerivedXmlSerializableType> GetTestData()
        {
            return new DerivedXmlSerializableType[] { 
                new DerivedXmlSerializableType(), 
                new DerivedXmlSerializableType(1, "SomeName", new PocoType(2, "SomeOtherName")) };
        }

        public static IEnumerable<DerivedXmlSerializableType> GetKnownTypeTestData()
        {
            return new DerivedXmlSerializableType[] { 
                new DerivedXmlSerializableType(), 
                new DerivedXmlSerializableType(1, "SomeName", null), 
                new DerivedXmlSerializableType(1, "SomeName", new DerivedPocoType(2, "SomeOtherName", null))};
        }
    }
}
