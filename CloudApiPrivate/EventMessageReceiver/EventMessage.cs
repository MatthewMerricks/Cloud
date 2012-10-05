//
//  EventMessage.cs
//  Cloud Windows
//
//  Created by DavidBruck
//  Copyright (c) Cloud.com. All rights reserved.
//

using CloudApiPrivate.Model;
using CloudApiPrivate.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPrivate.EventMessageReceiver
{
    public abstract class EventMessage : NotifiableObject<EventMessage>
    {
        #region constants
        protected static readonly TimeSpan DefaultFadeInTime = TimeSpan.FromSeconds(20);
        protected static readonly TimeSpan DefaultOpaqueTime = TimeSpan.FromSeconds(40);
        protected static readonly TimeSpan DefaultFadeInPlusOpaqueTime = DefaultFadeInTime.Add(DefaultOpaqueTime);
        protected static readonly TimeSpan DefaultFadeOutTime = TimeSpan.FromSeconds(20);
        protected static readonly TimeSpan DefaultFullDisplayTimeIncludingFadings = DefaultFadeInPlusOpaqueTime.Add(DefaultFadeOutTime);
        #endregion

        public abstract EventMessageImage Image { get; }
        public abstract string Message { get; }

        public virtual DateTime FadeInCompletion
        {
            get
            {
                return _fadeInCompletion;
            }
            protected set
            {
                if (value != _fadeInCompletion)
                {
                    _fadeInCompletion = value;
                    NotifyPropertyChanged(parent => parent.FadeInCompletion);
                }
            }
        }
        private DateTime _fadeInCompletion;

        public virtual DateTime StartFadeOut
        {
            get
            {
                return _startFadeOut;
            }
            protected set
            {
                if (value != _startFadeOut)
                {
                    _startFadeOut = value;
                    NotifyPropertyChanged(parent => parent.StartFadeOut);
                }
            }
        }
        private DateTime _startFadeOut;

        public virtual DateTime CompleteFadeOut
        {
            get
            {
                return _completeFadeOut;
            }
            protected set
            {
                if (value != _completeFadeOut)
                {
                    _completeFadeOut = value;
                    NotifyPropertyChanged(parent => parent.CompleteFadeOut);
                }
            }
        }
        private DateTime _completeFadeOut;

        protected EventMessage(Nullable<DateTime> fadeInCompletion = null,
            Nullable<DateTime> startFadeOut = null,
            Nullable<DateTime> completeFadeOut = null)
        {
            SetFadingTimes(fadeInCompletion,
                startFadeOut,
                completeFadeOut);
        }

        protected void SetFadingTimes(Nullable<DateTime> fadeInCompletion = null,
            Nullable<DateTime> startFadeOut = null,
            Nullable<DateTime> completeFadeOut = null)
        {
            Nullable<DateTime> utcTime = null;
            Func<DateTime> GetAndSetUtcTime = () => (DateTime)(utcTime = utcTime ?? DateTime.UtcNow);

            this._fadeInCompletion = fadeInCompletion ?? GetAndSetUtcTime().Add(DefaultFadeInTime);
            this._startFadeOut = startFadeOut ?? GetAndSetUtcTime().Add(DefaultFadeInPlusOpaqueTime);
            this._completeFadeOut = completeFadeOut ?? GetAndSetUtcTime().Add(DefaultFullDisplayTimeIncludingFadings);
        }
    }
}