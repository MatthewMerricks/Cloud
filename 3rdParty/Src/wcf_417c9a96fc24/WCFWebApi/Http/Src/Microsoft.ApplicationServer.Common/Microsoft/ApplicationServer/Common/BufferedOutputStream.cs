//----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common
{
    using System;
    using System.Globalization;
    using System.IO;

    class BufferedOutputStream : Stream
    {
        [Fx.Tag.Cache(typeof(byte), Fx.Tag.CacheAttrition.None, Scope = Fx.Tag.Strings.ExternallyManaged,
            SizeLimit = Fx.Tag.Strings.ExternallyManaged)]
        InternalBufferManager theBufferManager;

        [Fx.Tag.Queue(typeof(byte), SizeLimit = "BufferedOutputStream(maxSize)",
            StaleElementsRemovedImmediately = true, EnqueueThrowsIfFull = true)]
        byte[][] chunks;

        int chunkCount;
        byte[] currentChunk;
        int currentChunkSize;
        int maxSize;
        int theMaxSizeQuota;
        int totalSize;
        bool callerReturnsBuffer;
        bool bufferReturned;
        bool initialized;

        // requires an explicit call to Init() by the caller
        public BufferedOutputStream()
        {
            this.chunks = new byte[4][];
        }

        public BufferedOutputStream(int initialSize, int maxSize, InternalBufferManager bufferManager)
            : this()
        {
            this.Reinitialize(initialSize, maxSize, bufferManager);
        }

        public BufferedOutputStream(int maxSize)
            : this(0, maxSize, InternalBufferManager.Create(0, int.MaxValue))
        {
        }

        public override bool CanRead
        {
            get
            {
                return false;
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
                return true;
            }
        }

        public override long Length
        {
            get
            {
                return this.totalSize;
            }
        }

        public override long Position
        {
            get
            {
                throw Fx.Exception.AsError(new NotSupportedException(SRCore.SeekNotSupported));
            }

            set
            {
                throw Fx.Exception.AsError(new NotSupportedException(SRCore.SeekNotSupported));
            }
        }

        public void Reinitialize(int initialSize, int maxSizeQuota, InternalBufferManager bufferManager)
        {
            this.Reinitialize(initialSize, maxSizeQuota, maxSizeQuota, bufferManager);
        }

        public void Reinitialize(int initialSize, int maxSizeQuota, int effectiveMaxSize, InternalBufferManager bufferManager)
        {
            Fx.Assert(!this.initialized, "Clear must be called before re-initializing stream");
            this.theMaxSizeQuota = maxSizeQuota;
            this.maxSize = effectiveMaxSize;
            this.theBufferManager = bufferManager;
            this.currentChunk = bufferManager.TakeBuffer(initialSize);
            this.currentChunkSize = 0;
            this.totalSize = 0;
            this.chunkCount = 1;
            this.chunks[0] = this.currentChunk;
            this.initialized = true;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            throw Fx.Exception.AsError(new NotSupportedException(SRCore.ReadNotSupported));
        }

        public override int EndRead(IAsyncResult result)
        {
            throw Fx.Exception.AsError(new NotSupportedException(SRCore.ReadNotSupported));
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            this.Write(buffer, offset, size);
            return new CompletedAsyncResult(callback, state);
        }

        public override void EndWrite(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        public void Clear()
        {
            if (!this.callerReturnsBuffer)
            {
                for (int i = 0; i < this.chunkCount; i++)
                {
                    this.theBufferManager.ReturnBuffer(this.chunks[i]);
                    this.chunks[i] = null;
                }
            }

            this.callerReturnsBuffer = false;
            this.initialized = false;
            this.bufferReturned = false;
            this.chunkCount = 0;
            this.currentChunk = null;
        }

        public override void Close()
        {
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int size)
        {
            throw Fx.Exception.AsError(new NotSupportedException(SRCore.ReadNotSupported));
        }

        public override int ReadByte()
        {
            throw Fx.Exception.AsError(new NotSupportedException(SRCore.ReadNotSupported));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw Fx.Exception.AsError(new NotSupportedException(SRCore.SeekNotSupported));
        }

        public override void SetLength(long value)
        {
            throw Fx.Exception.AsError(new NotSupportedException(SRCore.SeekNotSupported));
        }

        public MemoryStream ToMemoryStream()
        {
            int bufferSize;
            byte[] buffer = this.ToArray(out bufferSize);
            return new MemoryStream(buffer, 0, bufferSize);
        }

        public byte[] ToArray(out int bufferSize)
        {
            Fx.Assert(this.initialized, "No data to return from uninitialized stream");
            Fx.Assert(!this.bufferReturned, "ToArray cannot be called more than once");

            byte[] buffer;
            if (this.chunkCount == 1)
            {
                buffer = this.currentChunk;
                bufferSize = this.currentChunkSize;
                this.callerReturnsBuffer = true;
            }
            else
            {
                buffer = this.theBufferManager.TakeBuffer(this.totalSize);
                int offset = 0;
                int count = this.chunkCount - 1;
                for (int i = 0; i < count; i++)
                {
                    byte[] chunk = this.chunks[i];
                    Buffer.BlockCopy(chunk, 0, buffer, offset, chunk.Length);
                    offset += chunk.Length;
                }

                Buffer.BlockCopy(this.currentChunk, 0, buffer, offset, this.currentChunkSize);
                bufferSize = this.totalSize;
            }

            this.bufferReturned = true;
            return buffer;
        }

        public void Skip(int size)
        {
            this.WriteCore(null, 0, size);
        }

        public override void Write(byte[] buffer, int offset, int size)
        {
            this.WriteCore(buffer, offset, size);
        }

        public override void WriteByte(byte value)
        {
            Fx.Assert(this.initialized, "Cannot write to uninitialized stream");
            Fx.Assert(!this.bufferReturned, "Cannot write to stream once ToArray has been called.");

            if (this.totalSize == this.maxSize)
            {
                throw Fx.Exception.AsError(this.CreateQuotaExceededException(this.maxSize));
            }

            if (this.currentChunkSize == this.currentChunk.Length)
            {
                this.AllocNextChunk(1);
            }

            this.currentChunk[this.currentChunkSize++] = value;
        }

        protected virtual Exception CreateQuotaExceededException(int maxSizeQuota)
        {
            return new InvalidOperationException(SRCore.BufferedOutputStreamQuotaExceeded(maxSizeQuota));
        }

        void WriteCore(byte[] buffer, int offset, int size)
        {
            Fx.Assert(this.initialized, "Cannot write to uninitialized stream");
            Fx.Assert(!this.bufferReturned, "Cannot write to stream once ToArray has been called.");

            if (size < 0)
            {
                throw Fx.Exception.ArgumentOutOfRange("size", size, SRCore.ValueMustBeNonNegative);
            }

            if ((int.MaxValue - size) < this.totalSize)
            {
                throw Fx.Exception.AsError(this.CreateQuotaExceededException(this.theMaxSizeQuota));
            }

            int newTotalSize = this.totalSize + size;
            if (newTotalSize > this.maxSize)
            {
                throw Fx.Exception.AsError(this.CreateQuotaExceededException(this.theMaxSizeQuota));
            }

            int remainingSizeInChunk = this.currentChunk.Length - this.currentChunkSize;
            if (size > remainingSizeInChunk)
            {
                if (remainingSizeInChunk > 0)
                {
                    if (buffer != null)
                    {
                        Buffer.BlockCopy(buffer, offset, this.currentChunk, this.currentChunkSize, remainingSizeInChunk);
                    }

                    this.currentChunkSize = this.currentChunk.Length;
                    offset += remainingSizeInChunk;
                    size -= remainingSizeInChunk;
                }

                this.AllocNextChunk(size);
            }

            if (buffer != null)
            {
                Buffer.BlockCopy(buffer, offset, this.currentChunk, this.currentChunkSize, size);
            }

            this.totalSize = newTotalSize;
            this.currentChunkSize += size;
        }

        void AllocNextChunk(int minimumChunkSize)
        {
            int newChunkSize;
            if (this.currentChunk.Length > (int.MaxValue / 2))
            {
                newChunkSize = int.MaxValue;
            }
            else
            {
                newChunkSize = this.currentChunk.Length * 2;
            }

            if (minimumChunkSize > newChunkSize)
            {
                newChunkSize = minimumChunkSize;
            }

            byte[] newChunk = this.theBufferManager.TakeBuffer(newChunkSize);
            if (this.chunkCount == this.chunks.Length)
            {
                byte[][] newChunks = new byte[this.chunks.Length * 2][];
                Array.Copy(this.chunks, newChunks, this.chunks.Length);
                this.chunks = newChunks;
            }

            this.chunks[this.chunkCount++] = newChunk;
            this.currentChunk = newChunk;
            this.currentChunkSize = 0;
        }
    }
}
