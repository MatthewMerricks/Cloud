using System;
using System.Net.Http;

namespace CloudApiPublic
{
    public interface CLRestClientDelegate
    {
    }
    public class CLRestClient
    {
        private CLRestClientDelegate _clientDelegate;
        public CLRestClientDelegate ClientDelegate
        {
            get
            {
                return _clientDelegate;
            }
            set
            {
                _clientDelegate = value;
            }
        }

        private int _defaultFileManager;
	    public int DefaultFileManager
	    {
    		get { return _defaultFileManager;}
		    set { _defaultFileManager = value;}
	    }
	
        private HttpClient _httpClient;
	    public HttpClient HttpClient
	    {
    		get { return _httpClient;}
		    set { _httpClient = value;}
	    }

        CLRestClient()
        {
            //_defaultFileManager = NSFileManager.DefaultManager();
            //_httpClient = new AFHTTPClient(NSURL.URLWithString(CLMetaDataServerURL));
            //_httpClient.SetAuthorizationHeaderWithToken((CLSettings.SharedSettings()).AKey());
            //(_httpClient.OperationQueue).SetMaxConcurrentOperationCount(2);
        }
        public void MoveFolderFromTo(string fromPath, string toPath)
        {
            //if (fromPath.HasPrefix((CLSettings.SharedSettings()).CloudFolderPath())) {
            //    fromPath = NSString.StringByRemovingCloudFolderPathFromPath(fromPath);
            //}

            //if (toPath.HasPrefix((CLSettings.SharedSettings()).CloudFolderPath())) {
            //    toPath = NSString.StringByRemovingCloudFolderPathFromPath(toPath);
            //}

            //NSDictionary parameters = new NSDictionary(fromPath, "from_path", toPath, "to_path", (CLSettings.SharedSettings()).Uuid(), "user_id", null);
            //Console.WriteLine("Folder move params: %@", parameters);
            //NSURLRequest request = (this.HttpClient).RequestWithMethodPathParameters("POST", "/folder_objects/move_folder", parameters);
            //AFHTTPRequestOperation operation = new AFHTTPRequestOperation(request);
            //operation.SetCompletionBlockWithSuccessFailure(^ (AFHTTPRequestOperation operation, object responseObject) {
            //    if ((operation.Response).StatusCode() == 200) {
            //        NSDictionary jsonResult = NSJSONSerialization.JSONObjectWithDataOptionsError(responseObject, NSJSONReadingMutableContainers, null);
            //        Console.WriteLine("Move File Json: %@", jsonResult);
            //    }

            //}
            //, ^ (AFHTTPRequestOperation operation, NSError error) {
            //    Console.WriteLine("%s - error code %ld", __FUNCTION__, (operation.Response).StatusCode());
            //}
            //);
            //(this.HttpClient).EnqueueHTTPRequestOperation(operation);
        }

        public void MoveFileFromTo(string fromPath, string toPath)
        {
            //NSDictionary fileMetaData = NSDictionary.AttributesForItemAtPath(toPath);
            //if (fromPath.HasPrefix((CLSettings.SharedSettings()).CloudFolderPath())) {
            //    fromPath = NSString.StringByRemovingCloudFolderPathFromPath(fromPath);
            //}

            //if (toPath.HasPrefix((CLSettings.SharedSettings()).CloudFolderPath())) {
            //    toPath = NSString.StringByRemovingCloudFolderPathFromPath(toPath);
            //}

            //NSDictionary parameters = new NSDictionary(fromPath, "from_path", toPath, "to_path", (CLSettings.SharedSettings()).Uuid(), "user_id", fileMetaData.
            //  ObjectForKey("file_hash"), "file_hash", null);
            //Console.WriteLine("File move params: %@", parameters);
            //NSURLRequest request = (this.HttpClient).RequestWithMethodPathParameters("POST", "/file_objects/private/move", parameters);
            //AFHTTPRequestOperation operation = new AFHTTPRequestOperation(request);
            //operation.SetCompletionBlockWithSuccessFailure(^ (AFHTTPRequestOperation operation, object responseObject) {
            //    if ((operation.Response).StatusCode() == 200) {
            //        NSDictionary jsonResult = NSJSONSerialization.JSONObjectWithDataOptionsError(responseObject, NSJSONReadingMutableContainers, null);
            //        Console.WriteLine("Move File Json: %@", jsonResult);
            //    }

            //}
            //, ^ (AFHTTPRequestOperation operation, NSError error) {
            //    Console.WriteLine("%s - error code %ld", __FUNCTION__, (operation.Response).StatusCode());
            //}
            //);
            //(this.HttpClient).EnqueueHTTPRequestOperation(operation);
        }

        public void ProccessBatchOfDirectoryPaths(Array /*NSArray*/ paths)
        {
            //NSMutableArray operations = new NSMutableArray();
            //foreach (string path in paths) {
            //    string fullSystemPath = NSString.StringWithFormat("%@%@", (CLSettings.SharedSettings()).CloudFolderPath(), path);
            //    NSDictionary fileMetaData = NSDictionary.AttributesForItemAtPath(fullSystemPath);
            //    NSURLRequest request = (this.HttpClient).RequestWithMethodPathParameters("POST", "/folder_objects/add_folder", fileMetaData);
            //    AFHTTPRequestOperation operation = new AFHTTPRequestOperation(request);
            //    operation.SetCompletionBlockWithSuccessFailure(^ (AFHTTPRequestOperation operation, object responseObject) {
            //        {
            //            CLMetadata folder;
            //            if ((operation.Response).StatusCode() == 201) {
            //                NSDictionary jsonResult = NSJSONSerialization.JSONObjectWithDataOptionsError(responseObject, NSJSONReadingMutableContainers, null);
            //                Console.WriteLine("Json Dic:%@", jsonResult);
            //                if (jsonResult) {
            //                    folder = new CLMetadata(jsonResult);
            //                }

            //                if ((this.Mydelegate).RespondsToSelector(@selector (restClient:createdFolder:))) {
            //                    (this.Mydelegate).RestClientCreatedFolder(this, folder);
            //                }

            //            }

            //        }
            //    }
            //    , ^ (AFHTTPRequestOperation operation, NSError error) {
            //        Console.WriteLine("%s - error code %ld", __FUNCTION__, (operation.Response).StatusCode());
            //        if ((this.Mydelegate).RespondsToSelector(@selector (restClient:createFolderFailedWithError:))) {
            //            (this.Mydelegate).RestClientCreateFolderFailedWithError(this, error);
            //        }

            //    }
            //    );
            //    operations.AddObject(operation);
            //}
            //(this.HttpClient).EnqueueBatchOfHTTPRequestOperationsProgressBlockCompletionBlock(operations, ^ (NSUInteger numberOfCompletedOperations, NSUInteger
            //  totalNumberOfOperations) {
            //    Console.WriteLine("Completed: %lu out of %lu  folder opperations.", numberOfCompletedOperations, totalNumberOfOperations);
            //}
            //, ^ (NSArray operations) {
            //    Console.WriteLine("All folder opperations Conpleted");
            //    (NSNotificationCenter.DefaultCenter()).PostNotificationNameMyobject("CLRestOperationQueDidFinish", this);
            //}
            //);
        }

        public void BatchProcessFileUploadsWithFiles(Array /*NSArray*/ files)
        {
            //NSMutableArray operations = new NSMutableArray();
            //foreach (string filePath in files) {
            //    operations.AddObject(this.UploadOperationForFile(filePath));
            //}
            //(this.HttpClient).EnqueueBatchOfHTTPRequestOperationsProgressBlockCompletionBlock(operations, ^ (NSUInteger numberOfCompletedOperations, NSUInteger
            //  totalNumberOfOperations) {
            //    Console.WriteLine("Completed: %lu out of %lu file upload operations.", numberOfCompletedOperations, totalNumberOfOperations);
            //}
            //, ^ (NSArray operations) {
            //    Console.WriteLine("All file upload operations Conpleted");
            //    (NSNotificationCenter.DefaultCenter()).PostNotificationNameMyobject("CLRestOperationQueDidFinish", this);
            //}
            //);
        }

        bool /*AFHTTPRequestOperation*/ UploadOperationForFile(string filePath)
        {
            //if (!filePath.HasPrefix((CLSettings.SharedSettings()).CloudFolderPath())) {
            //    filePath = NSString.StringWithFormat("%@%@", (CLSettings.SharedSettings()).CloudFolderPath(), filePath);
            //}

            //NSDictionary fileMetaData = NSDictionary.AttributesForItemAtPath(filePath);
            //Console.WriteLine("file: %@", fileMetaData);
            //NSData jsonData = NSJSONSerialization.DataWithJSONObjectOptionsError(fileMetaData, NSJSONReadingMutableContainers, null);
            //NSData fileData = NSData.DataWithContentsOfFile(filePath);
            //NSMutableURLRequest request = (this.HttpClient).MultipartFormRequestWithMethodPathParametersConstructingBodyWithBlock("POST",
            //  "/file_objects/add_file", null, ^ (AFMultipartFormData formData) {
            //    formData.AppendPartWithFileDataNameFileNameMimeType(fileData, "file", filePath.LastPathComponent(), NSString.MimeTypeForFileAtPath(filePath));
            //    formData.AppendPartWithFormDataName(jsonData, "file_object");
            //}
            //);
            //AFHTTPRequestOperation operation = new AFHTTPRequestOperation(request);
            //operation.SetUploadProgressBlock(^ (NSInteger bytesWritten, long long totalBytesWritten, long long totalBytesExpectedToWrite) {
            //    Console.WriteLine("Sent %llu of %llu bytes", totalBytesWritten, totalBytesExpectedToWrite);
            //}
            //);
            //operation.SetCompletionBlockWithSuccessFailure(^ (AFHTTPRequestOperation operation, object responseObject) {
            //    if ((operation.Response).StatusCode() == 201) {
            //        NSDictionary jsonResult = NSJSONSerialization.JSONObjectWithDataOptionsError(responseObject, NSJSONReadingMutableContainers, null);
            //        CLMetadata file = new CLMetadata(jsonResult);
            //        if ((this.Mydelegate).RespondsToSelector(@selector (restClient:uploadedFile:))) {
            //            (this.Mydelegate).RestClientUploadedFile(this, file);
            //        }

            //    }

            //}
            //, ^ (AFHTTPRequestOperation operation, NSError error) {
            //    Console.WriteLine("%s - error code %ld", __FUNCTION__, (operation.Response).StatusCode());
            //    if ((this.Mydelegate).RespondsToSelector(@selector (restClient:uploadFileFailedWithError:))) {
            //        (this.Mydelegate).RestClientUploadFileFailedWithError(this, error);
            //    }

            //}
            //);
            //return operation;
            return false;
        }

        public void BatchProcessFileDownloads(Array files)
        {
            //NSMutableArray operations = new NSMutableArray();
            //foreach (string filePath in files) {
            //    operations.AddObject(this.DownloadOperationForFile(filePath));
            //}
            //(this.HttpClient).EnqueueBatchOfHTTPRequestOperationsProgressBlockCompletionBlock(operations, ^ (NSUInteger numberOfCompletedOperations, NSUInteger
            //  totalNumberOfOperations) {
            //    Console.WriteLine("Completed: %lu out of %lu file downloads.", numberOfCompletedOperations, totalNumberOfOperations);
            //}
            //, ^ (NSArray operations) {
            //    Console.WriteLine("All file download operations Conpleted");
            //    (NSNotificationCenter.DefaultCenter()).PostNotificationNameMyobject("CLRestOperationQueDidFinish", this);
            //}
            //);
        }

        void /*AFHTTPRequestOperation*/ DownloadOperationForFile(string path)
        {
            //string metaDataPath = NSString.StringByRemovingCloudFolderPathFromPath(path);
            //NSDictionary Myparams = NSDictionary.DictionaryWithObjectsAndKeys(metaDataPath, "path", (CLSettings.SharedSettings()).Uuid(), "user_id", null);
            //NSURLRequest request = (this.HttpClient).RequestWithMethodPathParameters("GET", "/file_objects/get_file", Myparams);
            //Console.WriteLine("%s - URL: %@", __FUNCTION__, (request.URL()).AbsoluteString());
            //AFHTTPRequestOperation operation = new AFHTTPRequestOperation(request);
            //operation.SetCompletionBlockWithSuccessFailure(^ (AFHTTPRequestOperation operation, object responseObject) {
            //    Console.WriteLine("Download response: %lu", (operation.Response).StatusCode());
            //    if ((operation.Response).StatusCode() == 200) {
            //        if ((this.Mydelegate).RespondsToSelector(@selector (restClient:downloadedFile:forPath:))) {
            //            (this.Mydelegate).RestClientDownloadedFileForPath(this, responseObject, path);
            //        }

            //    }

            //}
            //, ^ (AFHTTPRequestOperation operation, NSError error) {
            //    Console.WriteLine("%s - error code %ld", __FUNCTION__, (operation.Response).StatusCode());
            //    if ((this.Mydelegate).RespondsToSelector(@selector (restClient:downloadFileFailedWithError:))) {
            //        (this.Mydelegate).RestClientDownloadFileFailedWithError(this, error);
            //    }

            //}
            //);
            //return operation;
        }

        public void UploadFile(string filePath)
        {
            //AFHTTPRequestOperation operation = this.UploadOperationForFile(filePath);
            //(this.HttpClient).EnqueueHTTPRequestOperation(operation);
        }

        public void DownloadFile(string path)
        {
            //AFHTTPRequestOperation operation = this.DownloadOperationForFile(path);
            //(this.HttpClient.OperationQueue).AddOperation(operation);
        }

        public void DeleteFile(string filePath)
        {
            //NSDictionary fileMetaData = NSDictionary.DictionaryWithObjectsAndKeys(NSString.StringByRemovingCloudFolderPathFromPath(filePath), "path", (
            //  CLSettings.SharedSettings()).Uuid(), "user_id", null);
            //NSURLRequest request = (this.HttpClient).RequestWithMethodPathParameters("POST", "/file_objects/delete", fileMetaData);
            //AFHTTPRequestOperation operation = new AFHTTPRequestOperation(request);
            //operation.SetCompletionBlockWithSuccessFailure(^ (AFHTTPRequestOperation operation, object responseObject) {
            //    if ((operation.Response).StatusCode() == 200) {
            //        NSDictionary jsonResult = NSJSONSerialization.JSONObjectWithDataOptionsError(responseObject, NSJSONReadingMutableContainers, null);
            //        CLMetadata file = new CLMetadata(jsonResult);
            //        if ((this.Mydelegate).RespondsToSelector(@selector (restClient:deletedFile:))) {
            //            (this.Mydelegate).RestClientDeletedFile(this, file);
            //        }

            //    }

            //}
            //, ^ (AFHTTPRequestOperation operation, NSError error) {
            //    Console.WriteLine("%s - error code %ld", __FUNCTION__, (operation.Response).StatusCode());
            //    if ((this.Mydelegate).RespondsToSelector(@selector (restClient:deleteFileFailedWithError:))) {
            //        (this.Mydelegate).RestClientDeleteFileFailedWithError(this, error);
            //    }

            //}
            //);
            //(this.HttpClient).EnqueueHTTPRequestOperation(operation);
        }

        public void CreateFolderAtPath(string path)
        {
            //NSDictionary fileMetaData = NSDictionary.AttributesForItemAtPath(path);
            //NSURLRequest request = (this.HttpClient).RequestWithMethodPathParameters("POST", "/folder_objects/add_folder", fileMetaData);
            //AFHTTPRequestOperation operation = new AFHTTPRequestOperation(request);
            //operation.SetCompletionBlockWithSuccessFailure(^ (AFHTTPRequestOperation operation, object responseObject) {
            //    {
            //        CLMetadata folder;
            //        if ((operation.Response).StatusCode() == 201) {
            //            NSDictionary jsonResult = NSJSONSerialization.JSONObjectWithDataOptionsError(responseObject, NSJSONReadingMutableContainers, null);
            //            if (jsonResult) {
            //                folder = new CLMetadata(jsonResult);
            //            }
            //            else {
            //                folder = new CLMetadata();
            //            }

            //            if ((this.Mydelegate).RespondsToSelector(@selector (restClient:createdFolder:))) {
            //                (this.Mydelegate).RestClientCreatedFolder(this, folder);
            //            }

            //        }

            //    }
            //}
            //, ^ (AFHTTPRequestOperation operation, NSError error) {
            //    Console.WriteLine("%s - error code %ld", __FUNCTION__, (operation.Response).StatusCode());
            //    if ((this.Mydelegate).RespondsToSelector(@selector (restClient:createFolderFailedWithError:))) {
            //        (this.Mydelegate).RestClientCreateFolderFailedWithError(this, error);
            //    }

            //}
            //);
            //(this.HttpClient.OperationQueue).AddOperation(operation);
        }

        public void AddDirectoryAtPath(string path)
        {
            //if (0 != ((this.DefaultFileManager).SubpathsAtPath(path)).Count()) {
            //    NSDirectoryEnumerator dirEnumerator = (this.DefaultFileManager).EnumeratorAtURLIncludingPropertiesForKeysOptionsErrorHandler(NSURL.
            //      URLWithString(path), NSArray.ArrayWithObjects(NSURLNameKey, NSURLIsDirectoryKey, null), NSDirectoryEnumerationSkipsHiddenFiles, ^ (NSURL url,
            //      NSError error) {
            //        Console.WriteLine("Error: %@", error.LocalizedDescription());
            //        return true;
            //    }
            //    );
            //    foreach (NSURL theURL in dirEnumerator) {
            //        NSNumber isDirectory;
            //        theURL.GetResourceValueForKeyError(isDirectory, NSURLIsDirectoryKey, NULL);
            //        if (isDirectory.BoolValue() == false) {
            //            this.UploadFile(theURL.Path());
            //        }

            //    }
            //}
            //else {
            //    this.CreateFolderAtPath(path);
            //}

        }

        public void RemoveDirectoryAtPath(string directoryPath)
        {
            //NSDictionary Myparams = NSDictionary.DictionaryWithObjectsAndKeys(NSString.StringByRemovingCloudFolderPathFromPath(directoryPath), "path", (
            //  CLSettings.SharedSettings()).Uuid(), "user_id", null);
            //NSURLRequest request = (this.HttpClient).RequestWithMethodPathParameters("POST", "/folder_objects/delete_folder", Myparams);
            //AFHTTPRequestOperation operation = new AFHTTPRequestOperation(request);
            //operation.SetCompletionBlockWithSuccessFailure(^ (AFHTTPRequestOperation operation, object responseObject) {
            //    Console.WriteLine("%s - Resonse status code: %ld", __FUNCTION__, (operation.Response).StatusCode());
            //    NSArray deletedFolders;
            //    if ((operation.Response).StatusCode() == 200) {
            //        NSDictionary jsonResult = NSJSONSerialization.JSONObjectWithDataOptionsError(responseObject, NSJSONReadingMutableContainers, null);
            //        deletedFolders = jsonResult.ValueForKey("objects");
            //    }

            //    if ((this.Mydelegate).RespondsToSelector(@selector (restClient:deletedFolders:))) {
            //        (this.Mydelegate).RestClientDeletedFolders(this, deletedFolders);
            //    }

            //}
            //, ^ (AFHTTPRequestOperation operation, NSError error) {
            //    Console.WriteLine("%s - error code %ld", __FUNCTION__, (operation.Response).StatusCode());
            //    if ((this.Mydelegate).RespondsToSelector(@selector (restClient:deleteFolderFailedWithError:))) {
            //        (this.Mydelegate).RestClientDeleteFolderFailedWithError(this, error);
            //    }

            //}
            //);
            //(this.HttpClient).EnqueueHTTPRequestOperation(operation);
        }

        public void MetaDataForDirectoryAtPathAndSID(string path, string sid)
        {
            //string user_id = (CLSettings.SharedSettings()).Uuid();
            //NSMutableDictionary Myparams = new NSMutableDictionary();
            //Myparams.SetValueForKey(user_id, "user_id");
            //if (path) {
            //    Myparams.SetValueForKey(path, "path");
            //}

            //if (sid) {
            //    Mtparams.SetValueForKey(sid, "sid");
            //}

            //NSURLRequest request = (this.HttpClient).RequestWithMethodPathParameters("GET", "/folder_objects/contents", Myparams);
            //Console.WriteLine("%s - URL: %@", __FUNCTION__, (request.URL()).AbsoluteString());
            //AFHTTPRequestOperation operation = new AFHTTPRequestOperation(request);
            //operation.SetCompletionBlockWithSuccessFailure(^ (AFHTTPRequestOperation operation, object responseObject) {
            //    NSError error;
            //    if ((operation.Response).StatusCode() == 200) {
            //        NSDictionary jsonResult = NSJSONSerialization.JSONObjectWithDataOptionsError(responseObject, NSJSONReadingAllowFragments, error);
            //        if (error) {
            //            Console.WriteLine("Json Error:%@", error.LocalizedDescription());
            //        }

            //        if ((this.Mydelegate).RespondsToSelector(@selector (restClient:metadataForDirectory:))) {
            //            (this.Mydelegate).RestClientMetadataForDirectory(this, jsonResult);
            //        }

            //    }

            //}
            //, ^ (AFHTTPRequestOperation operation, NSError error) {
            //    Console.WriteLine("%s - error code %ld", __FUNCTION__, (operation.Response).StatusCode());
            //    if ((this.Mydelegate).RespondsToSelector(@selector (restClient:metadataForDirectoryFailedWithError:))) {
            //        (this.Mydelegate).RestClientMetadataForDirectoryFailedWithError(this, error);
            //    }

            //}
            //);
            //(this.HttpClient).EnqueueHTTPRequestOperation(operation);
        }

        public void MetaDataForFolderAtPath(string path)
        {
            //NSDictionary Myparams = NSDictionary.DictionaryWithObjectsAndKeys(NSString.StringByRemovingCloudFolderPathFromPath(path), "path", (CLSettings.
            //  SharedSettings()).Uuid(), "user_id", null);
            //NSURLRequest request = (this.HttpClient).RequestWithMethodPathParameters("GET", "/folder_objects/metadata", Myparams);
            //Console.WriteLine("%s - URL: %@", __FUNCTION__, (request.URL()).AbsoluteString());
            //AFHTTPRequestOperation operation = new AFHTTPRequestOperation(request);
            //operation.SetCompletionBlockWithSuccessFailure(^ (AFHTTPRequestOperation operation, object responseObject) {
            //    NSError error;
            //    CLMetadata folderMetaData;
            //    if ((operation.Response).StatusCode() == 200) {
            //        NSDictionary jsonResult = NSJSONSerialization.JSONObjectWithDataOptionsError(responseObject, NSJSONReadingAllowFragments, error);
            //        folderMetaData = new CLMetadata(jsonResult);
            //        if (error) {
            //            Console.WriteLine("Json Error:%@", error.LocalizedDescription());
            //        }

            //    }

            //    if ((this.Mydelegate).RespondsToSelector(@selector (restClient:metadataForFolder:))) {
            //        (this.Mydelegate).RestClientMetadataForFolder(this, folderMetaData);
            //    }

            //}
            //, ^ (AFHTTPRequestOperation operation, NSError error) {
            //    Console.WriteLine("%s - error code %ld", __FUNCTION__, (operation.Response).StatusCode());
            //    if ((this.Mydelegate).RespondsToSelector(@selector (restClient:folderMetadataFailedWithError:))) {
            //        (this.Mydelegate).RestClientFolderMetadataFailedWithError(this, error);
            //    }

            //}
            //);
            //(this.HttpClient).EnqueueHTTPRequestOperation(operation);
        }

        public void MetadataForFileAtPath(string path)
        {
            //NSDictionary Myparams = NSDictionary.DictionaryWithObjectsAndKeys(NSString.StringByRemovingCloudFolderPathFromPath(path), "path", (CLSettings.
            //  SharedSettings()).Uuid(), "user_id", null);
            //NSURLRequest request = (this.HttpClient).RequestWithMethodPathParameters("GET", "/file_objects/metadata", Myparams);
            //AFHTTPRequestOperation operation = new AFHTTPRequestOperation(request);
            //operation.SetCompletionBlockWithSuccessFailure(^ (AFHTTPRequestOperation operation, object responseObject) {
            //    NSError error;
            //    CLMetadata file;
            //    if (((operation.Response).StatusCode() == 200)) {
            //        NSDictionary jsonResult = NSJSONSerialization.JSONObjectWithDataOptionsError(responseObject, NSJSONReadingAllowFragments, error);
            //        file = new CLMetadata(jsonResult);
            //        if (error) {
            //        }

            //    }

            //    if ((operation.Response).StatusCode() == 204) {
            //        file = null;
            //    }

            //    if ((this.Mydelegate).RespondsToSelector(@selector (restClient:metadataForFile:))) {
            //        (this.Mydelegate).RestClientMetadataForFile(this, file);
            //    }

            //}
            //, ^ (AFHTTPRequestOperation operation, NSError error) {
            //    Console.WriteLine("%s - error code %ld", __FUNCTION__, (operation.Response).StatusCode());
            //    if ((this.Mydelegate).RespondsToSelector(@selector (restClient:metadataForFileFailedWithError:))) {
            //        (this.Mydelegate).RestClientMetadataForFileFailedWithError(this, error);
            //    }

            //}
            //);
            //(this.HttpClient).EnqueueHTTPRequestOperation(operation);
        }

        public void UploadFileCompletionHandler(string filePath, bool /*void*/ handler)
        {
            //if (!filePath.HasPrefix((CLSettings.SharedSettings()).CloudFolderPath())) {
            //    filePath = NSString.StringWithFormat("%@%@", (CLSettings.SharedSettings()).CloudFolderPath(), filePath);
            //}

            //NSDictionary fileMetaData = NSDictionary.AttributesForItemAtPath(filePath);
            //NSData jsonData = NSJSONSerialization.DataWithJSONObjectOptionsError(fileMetaData, NSJSONReadingMutableContainers, null);
            //NSData fileData = NSData.DataWithContentsOfFile(filePath);
            //NSMutableURLRequest request = (this.HttpClient).MultipartFormRequestWithMethodPathParametersConstructingBodyWithBlock("POST",
            //  "/file_objects/add_file", null, xxx);
            //int BigStuffRemoved1 = 0;
            //int BigStuffRemoved2 = 0;
            //NSURLConnection.SendAsynchronousRequestQueueCompletionHandler(request, NSOperationQueue.CurrentQueue(), xxx);
        }

        void DownloadFileCompletionHandler(string path, bool /*void*/ xxx)
        {
            //int MyBlockModified1 = 0;
            //string metaDataPath = NSString.StringByRemovingCloudFolderPathFromPath(path);
            //NSDictionary Myparams = NSDictionary.DictionaryWithObjectsAndKeys(metaDataPath, "path", (CLSettings.SharedSettings()).Uuid(), "user_id", null);
            //NSURLRequest request = (this.HttpClient).RequestWithMethodPathParameters("GET", "/file_objects/get_file", Myparams);
            //int TWO_BLOCKS_REMOVED = 0;
        }

    }
}
