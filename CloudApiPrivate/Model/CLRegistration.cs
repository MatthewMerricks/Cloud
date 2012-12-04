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
using CloudApiPublic.Resources;
using CloudApiPublic.Static;
using System.IO;
using CloudApiPrivate.Static;

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
        private const string client_id = "7d5352411711b2435c3d5e8f7bcf9ee71e956637ef3efe47024ec56ab5164a07";
        private const string client_secret = "3c52734df439f457e4d6750662708108ebdaa13182ef4aed3238626474be444d";
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
                        err += ex;
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

            Func<bool, string> getBody = excludeAuthorization =>
                {
                    return String.Format(CLDefinitions.CLRegistrationCreateRequestBodyString,
                        Helpers.JavaScriptStringEncode(account.FirstName, true),
                        Helpers.JavaScriptStringEncode(account.LastName, true),
                        (excludeAuthorization
                            ? "---Username excluded---"
                            : Helpers.JavaScriptStringEncode(account.UserName, true)),
                        (excludeAuthorization
                            ? "---Password excluded---"
                            : Helpers.JavaScriptStringEncode(account.Password, true)),
                        Helpers.JavaScriptStringEncode(device.FriendlyName, true),
                        Helpers.JavaScriptStringEncode(device.Udid, true),
                        Helpers.JavaScriptStringEncode(device.OSType(), true),
                        Helpers.JavaScriptStringEncode(device.OSPlatform(), true),
                        Helpers.JavaScriptStringEncode(device.OSVersion(), true),
                        Helpers.JavaScriptStringEncode(CLDefinitions.AppVersion.ToString(), false),
                        Helpers.JavaScriptStringEncode(client_id, true),
                        (excludeAuthorization
                            ? "---Client secret excluded---"
                            : Helpers.JavaScriptStringEncode(client_secret, true)));
                };

            string body = getBody(false);
            string authorizationExcludedBody = null;
            if (((Settings.Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                && Settings.Settings.Instance.TraceExcludeAuthorization)
            {
                authorizationExcludedBody = getBody(true);
            }

            HttpContent content = new StringContent(body, Encoding.UTF8);
            content.Headers.ContentType.MediaType = "application/json";

            if (((Settings.Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication))
            {
                Trace.LogCommunication(Settings.Settings.Instance.TraceLocation,
                    Settings.Settings.Instance.Udid,
                    Settings.Settings.Instance.Uuid,
                    CommunicationEntryDirection.Request,
                    CLDefinitions.CLRegistrationCreateRequestURLString,
                    true,
                    client.DefaultRequestHeaders,
                    null,
                    (Settings.Settings.Instance.TraceExcludeAuthorization ? (new StringContent(authorizationExcludedBody, Encoding.UTF8)) : content),
                    null,
                    Settings.Settings.Instance.TraceExcludeAuthorization);
            }

            HttpResponseMessage result = null;

            // Perform the Post and wait for the result synchronously.
            try
            {
                result = client.Post(CLDefinitions.CLRegistrationCreateRequestURLString, content);
            }
            catch (AggregateException ex)
            {
                System.Net.WebException foundWebEx = null;

                Func<object, Exception, bool> findWebEx = (findFunc, toCheck) =>
                {
                    Func<object, Exception, bool> castFunc = findFunc as Func<object, Exception, bool>;
                    if (castFunc != null)
                    {
                        foundWebEx = toCheck as System.Net.WebException;
                        if (foundWebEx != null)
                        {
                            return true;
                        }
                        if (toCheck.InnerException != null)
                        {
                            return castFunc(findFunc, toCheck.InnerException);
                        }
                    }
                    return false;
                };

                foreach (Exception currentInnerException in ex.Flatten().InnerExceptions)
                {
                    if (findWebEx(findWebEx, currentInnerException))
                    {
                        break;
                    }
                }

                System.Net.HttpWebResponse exceptionResponse;
                if (foundWebEx != null
                    && (exceptionResponse = foundWebEx.Response as System.Net.HttpWebResponse) != null)
                {
                    try
                    {
                        string exceptionBody = null;
                        try
                        {
                            using (Stream createNewAccountResponseStream = exceptionResponse.GetResponseStream())
                            {
                                using (StreamReader createNewAccountResponseStreamReader = new StreamReader(createNewAccountResponseStream, Encoding.UTF8))
                                {
                                    exceptionBody = createNewAccountResponseStreamReader.ReadToEnd();
                                }
                            }
                        }
                        catch
                        {
                        }

                        if (((Settings.Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication))
                        {
                            Trace.LogCommunication(Settings.Settings.Instance.TraceLocation,
                                Settings.Settings.Instance.Udid,
                                Settings.Settings.Instance.Uuid,
                                CommunicationEntryDirection.Response,
                                CLDefinitions.CLRegistrationCreateRequestURLString,
                                true,
                                exceptionResponse.Headers,
                                exceptionBody,
                                (int)exceptionResponse.StatusCode,
                                Settings.Settings.Instance.TraceExcludeAuthorization);
                        }

                        error += new AggregateException("Create account error. Code: " + ((int)exceptionResponse.StatusCode).ToString() +
                            (string.IsNullOrEmpty(exceptionBody)
                            ? string.Empty
                            : Environment.NewLine + "Response: " + exceptionBody), ex);
                    }
                    finally
                    {
                        try
                        {
                            exceptionResponse.Close();
                        }
                        catch
                        {
                        }
                    }
                }
                else
                {
                    throw ex;
                }
            }

            if (result != null)
            {
                try
                {
                    if (((Settings.Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication))
                    {
                        Trace.LogCommunication(Settings.Settings.Instance.TraceLocation,
                            Settings.Settings.Instance.Udid,
                            Settings.Settings.Instance.Uuid,
                            CommunicationEntryDirection.Response,
                            CLDefinitions.CLRegistrationCreateRequestURLString,
                            true,
                            null,
                            result.Headers,
                            result.Content,
                            (int)result.StatusCode,
                            Settings.Settings.Instance.TraceExcludeAuthorization);
                    }

                    if (result.IsSuccessStatusCode)
                    {
                        string jsonResult = result.Content.ReadAsString();

                        _trace.writeToLog(1, "CLRegistration.cs: CreateNewAccount: Registration Response: {0}.", jsonResult);

                        isSuccess = processCreateNewAccountServerResponse(outRegistration, jsonResult, out error);
                    }
                    else
                    {
                        error += new Exception("Create account error: " + result.StatusCode.ToString() + "." +
                            (result.Content == null
                            ? string.Empty
                            : Environment.NewLine + GetErrorOrMessageFromJsonServerResponse(result.Content.ReadAsString())) + ".");
                    }
                }
                finally
                {
                    try
                    {
                        result.Dispose();
                    }
                    catch
                    {
                    }
                }
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
            Dictionary<string, object> returnDictionary = CLPrivateHelpers.JsonDeserializeToDictionary(response);

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

                    string apiKey = (string)returnDictionary[CLDefinitions.CLRegistrationAccessTokenKey];
                    string devicename = (string)deviceDictionary["friendly_name"];
                    string udid = (string)deviceDictionary["device_uuid"];
                    string uuid = userInfoDictionary["id"].ToString();
                    string username = (string)userInfoDictionary["email"];
                    string firstname = (string)userInfoDictionary["first_name"];
                    string lastname = (string)userInfoDictionary["last_name"];

                    outRegistration.Udid = udid;
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
                error.errorDescription = String.Format(CloudApiPublic.Resources.Resources.ExceptionCreatingUserRegistrationWithCode, 1400);
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
                    err += ex;
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

            Func<bool, string> getBody = (excludeAuthorization) =>
                {
                    return String.Format(CLDefinitions.CLRegistrationLinkRequestBodyString,
                        (excludeAuthorization
                            ? "---Username excluded---"
                            : Helpers.JavaScriptStringEncode(userName, true)),
                        (excludeAuthorization
                            ? "---Password excluded---"
                            : Helpers.JavaScriptStringEncode(password, true)),
                        Helpers.JavaScriptStringEncode(device.FriendlyName, true),
                        Helpers.JavaScriptStringEncode(device.Udid, true),
                        Helpers.JavaScriptStringEncode(device.OSType(), true),
                        Helpers.JavaScriptStringEncode(device.OSPlatform(), true),
                        Helpers.JavaScriptStringEncode(device.OSVersion(), true),
                        Helpers.JavaScriptStringEncode(CLDefinitions.AppVersion.ToString(), false),
                        Helpers.JavaScriptStringEncode(client_id, true),
                        (excludeAuthorization
                            ? "---Client secret excluded---"
                            : Helpers.JavaScriptStringEncode(client_secret, true)));
                };

            string body = getBody(false);
            string authorizationExcludedBody = null;
            if (((Settings.Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication)
                && Settings.Settings.Instance.TraceExcludeAuthorization)
            {
                authorizationExcludedBody = getBody(true);
            }

            this.Udid = device.Udid;
            _trace.writeToLog(1, "CLRegistration.cs: CreateNewAccount: UDid: <{0}>.", this.Udid);

            HttpContent content = new StringContent(body, Encoding.UTF8);
            content.Headers.ContentType.MediaType = "application/json";

            if (((Settings.Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication))
            {
                Trace.LogCommunication(Settings.Settings.Instance.TraceLocation,
                    Settings.Settings.Instance.Udid,
                    Settings.Settings.Instance.Uuid,
                    CommunicationEntryDirection.Request,
                    CLDefinitions.CLRegistrationLinkRequestURLString,
                    true,
                    client.DefaultRequestHeaders,
                    null,
                    (Settings.Settings.Instance.TraceExcludeAuthorization ? (new StringContent(authorizationExcludedBody, Encoding.UTF8)) : content),
                    null,
                    Settings.Settings.Instance.TraceExcludeAuthorization);
            }

            HttpResponseMessage result = null;

            // Perform the Post and wait for the result synchronously.
            try
            {
                result = client.Post(CLDefinitions.CLRegistrationLinkRequestURLString, content);
            }
            catch (AggregateException ex)
            {
                System.Net.WebException foundWebEx = null;

                Func<object, Exception, bool> findWebEx = (findFunc, toCheck) =>
                {
                    Func<object, Exception, bool> castFunc = findFunc as Func<object, Exception, bool>;
                    if (castFunc != null)
                    {
                        foundWebEx = toCheck as System.Net.WebException;
                        if (foundWebEx != null)
                        {
                            return true;
                        }
                        if (toCheck.InnerException != null)
                        {
                            return castFunc(findFunc, toCheck.InnerException);
                        }
                    }
                    return false;
                };

                foreach (Exception currentInnerException in ex.Flatten().InnerExceptions)
                {
                    if (findWebEx(findWebEx, currentInnerException))
                    {
                        break;
                    }
                }

                System.Net.HttpWebResponse exceptionResponse;
                if (foundWebEx != null
                    && (exceptionResponse = foundWebEx.Response as System.Net.HttpWebResponse) != null)
                {
                    try
                    {
                        string exceptionBody = null;
                        try
                        {
                            using (Stream linkResponseStream = exceptionResponse.GetResponseStream())
                            {
                                using (StreamReader linkResponseStreamReader = new StreamReader(linkResponseStream, Encoding.UTF8))
                                {
                                    exceptionBody = linkResponseStreamReader.ReadToEnd();
                                }
                            }
                        }
                        catch
                        {
                        }

                        if (((Settings.Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication))
                        {
                            Trace.LogCommunication(Settings.Settings.Instance.TraceLocation,
                                Settings.Settings.Instance.Udid,
                                Settings.Settings.Instance.Uuid,
                                CommunicationEntryDirection.Response,
                                CLDefinitions.CLRegistrationLinkRequestURLString,
                                true,
                                exceptionResponse.Headers,
                                exceptionBody,
                                (int)exceptionResponse.StatusCode,
                                Settings.Settings.Instance.TraceExcludeAuthorization);
                        }

                        error += new AggregateException("Login error. Code: " + ((int)exceptionResponse.StatusCode).ToString() +
                            (string.IsNullOrEmpty(exceptionBody)
                            ? string.Empty
                            : Environment.NewLine + "Response: " + exceptionBody), ex);
                    }
                    finally
                    {
                        try
                        {
                            exceptionResponse.Close();
                        }
                        catch
                        {
                        }
                    }
                }
                else
                {
                    throw ex;
                }
            }

            if (result != null)
            {
                try
                {
                    if (((Settings.Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication))
                    {
                        Trace.LogCommunication(Settings.Settings.Instance.TraceLocation,
                            Settings.Settings.Instance.Udid,
                            Settings.Settings.Instance.Uuid,
                            CommunicationEntryDirection.Response,
                            CLDefinitions.CLRegistrationLinkRequestURLString,
                            true,
                            null,
                            result.Headers,
                            result.Content,
                            (int)result.StatusCode,
                            Settings.Settings.Instance.TraceExcludeAuthorization);
                    }

                    if (result.IsSuccessStatusCode)
                    {
                        string jsonResult = result.Content.ReadAsString();

                        _trace.writeToLog(1, "CLRegistration.cs: LoginInternal: Registration Response: {0}.", jsonResult);

                        isSuccess = ProcessServerResponse(outRegistration, jsonResult, out error);

                        // Set the new UDID after successfull link.. This id is used by the notification server.. 
                        //CloudApiPrivate.Model.Settings.Settings.Instance.recordUDID(this.Udid);
                    }
                    else
                    {
                        error += new Exception("Login error: " + result.StatusCode.ToString() + "." +
                            (result.Content == null
                            ? string.Empty
                            : Environment.NewLine + GetErrorOrMessageFromJsonServerResponse(result.Content.ReadAsString())) + ".");
                    }
                }
                finally
                {
                    try
                    {
                        result.Dispose();
                    }
                    catch
                    {
                    }
                }
            }

            return isSuccess;
        }

        /// <summary>
        /// Retrieve a user-displayable string from a server response (JSON).  Pull out either the string
        /// value of the "error" or "message" token.
        /// </summary>
        /// <param name="serverResponse">The server response to parse</param>
        /// <returns>string: The user-displayable message.</returns>
        string GetErrorOrMessageFromJsonServerResponse(string serverResponse)
        {
            try
            {
                Dictionary<string, object> returnDictionary = CLPrivateHelpers.JsonDeserializeToDictionary(serverResponse);
                if (returnDictionary.ContainsKey("error"))
                {
                    return (string)returnDictionary["error"];
                }
                else if (returnDictionary.ContainsKey("message"))
                {
                    return (string)returnDictionary["message"];
                }
            }
            catch
            {
            }
            return serverResponse;
        }



        /// <summary>
        /// Private method that processes the Login JSON result string from the server.
        /// <param name="outRegistration">The registration object to fill in with information from the server.</param>
        /// <param name="response">The JSON response string.</param>
        /// <param name="error">An output error object.  This object will be null on a successful return.</param>
        /// <returns>(bool) true: Success</returns>
        /// </summary>
        bool ProcessServerResponse(CLRegistration outRegistration, string response, out CLError error)
        {
            bool retVal = true;

            error = null;
            Dictionary<string, object> returnDictionary = CLPrivateHelpers.JsonDeserializeToDictionary(response);

            if (returnDictionary != null)
            {
                if (!returnDictionary.ContainsKey("status") || ((string)returnDictionary["status"]) == "error")
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

                    //¡¡ If the access token is ever not a root property of the server response with key CLDefinitions.CLRegistrationAccessTokenKey, !!
                    //¡¡ then make sure to update CloudApiPublic.Static.Trace.LogCommunication private overload so that the property can be excluded when excludeAuthorization !!
                    string apiKey = (string)returnDictionary[CLDefinitions.CLRegistrationAccessTokenKey];
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
                error.errorDescription = String.Format(CloudApiPublic.Resources.Resources.ExceptionLoggingInWithCode, 1400);
                error.errorDomain = CLError.ErrorDomain_Application;
            }

            return retVal;
        }

        /// <summary>
        /// Unlink this device from the account by synchronous communication with the server.
        /// <param name="key">The Udid to unlink.</param>
        /// <param name="error">An output error object.  This object will be null on a successful return.</param>
        /// <returns>(bool) true: Success</returns>
        /// </summary>
        //- (BOOL)unlinkDeviceWithAccessKey:(NSString *)key
        public bool UnlinkDeviceWithAccessKey(string key, out CLError error)
        {
            // Merged 7/20/12
            // BOOL success = YES;

            // // create registration http request
            // AsyncHTTP *ahttp = [AsyncHTTP asyncHTTPWithRedirect:YES];

            // NSMutableURLRequest *request = [NSMutableURLRequest requestWithURL:[NSURL URLWithString:CLRegistrationUnlinkRequestURLString]];
            // [request setHTTPMethod:@"POST"];

            // NSString *body = [NSString stringWithFormat:CLRegistrationUnlinkRequestBodyString, key];

            // NSLog(@"%s - Link Request:\n\n%@\n\n", __FUNCTION__, body);
            // [request setHTTPBody:[body dataUsingEncoding:NSUTF8StringEncoding]];

            // // send request to server 
            // dispatch_sync(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{ 
            //    [ahttp issueRequest:request];
            // });

            // if ([ahttp error] != nil) {

            //     self.error_ = [ahttp error]; 
            //     success = NO;

            // } else {

            //     if ([ahttp statusCode] == HTTP_OK_200) {

            //         NSLog(@"%s - Registration Response:\n\n%@\n\n", __FUNCTION__, [ahttp dataAsString]);
            //         success = [self processServerResponse:[ahttp dataAsString]];

            //     } else {

            //         NSMutableDictionary *errorDetail = [NSMutableDictionary dictionary];
            //         [errorDetail setValue:[NSString stringWithFormat:@"Ops. We're sorry, it seems like something went wrong. Error: %ld", [ahttp statusCode]]
            //                        forKey:NSLocalizedDescriptionKey];

            //         self.error_ = [NSError errorWithDomain:@"com.cloud.error" code:[ahttp statusCode] userInfo:errorDetail];
            //         success = NO;
            //     }
            // }

            // return success;
            //&&&&

            bool isSuccess = false;
            error = null;

            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", "token=\"" + CloudApiPrivate.Model.Settings.Settings.Instance.Akey + "\"");

                //string body = String.Format(CLDefinitions.CLRegistrationUnlinkRequestBodyString, CloudApiPrivate.Model.Settings.Settings.Instance.Akey);
                string body = String.Empty;

                _trace.writeToLog(1, "CLRegistration.cs: Unlink. Udid: <{0}>.", CloudApiPrivate.Model.Settings.Settings.Instance.Udid);

                HttpContent content = new StringContent(body, Encoding.UTF8);
                content.Headers.ContentType.MediaType = "application/json";

                if (((Settings.Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication))
                {
                    Trace.LogCommunication(Settings.Settings.Instance.TraceLocation,
                        Settings.Settings.Instance.Udid,
                        Settings.Settings.Instance.Uuid,
                        CommunicationEntryDirection.Request,
                        CLDefinitions.CLRegistrationUnlinkRequestURLString,
                        true,
                        client.DefaultRequestHeaders,
                        null,
                        content,
                        null,
                        Settings.Settings.Instance.TraceExcludeAuthorization);
                }

                HttpResponseMessage result = null;

                // Perform the Post and wait for the result synchronously.
                try
                {
                    result = client.Post(CLDefinitions.CLRegistrationUnlinkRequestURLString, content);
                }
                catch (AggregateException ex)
                {
                    System.Net.WebException foundWebEx = null;

                    Func<object, Exception, bool> findWebEx = (findFunc, toCheck) =>
                        {
                            Func<object, Exception, bool> castFunc = findFunc as Func<object, Exception, bool>;
                            if (castFunc != null)
                            {
                                foundWebEx = toCheck as System.Net.WebException;
                                if (foundWebEx != null)
                                {
                                    return true;
                                }
                                if (toCheck.InnerException != null)
                                {
                                    return castFunc(findFunc, toCheck.InnerException);
                                }
                            }
                            return false;
                        };

                    foreach (Exception currentInnerException in ex.Flatten().InnerExceptions)
                    {
                        if (findWebEx(findWebEx, currentInnerException))
                        {
                            break;
                        }
                    }

                    System.Net.HttpWebResponse exceptionResponse;
                    if (foundWebEx != null
                        && (exceptionResponse = foundWebEx.Response as System.Net.HttpWebResponse) != null)
                    {
                        try
                        {
                            string exceptionBody = null;
                            try
                            {
                                using (Stream unlinkResponseStream = exceptionResponse.GetResponseStream())
                                {
                                    using (StreamReader unlinkResponseStreamReader = new StreamReader(unlinkResponseStream, Encoding.UTF8))
                                    {
                                        exceptionBody = unlinkResponseStreamReader.ReadToEnd();
                                    }
                                }
                            }
                            catch
                            {
                            }

                            if (((Settings.Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication))
                            {
                                Trace.LogCommunication(Settings.Settings.Instance.TraceLocation,
                                    Settings.Settings.Instance.Udid,
                                    Settings.Settings.Instance.Uuid,
                                    CommunicationEntryDirection.Response,
                                    CLDefinitions.CLRegistrationUnlinkRequestURLString,
                                    true,
                                    exceptionResponse.Headers,
                                    exceptionBody,
                                    (int)exceptionResponse.StatusCode,
                                    Settings.Settings.Instance.TraceExcludeAuthorization);
                            }

                            error += new AggregateException("Unlink error. Code: " + ((int)exceptionResponse.StatusCode).ToString() +
                                (string.IsNullOrEmpty(exceptionBody)
                                ? string.Empty
                                : Environment.NewLine + "Response: " + exceptionBody), ex);
                        }
                        finally
                        {
                            try
                            {
                                exceptionResponse.Close();
                            }
                            catch
                            {
                            }
                        }
                    }
                    else
                    {
                        throw ex;
                    }
                }

                if (result != null)
                {
                    try
                    {
                        if (((Settings.Settings.Instance.TraceType & TraceType.Communication) == TraceType.Communication))
                        {
                            Trace.LogCommunication(Settings.Settings.Instance.TraceLocation,
                                Settings.Settings.Instance.Udid,
                                Settings.Settings.Instance.Uuid,
                                CommunicationEntryDirection.Response,
                                CLDefinitions.CLRegistrationUnlinkRequestURLString,
                                true,
                                null,
                                result.Headers,
                                result.Content,
                                (int)result.StatusCode,
                                Settings.Settings.Instance.TraceExcludeAuthorization);
                        }

                        if (!result.IsSuccessStatusCode)
                        {
                            error += new Exception("Unlink error: " + result.StatusCode.ToString() + "." +
                                (result.Content == null
                                ? string.Empty
                                : Environment.NewLine + GetErrorOrMessageFromJsonServerResponse(result.Content.ReadAsString())) + ".");
                        }
                        else
                        {
                            isSuccess = true;
                        }
                    }
                    finally
                    {
                        try
                        {
                            result.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error += ex;
                error.LogErrors(CLTrace.Instance.TraceLocation, CLTrace.Instance.LogErrors);
                _trace.writeToLog(1, "CLRegistration: UnlinkDeviceWithAccessKey: ERROR: Exception. Msg: <{0}>. Code: {1}", error.errorDescription, error.errorCode);
            }

            return isSuccess;
        }

        #endregion
    }
}

