using CloudApiPublic;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudSDK_SmokeTest.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Helpers
{
    public class FileHelper
    {
        public static void HandleRestFailedResponse(CLHttpRestStatus restStatus, string requestType, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            switch (restStatus)
            {
                case CLHttpRestStatus.BadRequest:
                    RestStatusResponseManager.HandleBadRequest(requestType);
                    break;
                case CLHttpRestStatus.Cancelled:
                    RestStatusResponseManager.HandleCancelled(requestType);
                    break;
                case CLHttpRestStatus.ConnectionFailed:
                    RestStatusResponseManager.HandleConnectionFailed(requestType);
                    break;
                case CLHttpRestStatus.NoContent:
                    RestStatusResponseManager.HandleNoContent(requestType);
                    break;
                case CLHttpRestStatus.NotAuthorized:
                    RestStatusResponseManager.HandleNotAutorized(requestType);
                    break;
                case CLHttpRestStatus.NotFound:
                    RestStatusResponseManager.HandleNotFound(requestType);
                    break;
                case CLHttpRestStatus.QuotaExceeded:
                    RestStatusResponseManager.HandleQuotaExceeded(requestType);
                    break;
                case CLHttpRestStatus.ServerError:
                    RestStatusResponseManager.HandleServerError(requestType);
                    break;
            }
        }
    }
}
