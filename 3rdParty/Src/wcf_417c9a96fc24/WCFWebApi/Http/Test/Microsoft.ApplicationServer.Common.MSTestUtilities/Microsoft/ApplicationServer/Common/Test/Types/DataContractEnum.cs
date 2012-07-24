// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Types
{
    using System.Runtime.Serialization;

    [DataContract]
    public enum DataContractEnum
    {
        [EnumMember]
        First,

        [EnumMember]
        Second,

        Third
    }
}
