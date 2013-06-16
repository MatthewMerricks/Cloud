using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.Static;

namespace SampleLiveSync.EventMessageReceiver
{
    public sealed class InternetConnectivityMessage : EventMessage<InternetConnectivityMessage>
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
                return "Network status is: " + (_internetConnected ? "Connected" : "Disconnected");
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
                    .Add(InternetConnectivityMessage.DefaultOpaqueTime))
                .Add(InternetConnectivityMessage.DefaultFadeOutTime);
        }
    }
}