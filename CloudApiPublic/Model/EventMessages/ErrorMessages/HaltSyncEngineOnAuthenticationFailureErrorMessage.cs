//
// HaltSyncEngineOnAuthenticationFailureErrorMessage.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Model.EventMessages.ErrorMessages
{
    public sealed class HaltSyncEngineOnAuthenticationFailureErrorMessage : ErrorMessage
    {
        public bool TokenExpired
        {
            get
            {
                return _tokenExpired;
            }
        }
        private readonly bool _tokenExpired;

        internal HaltSyncEngineOnAuthenticationFailureErrorMessage(string Message, EventMessageLevel Importance, bool TokenExpired)
            : base(Message, Importance, ErrorMessageType.HaltSyncEngineOnAuthenticationFailure)
        {
            this._tokenExpired = TokenExpired;
        }
    }
}