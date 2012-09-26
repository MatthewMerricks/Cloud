//
//  CLDevices.cs
//  Cloud Windows
//
//  Created by BobS.
//  Changes Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using CloudApiPublic.Model;
using CloudApiPublic.Support;

namespace CloudApiPublic.Static
{
    public static class CLDevices
    {
        private static CLTrace _trace = CLTrace.Instance;

        private enum EnumTouchpadTypes
        {
            TouchpadTypeTrackPoint = 5,
            TouchpadTypeGlidePoint = 6,
        }

        public enum EnumComputerTypes
        {
            CLDeviceModelWindowsPhone = 201,
            CLDeviceModelWindowsDesktop = 202,
            CLDeviceModelWindowsNotebook = 203,
        }

        /// <summary>
        /// This function returns the type of the computer system the current executable is running on.
        /// TODO: Revise this logic when we have Metro support.
        /// The computer type is a notebook if it has a touchpad.  Otherwise it is a desktop.
        /// </summary>
        /// <returns></returns>
        public static int GetComputerType()
        {
            int returnType = (int) EnumComputerTypes.CLDeviceModelWindowsDesktop;     // assume it is a desktop
            try 
	        {	        
                // Is a touchpad installed?  Get a list of all of the installed pointing devices.
                ObjectQuery wql = new ObjectQuery("SELECT * FROM Win32_PointingDevice"); 
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql); 
                ManagementObjectCollection devices = searcher.Get();

                foreach (var device in devices)
                {
                    _trace.writeToLog(1, string.Format("CLDevices: GetComputerType: Device pointing type: <{0}>.", device["PointingType"]));
                    string touchpadType = device["PointingType"].ToString();
                    if (touchpadType == ((int)EnumTouchpadTypes.TouchpadTypeGlidePoint).ToString() || touchpadType == ((int)EnumTouchpadTypes.TouchpadTypeTrackPoint).ToString())
                    {
                        returnType = (int)EnumComputerTypes.CLDeviceModelWindowsNotebook;
                    }
                }
	        }
	        catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, string.Format("CLDevices: GetComputerType: ERROR: Exception: Msg: <{0}>.  Code: {1}.", error.errorDescription, error.errorCode));
	        }

            return returnType;
        }
    }
}
