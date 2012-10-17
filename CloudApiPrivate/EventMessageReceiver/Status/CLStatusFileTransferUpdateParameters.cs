//
//  CLStatusFileTransferUpdateParameters.cs
//  Cloud Windows
//
//  Created by DavidBruck.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPrivate.EventMessageReceiver.Status
{
    public struct CLStatusFileTransferUpdateParameters
    {
        public DateTime TransferStartTime
        {
            get
            {
                if (!IsValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid CLStatusFileTransferUpdateParameters");
                }
                return _transferStartTime;
            }
        }
        private DateTime _transferStartTime;

        public long ByteSize
        {
            get
            {
                if (!IsValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid CLStatusFileTransferUpdateParameters");
                }
                return _byteSize;
            }
        }
        private long _byteSize;

        public string RelativePath
        {
            get
            {
                if (!IsValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid CLStatusFileTransferUpdateParameters");
                }
                return _relativePath;
            }
        }
        private string _relativePath;

        public long ByteProgress
        {
            get
            {
                if (!IsValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid CLStatusFileTransferUpdateParameters");
                }
                return _byteProgress;
            }
        }
        private long _byteProgress;

        private bool IsValid;

        public CLStatusFileTransferUpdateParameters(DateTime TransferStartTime, long ByteSize, string RelativePath, long ByteProgress)
        {
            this._transferStartTime = TransferStartTime;
            this._byteSize = ByteSize;
            this._relativePath = RelativePath;
            this._byteProgress = ByteProgress;
            this.IsValid = true;
        }
    }
}