//
// SingleActionSetter.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncTestServer.Model
{
    public struct SingleActionSetter
    {
        public SingleActionSetter(bool startSet)
        {
            this.ActionSet = startSet;
            this.ActionSetLocker = new object();
            this._isValid = true;
        }

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        private bool ActionSet;
        private readonly object ActionSetLocker;
        public bool TrySet<T>(Action<T> setAction, T actionState)
        {
            if (!_isValid)
            {
                throw new ArgumentException("Cannot retrieve property values on an invalid SingleActionSetter");
            }

            lock (ActionSetLocker)
            {
                if (ActionSet)
                {
                    return false;
                }
                else
                {
                    if (setAction == null)
                    {
                        throw new NullReferenceException("setAction cannot be null");
                    }
                    setAction(actionState);

                    ActionSet = true;
                    return true;
                }
            }
        }
    }
}