//
//  CLSptConstants.cs
//  Cloud SDK Windows 
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

namespace CloudApi.Support
{
    public class CLSptConstants
    {
        public static string kResourcesName = "CloudApiClassLibrary.Resources.Resources";

        // Registration
        public const string CLRegistrationCreateRequestURLString  = "http://api.cloudburrito.com/user/create.json";
        public const string CLRegistrationCreateRequestBodyString = "user[first_name]={0}&user[last_name]={1}&user[email]={2}&user[password]={3}&device[friendly_name]={4}&device[device_uuid]={5}&device[os_type]={6}&device[os_version]={7}&device[app_version]={8}";

    }
}