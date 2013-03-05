using Cloud;
using CloudSDK_SmokeTest.Events.ManagerEventArgs;
using CloudSDK_SmokeTest.Interfaces;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public partial class LoginManager 
    {
        public static int Login(SmokeTestManagerEventArgs e)
        {
            int responseCode = 0;
            LoginRegister login = e.CurrentTask as LoginRegister;
            if(login == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            //TODO:Login Logic .... 
           

            if (responseCode == 0)
            {
                StringBuilder explanation = new StringBuilder("Successfully Logged In Using Credentials:");
                explanation.AppendLine();
                explanation.AppendLine(string.Format("      Username: {0}", login.Username));
                explanation.AppendLine(string.Format("      Password: {0}", login.Password));
            }
            return responseCode;
        }

        private static int BeginLogin()
        {
            int responseCode = 0;
            return responseCode;
        }


        public static int Register(SmokeTestManagerEventArgs e)
        {
            int responseCode = 0;
            return responseCode;
        }

        private static int BeginRegister()
        {
            int responseCode = 0;
            return responseCode;
        }
    }
}
