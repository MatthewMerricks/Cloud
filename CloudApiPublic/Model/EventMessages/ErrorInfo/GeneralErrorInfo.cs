﻿//
// GeneralErrorInfo.cs
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
    public sealed class GeneralErrorInfo : BaseErrorInfo
    {
        internal GeneralErrorInfo()
            : base(ErrorMessageType.General) { }
    }
}