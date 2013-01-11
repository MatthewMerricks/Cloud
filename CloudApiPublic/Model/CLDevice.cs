//
//  CLDevice.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using CloudApiPublic.Support;
using System.Resources;
using CloudApiPublic.Resources;
using CloudApiPublic.Static;

namespace CloudApiPublic.Model
{
    internal class CLDevice
    {
        public string DeviceName { get; set; }
        public string Udid { get; set; }
        public string FriendlyName
        {
            get { return DeviceName; }
        }
        
        public CLDevice()
        {
            throw new NotSupportedException("Default constructor not supported.");
        }

        public CLDevice(string name)
        {
            DeviceName = name;
            Udid = GenerateDeviceUDID();
        }

        public string OSType()
        {
            string osType = OSInfo.Name + " " + OSInfo.Edition + " " + OSInfo.Bits.ToString() + " " +  Resources.Resources.OsBits;
            return osType;
        }

        public string OSPlatform()
        {
            return CLDevices.GetComputerType().ToString();
        }

        public string OSVersion()
        {
            string osVersion = OSInfo.VersionString;
            return (osVersion);
        }


        string GenerateDeviceUDID()
        {
            return Guid.NewGuid().ToString("D").ToUpperInvariant();
        }
    }
}
