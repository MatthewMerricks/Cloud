//
// Device.cs
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
    public sealed class Device : NotifiableObject<Device>
    {
        /// <summary>
        /// Sets and gets the Id property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Guid Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyPropertyChanged(parent => parent.Id);
                }
            }
        }
        private Guid _id = Guid.Empty;

        /// <summary>
        /// Sets and gets the FriendlyName property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string FriendlyName
        {
            get { return _friendlyName; }
            set
            {
                if (_friendlyName != value)
                {
                    _friendlyName = value;
                    NotifyPropertyChanged(parent => parent.FriendlyName);
                }
            }
        }
        private string _friendlyName = null;

        /// <summary>
        /// Sets and gets the AuthorizationKey property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string AuthorizationKey
        {
            get { return _authorizationKey; }
            set
            {
                if (_authorizationKey != value)
                {
                    _authorizationKey = value;
                    NotifyPropertyChanged(parent => parent.AuthorizationKey);
                }
            }
        }
        private string _authorizationKey = null;
    }
}