//
// GeneralErrorMessage.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Model.EventMessages
{
    public sealed class GeneralErrorMessage : ErrorMessage
    {
        internal GeneralErrorMessage(string Message, EventMessageLevel Importance)
            : base(Message, Importance, ErrorMessageType.General) { }
    }
}