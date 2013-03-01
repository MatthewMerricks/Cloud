﻿//
// HaltSyncEngineOnConnectionFailureErrorInfo.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Model.EventMessages.ErrorInfo
{
    public sealed class HaltSyncEngineOnConnectionFailureErrorInfo : BaseErrorInfo
    {
        internal HaltSyncEngineOnConnectionFailureErrorInfo()
            : base(ErrorMessageType.HaltSyncEngineOnConnectionFailure) { }
    }
}