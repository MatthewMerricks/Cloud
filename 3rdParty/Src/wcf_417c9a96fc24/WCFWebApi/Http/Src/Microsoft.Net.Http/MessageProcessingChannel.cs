﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace System.Net.Http
{
    public abstract class MessageProcessingChannel : DelegatingChannel
    {
        protected MessageProcessingChannel(HttpMessageChannel innerChannel)
            : base(innerChannel)
        {
        }

        protected abstract HttpRequestMessage ProcessRequest(HttpRequestMessage request,
            CancellationToken cancellationToken);
        protected abstract HttpResponseMessage ProcessResponse(HttpResponseMessage response,
            CancellationToken cancellationToken);

        protected internal sealed override HttpResponseMessage Send(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request", "A request message must be provided. It cannot be null.");
            }

            HttpRequestMessage newRequestMessage = ProcessRequest(request, cancellationToken);
            HttpResponseMessage response = base.Send(request, cancellationToken);

            if (response == null)
            {
                throw new InvalidOperationException("Channel did not return a response message.");
            }

            return ProcessResponse(response, cancellationToken);
        }

        protected internal sealed override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken,
            object userstate = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request", "A request message must be provided. It cannot be null.");
            }

            // ProcessRequest() and ProcessResponse() are supposed to be fast, so we call ProcessRequest() on the same
            // thread SendAsync() was invoked to avoid context switches. However, if ProcessRequest() throws, we have 
            // to catch the exception since the caller doesn't expect exceptions when calling SendAsync(): The 
            // expectation is that the returned task will get faulted on errors, but the async call to SendAsync() 
            // should complete.
            TaskCompletionSource<HttpResponseMessage> tcs = new TaskCompletionSource<HttpResponseMessage>();
            try
            {
                HttpRequestMessage newRequestMessage = ProcessRequest(request, cancellationToken);
                Task<HttpResponseMessage> sendAsyncTask = base.SendAsync(newRequestMessage, cancellationToken);

                // We schedule a continuation task once the inner channel completes in order to trigger the response
                // processing method. ProcessResponse() is only called if the task wasn't canceled before.
                sendAsyncTask.ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        tcs.TrySetException(task.Exception.GetBaseException());
                        return;
                    }

                    if (task.IsCanceled)
                    {
                        tcs.TrySetCanceled();
                        return;
                    }

                    if (task.Result == null)
                    {
                        tcs.TrySetException(new InvalidOperationException("Channel did not return a response message."));
                        return;
                    }

                    try
                    {
                        HttpResponseMessage responseMessage = ProcessResponse(task.Result, cancellationToken);
                        tcs.TrySetResult(responseMessage);
                    }
                    catch (OperationCanceledException e)
                    {
                        // If ProcessResponse() throws an OperationCanceledException check wheter it is related to
                        // the cancellation token we received from the user. If so, cancel the Task.
                        HandleCanceledOperations(cancellationToken, tcs, e);
                    }
                    catch (Exception e)
                    {
                        tcs.TrySetException(e);
                    }

                    // We don't pass the cancellation token to the continuation task, since we want to get called even
                    // if the operation was canceled: We'll set the Task returned to the user to canceled. Passing the
                    // cancellation token here would result in the continuation task to not be called at all. I.e. we 
                    // would never complete the task returned to the caller of SendAsync().
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
            catch (OperationCanceledException e)
            {
                HandleCanceledOperations(cancellationToken, tcs, e);
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }

            return tcs.Task;
        }

        private static void HandleCanceledOperations(CancellationToken cancellationToken, 
            TaskCompletionSource<HttpResponseMessage> tcs, OperationCanceledException e)
        {
            // Check if the exception was due to a cancellation. If so, check if the OperationCanceledException is 
            // related to our CancellationToken. If it was indeed caused due to our cancellation token being
            // canceled, set the Task as canceled. Set it to faulted otherwise, since the OperationCanceledException
            // is not related to our cancellation token.
            if (cancellationToken.IsCancellationRequested && (e.CancellationToken == cancellationToken))
            {
                tcs.TrySetCanceled();
            }
            else
            {
                tcs.TrySetException(e);
            }
        }
    }
}
