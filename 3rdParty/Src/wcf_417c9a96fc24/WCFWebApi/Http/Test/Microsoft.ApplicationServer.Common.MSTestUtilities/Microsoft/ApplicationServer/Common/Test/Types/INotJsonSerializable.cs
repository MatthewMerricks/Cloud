// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Types
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Tagging interface to indicate types which we know Json cannot serialize.
    /// </summary>
    public interface INotJsonSerializable
    {
    }
}
