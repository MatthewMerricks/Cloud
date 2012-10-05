//
//  DownloadingMessage.cs
//  Cloud Windows
//
//  Created by DavidBruck
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPrivate.EventMessageReceiver
{
    public sealed class DownloadingMessage : EventMessage
    {
        #region EventMessage abstract overrides
        public override Static.EventMessageImage Image
        {
            get
            {
                return Static.EventMessageImage.Busy;
            }
        }

        public override string Message
        {
            get
            {
                return CurrentCount.ToString() + " file" +
                    (CurrentCount == 1
                        ? string.Empty
                        : "s") +
                    " downloading";
            }
        }
        #endregion

        #region private fields
        public uint CurrentCount { get; private set; }
        #endregion

        public DownloadingMessage(uint initialCount)
        {
            this.CurrentCount = initialCount;
        }

        public bool SetCount(uint newCount)
        {
            if (this.CurrentCount != newCount)
            {
                this.CurrentCount = newCount;
                NotifyPropertyChanged(parent => parent.Message);
                if (newCount == 0)
                {
                    DateTime startFadeOutTime = DateTime.UtcNow;
                    base.SetFadingTimes(startFadeOutTime,
                        startFadeOutTime,
                        startFadeOutTime.Add(EventMessage.DefaultFadeOutTime));
                }
                else
                {
                    base.SetFadingTimes();
                }
                NotifyPropertyChanged(parent => parent.FadeInCompletion);
                NotifyPropertyChanged(parent => parent.StartFadeOut);
                NotifyPropertyChanged(parent => parent.CompleteFadeOut);

                return true;
            }
            return false;
        }
    }
}