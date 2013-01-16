using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Static;

namespace CloudSdkSyncSample.EventMessageReceiver
{
    /// <summary>
    /// <see cref="EventMessage"/> for current number of files in the process of uploading.
    /// </summary>
    public sealed class UploadingMessage : EventMessage
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
                    " uploading";
            }
        }
        #endregion

        #region private fields
        private uint CurrentCount;
        #endregion

        internal UploadingMessage(uint initialCount)
        {
            this.CurrentCount = initialCount;
        }

        internal bool SetCount(uint newCount)
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
                    fadeInToAdd = UploadingMessage.DefaultFadeInTime
                        .Subtract(base.CompleteFadeOut.Subtract(currentTime));
                }
                else
                {
                    fadeInToAdd = TimeSpan.Zero;
                }

                if (newCount == 0)
                {
                    base.CompleteFadeOut = (base.StartFadeOut = base.FadeInCompletion = currentTime.Add(fadeInToAdd))
                        .Add(UploadingMessage.DefaultFadeOutTime);
                }
                else
                {
                    base.CompleteFadeOut = (base.StartFadeOut = (base.FadeInCompletion = currentTime.Add(fadeInToAdd))
                            .Add(UploadingMessage.DefaultOpaqueTime))
                        .Add(UploadingMessage.DefaultFadeOutTime);
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