//
//  CLDevice.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using Cloud.Support;
using System.Resources;
using Cloud.Static;
using Cloud.Static;

namespace CloudApiPrivate.Model
{
    public sealed class CLDevice
    {
        public string DeviceName { get; set; }
        public string Udid { get; set; }
        public string FriendlyName
        {
            get { return DeviceName; }
        }

        public CLDevice(string name)
        {
            DeviceName = name;
            Udid = GenerateDeviceUDID();
        }

        public string OSType()
        {
            string osType = OSVersionInfo.Name + " " + OSVersionInfo.Edition + " " + OSVersionInfo.OSBits.ToString() + " " /*CloudAppBuild:+ Resources.Resources.OsBits*/;
            return osType;
        }

        public string OSPlatform()
        {
            return CLDevices.GetComputerType().ToString();
        }

        public string OSVersion()
        {
            string osVersion = OSVersionInfo.VersionString;
            return (osVersion);
        }


        string GenerateDeviceUDID()
        {
            return Guid.NewGuid().ToString("D").ToUpperInvariant();
        }
    }
}
