//
//  DownloadedMessage.cs
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
    /// <summary>
    /// <see cref="EventMessage"/> for number of files which have been downloaded since this message was first created.
    /// </summary>
    public sealed class DownloadedMessage : EventMessage
    {
        #region EventMessage abstract overrides
        public override EventMessageImage Image
        {
            get
            {
                return EventMessageImage.Completion;
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
                    " downloaded";
            }
        }
        #endregion

        #region private fields
        private uint CurrentCount;
        #endregion

        internal DownloadedMessage(uint initialCount)
        {
            this.CurrentCount = initialCount;
        }

        internal void IncrementCount(uint incrementAmount = 1)
        {
            this.CurrentCount += incrementAmount;
            NotifyPropertyChanged(parent => parent.Message);

            DateTime currentTime = DateTime.UtcNow;
            TimeSpan fadeInToAdd;
            if (currentTime.CompareTo(base.FadeInCompletion) < 0)
            {
                fadeInToAdd = base.FadeInCompletion.Subtract(currentTime);
            }
            else
            {
                fadeInToAdd = TimeSpan.Zero;
            }

            base.CompleteFadeOut = (base.StartFadeOut = (base.FadeInCompletion = currentTime.Add(fadeInToAdd))
                    .Add(DownloadedMessage.DefaultOpaqueTime))
                .Add(DownloadedMessage.DefaultFadeOutTime);
        }
    }
}