//
// Events.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WebSolutionsPlatform.Event;

/// <summary>
/// Defines the PubSub events used in the badging subsystem.
/// </summary>
namespace BadgeNET.PubSubEvents
{
     public static class EventIds
     {
         public static Guid kEvent_BadgeCom_Initialized = new Guid(@"1F3EB44D-8E7F-4274-9FA9-69AA11DBFE9B");
         public static Guid kEvent_BadgeNet_AddSyncBoxFolderPath = new Guid(@"88416295-AB4E-44B9-838D-D730C3755000");
         public static Guid kEvent_BadgeNet_RemoveSyncBoxFolderPath = new Guid(@"60B884CD-5289-4D0F-BDC3-12AD96BB6CDF");
         public static Guid kEvent_BadgeNet_AddBadgePath = new Guid(@"CF325634-1B3E-44EC-BCBF-9C2964BA754C");
         public static Guid kEvent_BadgeNet_RemoveBadgePath = new Guid(@"19CC5C92-D9C2-4718-BC50-ED6211F87423");
     }


    /// <summary>
    /// This is the Event published by BadgeCom to inform BadgeNet that a new BadgeCom instance has been initialized.
    /// </summary>
    public class BadgeCom_Initialized : Event
    {

        /// <summary>
        /// The ID of the process (Explorer) containing the BadgeCom instance that published this event.
        /// </summary>
        public int ProcessId
        {
            get
            {
                return _processId;
            }

            set
            {
                _processId = value;
            }
        }
        private int _processId;

        /// <summary>
        /// The ID of the BadgeCom instance thread that published this event.
        /// </summary>
        public int ThreadId
        {
            get
            {
                return _threadId;
            }
            set
            {
                _threadId = value;
            }
        }
        private int _threadId;

        /// <summary>
        /// The type of badge icon processed by the BadgeCom instance that published this event.
        /// This is a value as specified by the cloudAppIconBadgeType enum.
        /// </summary>
        public int BadgeType
        {
            get
            {
                return _badgeType;
            }

            set
            {
                _badgeType = value;
            }
        }
        private int _badgeType;

		/// <summary>
		/// Base constructor to create a new event
		/// </summary>
        public BadgeCom_Initialized()
            : base()
		{
            EventType = EventIds.kEvent_BadgeCom_Initialized;
		}

        /// <summary>
        /// Base constructor to create a new event from a serialized event
        /// </summary>
        /// <param name="serializationData">Serialized event buffer</param>
        public BadgeCom_Initialized(byte[] serializationData)
            : base(serializationData)
        {
            EventType = EventIds.kEvent_BadgeCom_Initialized;
        }
  
		/// <summary>
		/// Used for event serialization.
		/// </summary>
		/// <param name="buffer">SerializationData object passed to store serialized object</param>
        public override void GetObjectData(WspBuffer buffer)
        {
            buffer.AddElement(@"ProcessId", ProcessId);
            buffer.AddElement(@"ThreadId", ThreadId);
            buffer.AddElement(@"BadgeType", BadgeType);
        }

        /// <summary>
        /// Set values on object during deserialization
        /// </summary>
        /// <param name="elementName">Name of property</param>
        /// <param name="elementValue">Value of property</param>
        /// <returns></returns>
        public override bool SetElement(string elementName, object elementValue)
        {
            switch (elementName)
            {
                case "ProcessId":
                    ProcessId = (int)elementValue;
                    break;

                case "ThreadId":
                    ThreadId = (int)elementValue;
                    break;

                case "BadgeType":
                    BadgeType = (int)elementValue;
                    break;

                default:
                    base.SetElement(elementName, elementValue);
                    break;
            }

            return true;
        }
    }

    /// <summary>
    /// This is the Event published by BadgeNet to inform BadgeCom that a new SyncBox has been added.
    /// </summary>
    public class BadgeNet_AddSyncBoxFolderPath : Event
    {
        /// <summary>
        /// The process ID of the BadgeNet process that published this event.
        /// </summary>
        public int ProcessId
        {
            get
            {
                return _processId;
            }

            set
            {
                _processId = value;
            }
        }
        private int _processId;

        /// <summary>
        /// The thread ID of the BadgeNet thread that published this event.
        /// </summary>
        public int ThreadId
        {
            get
            {
                return _threadId;
            }
            set
            {
                _threadId = value;
            }
        }
        private int _threadId;

        /// <summary>
        /// The full path on the local drive of the folder containing the SyncBox.
        /// </summary>
        public string SyncBoxFolderFullPath
        {
            get
            {
                return _syncBoxFolderFullPath;
            }

            set
            {
                _syncBoxFolderFullPath = value;
            }
        }
        private string _syncBoxFolderFullPath;

        /// <summary>
        /// Base constructor to create a new event
        /// </summary>
        public BadgeNet_AddSyncBoxFolderPath()
            : base()
        {
            EventType = EventIds.kEvent_BadgeNet_AddSyncBoxFolderPath;
        }

        /// <summary>
        /// Base constructor to create a new event from a serialized event
        /// </summary>
        /// <param name="serializationData">Serialized event buffer</param>
        public BadgeNet_AddSyncBoxFolderPath(byte[] serializationData)
            : base(serializationData)
        {
            EventType = EventIds.kEvent_BadgeNet_AddSyncBoxFolderPath;
        }

        /// <summary>
        /// Used for event serialization.
        /// </summary>
        /// <param name="buffer">SerializationData object passed to store serialized object</param>
        public override void GetObjectData(WspBuffer buffer)
        {
            buffer.AddElement(@"ProcessId", ProcessId);
            buffer.AddElement(@"ThreadId", ThreadId);
            buffer.AddElement(@"SyncBoxFolderFullPath", SyncBoxFolderFullPath);
        }

        /// <summary>
        /// Set values on object during deserialization
        /// </summary>
        /// <param name="elementName">Name of property</param>
        /// <param name="elementValue">Value of property</param>
        /// <returns></returns>
        public override bool SetElement(string elementName, object elementValue)
        {
            switch (elementName)
            {
                case "ProcessId":
                    ProcessId = (int)elementValue;
                    break;

                case "ThreadId":
                    ThreadId = (int)elementValue;
                    break;

                case "SyncBoxFolderFullPath":
                    SyncBoxFolderFullPath = (string)elementValue;
                    break;

                default:
                    base.SetElement(elementName, elementValue);
                    break;
            }

            return true;
        }
    }

    /// <summary>
    /// This is the Event published by BadgeNet to inform BadgeCom that a new SyncBox has been removed.
    /// </summary>
    public class BadgeNet_RemoveSyncBoxFolderPath : Event
    {
        /// <summary>
        /// The process ID of the BadgeNet process that published this event.
        /// </summary>
        public int ProcessId
        {
            get
            {
                return _processId;
            }

            set
            {
                _processId = value;
            }
        }
        private int _processId;

        /// <summary>
        /// The thread ID of the BadgeNet thread that published this event.
        /// </summary>
        public int ThreadId
        {
            get
            {
                return _threadId;
            }
            set
            {
                _threadId = value;
            }
        }
        private int _threadId;

        /// <summary>
        /// The full path on the local drive of the folder containing the SyncBox.
        /// </summary>
        public string SyncBoxFolderFullPath
        {
            get
            {
                return _syncBoxFolderFullPath;
            }

            set
            {
                _syncBoxFolderFullPath = value;
            }
        }
        private string _syncBoxFolderFullPath;

        /// <summary>
        /// Base constructor to create a new event
        /// </summary>
        public BadgeNet_RemoveSyncBoxFolderPath()
            : base()
        {
            EventType = EventIds.kEvent_BadgeNet_RemoveSyncBoxFolderPath;
        }

        /// <summary>
        /// Base constructor to create a new event from a serialized event
        /// </summary>
        /// <param name="serializationData">Serialized event buffer</param>
        public BadgeNet_RemoveSyncBoxFolderPath(byte[] serializationData)
            : base(serializationData)
        {
            EventType = EventIds.kEvent_BadgeNet_RemoveSyncBoxFolderPath;
        }

        /// <summary>
        /// Used for event serialization.
        /// </summary>
        /// <param name="buffer">SerializationData object passed to store serialized object</param>
        public override void GetObjectData(WspBuffer buffer)
        {
            buffer.AddElement(@"ProcessId", ProcessId);
            buffer.AddElement(@"ThreadId", ThreadId);
            buffer.AddElement(@"SyncBoxFolderFullPath", SyncBoxFolderFullPath);
        }

        /// <summary>
        /// Set values on object during deserialization
        /// </summary>
        /// <param name="elementName">Name of property</param>
        /// <param name="elementValue">Value of property</param>
        /// <returns></returns>
        public override bool SetElement(string elementName, object elementValue)
        {
            switch (elementName)
            {
                case "ProcessId":
                    ProcessId = (int)elementValue;
                    break;

                case "ThreadId":
                    ThreadId = (int)elementValue;
                    break;

                case "SyncBoxFolderFullPath":
                    SyncBoxFolderFullPath = (string)elementValue;
                    break;

                default:
                    base.SetElement(elementName, elementValue);
                    break;
            }

            return true;
        }
    }

    /// <summary>
    /// This is the Event published by BadgeNet to inform BadgeCom that a new badge file/folder path has been added, with the badge icon type to display at that location.
    /// </summary>
    public class BadgeNet_AddBadgePath : Event
    {
        /// <summary>
        /// The process ID of the BadgeNet process that published this event.
        /// </summary>
        public int ProcessId
        {
            get
            {
                return _processId;
            }

            set
            {
                _processId = value;
            }
        }
        private int _processId;

        /// <summary>
        /// The thread ID of the BadgeNet thread that published this event.
        /// </summary>
        public int ThreadId
        {
            get
            {
                return _threadId;
            }
            set
            {
                _threadId = value;
            }
        }
        private int _threadId;

        /// <summary>
        /// The full path to badge.
        /// </summary>
        public string BadgeFullPath
        {
            get
            {
                return _badgeFullPath;
            }

            set
            {
                _badgeFullPath = value;
            }
        }
        private string _badgeFullPath;

        /// <summary>
        /// The type of badge icon processed by the BadgeCom instance that published this event.
        /// This is a value as specified by the cloudAppIconBadgeType enum.
        /// </summary>
        public int BadgeType
        {
            get
            {
                return _badgeType;
            }

            set
            {
                _badgeType = value;
            }
        }
        private int _badgeType;

        /// <summary>
        /// Base constructor to create a new event
        /// </summary>
        public BadgeNet_AddBadgePath()
            : base()
        {
            EventType = EventIds.kEvent_BadgeNet_AddBadgePath;
        }

        /// <summary>
        /// Base constructor to create a new event from a serialized event
        /// </summary>
        /// <param name="serializationData">Serialized event buffer</param>
        public BadgeNet_AddBadgePath(byte[] serializationData)
            : base(serializationData)
        {
            EventType = EventIds.kEvent_BadgeNet_AddBadgePath;
        }

        /// <summary>
        /// Used for event serialization.
        /// </summary>
        /// <param name="buffer">SerializationData object passed to store serialized object</param>
        public override void GetObjectData(WspBuffer buffer)
        {
            buffer.AddElement(@"ProcessId", ProcessId);
            buffer.AddElement(@"ThreadId", ThreadId);
            buffer.AddElement(@"BadgeFullPath", BadgeFullPath);
            buffer.AddElement(@"BadgeType", BadgeType);
        }

        /// <summary>
        /// Set values on object during deserialization
        /// </summary>
        /// <param name="elementName">Name of property</param>
        /// <param name="elementValue">Value of property</param>
        /// <returns></returns>
        public override bool SetElement(string elementName, object elementValue)
        {
            switch (elementName)
            {
                case "ProcessId":
                    ProcessId = (int)elementValue;
                    break;

                case "ThreadId":
                    ThreadId = (int)elementValue;
                    break;

                case "SyncBoxFolderFullPath":
                    BadgeFullPath = (string)elementValue;
                    break;

                case "BadgeType":
                    BadgeType = (int)elementValue;
                    break;

                default:
                    base.SetElement(elementName, elementValue);
                    break;
            }

            return true;
        }
    }

    /// <summary>
    /// This is the Event published by BadgeNet to inform BadgeCom that a new badge file/folder path has been removed, with the badge icon type being removed at that location.
    /// </summary>
    public class BadgeNet_RemoveBadgePath : Event
    {
        /// <summary>
        /// The process ID of the BadgeNet process that published this event.
        /// </summary>
        public int ProcessId
        {
            get
            {
                return _processId;
            }

            set
            {
                _processId = value;
            }
        }
        private int _processId;

        /// <summary>
        /// The thread ID of the BadgeNet thread that published this event.
        /// </summary>
        public int ThreadId
        {
            get
            {
                return _threadId;
            }
            set
            {
                _threadId = value;
            }
        }
        private int _threadId;

        /// <summary>
        /// The full path to badge.
        /// </summary>
        public string BadgeFullPath
        {
            get
            {
                return _badgeFullPath;
            }

            set
            {
                _badgeFullPath = value;
            }
        }
        private string _badgeFullPath;

        /// <summary>
        /// The type of badge icon processed by the BadgeCom instance that published this event.
        /// This is a value as specified by the cloudAppIconBadgeType enum.
        /// </summary>
        public int BadgeType
        {
            get
            {
                return _badgeType;
            }

            set
            {
                _badgeType = value;
            }
        }
        private int _badgeType;

        /// <summary>
        /// Base constructor to create a new event
        /// </summary>
        public BadgeNet_RemoveBadgePath()
            : base()
        {
            EventType = EventIds.kEvent_BadgeNet_RemoveBadgePath;
        }

        /// <summary>
        /// Base constructor to create a new event from a serialized event
        /// </summary>
        /// <param name="serializationData">Serialized event buffer</param>
        public BadgeNet_RemoveBadgePath(byte[] serializationData)
            : base(serializationData)
        {
            EventType = EventIds.kEvent_BadgeNet_RemoveBadgePath;
        }

        /// <summary>
        /// Used for event serialization.
        /// </summary>
        /// <param name="buffer">SerializationData object passed to store serialized object</param>
        public override void GetObjectData(WspBuffer buffer)
        {
            buffer.AddElement(@"ProcessId", ProcessId);
            buffer.AddElement(@"ThreadId", ThreadId);
            buffer.AddElement(@"BadgeFullPath", BadgeFullPath);
            buffer.AddElement(@"BadgeType", BadgeType);
        }

        /// <summary>
        /// Set values on object during deserialization
        /// </summary>
        /// <param name="elementName">Name of property</param>
        /// <param name="elementValue">Value of property</param>
        /// <returns></returns>
        public override bool SetElement(string elementName, object elementValue)
        {
            switch (elementName)
            {
                case "ProcessId":
                    ProcessId = (int)elementValue;
                    break;

                case "ThreadId":
                    ThreadId = (int)elementValue;
                    break;

                case "SyncBoxFolderFullPath":
                    BadgeFullPath = (string)elementValue;
                    break;

                case "BadgeType":
                    BadgeType = (int)elementValue;
                    break;

                default:
                    base.SetElement(elementName, elementValue);
                    break;
            }

            return true;
        }
    }
}
