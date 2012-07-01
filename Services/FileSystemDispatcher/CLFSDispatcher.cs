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
using CloudApiPrivate.Model.Settings;
using System.IO;

namespace win_client.Services.FileSystemDispatcher
{
    public sealed class CLFSDispatcher
    {
        static readonly CLFSDispatcher _instance = new CLFSDispatcher();
        private static Boolean _isLoaded = false;
        private static CLTrace _trace;
        private static DispatchQueueGeneric _com_cloud_fsd_queue = null;
        public static DispatchQueueGeneric Get_cloud_FSD_queue()
        {
            //if (com_cloud_fsd_queue == nil)
            //{
            //    com_cloud_fsd_queue = dispatch_queue_create("com.cloud.filesystem.dispatcher.processing", NULL);
            //}
            //return com_cloud_fsd_queue;
            //&&&&

            if (_com_cloud_fsd_queue == null)
            {
                _com_cloud_fsd_queue = Dispatch.Queue_Create();
            }
            return _com_cloud_fsd_queue;
        }


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

        //TODO: Not used?  Thankfully.
        //- (BOOL)createFileWithData:(NSData *)data atPath:(NSString *)path error:(NSError * __strong *)error
        //{
        //    __block BOOL rc = NO;
        //    __block NSError *err = nil;
    
        //    path = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
       
        //    dispatch_sync(get_cloud_FSD_queue(), ^(void) {
        //        rc = [[NSFileManager defaultManager] createFileAtPath:path contents:data attributes:nil];
        //    });
        //        if (err != nil) {
        //        error = &err;
        //        NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
        //    }

        //    return rc;
        //}


        //- (BOOL)createDirectoryAtPath:(NSString *)path error:(NSError* __strong *)error
        bool CreateDirectoryAtPath_error(string path, CLError error)
        {
            // __block BOOL rc = NO;
            // __block NSError *err = nil;
            // path = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            //     if (![[NSFileManager defaultManager] fileExistsAtPath:path]) {
            //         rc = [[NSFileManager defaultManager] createDirectoryAtPath:path withIntermediateDirectories:YES attributes:nil error:&err];
            //     } else {
            //         rc = YES;
            //     }
            // });
            // if (err != nil) {
            //     error = &err;
            //     NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
            // }
            // return rc;
            //&&&&

            // __block BOOL rc = NO;
            // __block NSError *err = nil;
            bool rc = false;
            CLError err = null;

            // path = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
            string localPath = Settings.Instance.CloudFolderPath + path;

            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            Dispatch.Sync(Get_cloud_FSD_queue(), (Action obj) =>
            {
                // if (![[NSFileManager defaultManager] fileExistsAtPath:path]) {
                if (!File.Exists(localPath))
                {
                    // rc = [[NSFileManager defaultManager] createDirectoryAtPath:path withIntermediateDirectories:YES attributes:nil error:&err];
                    try
                    {
                        Directory.CreateDirectory(localPath);
                        rc = true;
                    }
                    catch (Exception e)
                    {
                        err = e;
                    }
                }
                else
                {
                    // rc = YES;
                    rc = true;
                }
            }, null);

            // if (err != nil) {
            if (err != null)
            {
                //  error = &err;
                error = err;

                //  NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
                _trace.writeToLog(1, "CLFSDispatcher: CreateDirectoryAtPath_error: ERROR: {0}, code: {1}.", err.errorDescription, err.errorCode);
            }

            // return rc;
            return rc;
        }


        //- (BOOL)copyItemAtFileSystemPath:(NSString *)path toCloudPath:(NSString *)newPath
        bool CopyItemAtFileSystemPath_toCloudPath(string path, string newPath)
        {
            //__block BOOL rc = NO;
            //__block NSError *err = nil;  
            //newPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:newPath];
            //dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            //    rc = [[NSFileManager defaultManager] copyItemAtPath:path toPath:newPath error:&err];
            //});
            //if (err != nil) {
            //    NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
            //}
            //return rc;
            //&&&&

            // __block BOOL rc = NO;
            // __block NSError *err = nil;
            bool rc = false;
            CLError err = null;
    
            // newPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:newPath];
            string localNewPath = Settings.Instance.CloudFolderPath + newPath;
    
            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            Dispatch.Sync(Get_cloud_FSD_queue(), (Action obj) =>
            {
                // rc = [[NSFileManager defaultManager] copyItemAtPath:path toPath:newPath error:&err];
                try
                {
                    File.Copy(path, localNewPath);
                    rc = true;
                }
                catch (Exception e)
                {
                    err = e;
                }
            }, null);
    
            // if (err != nil) {
            if (err != null)
            {
                // NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
                _trace.writeToLog(1, "CLFSDispatcher: CopyItemAtFileSystemPath_toCloudPath: ERROR: {0}, code: {1}.", err.errorDescription, err.errorCode);
            }
    
            // return rc;
            return rc;
        }

        //- (BOOL)copyExternalItemAtFileSystemPath:(NSString *)path toCloudPath:(NSString *)newPath
        //{
        //    __block BOOL rc = YES;
    
        //    // copy the file using cp command so our FSM can pick it up.

        //    dispatch_sync(get_cloud_FSD_queue(), ^(void) {
        
        //        NSString *destPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:newPath];
        //        NSArray *shArgs = @[@"-R", path, destPath];
        //        [NSTask launchedTaskWithLaunchPath:@"/bin/cp" arguments:shArgs];
        //    });
    
        //    // todo, check return code from NStask operation, and handle any errors.

        //    return rc;
        //}

        //- (BOOL)moveItemAtPath:(NSString *)path to:(NSString *)newPath error:( NSError* __strong *)error
        //{
        //    __block BOOL rc = NO;
        //    __block NSError *err = nil;

        //    path = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
        //    newPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:newPath];
    
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
        //}

        //- (BOOL)deleteItemAtPath:(NSString *)path error:( NSError* __strong *)error
        //{
        //    __block BOOL rc = NO;
        //    __block NSError *err = nil;

        //    path = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
    
        //    dispatch_sync(get_cloud_FSD_queue(), ^(void) {

        //        if ([[NSFileManager defaultManager] fileExistsAtPath:path]) {
        //            rc = [[NSFileManager defaultManager] removeItemAtPath:path error:&err];
        //        } else {
        //            rc = YES;
        //        }
        //    });
    
        //    if (err != nil) {
        //        error = &err;
        //        NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
        //    }
    
        //    return rc;
        //}

        //- (BOOL)renameItemAtPath:(NSString *)path to:(NSString *)newName error:( NSError* __strong *)error
        //{
        //    __block BOOL rc = NO;
        //    __block NSError *err = nil;

        //    path = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
        //    newName = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:newName];

        //    dispatch_sync(get_cloud_FSD_queue(), ^(void) {
        
        //        if ([[NSFileManager defaultManager] fileExistsAtPath:path]) {
        //            rc = [[NSFileManager defaultManager] moveItemAtPath:path toPath:newName error:&err];
        //        } else {
        //            rc = YES;
        //        }
        //    });
        
        //    if (err != nil) {
        //        error = &err;
        //        NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
        //    }
    
        //    return rc;
        //}

        //- (BOOL)updateAttributesUsingMetadata:(CLMetadata *)metadata forItemAtPath:(NSString *)path error:(NSError *__strong *)error
        //{
        //    __block BOOL rc = NO;
        //    __block NSError *err = nil;

        //    // This is the proper format if and when we decide to add perrmision attributes.
        //    //    [attributes setValue:[NSNumber numberWithShort:0777] 
        //    //                  forKey:NSFilePosixPermissions]; // chmod permissions 777
    
        //    NSDate *createDate = [NSDate dateFromISO8601String:metadata.createDate];
        //    NSDate *modifiedDate = [NSDate dateFromISO8601String:metadata.modifiedDate];
    
        //    if ([metadata.customAttributes isKindOfClass:[NSString class]]) {
        
        //        NSDictionary *xtAttributes = [CLExtendedAttributes unarchiveAndDecodeExtendedAttributesBase64String:metadata.customAttributes];
        //        [xtAttributes enumerateKeysAndObjectsUsingBlock:^(id key, id obj, BOOL *stop) {
            
        //            BOOL addedXtraAttributes;
        //            if ([obj isKindOfClass:[NSString class]]) {
        //               addedXtraAttributes = [CLExtendedAttributes addExtendedAttributeString:obj withAttributeName:key toItemAtPath:path];
        //            }else {
        //               addedXtraAttributes = [CLExtendedAttributes addExtendedAttributeData:obj withAttributeName:key toItemAtPath:path];
        //            }
            
        //            if (addedXtraAttributes == NO) {
        //                NSLog(@"%s - There was a problem adding extra attributes.", __FUNCTION__);
        //            }
            
        //        }];

        //    }
    
        //    NSDictionary *attributes = [NSDictionary dictionaryWithObjectsAndKeys:
        //                                 createDate ,NSFileCreationDate,
        //                                 modifiedDate, NSFileModificationDate, nil];

        //    dispatch_sync(get_cloud_FSD_queue(), ^(void) {
    
        //    if ([[NSFileManager defaultManager] fileExistsAtPath:path]) {
        //        rc = [[NSFileManager defaultManager] setAttributes:attributes ofItemAtPath:path error:&err];
        //    } else {
        //        rc = NO;
        //    }
    
        //    });

        //    if (err != nil) {
        //        error = &err;
        //        NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
        //    }

        //    return rc;
        //}


        //- (BOOL)createSymbLinkAtPath:(NSString *)path withTarget:(NSString *)target
        //{
        //    __block BOOL rc = NO;
        //    __block NSError *err = nil;
    
        //    path = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
    
        //    dispatch_sync(get_cloud_FSD_queue(), ^(void) {
        //        rc = [[NSFileManager defaultManager] createSymbolicLinkAtPath:path withDestinationPath:target error:&err];
        //    });
        //    if (err != nil) {
        //        NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
        //    }
    
        //    return rc;
        //}

    }
}
