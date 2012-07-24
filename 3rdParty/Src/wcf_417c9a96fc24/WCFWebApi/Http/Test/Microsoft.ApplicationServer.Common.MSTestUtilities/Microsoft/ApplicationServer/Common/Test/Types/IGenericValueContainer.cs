// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Types
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Tagging interface to assist comparing instances of these types.
    /// </summary>
    public interface IGenericValueContainer
    {
        object GetValue();
    }
}
