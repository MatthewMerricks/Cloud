//
//  CLDevice.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using CloudApiPublic.Support;
using System.Resources;

namespace CloudApiPublic.Model
{
    public class CLDevice
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
