// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Mocks
{
    using System.ServiceModel.Channels;

    public class MockBufferManager : BufferManager
    {
        public bool ReturnBufferCalled { get; set; }

        public bool TakeBufferCalled { get; set; }

        public byte[] BufferReturned { get; private set; }

        public byte[] BufferTaken { get; private set; }

        public override void Clear()
        {
            // do nothing
        }

        public override void ReturnBuffer(byte[] buffer)
        {
            this.ReturnBufferCalled = true;
            this.BufferReturned = buffer;
        }

        public override byte[] TakeBuffer(int bufferSize)
        {
            this.TakeBufferCalled = true;
            this.BufferTaken = new byte[bufferSize];
            return this.BufferTaken;
        }
    }
}
