﻿//
// InformationalMessage.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPrivate.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPrivate.EventMessageReceiver
{
    public sealed class InformationalMessage : EventMessage
    {
        #region EventMessage abstract overrides
        public override EventMessageImage Image
        {
            get
            {
                return EventMessageImage.Informational;
            }
        }

        public override string Message
        {
            get
            {
                return _message;
            }
        }
        private string _message;
        #endregion

        public InformationalMessage(string informationalMessage)
            : base()
        {
            this._message = informationalMessage
                ?? "Information";
        }
    }
}