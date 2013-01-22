// 
// CLCredential.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

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
    /// The CLCredential class allows the developer to represent both the Application’s credential as well as temporary credential. The Application’s credential provides access to all of your Application’s SyncBoxes. Using a temporary credential, access can be limited to an individual SyncBox.
    ///
    /// If the CLCredential object does not contain a token, all authentication and authorization attempts will be made by looking up the credential in the Application space.
    ///
    /// If the CLCredential object contains a token, all authentication and authorization attempts will be made by looking up the credential in the temporary session space.
    /// </summary>
    public sealed class CLCredential
    {
        /// <summary>
        /// The public key that identifies this application.
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
        /// The application secret private key.
        /// </summary>
        internal string Secret
        {
            get
            {
                return _secret;
            }
        }
        private readonly string _secret;

        //TODO: Add support for temporary credential.
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
            string Token = null)  //TODO: Add support for temporary credential.
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
            string Token,           //TODO: Provide support for temporary tokens.
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

            //TODO: Provide support for temporary credential.
            // Since we allow null then reverse-null coalesce from empty string
            if (Token == string.Empty)
            {
                Token = null;
            }

            this._key = Key;
            this._secret = Secret;
            
            this._token = Token;        //TODO: Provide support for temporary credential.
        }

        /// <summary>
        /// Determine whether the credential was instantiated with a temporary token.
        /// </summary>
        /// <returns>bool: true: The token exists.</returns>
        public bool CredentialHasToken()
        {
            return !String.IsNullOrEmpty(_token);
        }
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