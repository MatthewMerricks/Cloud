using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;

[assembly: CLSCompliant(true)]
namespace Microsoft.WebSolutionsPlatform.Event
{
    /// <summary>
    /// Base class for events. All events must inherit from this class.
    /// </summary>
    abstract public class Event
	{
        private static string baseVersion = @"2.0.0.0";

        private string originatingRouterName = string.Empty;
        /// <summary>
        /// Router that the event originated from
        /// </summary>
        public string OriginatingRouterName
        {
            get
            {
                if (originatingRouterName.Length == 0)
                {
                    originatingRouterName = Dns.GetHostName();
                }

                return originatingRouterName;
            }

            set
            {
                originatingRouterName = value;
            }
        }

        private string inRouterName = string.Empty;
        /// <summary>
        /// Router the event was passed from
        /// </summary>
        public string InRouterName
        {
            get
            {
                if (inRouterName.Length == 0)
                {
                    inRouterName = Dns.GetHostName();
                }

                return inRouterName;
            }

            set
            {
                inRouterName = value;
            }
        }

        private static Guid subscriptionEvent = new Guid(@"3D7B4317-C051-4e1a-8379-B6E2D6C107F9");
        /// <summary>
        /// Event type for a Subscription Event
        /// </summary>
        public static Guid SubscriptionEvent
        {
            get
            {
                return subscriptionEvent;
            }

            set
            {
                subscriptionEvent = value;
            }
        }

        private Guid eventType;
		/// <summary>
		/// Type of the event
		/// </summary>
        public Guid EventType
		{
			get
			{
				return eventType;
			}

			set
			{
				eventType = value;
			}
		}

		private Version eventVersion;
		/// <summary>
		/// Version of the event
		/// </summary>
		public Version EventVersion
		{
			get
			{
				return eventVersion;
			}

			set
			{
				eventVersion = value;
			}
		}

		private string eventName;
		/// <summary>
		/// Friendly name of the event
		/// </summary>
		public string EventName
		{
			get
			{
				return eventName;
			}

			set
			{
				eventName = value;
			}
		}

		private long eventTime;
		/// <summary>
		/// UTC time in ticks of when the event is published
		/// </summary>
		public long EventTime
		{
			get
			{
				return eventTime;
			}

            set
            {
                eventTime = value;
            }
        }

		private string eventPublisher;
		/// <summary>
		/// Friendly name of the event
		/// </summary>
		public string EventPublisher
		{
			get
			{
				return eventPublisher;
			}

			set
			{
				eventPublisher = value;
			}
		}

		private WspBuffer serializedEvent = null;
		/// <summary>
		/// Serialized version of the event
		/// </summary>
        public WspBuffer SerializedEvent
		{
			get
			{
				return serializedEvent;
			}

            set
            {
                serializedEvent = value;
            }
        }

		/// <summary>
		/// Base constructor to create a new event
		/// </summary>
        public Event()
		{
            InitializeEvent();
		}

		/// <summary>
		/// Base contructor to re-instantiate an existing event
		/// </summary>
        /// <param name="serializationData">Serialized event buffer</param>
        public Event(byte[] serializationData)
		{
            InitializeEvent();

            Deserialize(serializationData);
		}

		/// <summary>
		/// Initializes a new event object
		/// </summary>
        private void InitializeEvent()
		{
            eventType = Guid.Empty;
			eventVersion = new Version(baseVersion);
			eventName = string.Empty;
			eventTime = 0;
		}

        /// <summary>
        /// Generic method to set a property on an object.
        /// </summary>
        /// <param name="elementName">Property name to be set</param>
        /// <param name="elementValue">Value object</param>
        /// <returns>true if success and false if failed</returns>
        virtual public bool SetElement(string elementName, object elementValue)
        {
            bool rc = true;
            PropertyInfo prop;

            prop = this.GetType().GetProperty(elementName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (prop == null)
            {
                rc = false;
            }
            else
            {
                try
                {
                    prop.SetValue(this, elementValue, null);
                }
                catch
                {
                    rc = false;
                }
            }

            return rc;
        }

		/// <summary>
		/// Serializes the event and puts it in the SerializedEvent property
		/// </summary>
        /// <returns>Serialized version of the event</returns>
		public byte[] Serialize()
		{
            if (serializedEvent == null)
            {
                serializedEvent = new WspBuffer();
            }

            serializedEvent.Reset();

            this.eventTime = DateTime.UtcNow.Ticks;

            serializedEvent.Write(OriginatingRouterName);
            serializedEvent.Write(InRouterName);
            serializedEvent.Write(EventType);
            serializedEvent.AddElement(@"EventBaseVersion", baseVersion);
            serializedEvent.AddElement(@"EventType", eventType);
            serializedEvent.AddElement(@"EventVersion", eventVersion);
            serializedEvent.AddElement(@"EventName", eventName);
            serializedEvent.AddElement(@"EventTime", eventTime);
            serializedEvent.AddElement(@"EventPublisher", System.AppDomain.CurrentDomain.FriendlyName);

            GetObjectData(serializedEvent);

            return serializedEvent.ToByteArray();
		}

        /// <summary>
        /// Used for event serialization.
        /// </summary>
        /// <param name="buffer">SerializationData object passed to store serialized object</param>
        abstract public void GetObjectData(WspBuffer buffer);

		/// <summary>
		/// Deserializes the event
		/// </summary>
		public virtual void Deserialize( byte[] serializationData )
		{
            string propName;
            byte propType;

            string stringValue = string.Empty;
            byte byteValue = 0;
            SByte sbyteValue = 0;
            byte[] byteArrayValue = null;
            char charValue = Char.MinValue;
            char[] charArrayValue = null;
            bool boolValue = false;
            Int16 int16Value = 0;
            Int32 int32Value = 0;
            Int64 int64Value = 0;
            UInt16 uint16Value = 0;
            UInt32 uint32Value = 0;
            UInt64 uint64Value = 0;
            Single singleValue = 0;
            Double doubleValue = 0;
            Decimal decimalValue = 0;
            Version versionValue = null;
            DateTime dateTimeValue = DateTime.MinValue;
            Guid guidValue = Guid.Empty;
            IPAddress ipAddressValue = null;
            Uri uriValue = null;
            Dictionary<string, string> stringDictionaryValue = null;
            Dictionary<string, object> objectDictionaryValue = null;
            List<string> stringListValue = null;
            List<object> objectListValue = null;

            serializedEvent = new WspBuffer(serializationData);

            if (serializedEvent.GetHeader(out originatingRouterName, out inRouterName, out eventType) == false)
            {
                throw new EventDeserializationException("Error reading OriginatingRouterName from serializedEvent");
            }

            while (serializedEvent.Position < serializedEvent.Size)
            {
                if (serializedEvent.Read(out propName) == false)
                {
                    throw new EventDeserializationException("Error reading PropertyName from buffer");
                }

                if (serializedEvent.Read(out propType) == false)
                {
                    throw new EventDeserializationException("Error reading PropertyType from buffer");
                }

                switch (propType)
                {
                    case (byte)PropertyType.String:
                        if (serializedEvent.Read(out stringValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        if (propName == @"EventBaseVersion")
                        {
                            baseVersion = stringValue;

                            continue;
                        }

                        if (propName == @"EventName")
                        {
                            eventName = stringValue;

                            continue;
                        }

                        if (propName == @"EventPublisher")
                        {
                            eventPublisher = stringValue;

                            continue;
                        }

                        SetElement(propName, stringValue);

                        continue;

                    case (byte)PropertyType.Boolean:
                        if (serializedEvent.Read(out boolValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, boolValue);

                        continue;

                    case (byte)PropertyType.Int32:
                        if (serializedEvent.Read(out int32Value) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, int32Value);

                        continue;

                    case (byte)PropertyType.Int64:
                        if (serializedEvent.Read(out int64Value) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        if (propName == @"EventTime")
                        {
                            eventTime = int64Value;

                            continue;
                        }

                        SetElement(propName, int64Value);

                        continue;

                    case (byte)PropertyType.SByte:
                        if (serializedEvent.Read(out sbyteValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, sbyteValue);

                        continue;

                    case (byte)PropertyType.Double:
                        if (serializedEvent.Read(out doubleValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, doubleValue);

                        continue;

                    case (byte)PropertyType.Decimal:
                        if (serializedEvent.Read(out decimalValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, decimalValue);

                        continue;

                    case (byte)PropertyType.Byte:
                        if (serializedEvent.Read(out byteValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, byteValue);

                        continue;

                    case (byte)PropertyType.Char:
                        if (serializedEvent.Read(out charValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, charValue);

                        continue;

                    case (byte)PropertyType.Version:
                        if (serializedEvent.Read(out versionValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        if (propName == @"EventVersion")
                        {
                            eventVersion = versionValue;

                            continue;
                        }

                        SetElement(propName, versionValue);

                        continue;

                    case (byte)PropertyType.DateTime:
                        if (serializedEvent.Read(out dateTimeValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, dateTimeValue);

                        continue;

                    case (byte)PropertyType.Guid:
                        if (serializedEvent.Read(out guidValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        if (propName == @"EventType")
                        {
                            eventType = guidValue;

                            continue;
                        }

                        SetElement(propName, guidValue);

                        continue;

                    case (byte)PropertyType.Uri:
                        if (serializedEvent.Read(out uriValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, uriValue);

                        continue;

                    case (byte)PropertyType.Int16:
                        if (serializedEvent.Read(out int16Value) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, int16Value);

                        continue;

                    case (byte)PropertyType.Single:
                        if (serializedEvent.Read(out singleValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, singleValue);

                        continue;

                    case (byte)PropertyType.UInt16:
                        if (serializedEvent.Read(out uint16Value) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, uint16Value);

                        continue;

                    case (byte)PropertyType.UInt32:
                        if (serializedEvent.Read(out uint32Value) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, uint32Value);

                        continue;

                    case (byte)PropertyType.UInt64:
                        if (serializedEvent.Read(out uint64Value) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, uint64Value);

                        continue;

                    case (byte)PropertyType.IPAddress:
                        if (serializedEvent.Read(out ipAddressValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, ipAddressValue);

                        continue;

                    case (byte)PropertyType.ByteArray:
                        if (serializedEvent.Read(out byteArrayValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, byteArrayValue);

                        continue;

                    case (byte)PropertyType.CharArray:
                        if (serializedEvent.Read(out charArrayValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, charArrayValue);

                        continue;

                    case (byte)PropertyType.StringDictionary:
                        if (serializedEvent.Read(out stringDictionaryValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, stringDictionaryValue);

                        continue;

                    case (byte)PropertyType.ObjectDictionary:
                        if (serializedEvent.Read(out objectDictionaryValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, objectDictionaryValue);

                        continue;

                    case (byte)PropertyType.StringList:
                        if (serializedEvent.Read(out stringListValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, stringListValue);

                        continue;

                    case (byte)PropertyType.ObjectList:
                        if (serializedEvent.Read(out objectListValue) == false)
                        {
                            throw new EventDeserializationException("Error reading PropertyType from buffer");
                        }

                        SetElement(propName, objectListValue);

                        continue;

                    default:
                        ResourceManager rm = new ResourceManager("WspEvent.WspEvent", Assembly.GetExecutingAssembly());

                        throw new EventTypeNotSupportedException(rm.GetString("CannotDeserialize"));
                }
            }
		}

        /// <summary>
        /// Method returns the event's header properties.
        /// </summary>
        /// <param name="buffer">Serialized event buffer</param>
        /// <param name="originatingRouterName">Machine were event originated from</param>
        /// <param name="inRouterName">Machine which passed the event to this machine</param>
        /// <param name="eventType">Event type</param>
        /// <returns>Number of bytes of the buffer which was the header</returns>
        public static int GetHeader(byte[] buffer, out string originatingRouterName, out string inRouterName, out Guid eventType)
        {
            byte byteIn;
            Int32 stringLength = 0;
            Int32 shiftBits = 0;
            Int32 position = 0;

            do
            {
                byteIn = buffer[position++];

                stringLength |= (byteIn & 0x7f) << shiftBits;

                shiftBits += 7;
            } while ((byteIn & 0x80) != 0);

            if (stringLength > 0)
            {
                originatingRouterName = Encoding.UTF8.GetString(buffer, position, stringLength);
            }
            else
            {
                originatingRouterName = string.Empty;
            }

            position = position + stringLength;

            stringLength = 0;
            shiftBits = 0;

            do
            {
                byteIn = buffer[position++];

                stringLength |= (byteIn & 0x7f) << shiftBits;

                shiftBits += 7;
            } while ((byteIn & 0x80) != 0);

            if (stringLength > 0)
            {
                inRouterName = Encoding.UTF8.GetString(buffer, position, stringLength);
            }
            else
            {
                inRouterName = string.Empty;
            }

            position = position + stringLength;

            stringLength = 0;
            shiftBits = 0;

            do
            {
                byteIn = buffer[position++];

                stringLength |= (byteIn & 0x7f) << shiftBits;

                shiftBits += 7;
            } while ((byteIn & 0x80) != 0);

            if (stringLength > 0)
            {
                eventType = new Guid(Encoding.UTF8.GetString(buffer, position, stringLength));
            }
            else
            {
                eventType = Guid.Empty;
            }

            position = position + stringLength;

            return position;
        }
	}
}
