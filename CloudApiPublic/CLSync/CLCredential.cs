// 
// CLCredential.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic
{
    /// <summary>
    /// Contains authentication information required for all communication and services
    /// 
    /// The CLCredential class declares the interface used for authentication and authorization to Cloud.com <http://Cloud.com>.
    ///
    /// The CLCredential class allows the developer to represent both the Application’s credential as well as temporary session credential. The Application’s credential provides access to all of your Application’s SyncBoxes. Using a temporary credential, access can be limited to an individual SyncBox.
    ///
    /// If the CLCredential object does not contain a token, all authentication and authorization attempts will be made by looking up the credential in the Application space.
    ///
    /// If the CLCredential object contains a token, all authentication and authorization attempts will be made by looking up the credential in the temporary session space.
    /// </summary>
    public sealed class CLCredential
    {
        /// <summary>
        /// The public key that identifies this application or session.
        /// </summary>
        internal string Key
        {
            get
            {
                return _key;
            }
        }
        private readonly string _key;

        /// <summary>
        /// The application or session secret private key.
        /// </summary>
        internal string Secret
        {
            get
            {
                return _secret;
            }
        }
        private readonly string _secret;

        /// <summary>
        /// The session token.
        /// </summary>
        internal string Token
        {
            get
            {
                return _token;
            }
        }
        private readonly string _token;

        /// <summary>
        /// Outputs a new credential object from key/secret
        /// </summary>
        /// <param name="Key">The public key that identifies this application.</param>
        /// <param name="Secret">The application secret private key.</param>
        /// <param name="credential">(output) Created credential object</param>
        /// <param name="status">(output) Status of creation, check this for Success</param>
        /// <param name="Token">(optional) The temporary token to use.  Default: null.</param>
        /// <returns>Returns any error that occurred in construction, if any, or null.</returns>
        public static CLError CreateAndInitialize(
            string Key,
            string Secret,
            out CLCredential credential,
            out CLCredentialCreationStatus status,
            string Token = null)
        {
            status = CLCredentialCreationStatus.ErrorUnknown;

            try
            {
                credential = new CLCredential(
                    Key,
                    Secret,
                    Token,
                    ref status);
            }
            catch (Exception ex)
            {
                credential = Helpers.DefaultForType<CLCredential>();
                return ex;
            }

            status = CLCredentialCreationStatus.Success;
            return null;
        }

        /// <summary>
        /// Private constructor
        /// </summary>
        private CLCredential(
            string Key,
            string Secret,
            string Token, 
            ref CLCredentialCreationStatus status)
        {
            // check input parameters

            if (string.IsNullOrEmpty(Key))
            {
                status = CLCredentialCreationStatus.ErrorNullKey;
                throw new NullReferenceException("Key cannot be null");
            }
            if (string.IsNullOrEmpty(Secret))
            {
                status = CLCredentialCreationStatus.ErrorNullSecret;
                throw new NullReferenceException("Secret cannot be null");
            }

            // Since we allow null then reverse-null coalesce from empty string
            if (Token == string.Empty)
            {
                Token = null;
            }

            this._key = Key;
            this._secret = Secret;
            
            this._token = Token;
        }

        /// <summary>
        /// Determine whether the credential was instantiated with a temporary token.
        /// </summary>
        /// <returns>bool: true: The token exists.</returns>
        public bool CredentialHasToken()
        {
            return !String.IsNullOrEmpty(_token);
        }

        #region public authorization HTTP API calls
        private sealed class NullSyncRoot : ICLSyncSettings
        {
            public string SyncRoot
            {
                get
                {
                    return null;
                }
            }

            public static readonly NullSyncRoot Instance = new NullSyncRoot();

            private NullSyncRoot() { }
        }

        /// <summary>
        /// Add a Sync box on the server for the current application
        /// </summary>
        /// <param name="timeoutMilliseconds">Milliseconds before HTTP timeout exception</param>
        /// <param name="status">(output) success/failure status of communication</param>
        /// <param name="response">(output) response object from communication</param>
        /// <param name="friendlyName">(optional) Friendly name of the Sync box</param>
        /// <returns>Returns any error that occurred during communication, if any</returns>
        public CLError AddSyncBoxOnServer(int timeoutMilliseconds, out CLHttpRestStatus status, out JsonContracts.CreateSyncBox response, ICLCredentialSettings settings = null, string friendlyName = null)
        {
            // start with bad request as default if an exception occurs but is not explicitly handled to change the status
            status = CLHttpRestStatus.BadRequest;
            // try/catch to process the metadata query, on catch return the error
            try
            {
                // copy settings so they don't change while processing; this also defaults some values
                ICLSyncSettingsAdvanced copiedSettings = (settings == null
                    ? NullSyncRoot.Instance.CopySettings()
                    : settings.CopySettings());

                // check input parameters

                if (!(timeoutMilliseconds > 0))
                {
                    throw new ArgumentException("timeoutMilliseconds must be greater than zero");
                }

                JsonContracts.CreateSyncBox inputBox = (string.IsNullOrEmpty(friendlyName)
                    ? null
                    : new JsonContracts.CreateSyncBox()
                        {
                            SyncBox = new JsonContracts.SyncBox()
                            {
                                FriendlyName = friendlyName
                            }
                        });

                response = Helpers.ProcessHttp<JsonContracts.CreateSyncBox>(
                    inputBox,
                    CLDefinitions.CLPlatformAuthServerURL,
                    CLDefinitions.MethodPathAuthCreateSyncBox,
                    Helpers.requestMethod.post,
                    timeoutMilliseconds,
                    null, // not an upload nor download
                    Helpers.HttpStatusesOkAccepted,
                    ref status,
                    copiedSettings,
                    this,
                    null);
            }
            catch (Exception ex)
            {
                response = Helpers.DefaultForType<JsonContracts.CreateSyncBox>();
                return ex;
            }
            return null;
        }
        #endregion
    }
    /// <summary>
    /// Status of creation of <see cref="CLCredential"/>
    /// </summary>
    public enum CLCredentialCreationStatus : byte
    {
        Success,
        ErrorNullKey,
        ErrorNullSecret,
        ErrorUnknown
    }
}