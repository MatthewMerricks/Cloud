//
// HaltSyncEngineOnAuthenticationFailureErrorInfo.cs
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
    public sealed class HaltSyncEngineOnAuthenticationFailureErrorInfo : BaseErrorInfo
    {
        public bool TokenExpired
        {
            get
            {
                return _tokenExpired;
            }
        }
        private readonly bool _tokenExpired;

        internal HaltSyncEngineOnAuthenticationFailureErrorInfo(bool TokenExpired)
            : base(ErrorMessageType.HaltSyncEngineOnAuthenticationFailure)
        {
            this._tokenExpired = TokenExpired;
        }
    }
}