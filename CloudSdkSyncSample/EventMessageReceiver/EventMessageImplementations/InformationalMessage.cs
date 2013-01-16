﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Static;

namespace CloudSdkSyncSample.EventMessageReceiver
{
    /// <summary>
    /// <see cref="EventMessage"/> for some information.
    /// </summary>
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

        internal InformationalMessage(string informationalMessage)
            : base()
        {
            this._message = informationalMessage
                ?? "Information";
        }
    }
}