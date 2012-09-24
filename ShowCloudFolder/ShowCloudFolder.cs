//
//  ShowCloudFolder.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPrivate.Model.Settings;
using CloudApiPrivate.Common;

namespace ShowCloudFolder
{
    class ShowCloudFolder
    {
        static void Main(string[] args)
        {
            // Get the Cloud folder location.
            string cloudFolderPath = Settings.Instance.CloudFolderPath;

            // Start Explorer to show the Cloud folder
            if (!String.IsNullOrWhiteSpace(cloudFolderPath))
            {
                // Launch the process
                CLShortcuts.LaunchExplorerToFolder(Settings.Instance.CloudFolderPath);
            }
        }
    }
}
