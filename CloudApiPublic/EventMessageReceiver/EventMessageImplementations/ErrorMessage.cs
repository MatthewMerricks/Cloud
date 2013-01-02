﻿//
//  ErrorMessage.cs
//  Cloud Windows
//
//  Created by DavidBruck
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Static;

namespace CloudApiPublic.EventMessageReceiver
{
    public sealed class ErrorMessage : EventMessage
    {
        new private static readonly TimeSpan DefaultOpaqueTime = TimeSpan.FromSeconds(8); // longer time to display error
        new private static readonly TimeSpan DefaultFadeInPlusOpaqueTime = DefaultFadeInTime.Add(DefaultOpaqueTime); // need to redefine calculated values based on changed opaqueTime
        new private static readonly TimeSpan DefaultFullDisplayTimeIncludingFadings = DefaultFadeInPlusOpaqueTime.Add(DefaultFadeOutTime); // need to redefine calculated values based on changed opaqueTime

        #region EventMessage abstract overrides
        public override EventMessageImage Image
        {
            get
            {
                return EventMessageImage.Error;
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

        public ErrorMessage(string errorMessage)
            : base(DateTime.UtcNow,
                ErrorMessage.DefaultFadeInTime,
                ErrorMessage.DefaultFadeInPlusOpaqueTime,
                ErrorMessage.DefaultFullDisplayTimeIncludingFadings)
        {
            this._message = errorMessage
                ?? "An error occurred";
        }
    }
}