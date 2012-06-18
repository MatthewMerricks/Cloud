//
//  CLFSDispatcher.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Model;
using CloudApiPublic.Support;

namespace win_client.Services.FileSystemDispatcher
{
    public sealed class CLFSDispatcher
    {
        static readonly CLFSDispatcher _instance = new CLFSDispatcher();
        private static Boolean _isLoaded = false;
        private static CLTrace _trace;

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLFSDispatcher Instance
        {
            get
            {
                if (!_isLoaded)
                {
                    _isLoaded = true;

                    // Initialize at first Instance access here
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLFSDispatcher()
        {
            // Initialize members, etc. here (at static initialization time).
            _trace = CLTrace.Instance;
        }

        /// <summary>
        /// Start the file system monitoring service.
        /// </summary>
        public void BeginFileSystemMonitoring()
        {

        }

        /// <summary>
        /// Stop the file system monitoring service.
        /// </summary>
        public void EndFileSystemMonitoring()
        {

        }


        //- (BOOL)moveItemAtPath:(NSString *)path to:(NSString *)newPath error:( NSError* __strong *)error
        public bool MoveItemAtPath(string fromPath, string toPath, out CLError error)
        
        {
            //TODO: Implement this
            //    __block BOOL rc = NO;
            //    __block NSError *err = nil;

            //    path = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingString:path];
            //    newPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingString:newPath];
    
            //    dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            //        if ([[NSFileManager defaultManager] fileExistsAtPath:path]) {
            //            rc = [[NSFileManager defaultManager] moveItemAtPath:path toPath:newPath error:&err];
            //        } else {
            //            rc = YES;
            //        }
            //    });

            //    if (err != nil) {
            //        error = &err;
            //        NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
            //    }

            //    return rc;
            error = null;
            return true;
        }
    }
}
