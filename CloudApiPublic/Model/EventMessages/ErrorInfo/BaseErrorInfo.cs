//
// BaseErrorInfo.cs
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
    public abstract class BaseErrorInfo
    {
        public ErrorMessageType ErrorType
        {
            get
            {
                return _errorType;
            }
        }
        private readonly ErrorMessageType _errorType;

        internal protected BaseErrorInfo(ErrorMessageType ErrorType)
        {
            this._errorType = ErrorType;
        }
    }
}