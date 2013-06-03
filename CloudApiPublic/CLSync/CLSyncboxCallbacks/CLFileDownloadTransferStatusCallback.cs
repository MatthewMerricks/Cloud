//
// CLFileDownloadTransferStatusCallback.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.CLSync
{
    /// <summary>
    /// Status update callback for download of a file CLFileItem.
    /// </summary>
    /// <param name="byteProgress">Bytes already transferred for the file</param>
    /// <param name="totalByteSize">Total byte size of the file</param>
    /// <param name="userState">A user-provided state which will be passed whenever the status callback fires</param>
    public delegate void CLFileDownloadTransferStatusCallback(long byteProgress, long totalByteSize, object userState);
}