//
//  CLApiDevice.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using CloudApi.Support;
using System.Resources;

namespace CloudApi
{
    public class CLApiDevice
    {
        public string DeviceName { get; set; }
        public string Udid { get; set; }
        public string FriendlyName
        {
            get { return DeviceName; }
        }
        
        public CLApiDevice()
        {
            throw new NotSupportedException("Default constructor not supported.");
        }

        public CLApiDevice(string name)
        {
            DeviceName = name;
            Udid = GenerateDeviceUDID();
        }

        public string OSType()
        {
            string osType = OSInfo.Name + " " + OSInfo.Edition + " " + OSInfo.Bits.ToString() + " " + CLSptResourceManager.Instance.ResMgr.GetString("OsBits");
            return osType;
        }

        public string OSVersion()
        {
            string osVersion = OSInfo.VersionString;
            return (osVersion);
        }


        string GenerateDeviceUDID()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
