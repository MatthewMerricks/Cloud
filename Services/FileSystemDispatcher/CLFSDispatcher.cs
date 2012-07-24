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
using System.Diagnostics;
using IWshRuntimeLibrary;
using System.Windows.Forms;

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
        public bool CreateDirectoryAtPath_error(string path, out CLError error)
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
            string localPath = Settings.Instance.CloudFolderPath + path.Replace('\\', '/');

            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            Dispatch.Sync(Get_cloud_FSD_queue(), (object obj, object userState) =>
            {
                // if (![[NSFileManager defaultManager] fileExistsAtPath:path]) {
                if (!System.IO.File.Exists(localPath))
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
            }, null, null);

            // if (err != nil) {
            if (err != null)
            {
                //  error = &err;
                // Note: set below.

                //  NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
                _trace.writeToLog(1, "CLFSDispatcher: CreateDirectoryAtPath_error: ERROR: {0}, code: {1}.", err.errorDescription, err.errorCode);
            }

            // return rc;
            error = err;
            return rc;
        }


        //- (BOOL)copyItemAtFileSystemPath:(NSString *)path toCloudPath:(NSString *)newPath
        public bool CopyItemAtFileSystemPath_toCloudPath(string path, string newPath)
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
            string localNewPath = Settings.Instance.CloudFolderPath + newPath.Replace('/', '\\');
    
            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            Dispatch.Sync(Get_cloud_FSD_queue(), (object obj, object userState) =>
            {
                // rc = [[NSFileManager defaultManager] copyItemAtPath:path toPath:newPath error:&err];
                try
                {
                    System.IO.File.Copy(path.Replace('/', '\\'), localNewPath);
                    rc = true;
                }
                catch (Exception e)
                {
                    err = e;
                }
            }, null, null);
    
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
        public bool CopyExternalItemAtFileSystemPath_toCloudPath(string path, string newPath)
        {
            // __block BOOL rc = YES;
    
            // Copy the file using cp command so our FSM can pick it up.

            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
        
            //     NSString *destPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:newPath];
            //     NSArray *shArgs = @[@"-R", path, destPath];
            //     [NSTask launchedTaskWithLaunchPath:@"/bin/cp" arguments:shArgs];
            // });
    
            // Todo, check return code from NStask operation, and handle any errors.
            // return rc;
            //&&&&

            // Fix up the slashes
            path = path.Replace('/', '\\');
            newPath = newPath.Replace('/', '\\');

            // __block BOOL rc = YES;
            bool rc = true;

            // Copy the file using cp command so our FSM can pick it up.
            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            CLError error = null;
            Dispatch.Sync(Get_cloud_FSD_queue(), (object obj, object userState) =>
            {
                // NSString *destPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:newPath];
                // NSArray *shArgs = @[@"-R", path, destPath];
                // [NSTask launchedTaskWithLaunchPath:@"/bin/cp" arguments:shArgs];
                try
                {
                    string destPath = Settings.Instance.CloudFolderPath + newPath;

                    if (System.IO.File.Exists(path))
                    {
                        FileAttributes attr = System.IO.File.GetAttributes(path);
                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        {

                            if (System.IO.File.Exists(destPath))
                            {
                                attr = System.IO.File.GetAttributes(destPath);
                                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                                {
                                    // Issue the command "Copy /Y path\ destPath\"
                                    int launchRc = LaunchCommandLineApp("Copy", "/Y " + path + "\\ " + destPath + "\\", out error);
                                    if (launchRc != 0)
                                    {
                                        error += new Exception("Error copying directory (" + path + ") to directory (" + destPath + "), code: " + launchRc.ToString() + ".");
                                    }
                                }
                                else
                                {
                                    // Copying a directory to a file
                                    error += new Exception("Copying a directory (" + path + ") to a file (" + destPath + ").");
                                }
                            }
                            else
                            {
                                // Copying a directory, and the target directory doesn't exist.
                                Directory.CreateDirectory(destPath);
                                int launchRc = LaunchCommandLineApp("Copy", "/Y " + path + "\\ " + destPath + "\\", out error);
                                if (launchRc != 0)
                                {
                                    error += new Exception("Error copying directory (" + path + ") to directory (" + destPath + "), code: " + launchRc.ToString() + ". (2)");
                                }
                            }
                        }
                        else
                        {
                            // Copying a file to a file.
                            int launchRc = LaunchCommandLineApp("Copy", "/Y " + path + " " + destPath + "", out error);
                            if (launchRc != 0)
                            {
                                error += new Exception("Error copying file (" + path + ") to file (" + destPath + "), code: " + launchRc.ToString() + ".");
                            }
                        }
                    }
                    else
                    {
                        // Source file doesn't exist
                        error += new Exception("Source filee (" + path + ") does not exist.");
                    }
                }
                catch (Exception e)
                {
                    error += e;
                }

            }, null, null);

            if (error != null)
            {
                _trace.writeToLog(1, "CLFSDispatcher: CopyExternalItemAtFileSystemPath_toCloudPath: ERROR: {0}, code: {1}.", error.errorDescription, error.errorCode);
                rc = false;
            }

            //TODO: Check return code from NStask operation, and handle any errors.
            // return rc;
            return rc;
        }

        //- (BOOL)moveItemAtPath:(NSString *)path to:(NSString *)newPath error:( NSError* __strong *)error
        public bool MoveItemAtPath_to_error(string path, string newPath, out CLError error)
        {
            // __block BOOL rc = NO;
            // __block NSError *err = nil;
            // path = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
            // newPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:newPath];
            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            //     if ([[NSFileManager defaultManager] fileExistsAtPath:path]) {
            //         rc = [[NSFileManager defaultManager] moveItemAtPath:path toPath:newPath error:&err];
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

            // Fix up the slashes
            path = path.Replace('/', '\\');
            newPath = newPath.Replace('/', '\\');

            // __block BOOL rc = NO;
            // __block NSError *err = nil;
            bool rc = false;
            CLError err = null;

            // path = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
            // newPath = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:newPath];
            path = Settings.Instance.CloudFolderPath + path;
            newPath = Settings.Instance.CloudFolderPath + newPath;

            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            Dispatch.Sync(Get_cloud_FSD_queue(), (object obj, object userState) =>
            {
                try
                {
                    // if ([[NSFileManager defaultManager] fileExistsAtPath:path]) {
                    if (System.IO.File.Exists(path))
                    {
                        // rc = [[NSFileManager defaultManager] moveItemAtPath:path toPath:newPath error:&err];
                        if (System.IO.File.Exists(newPath))
                        {
                            // The target item already exists.
                            err += new Exception("Error moving item (" + path + ") to (" + newPath + "), the destination item already exists.");
                        }
                        else
                        {
                            System.IO.File.Move(path, newPath);
                            rc = true;
                        }
                    }
                    else
                    {
                        // rc = YES;  // Looks wrong.

                        // The source item doesn't exist
                        err += new Exception("Error moving item (" + path + ") to (" + newPath + "), the source item does not exist.");
                    }
                }
                catch (Exception e)
                {
                    err += e;
                }
            }, null, null);

            // if (err != nil) {
            if (err != null)
            {
                // error = &err;
                // NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
                _trace.writeToLog(1, "CLFSDispatcher: MoveItemAtPath_to_error: ERROR: {0}, code: {1}.", err.errorDescription, err.errorCode);
            }

            // return rc;
            error = err;
            return rc;
        }

        //- (BOOL)deleteItemAtPath:(NSString *)path error:( NSError* __strong *)error
        public bool DeleteItemAtPath_error(string path, out CLError error)
        {
            // __block BOOL rc = NO;
            // __block NSError *err = nil;
            // path = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            //     if ([[NSFileManager defaultManager] fileExistsAtPath:path]) {
            //         rc = [[NSFileManager defaultManager] removeItemAtPath:path error:&err];
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


            // Fix up the slashes
            path = path.Replace('/', '\\');
            
            // __block BOOL rc = NO;
            // __block NSError *err = nil;
            bool rc = false;
            CLError err = null;
             
            // path = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
            path = Settings.Instance.CloudFolderPath + path;

            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            Dispatch.Sync(Get_cloud_FSD_queue(), (object obj, object userState) =>
            {
                try
                {
                    // if ([[NSFileManager defaultManager] fileExistsAtPath:path]) {
                    if (System.IO.File.Exists(path))
                    {
                        // rc = [[NSFileManager defaultManager] removeItemAtPath:path error:&err];
                        FileAttributes attr = System.IO.File.GetAttributes(path);
                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            Directory.Delete(path, true);
                        }
                        else
                        {
                            System.IO.File.Delete(path);
                        }
                        rc = true;
                    }
                    else
                    {
                        // rc = YES;
                        rc = true;
                    }
                }
                catch (Exception e)
                {
                    err += e;
                }
            }, null, null);

            // if (err != nil) {
            if (err != null)
            {
                // error = &err;
                // NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
                _trace.writeToLog(1, "CLFSDispatcher: DeleteItemAtPath_error: ERROR: {0}, code: {1}.", err.errorDescription, err.errorCode);
            }

            // return rc;
            error = err;
            return rc;
        }

        //TODO: Not used
        //- (BOOL)renameItemAtPath:(NSString *)path to:(NSString *)newName error:( NSError* __strong *)error
        //{
            // __block BOOL rc = NO;
            // __block NSError *err = nil;
            // path = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
            // newName = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:newName];
            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            //     if ([[NSFileManager defaultManager] fileExistsAtPath:path]) {
            //         rc = [[NSFileManager defaultManager] moveItemAtPath:path toPath:newName error:&err];
            //     } else {
            //         rc = YES;
            //     }
            // });
            // if (err != nil) {
            //     error = &err;
            //     NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
            // }
            // return rc;
        //}

        //- (BOOL)updateAttributesUsingMetadata:(CLMetadata *)metadata forItemAtPath:(NSString *)path error:(NSError *__strong *)error
        public bool UpdateAttributesUsingMetadata_forItemAtPath_error(CLMetadata metadata, string path, out CLError error)
        {
            // __block BOOL rc = NO;
            // __block NSError *err = nil;
            // This is the proper format if and when we decide to add perrmision attributes.
            //   [attributes setValue:[NSNumber numberWithShort:0777] 
            //                 forKey:NSFilePosixPermissions]; // chmod permissions 777
            // NSDate *createDate = [NSDate dateFromISO8601String:metadata.createDate];
            // NSDate *modifiedDate = [NSDate dateFromISO8601String:metadata.modifiedDate];
            // if ([metadata.customAttributes isKindOfClass:[NSString class]]) {
            //     NSDictionary *xtAttributes = [CLExtendedAttributes unarchiveAndDecodeExtendedAttributesBase64String:metadata.customAttributes];
            //     [xtAttributes enumerateKeysAndObjectsUsingBlock:^(id key, id obj, BOOL *stop) {
            //         BOOL addedXtraAttributes;
            //         if ([obj isKindOfClass:[NSString class]]) {
            //            addedXtraAttributes = [CLExtendedAttributes addExtendedAttributeString:obj withAttributeName:key toItemAtPath:path];
            //         }else {
            //            addedXtraAttributes = [CLExtendedAttributes addExtendedAttributeData:obj withAttributeName:key toItemAtPath:path];
            //         }
            //         if (addedXtraAttributes == NO) {
            //             NSLog(@"%s - There was a problem adding extra attributes.", __FUNCTION__);
            //         }
            //     }];
            // }

            // NSDictionary *attributes = [NSDictionary dictionaryWithObjectsAndKeys:
            //                              createDate ,NSFileCreationDate,
            //                              modifiedDate, NSFileModificationDate, nil];
            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            // if ([[NSFileManager defaultManager] fileExistsAtPath:path]) {
            //     rc = [[NSFileManager defaultManager] setAttributes:attributes ofItemAtPath:path error:&err];
            // } else {
            //     rc = NO;
            // }
            //});

            //if (err != nil) {
            //    error = &err;
            //    NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
            //}

            //return rc;
            //&&&&


            // Fix up the slashes
            path = path.Replace('/', '\\');

            // __block BOOL rc = NO;
            // __block NSError *err = nil;
            GenericHolder<bool> rc = new GenericHolder<bool>()
            {
                Value = false
            };
            CLError err = null;

            // This is the proper format if and when we decide to add perrmision attributes.
            //   [attributes setValue:[NSNumber numberWithShort:0777] 
            //                 forKey:NSFilePosixPermissions]; // chmod permissions 777
            // NSDate *createDate = [NSDate dateFromISO8601String:metadata.createDate];
            // NSDate *modifiedDate = [NSDate dateFromISO8601String:metadata.modifiedDate];
            DateTime createDate = DateTime.Parse(metadata.CreateDate, null, System.Globalization.DateTimeStyles.RoundtripKind);   // ISO 8601
            DateTime modifiedDate = DateTime.Parse(metadata.ModifiedDate, null, System.Globalization.DateTimeStyles.RoundtripKind);  // ISO 8601

            //TODO: Not implemented because Windows doesn't support non-dat
            // if ([metadata.customAttributes isKindOfClass:[NSString class]]) {
            //     NSDictionary *xtAttributes = [CLExtendedAttributes unarchiveAndDecodeExtendedAttributesBase64String:metadata.customAttributes];
            //     [xtAttributes enumerateKeysAndObjectsUsingBlock:^(id key, id obj, BOOL *stop) {
            //         BOOL addedXtraAttributes;
            //         if ([obj isKindOfClass:[NSString class]]) {
            //            addedXtraAttributes = [CLExtendedAttributes addExtendedAttributeString:obj withAttributeName:key toItemAtPath:path];
            //         }else {
            //            addedXtraAttributes = [CLExtendedAttributes addExtendedAttributeData:obj withAttributeName:key toItemAtPath:path];
            //         }
            //         if (addedXtraAttributes == NO) {
            //             NSLog(@"%s - There was a problem adding extra attributes.", __FUNCTION__);
            //         }
            //     }];
            // }

            // NSDictionary *attributes = [NSDictionary dictionaryWithObjectsAndKeys:
            //                              createDate ,NSFileCreationDate,
            //                              modifiedDate, NSFileModificationDate, nil];
            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            Dispatch.Sync(Get_cloud_FSD_queue(), (object obj, object userState) =>
            {
                try
                {
                    // if ([[NSFileManager defaultManager] fileExistsAtPath:path]) {
                    if (System.IO.File.Exists(path))
                    {
                        // rc = [[NSFileManager defaultManager] setAttributes:attributes ofItemAtPath:path error:&err];
                        FileAttributes attr = System.IO.File.GetAttributes(path);
                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            Directory.SetCreationTime(path, createDate);
                            Directory.SetLastWriteTime(path, modifiedDate);
                            Directory.SetLastAccessTime(path, modifiedDate);
                        }
                        else
                        {
                            System.IO.File.SetCreationTime(path, createDate);
                            System.IO.File.SetLastWriteTime(path, modifiedDate);
                            System.IO.File.SetLastAccessTime(path, modifiedDate);
                        }

                        GenericHolder<bool>.GenericSet(userState, true);
                    }
                    else
                    {
                        // rc = NO;  // This looks wrong
                        err = new Exception("Error setting file attributes for item (" + path + ").  The item does not exist.");
                    }
                }
                catch (Exception e)
                {
                    err = e;
                }
            }, null, rc);

            if (err != null)
            {
                // error = &err;
                // NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
                _trace.writeToLog(1, "CLFSDispatcher: UpdateAttributesUsingMetadata_forItemAtPath_error: ERROR: {0}, code: {1}.", err.errorDescription, err.errorCode);
            }
             
            //return rc;
            error = err;
            return rc.Value;
        }

        //- (BOOL)createSymbLinkAtPath:(NSString *)path withTarget:(NSString *)target
        public bool CreateSymbLinkAtPath_withTarget(string path, string target)
        {
            // __block BOOL rc = NO;
            // __block NSError *err = nil;
            // path = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            //     rc = [[NSFileManager defaultManager] createSymbolicLinkAtPath:path withDestinationPath:target error:&err];
            // });
            // if (err != nil) {
            //     NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
            // }
            // return rc;
            //&&&&


            // Fix up the slashes
            path = path.Replace('/', '\\');
            target = target.Replace('/', '\\');

            // __block BOOL rc = NO;
            // __block NSError *err = nil;
            bool rc = false;
            CLError err = null;

            // path = [[[CLSettings sharedSettings] cloudFolderPath] stringByAppendingPathComponent:path];
            path = Settings.Instance.CloudFolderPath + path;

            // dispatch_sync(get_cloud_FSD_queue(), ^(void) {
            Dispatch.Sync(Get_cloud_FSD_queue(), (object obj, object userState) =>
            {
                try
                {
                    // rc = [[NSFileManager defaultManager] createSymbolicLinkAtPath:path withDestinationPath:target error:&err];
                    //TODO: Get the shortcut info from the server packet.
                    CreateShortcut(path, "Shortcut Description", Application.ExecutablePath, Application.StartupPath + @"App.ico");
                    rc = true;
                }
                catch (Exception e)
                {
                    err = e;
                }
            }, null, null);

            // if (err != nil) {
            if (err != null)
            {
                // NSLog(@"%s - Error: %@, Code: %ld", __FUNCTION__, [err localizedDescription], [err code]);
                _trace.writeToLog(1, "CLFSDispatcher: CreateSymbLinkAtPath_withTarget: ERROR: {0}, code: {1}.", err.errorDescription, err.errorCode);
            }

            // return rc;
            return rc;
        }

        /// <summary>
        /// Create a shortcut.
        /// <param name="shortcutPath">The path of the shortcut item to be created.</param>
        /// <param name="shortcutDescription">The description of the shortcut.</param>
        /// <param name="shortcutTargetPath">The program or file to be opened when the shortcut is clicked.</param>
        /// <param name="shortcutIconPath">The path to the shortcut's icon file.</param>
        /// <returns>void</returns>
        /// </summary>
        public void CreateShortcut(string shortcutPath, string shortcutDescription, string shortcutTargetPath, string shortcutIconPath)
        {

            // Fix up the slashes
            shortcutPath = shortcutPath.Replace('/', '\\');
            shortcutTargetPath = shortcutTargetPath.Replace('/', '\\');
            shortcutIconPath = shortcutIconPath.Replace('/', '\\');
            
            // Create a new instance of WshShellClass
            WshShell WshShell = new WshShell();

            // Create the shortcut
            IWshRuntimeLibrary.IWshShortcut MyShortcut;

            // Choose the path for the shortcut
            MyShortcut = (IWshRuntimeLibrary.IWshShortcut)WshShell.CreateShortcut(shortcutPath);

            // Where the shortcut should point to
            MyShortcut.TargetPath = Application.ExecutablePath;

            // Description for the shortcut
            //TODO: This
            MyShortcut.Description = "Launch the dummy cloud application";

            // Location for the shortcut's icon
            MyShortcut.IconLocation = Application.StartupPath + @"\app.ico";

            // Create the shortcut at the given path
            MyShortcut.Save();
        }


        /// <summary>
        /// Launch an application.
        /// <param name="fileName">The path\filename.ext of the program to run.</param>
        /// <param name="arguments">The arguments to pass to the program on the command line.</param>
        /// <param name="error">A potential output error.</param>
        /// <returns>int, The return code from the program, if error is null, or -1.</returns>
        /// </summary>
        public static int LaunchCommandLineApp(string fileName, string arguments, out CLError error)
        {

            // Fix up the slashes
            fileName = fileName.Replace('/', '\\');

            // Use ProcessStartInfo class
            int rc = -1;
            error = null;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = fileName;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = arguments;

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                    rc = exeProcess.ExitCode;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return rc;
        }
    }
}
