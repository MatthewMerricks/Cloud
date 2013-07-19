//
// Session.cs
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
    /// Contains actual HTTP response fields, representing a session.
    /// </summary>
    [Obfuscation(Exclude = true)]
    [Serializable] // -David: Had to convert from DataContract to Serializable because parent object CredentialsSessionsResponse is Serializable
    [KnownType(typeof(Service[]))]
    [KnownType(typeof(long[]))]
    public sealed class Session : ISerializable // -David: Had to convert from DataContract to Serializable because parent object CredentialsSessionsResponse is Serializable
    {
        public string Key { get; set; }

        public string Secret { get; set; }

        public string Token { get; set; }

        public Service[] Services { get; set; }

        public string ExpiresAtString
        {
            get
            {
                if (ExpiresAt == null)
                {
                    return null;
                }

                return ((DateTime)ExpiresAt).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"); // ISO 8601 (dropped seconds)
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    ExpiresAt = null;
                }
                else
                {
                    DateTime tempExpiresAtDate = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind); // ISO 8601
                    ExpiresAt = ((tempExpiresAtDate.Ticks == FileConstants.InvalidUtcTimeTicks
                            || (tempExpiresAtDate = tempExpiresAtDate.ToUniversalTime()).Ticks == FileConstants.InvalidUtcTimeTicks)
                        ? (Nullable<DateTime>)null
                        : tempExpiresAtDate.DropSubSeconds());
                }
            }
        }
        public Nullable<DateTime> ExpiresAt { get; set; }

        public long[] SyncboxIds { get; set; }

        #region ISerializable -David: Had to convert from DataContract to Serializable because parent object CredentialsSessionsResponse is Serializable
        
        // why does this work?? (called upon ReadObject on a JsonDataContractSerializer on a response stream)
        protected Session(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry toAdd in info)
            {
                switch (toAdd.Name)
                {
                    case CLDefinitions.RESTResponseSession_Key:
                        if (toAdd.ObjectType == typeof(string))
                        {
                            this.Key = (string)toAdd.Value;
                        }
                        else
                        {
                            throw new CLInvalidOperationException(
                                CLExceptionCode.General_Arguments,
                                string.Format(
                                    Resources.ExceptionSerializationInfoInvalidObjectType,
                                    CLDefinitions.RESTResponseSession_Key,
                                    typeof(string),
                                    toAdd.ObjectType));
                        }
                        break;

                    case CLDefinitions.RESTResponseSession_Secret:
                        if (toAdd.ObjectType == typeof(string))
                        {
                            this.Secret = (string)toAdd.Value;
                        }
                        else
                        {
                            throw new CLInvalidOperationException(
                                CLExceptionCode.General_Arguments,
                                string.Format(
                                    Resources.ExceptionSerializationInfoInvalidObjectType,
                                    CLDefinitions.RESTResponseSession_Secret,
                                    typeof(string),
                                    toAdd.ObjectType));
                        }
                        break;

                    case CLDefinitions.RESTResponseSession_Token:
                        if (toAdd.ObjectType == typeof(string))
                        {
                            this.Token = (string)toAdd.Value;
                        }
                        else
                        {
                            throw new CLInvalidOperationException(
                                CLExceptionCode.General_Arguments,
                                string.Format(
                                    Resources.ExceptionSerializationInfoInvalidObjectType,
                                    CLDefinitions.RESTResponseSession_Token,
                                    typeof(string),
                                    toAdd.ObjectType));
                        }
                        break;

                    case CLDefinitions.RESTRequestSession_Services:
                        object[] tempServices;
                        if (typeof(Service[]).IsAssignableFrom(toAdd.ObjectType))
                        {
                            this.Services = (Service[])toAdd.Value;
                        }
                        else if (typeof(object[]).IsAssignableFrom(toAdd.ObjectType)
                            && ((tempServices = (object[])toAdd.Value) == null
                                || tempServices.All(tempService => tempService == null || (tempService.GetType().IsCastableTo(typeof(Service))))))
                        {
                            if (tempServices == null)
                            {
                                this.Services = null;
                            }
                            else
                            {
                                this.Services = new Service[tempServices.Length];
                                for (int tempServiceIndex = 0; tempServiceIndex < tempServices.Length; tempServiceIndex++)
                                {
                                    this.Services[tempServiceIndex] = (Service)tempServices[tempServiceIndex];
                                }
                            }
                        }
                        else
                        {
                            throw new CLInvalidOperationException(
                                CLExceptionCode.General_Arguments,
                                string.Format(
                                    Resources.ExceptionSerializationInfoInvalidObjectType,
                                    CLDefinitions.RESTRequestSession_Services,
                                    typeof(Service[]),
                                    toAdd.ObjectType));
                        }
                        break;

                    case CLDefinitions.RESTResponseSession_ExpiresAt:
                        if (toAdd.ObjectType == typeof(string))
                        {
                            this.ExpiresAtString = (string)toAdd.Value;
                        }
                        else
                        {
                            throw new CLInvalidOperationException(
                                CLExceptionCode.General_Arguments,
                                string.Format(
                                    Resources.ExceptionSerializationInfoInvalidObjectType,
                                    CLDefinitions.RESTResponseSession_ExpiresAt,
                                    typeof(string),
                                    toAdd.ObjectType));
                        }
                        break;

                    case CLDefinitions.RESTResponseSession_SyncboxIds:
                        object[] tempSyncboxIds;
                        if (typeof(long[]).IsAssignableFrom(toAdd.ObjectType))
                        {
                            this.SyncboxIds = (long[])toAdd.Value;
                        }
                        else if (typeof(object[]).IsAssignableFrom(toAdd.ObjectType)
                            && ((tempSyncboxIds = (object[])toAdd.Value) == null
                                || tempSyncboxIds.All(tempSyncboxId => tempSyncboxId != null && tempSyncboxId.GetType().IsCastableTo(typeof(long)))))
                        {
                            if (tempSyncboxIds == null)
                            {
                                this.SyncboxIds = null;
                            }
                            else
                            {
                                this.SyncboxIds = new long[tempSyncboxIds.Length];
                                for (int tempSyncboxIdIndex = 0; tempSyncboxIdIndex < tempSyncboxIds.Length; tempSyncboxIdIndex++)
                                {
                                    this.SyncboxIds[tempSyncboxIdIndex] = Convert.ToInt64(tempSyncboxIds[tempSyncboxIdIndex]);
                                }
                            }
                        }
                        else
                        {
                            throw new CLInvalidOperationException(
                                CLExceptionCode.General_Arguments,
                                string.Format(
                                    Resources.ExceptionSerializationInfoInvalidObjectType,
                                    CLDefinitions.RESTResponseSession_SyncboxIds,
                                    typeof(long[]),
                                    toAdd.ObjectType));
                        }
                        break;
                }
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (this.Key != null)
            {
                info.AddValue(CLDefinitions.RESTResponseSession_Key, this.Key);
            }

            if (this.Secret != null)
            {
                info.AddValue(CLDefinitions.RESTResponseSession_Secret, this.Secret);
            }

            if (this.Token != null)
            {
                info.AddValue(CLDefinitions.RESTResponseSession_Token, this.Token);
            }

            if (this.Services != null)
            {
                info.AddValue(CLDefinitions.RESTRequestSession_Services, this.Services);
            }

            if (this.ExpiresAtString != null)
            {
                info.AddValue(CLDefinitions.RESTResponseSession_ExpiresAt, this.ExpiresAtString);
            }

            if (this.SyncboxIds != null)
            {
                info.AddValue(CLDefinitions.RESTResponseSession_SyncboxIds, this.SyncboxIds);
            }
        }

        #endregion
    }
}