//
// HaltSyncboxOnConnectionFailureErrorInfo.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model.EventMessages.ErrorInfo
{
    public sealed class HaltSyncboxOnConnectionFailureErrorInfo : BaseErrorInfo
    {
        internal HaltSyncboxOnConnectionFailureErrorInfo()
            : base(ErrorMessageType.HaltSyncboxOnConnectionFailure) { }
    }
}