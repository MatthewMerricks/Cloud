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
using CloudApiPublic.Static;

namespace CloudApiPublic.EventMessageReceiver
{
    public sealed class DownloadingMessage : EventMessage
    {
        #region EventMessage abstract overrides
        public override EventMessageImage Image
        {
            get
            {
                return EventMessageImage.Busy;
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
        private uint CurrentCount;
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

                TimeSpan fadeInToAdd;
                DateTime currentTime = DateTime.UtcNow;
                if (currentTime.CompareTo(base.FadeInCompletion) < 0)
                {
                    fadeInToAdd = base.FadeInCompletion.Subtract(currentTime);
                }
                else if (currentTime.CompareTo(base.StartFadeOut) > 0)
                {
                    fadeInToAdd = DownloadingMessage.DefaultFadeInTime
                        .Subtract(base.CompleteFadeOut.Subtract(currentTime));
                }
                else
                {
                    fadeInToAdd = TimeSpan.Zero;
                }

                if (newCount == 0)
                {
                    base.CompleteFadeOut = (base.StartFadeOut = base.FadeInCompletion = currentTime.Add(fadeInToAdd))
                        .Add(DownloadingMessage.DefaultFadeOutTime);
                }
                else
                {
                    base.CompleteFadeOut = (base.StartFadeOut = (base.FadeInCompletion = currentTime.Add(fadeInToAdd))
                            .Add(DownloadingMessage.DefaultOpaqueTime))
                        .Add(DownloadingMessage.DefaultFadeOutTime);
                }

                return true;
            }
            return false;
        }

        public override bool ShouldRemove
        {
            get
            {
                return this.CurrentCount == 0
                    && base.ShouldRemove;
            }
        }
    }
}