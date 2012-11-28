//
// User.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace SyncTestServer.Model
{
    public sealed class User : NotifiableObject<User>
    {
        private static int IdCounter = 0;
        private static readonly object IdCounterLocker= new object();
        public int Id
        {
            get
            {
                return _id;
            }
        }
        private int _id;

        /// <summary>
        /// Sets and gets the Username property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Username
        {
            get { return _username; }
            set
            {
                if (_username != value)
                {
                    _username = value;
                    NotifyPropertyChanged(parent => parent.Username);
                }
            }
        }
        private string _username = null;

        /// <summary>
        /// Sets and gets the Password property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Password
        {
            get { return _password; }
            set
            {
                if (_password != value)
                {
                    _password = value;
                    NotifyPropertyChanged(parent => parent.Password);
                }
            }
        }
        private string _password = null;

        public ObservableCollection<Device> Devices
        {
            get
            {
                return _devices;
            }
        }
        private readonly ObservableCollection<Device> _devices = new ObservableCollection<Device>();

        public User(Nullable<int> Id = null)
        {
            if (Id == null)
            {
                lock (IdCounterLocker)
                {
                    IdCounter++;
                    this._id = IdCounter;
                }
            }
            else
            {
                lock (IdCounterLocker)
                {
                    if (IdCounter >= ((int)Id))
                    {
                        throw new ArgumentException("IdCounter is already at least as high as the new Id to set");
                    }
                    IdCounter = this._id = ((int)Id);
                }
            }
        }
    }
}