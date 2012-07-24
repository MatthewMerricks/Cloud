﻿// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Types
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [Serializable]
    public class ISerializableType : ISerializable, INameAndIdContainer
    {
        private int id;
        private string name;

        public ISerializableType()
        {
        }

        public ISerializableType(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public ISerializableType(SerializationInfo information, StreamingContext context)
        {
            this.id = information.GetInt32("Id");
            this.name = information.GetString("Name");
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

        public bool IdSet { get; private set; }

        public bool NameSet { get; private set; }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Id", this.Id);
            info.AddValue("Name", this.Name);
        }

        public static IEnumerable<ISerializableType> GetTestData()
        {
            return new ISerializableType[] { new ISerializableType(), new ISerializableType(1, "SomeName") };
        }
    }
}
