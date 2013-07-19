//
// Service.cs
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
    /// Contains actual HTTP response fields, representing a service.
    /// </summary>
    [Obfuscation(Exclude = true)]
    [Serializable] // -David had to convert from DataContract to Serializable because json value for SyncboxIds can be a string or an array of long
    [KnownType(typeof(long[]))]
    public sealed class Service : ISerializable // -David had to convert from DataContract to Serializable because json value for SyncboxIds can be a string or an array of long
    {
        public string ServiceType { get; set; }

        /// <summary>
        /// The list of syncbox IDs included in this service.  If this field is null, all syncbox IDs are included.
        /// </summary>
        public long[] SyncboxIds { get; set; }

        // have to define a public constructor explicitly because this object is ISerializable (and thus requires a specific type of protected constructor)
        public Service() { }

        #region ISerializable members -David had to convert from DataContract to Serializable because json value for SyncboxIds can be a string or an array of long

        // why does this work?? (called upon ReadObject on a JsonDataContractSerializer on a response stream)
        protected Service(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry toAdd in info)
            {
                switch (toAdd.Name)
                {
                    case CLDefinitions.JsonServiceType:
                        if (toAdd.ObjectType == typeof(string))
                        {
                            this.ServiceType = (string)toAdd.Value;
                        }
                        else
                        {
                            throw new CLInvalidOperationException(
                                CLExceptionCode.General_Arguments,
                                string.Format(
                                    Resources.ExceptionSerializationInfoInvalidObjectType,
                                    CLDefinitions.JsonServiceType,
                                    typeof(string),
                                    toAdd.ObjectType));
                        }
                        break;

                    case CLDefinitions.RESTResponseSession_SyncboxIds:
                        object[] tempSyncboxIds;
                        if (toAdd.ObjectType == typeof(string))
                        {
                            if (string.Equals(((string)toAdd.Value), CLDefinitions.RESTRequestSession_SyncboxIdsAll, StringComparison.OrdinalIgnoreCase))
                            {
                                this.SyncboxIds = null;
                            }
                            else
                            {
                                this.SyncboxIds = new long[0];
                            }
                        }
                        else if (typeof(long[]).IsAssignableFrom(toAdd.ObjectType))
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
            if (this.ServiceType != null)
            {
                info.AddValue(CLDefinitions.JsonServiceType, this.ServiceType);
            }

            if (this.SyncboxIds != null)
            {
                info.AddValue(CLDefinitions.RESTResponseSession_SyncboxIds, this.SyncboxIds);
            }
        }

        #endregion
    }
}