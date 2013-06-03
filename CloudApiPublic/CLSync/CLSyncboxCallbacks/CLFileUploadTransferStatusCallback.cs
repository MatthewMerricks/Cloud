//
// CLFileUploadTransferStatusCallback.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.CLSync
{
    /// <summary>
    /// Status update callback for upload of a file CLFileItem
    /// </summary>
    /// <param name="inputItemIndex">The zero-based array index of the input parameter which led to the transfer</param>
    /// <param name="byteProgress">Bytes already transferred for the file</param>
    /// <param name="totalByteSize">Total byte size of the file</param>
    /// <param name="userState">A user-provided state which will be passed whenever the status callback fires</param>
    public delegate void CLFileUploadTransferStatusCallback(int inputItemIndex, long byteProgress, long totalByteSize, object userState);
}