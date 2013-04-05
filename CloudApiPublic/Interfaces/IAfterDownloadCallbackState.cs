//
// IAfterDownloadCallbackState.cs
// Cloud SDK Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Interfaces
{
    internal interface IAfterDownloadCallbackState
    {
        object LockerForDownloadedFileAccess { get; }
        void SetFileNotFound();
    }
}