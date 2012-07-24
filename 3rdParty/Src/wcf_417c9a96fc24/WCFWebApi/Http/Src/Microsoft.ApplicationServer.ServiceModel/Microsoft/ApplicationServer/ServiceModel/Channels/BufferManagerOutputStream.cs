// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel.Channels
{
    using System;
    using System.Globalization;
    using System.ServiceModel; // for QuotaExceededException
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common;

    internal class BufferManagerOutputStream : BufferedOutputStream
    {
        private string quotaExceededString;

        public BufferManagerOutputStream(string quotaExceededString)
            : base()
        {
            this.quotaExceededString = quotaExceededString;
        }

        public BufferManagerOutputStream(string quotaExceededString, int maxSize)
            : base(maxSize)
        {
            this.quotaExceededString = quotaExceededString;
        }

        // ALTERED_FOR_PORT:
        // We're not getting the internal buffer manager as we do in the framework but just wrapping the bufferManager
        public BufferManagerOutputStream(string quotaExceededString, int initialSize, int maxSize, BufferManager bufferManager)
            : base(initialSize, maxSize, GetInternalBufferManager(bufferManager))
        {
            this.quotaExceededString = quotaExceededString;
        }

        public void Init(int initialSize, int maxSizeQuota, BufferManager bufferManager)
        {
            this.Init(initialSize, maxSizeQuota, maxSizeQuota, bufferManager);
        }

        public void Init(int initialSize, int maxSizeQuota, int effectiveMaxSize, BufferManager bufferManager)
        {
            // ALTERED_FOR_PORT:
            // We're not getting the internal buffer manager as we do in the framework but just wrapping the bufferManager
            this.Reinitialize(initialSize, maxSizeQuota, effectiveMaxSize, GetInternalBufferManager(bufferManager));
        }

        protected override Exception CreateQuotaExceededException(int maxSizeQuota)
        {
            string excMsg = string.Format(CultureInfo.CurrentCulture, this.quotaExceededString, maxSizeQuota);

            // TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available:
            //// if (TD.MaxSentMessageSizeExceededIsEnabled())
            //// {
            ////     TD.MaxSentMessageSizeExceeded(excMsg);
            //// }
            return new QuotaExceededException(excMsg);
        }

        private static InternalBufferManager GetInternalBufferManager(BufferManager bufferManager)
        {
            Fx.Assert(bufferManager != null, "The 'bufferManager' parameter should not be null.");

            return new WrappingInternalBufferManager(bufferManager);
        }

        private class WrappingInternalBufferManager : InternalBufferManager
        {
            private BufferManager bufferManager;

            public WrappingInternalBufferManager(BufferManager bufferManager)
            {
                Fx.Assert(bufferManager != null, "The 'bufferManager' parameter should not be null.");

                this.bufferManager = bufferManager;
            }

            public override byte[] TakeBuffer(int bufferSize)
            {
                return this.bufferManager.TakeBuffer(bufferSize);
            }

            public override void ReturnBuffer(byte[] buffer)
            {
                this.bufferManager.ReturnBuffer(buffer);
            }

            public override void Clear()
            {
                this.bufferManager.Clear();
            }
        }
    }
}
