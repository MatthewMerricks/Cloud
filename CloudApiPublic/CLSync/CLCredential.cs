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
    /// Declares the interface used for authentication and authorization to Cloud.com <http://Cloud.com>.
    /// </summary>
    public sealed class CLCredential
    {
        /// <summary>
        /// The public key that identifies this credential.
        /// </summary>
        public string Key
        {
            get
            {
                return _key;
            }
        }
        private readonly string _key;

        /// <summary>
        /// The secret private key.
        /// </summary>
        internal string Secret
        {
            get
            {
                return _secret;
            }
        }
        private readonly string _secret;

        //// we don't support temporary credentials yet
        //internal string Token
        //{
        //    get
        //    {
        //        return _token;
        //    }
        //}
        //private readonly string _token;
        
        /// <summary>
        /// Whether these credentials are temporary and thus have a temporary credential token
        /// </summary>
        public bool HasToken
        {
            get
            {
                //// we don't support temporary credentials yet
                //return _token != null;
                
                // once we support temporary credentials, remove the next line
                return false;
            }
        }

        /// <summary>
        /// Outputs a new credentials object from key/secret
        /// </summary>
        /// <param name="Key">The public key that identifies this credential.</param>
        /// <param name="Secret">The secret private key.</param>
        /// <param name="credentials">(output) Created credentials object</param>
        /// <param name="status">(output) Status of creation, check this for Success</param>
        /// <returns>Returns any error that occurred in construction, if any</returns>
        public static CLError CreateAndInitialize(
            string Key,
            string Secret,
            out CLCredential credentials,
            out CLCredentialsCreationStatus status/*,
            string Token = null*/) // we don't support temporary credentials yet
        {
            status = CLCredentialsCreationStatus.ErrorUnknown;

            try
            {
                credentials = new CLCredential(
                    Key,
                    Secret/*,
                    Token*/,
                    ref status);
            }
            catch (Exception ex)
            {
                credentials = Helpers.DefaultForType<CLCredential>();
                return ex;
            }

            status = CLCredentialsCreationStatus.Success;
            return null;
        }
        private CLCredential(
            string Key,
            string Secret/*,
            string Token*/,
            ref CLCredentialsCreationStatus status) // we don't support temporary credentials yet
        {
            // check input parameters

            if (string.IsNullOrEmpty(Key))
            {
                status = CLCredentialsCreationStatus.ErrorNullKey;
                throw new NullReferenceException("Key cannot be null");
            }
            if (string.IsNullOrEmpty(Secret))
            {
                status = CLCredentialsCreationStatus.ErrorNullSecret;
                throw new NullReferenceException("Secret cannot be null");
            }

            //// we don't support temporary credentials yet
            //// since we allow null then reverse-null coalesce from empty string
            //if (Token == string.Empty)
            //{
            //    Token = null;
            //}

            this._key = Key;
            this._secret = Secret;
            //// we don't support temporary credentials yet
            //this._token = Token;
        }
    }
    /// <summary>
    /// Status of creation of <see cref="CLCredential"/>
    /// </summary>
    public enum CLCredentialsCreationStatus : byte
    {
        Success,
        ErrorNullKey,
        ErrorNullSecret,
        ErrorUnknown
    }
}