using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.Static;

namespace SampleLiveSync.EventMessageReceiver
{
    public sealed class InternetConnectivityMessage : EventMessage
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
                return "todo: Network status is...";
            }
        }
        #endregion

        #region private fields
        private bool _internetConnected;
        #endregion

        internal InternetConnectivityMessage(bool internetConnected)
        {
            _internetConnected = internetConnected;
        }

        internal void SetInternetConnected(bool internetConnected)
        {
            _internetConnected = internetConnected;
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