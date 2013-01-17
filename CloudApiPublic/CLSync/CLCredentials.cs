//
// CLCredentials.cs
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
    /// </summary>
    public sealed class CLCredentials
    {
        /// <summary>
        /// The public key that identifies this application.
        /// </summary>
        public string ApplicationKey
        {
            get
            {
                return _applicationKey;
            }
        }
        private readonly string _applicationKey;

        /// <summary>
        /// The application secret private key.
        /// </summary>
        public string ApplicationSecret
        {
            get
            {
                return _applicationSecret;
            }
        }
        private readonly string _applicationSecret;

        //// we don't support temporary credentials yet
        //public string TemporaryToken
        //{
        //    get
        //    {
        //        return _temporaryToken;
        //    }
        //}
        //private readonly string _temporaryToken;

        /// <summary>
        /// Outputs a new credentials object from key/secret
        /// </summary>
        /// <param name="ApplicationKey">The public key that identifies this application.</param>
        /// <param name="ApplicationSecret">The application secret private key.</param>
        /// <param name="credentials">(output) Created credentials object</param>
        /// <param name="status">(output) Status of creation, check this for Success</param>
        /// <returns>Returns any error that occurred in construction, if any</returns>
        public static CLError CreateAndInitialize(
            string ApplicationKey,
            string ApplicationSecret,
            out CLCredentials credentials,
            out CLCredentialsCreationStatus status/*,
            string TemporaryToken = null*/) // we don't support temporary credentials yet
        {
            status = CLCredentialsCreationStatus.ErrorUnknown;

            try
            {
                credentials = new CLCredentials(
                    ApplicationKey,
                    ApplicationSecret/*,
                    TemporaryToken*/,
                    ref status);
            }
            catch (Exception ex)
            {
                credentials = Helpers.DefaultForType<CLCredentials>();
                return ex;
            }

            status = CLCredentialsCreationStatus.Success;
            return null;
        }
        private CLCredentials(
            string ApplicationKey,
            string ApplicationSecret/*,
            string TemporaryToken*/,
            ref CLCredentialsCreationStatus status) // we don't support temporary credentials yet
        {
            // check input parameters

            if (string.IsNullOrEmpty(ApplicationKey))
            {
                status = CLCredentialsCreationStatus.ErrorNullApplicationKey;
                throw new NullReferenceException("ApplicationKey cannot be null");
            }
            if (string.IsNullOrEmpty(ApplicationSecret))
            {
                status = CLCredentialsCreationStatus.ErrorNullApplicationSecret;
                throw new NullReferenceException("ApplicationSecret cannot be null");
            }

            //// we don't support temporary credentials yet
            //// since we allow null then reverse-null coalesce from empty string
            //if (TemporaryToken == string.Empty)
            //{
            //    TemporaryToken = null;
            //}

            this._applicationKey = ApplicationKey;
            this._applicationSecret = ApplicationSecret;
            //// we don't support temporary credentials yet
            //this._temporaryToken = TemporaryToken;
        }
    }
    /// <summary>
    /// Status of creation of <see cref="CLCredentials"/>
    /// </summary>
    public enum CLCredentialsCreationStatus : byte
    {
        Success,
        ErrorNullApplicationKey,
        ErrorNullApplicationSecret,
        ErrorUnknown
    }
}