//
//  CLPrivateDefinitions.cs
//  Cloud SDK Windows 
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

namespace CloudApiPrivate.Model
{
    public class CLPrivateDefinitions
    {
        // Client version
        public const string CLClientVersion = "W01";
        public const string CLClientVersionHeaderName = "X-Cld-Client-Version";

        // OS constants
        public const string ShortcutExtension = "lnk";

        // Cloud directory constants
        public const string CloudAppName = "Cloud";
        public const string CloudDirectoryName = "Cloud";
        public const string CloudFolderShortcutFilename = "Show Cloud folder";
        public const string CloudIndexDatabaseLocation = "\\Cloud\\IndexDB.sdf";
        public const string CloudFolderInProgramFiles = "\\Cloud.com\\Cloud";
        public const string ShowCloudFolderProgramFilenameOnly = "ShowCloudFolder";
        public const string CloudFolderInProgramFilesCommon = "\\Cloud.com\\Cloud";


        // General constants
        public const int MaxPathCharsIncludingTermination = 260;

    }
}