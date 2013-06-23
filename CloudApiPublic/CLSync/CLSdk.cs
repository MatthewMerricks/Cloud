//  CLSdk.cs
//  Cloud Windows
//
//  Created by BobS{.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.Static;

namespace Cloud
{
    /// <summary>
    /// Represents the Cloud SDK assembly.
    /// </summary>
    public static class CLSdk
    {
        /// <summary>
        /// Returns the version of the Cloud SDK.  The version is four numbers: Major version, Minor version, Build number, Revision.
        /// e.g.: "1.0.0.0".
        /// </summary>
        public static string SdkVersion
        {
            get
            {
                return OSVersionInfo.GetSdkVersion(); 
            }
        }

    }
}
