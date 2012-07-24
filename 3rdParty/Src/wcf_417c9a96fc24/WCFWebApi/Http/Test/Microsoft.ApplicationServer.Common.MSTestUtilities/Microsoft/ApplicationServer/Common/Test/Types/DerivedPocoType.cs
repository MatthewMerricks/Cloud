// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Types
{
    using System.Xml.Serialization;
    using System.Runtime.Serialization;

    public class DerivedPocoType : PocoType
    {
        private PocoType reference;

        public DerivedPocoType()
        {
        }

        public DerivedPocoType(int id, string name, PocoType reference)
            : base(id, name)
        {
            this.reference = reference;
        }

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

        [IgnoreDataMember]
        [XmlIgnore]
        public bool ReferenceSet { get; private set; }
    }
}
