// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Mocks
{
    using System;
    using System.IO;
    using System.ServiceModel.Channels;

    public class MockUnseekableStream : Stream
    {
        private Stream innerStream;

        public MockUnseekableStream(Stream innerStream)
        {
            if (innerStream == null)
            {
                throw new ArgumentNullException("stream");
            }

            this.innerStream = innerStream;
        }

        public override bool CanRead
        {
            get 
            { 
                return this.innerStream.CanRead; 
            }
        }

        public override bool CanSeek
        {
            get 
            {
                return false; 
            }
        }

        public override bool CanWrite
        {
            get 
            {
                return this.innerStream.CanWrite; 
            }
        }

        public override void Flush()
        {
            this.innerStream.Flush();
        }

        public override long Length
        {
            get 
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                return this.innerStream.Position;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.innerStream.Read(buffer, offset, count); 
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.innerStream.Write(buffer, offset, count); 
        }
    }
}
