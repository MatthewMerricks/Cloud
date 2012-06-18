using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using CloudApiPublic.Model;
using System.IO;

namespace win_client.Network
{
    class CLHttpConnectionOperation
    {
        private string _path = null;
        private string _syncId = null;
        private ulong _eventId = 0;
        private CLMetadata _metadata = null;

        private CLError _error = null;
        private byte [] _responseData = null;
        private string _urlConnection = null;
        private HttpRequestMessage _operationRequest = null;
        private StreamReader _inputStream = null;
        private StreamWriter _outputStream = null;
        private bool _executing = false;
        private bool _finished = false;
        private string _responseFilePath = null;

        /// <summary>
        /// Sets a callback to be called when an undetermined number of bytes have been downloaded from the server.
        /// <param name="action">An Action object to be executed when an undetermined number of bytes have been 
        /// downloaded from the server. This Task has no return value and takes three arguments: 
        /// the number of bytes written since the last time the upload progress block was called, 
        /// the total bytes written, and the total bytes expected to be written during the request, 
        /// as initially determined by the length of the HTTP body. This block may be called multiple times.</param>
        /// <remarks>See SetDownloadProgressBlock</remarks>
        /// </summary>

        public void SetUploadProgressBlock(Action<uint, uint, uint> block)
        {
            //TODO: Implement this to display progress.
        }

        /// <summary>
        /// Sets a callback to be called when an undetermined number of bytes have been uploaded to the server.
        /// <param name="action">An Action object to be executed when an undetermined number of bytes have been 
        /// uploaded to the server. This Task has no return value and takes three arguments: 
        /// the number of bytes read since the last time the upload progress block was called, 
        /// the total bytes read, and the total bytes expected to be read during the request, 
        /// as initially determined by the length of the HTTP body. This block may be called multiple times.</param>
        /// <remarks>See SetDownloadProgressBlock</remarks>
        /// </summary>

        public void SetDownloadProgressBlock(Action<uint, uint, uint> block)
            //TODO: Implement this to display progress.
        {
        }

        /// <summary>
        /// Sets the `completionBlock` property with a block that executes either the specified 
        /// success or failure block, depending on the state of the request on completion. 
        /// If `error` returns a value, which can be caused by an unacceptable status code or content type, 
        /// then `failure` is executed. Otherwise, `success` is executed.
        /// <param name="success">The Action to be executed on the completion of a successful request. 
        /// This block has no return value and takes two arguments: the receiver operation,
        /// and the object constructed from the response data of the request.</param>
        /// <param name="failure">The Action to be executed on the completion of an unsuccessful request. 
        /// This block has no return value and takes two arguments: the receiver operation,
        /// and the error that occured during the request.</param>
        /// <remarks>This method should be overridden in subclasses in order to specify the response object passed into the success block.</remarks>
        /// </summary>

        public void setCompletionBlockWithSuccess(Action<CLHttpConnectionOperation, object> success,
                                                  Action<CLHttpConnectionOperation, object> failure)
        {
        }

        /// <summary>
        /// Sets the operation's completionBlock.
        /// <param name="completionBlock">The Action to be executed on the completion of this operation.
        /// This Action has no return value and takes three arguments: the operation,
        /// the response object, and a possible error.</param>
        /// </summary>

        public void setOperationCompletionBlock(Action<CLHttpConnectionOperation, object, CLError> completionBlock)
        {
        }
    }
}
