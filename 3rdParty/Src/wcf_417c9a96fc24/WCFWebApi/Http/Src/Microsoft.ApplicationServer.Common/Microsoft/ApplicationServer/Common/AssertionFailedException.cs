//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Runtime.Serialization;

    [SuppressMessage(FxCop.Category.Design, FxCop.Rule.ExceptionsShouldBePublic, Justification = "Asserts should not be seen by users.", Scope = "Type", Target = "Microsoft.ApplicationServer.Common.AssertionFailedException")]
    [Serializable]
    class AssertionFailedException:Exception
    {
        public AssertionFailedException(string description)
            : base(SRCore.ShipAssertExceptionMessage(description))
        {
        }

        protected AssertionFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
