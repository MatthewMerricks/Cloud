// 
// CredentialsSeperatedParams.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Parameters
{
    /// <summary>
    /// Contains the seperated components of a CLCredentials object for use in returning the list of all sessions via <see cref="Cloud.CLCredentials.ListAllActiveSessionCredentials"/>
    /// </summary>
    public sealed class CredentialsSeperatedParams
    {
        /// <summary>
        /// The public key that identifies this application or session.
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
        /// The application or session secret private key.
        /// </summary>
        public string Secret
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
        public string Token
        {
            get
            {
                return _token;
            }
        }
        private readonly string _token;

        internal CredentialsSeperatedParams(string Key, string Secret, string Token)
        {
            this._key = Key;
            this._secret = Secret;
            this._token = Token;
        }
    }
}