//
//  EventMessage.cs
//  Cloud Windows
//
//  Created by DavidBruck
//  Copyright (c) Cloud.com. All rights reserved.
//

using CloudApiPublic.Model;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.EventMessageReceiver
{
    public abstract class EventMessage : NotifiableObject<EventMessage>
    {
        #region constants
        protected static readonly TimeSpan DefaultFadeInTime = TimeSpan.FromSeconds(1);
        protected static readonly TimeSpan DefaultOpaqueTime = TimeSpan.FromSeconds(4);
        protected static readonly TimeSpan DefaultFadeInPlusOpaqueTime = DefaultFadeInTime.Add(DefaultOpaqueTime);
        protected static readonly TimeSpan DefaultFadeOutTime = TimeSpan.FromSeconds(1);
        protected static readonly TimeSpan DefaultFullDisplayTimeIncludingFadings = DefaultFadeInPlusOpaqueTime.Add(DefaultFadeOutTime);
        #endregion

        #region overridable properties
        public abstract EventMessageImage Image { get; }
        public abstract string Message { get; }

        public virtual bool ShouldRemove
        {
            get
            {
                return DateTime.UtcNow.CompareTo(CompleteFadeOut) >= 0;
            }
        }
        #endregion

        #region remaining public properties
        public virtual DateTime FadeInCompletion
        {
            get
            {
                return _fadeInCompletion;
            }
            set
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
            set
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
            set
            {
                if (value != _completeFadeOut)
                {
                    _completeFadeOut = value;
                    NotifyPropertyChanged(parent => parent.CompleteFadeOut);
                }
            }
        }
        private DateTime _completeFadeOut;
        #endregion

        #region constructors
        protected EventMessage(Nullable<DateTime> fadeInCompletion = null,
            Nullable<DateTime> startFadeOut = null,
            Nullable<DateTime> completeFadeOut = null)
        {
            SetFadingTimesOnConstruction(fadeInCompletion,
                startFadeOut,
                completeFadeOut);
        }

        protected EventMessage(DateTime baseTime,
            Nullable<TimeSpan> fadeInOffset = null,
            Nullable<TimeSpan> startFadeOutOffset = null,
            Nullable<TimeSpan> completeFadeOutOffset = null)
        {
            SetFadingTimesOnConstruction((fadeInOffset == null
                    ? (Nullable<DateTime>)null
                    : baseTime.Add((TimeSpan)fadeInOffset)),
                (startFadeOutOffset == null
                    ? (Nullable<DateTime>)null
                    : baseTime.Add((TimeSpan)startFadeOutOffset)),
                (completeFadeOutOffset == null
                    ? (Nullable<DateTime>)null
                    : baseTime.Add((TimeSpan)completeFadeOutOffset)),
                () => baseTime);
        }
        #endregion

        #region protected methods
        protected void SetFadingTimesOnConstruction(Nullable<DateTime> fadeInCompletion = null,
            Nullable<DateTime> startFadeOut = null,
            Nullable<DateTime> completeFadeOut = null,
            Func<DateTime> GetAndSetUtcTime = null)
        {
            if (GetAndSetUtcTime == null)
            {
                Nullable<DateTime> utcTime = null;
                GetAndSetUtcTime = () => (DateTime)(utcTime = utcTime ?? DateTime.UtcNow);
            }

            this._fadeInCompletion = fadeInCompletion ?? GetAndSetUtcTime().Add(DefaultFadeInTime);
            this._startFadeOut = startFadeOut ?? GetAndSetUtcTime().Add(DefaultFadeInPlusOpaqueTime);
            this._completeFadeOut = completeFadeOut ?? GetAndSetUtcTime().Add(DefaultFullDisplayTimeIncludingFadings);
        }
        #endregion
    }
}