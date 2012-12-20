//
//  UploadedMessage.cs
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
    public sealed class UploadedMessage : EventMessage
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
                    " uploaded";
            }
        }
        #endregion

        #region private fields
        private uint CurrentCount;
        #endregion

        public UploadedMessage(uint initialCount)
        {
            this.CurrentCount = initialCount;
        }

        public void IncrementCount(uint incrementAmount = 1)
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
                    .Add(UploadedMessage.DefaultOpaqueTime))
                .Add(UploadedMessage.DefaultFadeOutTime);
        }
    }
}