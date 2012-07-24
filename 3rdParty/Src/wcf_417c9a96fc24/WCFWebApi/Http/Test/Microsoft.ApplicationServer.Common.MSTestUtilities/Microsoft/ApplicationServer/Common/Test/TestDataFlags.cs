// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test
{
    using System;

    [Flags]
    public enum TestDataFlags
    {
        AsInstance = 0x1,
        AsList = 0x2,
        AsArray = 0x4,
        AsIEnumerable = 0x8,
        AsIQueryable = 0x10,
        AsNullable = 0x20,
        AsAllButIQueryable = 0x2F,
        AsAll = 0x3F
    }
}
