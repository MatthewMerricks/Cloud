using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public sealed class RestStatusResponseManager
    {
        #region Properties
        #endregion 

        #region Init
        #endregion 

        #region Implementation 
        #endregion 

        #region Static
        public static void HandleBadRequest(string opperationTarget)
        {
            if (opperationTarget == ManagerConstants.RequestTypes.PostFileChange)
            { 
            }
            //TODO:Create logic for handling a bad request. 
            Console.WriteLine("The Rest Reponse for Type {0} is Bad Request.", opperationTarget);
        }

        public static void HandleCancelled(string opperationTarget)
        {
            //TODO:Create logic for handling a cancelled request.
            Console.WriteLine("The Rest Reponse for Type {0} is Cancelled.", opperationTarget);
        }

        public static void HandleConnectionFailed(string opperationTarget)
        {
            //TODO:Create logic for handling a request where the connection failed.
            Console.WriteLine("The Rest Reponse for Type {0} is Connection Failed.", opperationTarget);
        }

        public static void HandleNoContent(string opperationTarget)
        {
            //TODO:Create logic for handling a request where file has no content. 
            Console.WriteLine("The Rest Reponse for Type {0} is No Content.", opperationTarget);
        }

        public static void HandleNotAutorized(string opperationTarget)
        {
            //TODO:Create logic for handling a request where the operation is not authorized.
            Console.WriteLine("The Rest Reponse for Type {0} is Not Authorized.", opperationTarget);
        }

        public static void HandleNotFound(string opperationTarget)
        {
            //TODO:Create logic for handling a request where the file is not found. 
            Console.WriteLine("The Rest Reponse for Type {0} is Not Found.", opperationTarget);
        }

        public static void HandleQuotaExceeded(string opperationTarget)
        {
            //TODO:Create logic for handling a request where the syncbox Quota is exceeded.
            Console.WriteLine("The Rest Reponse for Type {0} is Quota Exceeded.", opperationTarget);
        }

        public static void HandleServerError(string opperationTarget)
        {
            //TODO:Create logic for handling a request where server returns a Server errror. 
            Console.WriteLine("The Rest Reponse for Type {0} is Server Error.", opperationTarget);
        }
        #endregion 

        #region Private
        #endregion
    }
}
