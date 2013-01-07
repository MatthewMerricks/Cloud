//
// TransferProgress.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.REST
{
    public sealed class TransferProgress
    {
        public long BytesTransferred
        {
            get
            {
                return _bytesTransferred;
            }
        }
        private readonly long _bytesTransferred;

        public long TotalByteSize
        {
            get
            {
                return _totalByteSize;
            }
        }
        private readonly long _totalByteSize;

        internal TransferProgress(long BytesTransferred, long TotalByteSize)
        {
            this._bytesTransferred = BytesTransferred;
            this._totalByteSize = TotalByteSize;
        }
    }
}