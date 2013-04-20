//
// HaltSyncboxOnAuthenticationFailureErrorInfo.cs
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
    public sealed class HaltSyncboxOnAuthenticationFailureErrorInfo : BaseErrorInfo
    {
        public bool TokenExpired
        {
            get
            {
                return _tokenExpired;
            }
        }
        private readonly bool _tokenExpired;

        internal HaltSyncboxOnAuthenticationFailureErrorInfo(bool TokenExpired)
            : base(ErrorMessageType.HaltSyncboxOnAuthenticationFailure)
        {
            this._tokenExpired = TokenExpired;
        }
    }
}