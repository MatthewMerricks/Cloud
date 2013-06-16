//
// AuthenticationError.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Cloud.JsonContracts
{
    /// <summary>
    /// Contains properties for an authentication error. An array of this error info can be found in <see cref="AuthenticationErrorResponse"/>
    /// </summary>
    [Obfuscation(Exclude = true)]
    [DataContract]

    internal sealed class AuthenticationError
    {
        [DataMember(Name = CLDefinitions.JsonServiceTypeFieldCode, IsRequired = false)]
        public Nullable<ulong> Code
        {
            get
            {
                return (ulong)CodeAsEnum;
            }
            set
            {
                CodeAsEnum = (value == null ? (Nullable<AuthenticationErrorType>)null : (AuthenticationErrorType)value);
            }
        }

        public Nullable<AuthenticationErrorType> CodeAsEnum { get; set; }

        [DataMember(Name = CLDefinitions.RESTResponseMessage, IsRequired = false)]
        public string Message { get; set; }
    }
}