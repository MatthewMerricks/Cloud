//
//  CLRegistration.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Linq;
using CloudApiPublic.Support;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Net.Http.Properties;
using System.Text;
using System.Collections.Generic;
using CloudApiPublic.Model;
using CloudApiPrivate.Model;
using CloudApiPrivate.Model.Settings;

namespace CloudApiPrivate.Model
{
    public class CLRegistration
    {
        #region "Properties"
        public string Uuid { get; set; }
        public string Udid { get; set; }
        public string Token { get; set; }
        public CLAccount LinkedAccount { get; set; }
        public string LinkedDeviceName { get; set; }
        #endregion

        #region "Public Definitions"
        public delegate void CreateNewAccountAsyncCallback(CLRegistration registration, bool isSuccess, CLError error);
        #endregion

        #region "Private Fields"
        private CLTrace _trace = CLTrace.Instance;
        #endregion

        #region "Life Cycle"

        public CLRegistration()
        {
        }

        #endregion

        #region "Public Methods"

        /// <summary>
        /// Public method that will asynchronously ask the server to create a new account for this user.
        /// <param name="account">The user's account information</param>
        /// <param name="device">The information about this unique device</param>
        /// <param name="callback">The user's callback function which will execute when the asynchronous request is complete.</param>
        /// <param name="timeoutInSeconds">The maximum time that this request will remain active.  It will be cancelled if it is not complete within this time.  Specify Double.MaxValue for no timeout.</param>
        /// </summary>
        public void CreateNewAccountAsync(CLAccount account, CLDevice device, CreateNewAccountAsyncCallback callback, double timeoutInSeconds, CLRegistration outRegistration)
        {
            var tsMain = new CancellationTokenSource();
            CancellationToken ctMain = tsMain.Token;

            var tsTimeout = new CancellationTokenSource();
            CancellationToken ctTimeout = tsMain.Token;

            // Start the thread to be used to communicate with the server.
            CLError errorFromAsync = null;
            Task<bool>.Factory.StartNew(() => CreateNewAccountInternal(outRegistration, account, device, out errorFromAsync)).ContinueWith(task =>
                {
                    bool isSuccess = true;
                    bool bResult = false;
                    CLError err = null;

                    Exception ex = task.Exception;
                    if (ex == null)
                    {
                        bResult = task.Result;
                    }
                    
                    if (ex != null)
                    {
                        err = new CLError();
                        err.errorDomain = CLError.ErrorDomain_Application;
                        err.errorDescription = CLSptResourceManager.Instance.ResMgr.GetString("ExceptionCreatingUserRegistration");
                        err.errorCode = (int)CLError.ErrorCodes.Exception;
                        err.errorInfo = new Dictionary<string,object>();
                        err.errorInfo.Add(CLError.ErrorInfo_Exception, ex);
                        isSuccess = false;
                    }
                    else if(!bResult)
                    {
                        err = errorFromAsync;
                        isSuccess = false;
                    }

                    // The server communication is complete.  Kill the timeout thread.
                    tsTimeout.Cancel();

                    // Call the user's (of the API) callback.  This callback will execute on the main thread.
                    // The user's callback function may crash.  Just let the application crash if that happens.
                    // Exit this thread after the callback returns.
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        callback(outRegistration, isSuccess, err);
                    }));
                }, ctMain);

            // Start timeout thread
            Task.Factory.StartNew(() => 
            {
                int ticksUntilTimeout = (int)(timeoutInSeconds / 0.100);
                for (int i = 0; i < ticksUntilTimeout; ++i)
                {
                    if (ctTimeout.IsCancellationRequested)
                    {
                        // We were cancelled because the HTTP request completed.  Exit the timeout thread.
                        return;
                    }
                    Thread.Sleep(100);
                }

                // We timed out.  Kill the main thread and exit ours.
                tsMain.Cancel();
            }, ctTimeout);
        }

        #endregion

        #region "Support Functions"

        /// <summary>
        /// Private method that sends the CreateNewAccount HTTP Post message and waits for the result.
        /// <param name="outRegistration">The registration object to fill in with the information from the server.</param>
        /// <param name="account">The user's account information</param>
        /// <param name="device">The information about this unique device</param>
        /// <param name="error">An output error object.  This object will be null on a successful return.</param>
        /// <returns>(bool) true: Success</returns>
        /// </summary>
        private bool CreateNewAccountInternal(CLRegistration outRegistration, CLAccount account, CLDevice device, out CLError error)
        {
            bool isSuccess = false;
            error = null;

            HttpClient client = new HttpClient(); 

            string body = String.Format(CLSptConstants.CLRegistrationCreateRequestBodyString,  
                              account.FirstName, 
                              account.LastName, 
                              account.UserName, 
                              account.Password,
                              device.FriendlyName,
                              device.Udid,
                              device.OSType(),
                              device.OSVersion(),
                              "1.0");
            HttpContent content = new StringContent(body, Encoding.UTF8);
            content.Headers.ContentType.MediaType = "application/x-www-form-urlencoded";

            // Perform the Post and wait for the result synchronously.
            var result = client.PostAsync(CLDefinitions.CLRegistrationCreateRequestURLString, content).Result;
            if (result.IsSuccessStatusCode)
            {
            
                string jsonResult = result.Content.ReadAsStringAsync().Result;

                _trace.writeToLog(1, "CLRegistration.cs: CreateNewAccount: Registration Response: {0}.", jsonResult);

                isSuccess = processCreateNewAccountServerResponse(outRegistration, jsonResult, out error);
            } 
            else 
            {
                error = new CLError();
                error.errorCode = (int)result.StatusCode;
                error.errorDescription = String.Format(CLSptResourceManager.Instance.ResMgr.GetString("ExceptionCreatingUserRegistrationWithCode"), error.errorCode);
                error.errorDomain = CLError.ErrorDomain_Application;
                isSuccess = false;
            }

            return isSuccess;
        }

        /// <summary>
        /// Private method that processes the CreateNewAccount JSON result string from the server for the CreateNewAccount method.
        /// <param name="outRegistration">The registration object to fill in with information from the server.</param>
        /// <param name="response">The JSON response string.</param>
        /// <param name="error">An output error object.  This object will be null on a successful return.</param>
        /// <returns>(bool) true: Success</returns>
        /// </summary>
        bool processCreateNewAccountServerResponse(CLRegistration outRegistration, string response, out CLError error)
        {
            bool retVal = true;
    
            error = null;
            Dictionary<string, object> returnDictionary = CLSptJson.CLSptJsonDeserializeToDictionary(response);

            if (returnDictionary != null)
            {
                if (((string)returnDictionary["status"]) == "error")
                {
                    // The server returned an errlr
                    retVal = false;
                    error = new CLError();
                    error.errorCode = 1400;
                    error.errorDescription = String.Format((string)returnDictionary["message"], 1400) + ".";
                    error.errorDomain = CLError.ErrorDomain_Application;            
                }
                else
                {
                    // Successful response from server
                    // user dictionary.
                    //Dictionary<string, object> userInfoDictionary = returnDictionary["user"].ToDictionary(myKey => myKey.Key,
                    //    myValue => myValue.Value);

                    Dictionary<string, object> userInfoDictionary = new Dictionary<string, object>((Dictionary<string, object>)returnDictionary["user"]);

                    // device dictionary
                    Dictionary<string, object> deviceDictionary = new Dictionary<string, object>((Dictionary<string, object>)returnDictionary["device"]);
            
                    string apiKey = (string)returnDictionary["access_token"];
                    string devicename = (string)deviceDictionary["friendly_name"];
                    string uuid = userInfoDictionary["id"].ToString();
                    string username = (string)userInfoDictionary["email"];
                    string firstname = (string)userInfoDictionary["first_name"];
                    string lastname = (string)userInfoDictionary["last_name"];
            
                    outRegistration.Uuid = uuid;
                    outRegistration.Token = apiKey;
                    outRegistration.LinkedDeviceName = devicename;
                    outRegistration.LinkedAccount = new CLAccount(username, firstname, lastname, null);
                }
                
            } 
            else
            {
                // JSON parse error
                retVal = false;
                error = new CLError();
                error.errorCode = 1400;
                error.errorDescription = String.Format(CLSptResourceManager.Instance.ResMgr.GetString("ExceptionCreatingUserRegistrationWithCode"), 1400);
                error.errorDomain = CLError.ErrorDomain_Application;            
            }
    
            return retVal;
        }

        public void LinkNewDeviceWithLoginAsync(string username, string password, CLDevice device, CreateNewAccountAsyncCallback callback, double timeoutInSeconds, CLRegistration outRegistration)
        {
            var tsMain = new CancellationTokenSource();
            CancellationToken ctMain = tsMain.Token;

            var tsTimeout = new CancellationTokenSource();
            CancellationToken ctTimeout = tsMain.Token;

            // Start the thread to be used to communicate with the server.
            CLError errorFromAsync = null;
            Task<bool>.Factory.StartNew(() => LoginInternal(outRegistration, username, password, device, out errorFromAsync)).ContinueWith(task =>
            {
                bool isSuccess = true;
                bool bResult = false;
                CLError err = null;

                Exception ex = task.Exception;
                if (ex == null)
                {
                    bResult = task.Result;
                }

                if (ex != null)
                {
                    err = new CLError();
                    err.errorDomain = CLError.ErrorDomain_Application;
                    err.errorDescription = CLSptResourceManager.Instance.ResMgr.GetString("ExceptionLoggingIn");
                    err.errorCode = (int)CLError.ErrorCodes.Exception;
                    err.errorInfo = new Dictionary<string, object>();
                    err.errorInfo.Add(CLError.ErrorInfo_Exception, ex);
                    isSuccess = false;
                }
                else if (!bResult)
                {
                    err = errorFromAsync;
                    isSuccess = false;
                }

                // The server communication is complete.  Kill the timeout thread.
                tsTimeout.Cancel();

                // Call the user's (of the API) callback.  This callback will execute on the main thread.
                // The user's callback function may crash.  Just let the application crash if that happens.
                // Exit this thread after the callback returns.
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    callback(outRegistration, isSuccess, err);
                }));
            }, ctMain);

            // Start timeout thread
            Task.Factory.StartNew(() =>
            {
                int ticksUntilTimeout = (int)(timeoutInSeconds / 0.100);
                for (int i = 0; i < ticksUntilTimeout; ++i)
                {
                    if (ctTimeout.IsCancellationRequested)
                    {
                        // We were cancelled because the HTTP request completed.  Exit the timeout thread.
                        return;
                    }
                    Thread.Sleep(100);
                }

                // We timed out.  Kill the main thread and exit ours.
                tsMain.Cancel();
            }, ctTimeout);
        }

        /// <summary>
        /// Private method that sends the Login HTTP Post message and waits for the result.
        /// <param name="outRegistration">The registration object to fill in with the information from the server.</param>
        /// <param name="userName">The user's account name</param>
        /// <param name="password">The user's password</param>
        /// <param name="device">The information about this unique device</param>
        /// <param name="error">An output error object.  This object will be null on a successful return.</param>
        /// <returns>(bool) true: Success</returns>
        /// </summary>
        private bool LoginInternal(CLRegistration outRegistration, string userName, string password, CLDevice device, out CLError error)
        {
            bool isSuccess = false;
            error = null;

            HttpClient client = new HttpClient();

            string body = String.Format(CLDefinitions.CLRegistrationLinkRequestBodyString,
                              userName,
                              password,
                              device.FriendlyName,
                              device.Udid,
                              device.OSType(),
                              device.OSVersion(),
                              "1.0");

            this.Udid = device.Udid;
            _trace.writeToLog(1, "CLRegistration.cs: CreateNewAccount: UDid: <{0}>.", this.Udid);

            HttpContent content = new StringContent(body, Encoding.UTF8);
            content.Headers.ContentType.MediaType = "application/x-www-form-urlencoded";

            // Perform the Post and wait for the result synchronously.
            var result = client.PostAsync(CLDefinitions.CLRegistrationLinkRequestURLString, content).Result;
            if (result.IsSuccessStatusCode)
            {

                string jsonResult = result.Content.ReadAsStringAsync().Result;

                _trace.writeToLog(1, "CLRegistration.cs: CreateNewAccount: Registration Response: {0}.", jsonResult);

                isSuccess = processLoginServerResponse(outRegistration, jsonResult, out error);

                // Set the new UDID after successfull link.. This id is used by the notification server.. 
                CloudApiPrivate.Model.Settings.Settings.Instance.recordUDID(this.Udid);
            }
            else
            {
                error = new CLError();
                error.errorCode = (int)result.StatusCode;
                error.errorDescription = String.Format(CLSptResourceManager.Instance.ResMgr.GetString("ExceptionLoggingInWithCode"), error.errorCode);
                error.errorDomain = CLError.ErrorDomain_Application;
                isSuccess = false;
            }

            return isSuccess;
        }

        /// <summary>
        /// Private method that processes the Login JSON result string from the server.
        /// <param name="outRegistration">The registration object to fill in with information from the server.</param>
        /// <param name="response">The JSON response string.</param>
        /// <param name="error">An output error object.  This object will be null on a successful return.</param>
        /// <returns>(bool) true: Success</returns>
        /// </summary>
        bool processLoginServerResponse(CLRegistration outRegistration, string response, out CLError error)
        {
            bool retVal = true;

            error = null;
            Dictionary<string, object> returnDictionary = CLSptJson.CLSptJsonDeserializeToDictionary(response);

            if (returnDictionary != null)
            {
                if (((string)returnDictionary["status"]) == "error")
                {
                    // The server returned an errlr
                    retVal = false;
                    error = new CLError();
                    error.errorCode = 1400;
                    error.errorDescription = String.Format((string)returnDictionary["message"], 1400) + ".";
                    error.errorDomain = CLError.ErrorDomain_Application;
                }
                else
                {
                    // Successful response from server
                    // user dictionary.
                    //Dictionary<string, object> userInfoDictionary = returnDictionary["user"].ToDictionary(myKey => myKey.Key,
                    //    myValue => myValue.Value);

                    Dictionary<string, object> userInfoDictionary = new Dictionary<string, object>((Dictionary<string, object>)returnDictionary["user"]);

                    // device dictionary
                    Dictionary<string, object> deviceDictionary = new Dictionary<string, object>((Dictionary<string, object>)returnDictionary["device"]);

                    string apiKey = (string)returnDictionary["access_token"];
                    string devicename = (string)deviceDictionary["friendly_name"];
                    string uuid = userInfoDictionary["id"].ToString();
                    string username = (string)userInfoDictionary["email"];
                    string firstname = (string)userInfoDictionary["first_name"];
                    string lastname = (string)userInfoDictionary["last_name"];

                    outRegistration.Uuid = uuid;
                    outRegistration.Token = apiKey;
                    outRegistration.LinkedDeviceName = devicename;
                    outRegistration.LinkedAccount = new CLAccount(username, firstname, lastname, null);
                }

            }
            else
            {
                // JSON parse error
                retVal = false;
                error = new CLError();
                error.errorCode = 1400;
                error.errorDescription = String.Format(CLSptResourceManager.Instance.ResMgr.GetString("ExceptionLoggingInWithCode"), 1400);
                error.errorDomain = CLError.ErrorDomain_Application;
            }

            return retVal;
        }

        #endregion
    }
}

