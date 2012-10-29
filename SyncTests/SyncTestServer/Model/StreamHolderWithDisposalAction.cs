using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SyncTestServer.Model
{
    public sealed class StreamHolderWithDisposalAction : Stream
    {
        public override bool CanRead
        {
            get { return innerStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return innerStream.CanSeek; }
        }

        public override bool CanTimeout
        {
            get
            {
                return innerStream.CanTimeout;
            }
        }

        public override bool CanWrite
        {
            get { return innerStream.CanWrite; }
        }

        public override long Length
        {
            get { return innerStream.Length; }
        }

        public override long Position
        {
            get
            {
                return innerStream.Position;
            }
            set
            {
                innerStream.Position = value;
            }
        }

        public override int ReadTimeout
        {
            get
            {
                return innerStream.ReadTimeout;
            }
            set
            {
                innerStream.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                return innerStream.WriteTimeout;
            }
            set
            {
                innerStream.WriteTimeout = value;
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return innerStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return innerStream.BeginWrite(buffer, offset, count, callback, state);
        }

        // the whole reason for this wrapper class:
        public override void Close()
        {
            innerStream.Close();
            base.Close();

            runOnDispose.Key(runOnDispose.Value);
        }

        //// unable to forward this virtual call through innerStream
        //protected override System.Threading.WaitHandle CreateWaitHandle()
        //{
        //    return innerStream.CreateWaitHandle();
        //}

        protected override void Dispose(bool disposing)
        {
            innerStream.Dispose();
            base.Dispose(disposing);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return innerStream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            innerStream.EndWrite(asyncResult);
        }

        public override void Flush()
        {
            innerStream.Flush();
        }

        ////cannot forward to innerStream
        //protected override void ObjectInvariant()
        //{
        //    innerStream.ObjectInvariant();
        //}

        public override int Read(byte[] buffer, int offset, int count)
        {
            return innerStream.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            return innerStream.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            innerStream.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            innerStream.WriteByte(value);
        }

        private readonly Stream innerStream;
        private readonly KeyValuePair<Action<object>, object> runOnDispose;

        public StreamHolderWithDisposalAction(Stream innerStream, KeyValuePair<Action<object>, object> runOnDispose)
        {
            if (innerStream == null)
            {
                throw new NullReferenceException("innerStream cannot be null");
            }
            if (runOnDispose.Key == null)
            {
                throw new NullReferenceException("Action<object> in runOnDispose cannot be null");
            }

            this.innerStream = innerStream;
            this.runOnDispose = runOnDispose;
        }
    }
}