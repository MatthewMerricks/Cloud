//
// CredentialsSessionsResponse.cs
// Cloud Windows
//
// Created By BobS.
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
    /// Result from <see cref="Cloud.CLCredentials.SessionCredentialsForKey"/> and <see cref="Cloud.CLCredentials.ListAllActiveSessionCredentials"/>
    /// </summary>
    [Obfuscation(Exclude = true)]
    [Serializable] // -David: converted to ISerializable to prevent Newtonsoft.Json manual parsing
    [KnownType(typeof(Session[]))]
    [KnownType(typeof(Session))]
    public sealed class CredentialsSessionsResponse : ISerializable  // -David: converted to ISerializable to prevent Newtonsoft.Json manual parsing
    {
        public string Status { get; set; }

        public string Message { get; set; }

        public Session[] Sessions { get; set; }

        #region ISerializable members -David: converted to ISerializable to prevent Newtonsoft.Json manual parsing

        // why does this work?? (called upon ReadObject on a JsonDataContractSerializer on a response stream)
        protected CredentialsSessionsResponse(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry toAdd in info)
            {
                switch (toAdd.Name)
                {
                    case CLDefinitions.RESTResponseStatus:
                        if (toAdd.ObjectType == typeof(string))
                        {
                            this.Status = (string)toAdd.Value;
                        }
                        else
                        {
                            throw new CLInvalidOperationException(
                                CLExceptionCode.General_Arguments,
                                string.Format(
                                    Resources.ExceptionSerializationInfoInvalidObjectType,
                                    CLDefinitions.RESTResponseStatus,
                                    typeof(string),
                                    toAdd.ObjectType));
                        }
                        break;

                    case CLDefinitions.RESTResponseMessage:
                        if (toAdd.ObjectType == typeof(string))
                        {
                            this.Message = (string)toAdd.Value;
                        }
                        else
                        {
                            throw new CLInvalidOperationException(
                                CLExceptionCode.General_Arguments,
                                string.Format(
                                    Resources.ExceptionSerializationInfoInvalidObjectType,
                                    CLDefinitions.RESTResponseMessage,
                                    typeof(string),
                                    toAdd.ObjectType));
                        }
                        break;

                    case CLDefinitions.RESTResponseSession:
                        //if (toAdd.ObjectType == typeof(string))
                        //{
                        //    this.ServiceType = (string)toAdd.Value;
                        //}
                        //else
                        //{
                        throw new CLInvalidOperationException(
                            CLExceptionCode.General_Arguments,
                            string.Format(
                                Resources.ExceptionSerializationInfoInvalidObjectType,
                                CLDefinitions.RESTResponseSession,
                                typeof(string),
                                toAdd.ObjectType));
                    //}
                    //break;

                    case CLDefinitions.RESTResponseSession_Sessions:
                        //if (toAdd.ObjectType == typeof(string))
                        //{
                        //    this.ServiceType = (string)toAdd.Value;
                        //}
                        //else
                        //{
                        throw new CLInvalidOperationException(
                            CLExceptionCode.General_Arguments,
                            string.Format(
                                Resources.ExceptionSerializationInfoInvalidObjectType,
                                CLDefinitions.RESTResponseSession_Sessions,
                                typeof(string),
                                toAdd.ObjectType));
                    //}
                    //break;
                }
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (this.Status != null)
            {
                info.AddValue(CLDefinitions.RESTResponseStatus, this.Status);
            }

            if (this.Message != null)
            {
                info.AddValue(CLDefinitions.RESTResponseMessage, this.Message);
            }

            if (this.Sessions != null)
            {
                if (this.Sessions.Length == 1)
                {
                    info.AddValue(CLDefinitions.RESTResponseSession, this.Sessions[0]);
                }

                info.AddValue(CLDefinitions.RESTResponseSession_Sessions, this.Sessions);
            }
        }

        #endregion
    }
}