// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Mocks
{
    using System;

    public class MockAsyncCallback
    {
        public static AsyncCallback Create()
        {
            return new AsyncCallback(SomeCallback);
        }

        static void SomeCallback(IAsyncResult result)
        {

        }
    }
}
