﻿//
//  CLApiRegistration.cs
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
using CloudApi.Support;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Net.Http.Properties;
using System.Text;
using System.Collections.Generic;

namespace CloudApi
{
    public class CLApiRegistration
    {
        #region "Properties"
        public string Uuid { get; set; }
        public string Token { get; set; }
        public CLApiAccount LinkedAccount { get; set; }
        public string LinkedDeviceName { get; set; }
        #endregion

        #region "Public Definitions"
        public delegate void CreateNewAccountAsyncCallback(CLApiRegistration registration, bool isSuccess, CLApiError error);
        #endregion

        #region "Private Fields"
        private CLSptTrace _trace = CLSptTrace.Instance;
        #endregion

        #region "Life Cycle"

        public CLApiRegistration()
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
        public void CreateNewAccountAsync(CLApiAccount account, CLApiDevice device, CreateNewAccountAsyncCallback callback, double timeoutInSeconds, CLApiRegistration outRegistration)
        {

            var tsMain = new CancellationTokenSource();
            CancellationToken ctMain = tsMain.Token;

            var tsTimeout = new CancellationTokenSource();
            CancellationToken ctTimeout = tsMain.Token;

            // Start the thread to be used to communicate with the server.
            CLApiError errorFromAsync = null;
            Task<bool>.Factory.StartNew(() => CreateNewAccountInternal(outRegistration, account, device, out errorFromAsync)).ContinueWith(task =>
                {
                    bool isSuccess = true;
                    bool bResult = false;
                    CLApiError err = null;

                    Exception ex = task.Exception;
                    if (ex == null)
                    {
                        bResult = task.Result;
                    }
                    
                    if (ex != null)
                    {
                        err = new CLApiError();
                        err.errorDomain = CLApiError.ErrorDomain_Application;
                        err.errorDescription = CLSptResourceManager.Instance.ResMgr.GetString("ExceptionCreatingUserRegistration");
                        err.errorCode = (int)CLApiError.ErrorCodes.Exception;
                        err.errorInfo = new Dictionary<string,object>();
                        err.errorInfo.Add(CLApiError.ErrorInfo_Exception, ex);
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
        private bool CreateNewAccountInternal(CLApiRegistration outRegistration, CLApiAccount account, CLApiDevice device, out CLApiError error)
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
            //content.Headers.Add("Accept", "*/*");

            // Perform the Post and wait for the result synchronously.
            var result = client.PostAsync(CLApiConstants.CLRegistrationCreateRequestURLString, content).Result;
            if (result.IsSuccessStatusCode)
            {
            
                string jsonResult = result.Content.ReadAsStringAsync().Result;

                _trace.writeToLog(1, "CLApiRegistration.cs: CreateNewAccount: Registration Response: {0}.", jsonResult);

                isSuccess = processServerResponse(outRegistration, jsonResult, out error);

            } 
            else 
            {
                error = new CLApiError();
                error.errorCode = (int)result.StatusCode;
                error.errorDescription = String.Format(CLSptResourceManager.Instance.ResMgr.GetString("ExceptionCreatingUserRegistrationWithCode"), error.errorCode);
                error.errorDomain = CLApiError.ErrorDomain_Application;
                isSuccess = false;
            }

            return isSuccess;
        }

        /// <summary>
        /// Private method that processes the CreateNewAccount JSON result string from the server.
        /// <param name="outRegistration">The registration object to fill in with information from the server.</param>
        /// <param name="response">The JSON response string.</param>
        /// <param name="error">An output error object.  This object will be null on a successful return.</param>
        /// <returns>(bool) true: Success</returns>
        /// </summary>
        bool processServerResponse(CLApiRegistration outRegistration, string response, out CLApiError error)
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
                    error = new CLApiError();
                    error.errorCode = 1400;
                    error.errorDescription = String.Format((string)returnDictionary["message"], 1400) + ".";
                    error.errorDomain = CLApiError.ErrorDomain_Application;            
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
                    outRegistration.LinkedAccount = new CLApiAccount(username, firstname, lastname, null);
                }
                
            } 
            else
            {
                // JSON parse error
                retVal = false;
                error = new CLApiError();
                error.errorCode = 1400;
                error.errorDescription = String.Format(CLSptResourceManager.Instance.ResMgr.GetString("ExceptionCreatingUserRegistrationWithCode"), 1400);
                error.errorDomain = CLApiError.ErrorDomain_Application;            
            }
    
            return retVal;
        }
        #endregion
    }
}

