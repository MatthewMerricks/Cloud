// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Mocks
{
    using System;
    using System.Threading;

    public class MockAsyncResult : IAsyncResult
    {
        private object asyncState = new object();

        public object AsyncState
        {
            get
            {
                return this.asyncState;
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool CompletedSynchronously
        {
            get
            {
                return true;
            }
        }

        public bool IsCompleted
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void SetAsyncState(object obj)
        {
            this.asyncState = obj;
        }
    }
}
