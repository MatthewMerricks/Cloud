// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Types
{
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using System.Collections.Generic;

    [KnownType(typeof(DerivedPocoType))]
    [XmlInclude(typeof(DerivedPocoType))]
    public class PocoType : INameAndIdContainer
    {
        private int id;
        private string name;

        public PocoType()
        {
        }

        public PocoType(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

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

        [IgnoreDataMember]
        [XmlIgnore]
        public bool IdSet { get; private set; }

        [IgnoreDataMember]
        [XmlIgnore]
        public bool NameSet { get; private set; }

        public static IEnumerable<PocoType> GetTestData()
        {
            return new PocoType[] { new PocoType(), new PocoType(1, "SomeName") };
        }

        public static IEnumerable<PocoType> GetTestDataWithNull()
        {
            return GetTestData().Concat(new PocoType[] { null });
        }

        public static IEnumerable<DerivedPocoType> GetDerivedTypeTestData()
        {
            return new DerivedPocoType[] { 
                new DerivedPocoType(), 
                new DerivedPocoType(1, "SomeName", null), 
                new DerivedPocoType(1, "SomeName", new PocoType(2, "SomeOtherName"))};
        }

        public static IEnumerable<DerivedPocoType> GetDerivedTypeTestDataWithNull()
        {
            return GetDerivedTypeTestData().Concat(new DerivedPocoType[] { null });
        }
    }
}
