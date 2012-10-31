using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Net;
using System.Security.Cryptography;

namespace Microsoft.WebSolutionsPlatform.Event
{
    /// <summary>
    /// The Subscription class defines the subscription objects which are published 
    /// when an application subscribes to event types.
    /// </summary>
	public class Subscription : Event
	{
		private Guid subscriptionId;
		/// <summary>
		/// ID for subscription
		/// </summary>
        public Guid SubscriptionId
		{
			get
			{
                return subscriptionId;
			}
			set
			{
                subscriptionId = value;
			}
		}

		private Guid subscriptionEventType;
		/// <summary>
		/// Event registering/unregistering for
		/// </summary>
        public Guid SubscriptionEventType
		{
			get
			{
                return subscriptionEventType;
			}
			set
			{
                subscriptionEventType = value;
			}
		}

        private bool subscribe;
        /// <summary>
        /// Subscribe is true; Unsubscribe is false
        /// </summary>
        public bool Subscribe
        {
            get
            {
                return subscribe;
            }
            set
            {
                subscribe = value;
            }
        }

		private bool localOnly;
		/// <summary>
		/// Register for the event only on the local machine
		/// </summary>
		public bool LocalOnly
		{
			get
			{
				return localOnly;
			}
			set
			{
				localOnly = value;
			}
		}

		/// <summary>
		/// Base constructor to create a new subscription event
		/// </summary>
        public Subscription() :
            base()
		{
            EventType = Event.SubscriptionEvent;
			EventVersion = new Version(@"2.0.0.0");
            subscribe = true;
		}

		/// <summary>
		/// Used for event serialization.
		/// </summary>
		/// <param name="buffer">SerializationData object passed to store serialized object</param>
		public override void GetObjectData( WspBuffer buffer )
		{
            buffer.AddElement(@"SubscriptionId", subscriptionId);
            buffer.AddElement(@"SubscriptionEventType", subscriptionEventType);
            buffer.AddElement(@"Subscribe", subscribe);
            buffer.AddElement(@"LocalOnly", localOnly);
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
                case "SubscriptionId":
                    SubscriptionId = (Guid)elementValue;
                    break;

                case "SubscriptionEventType":
                    SubscriptionEventType = (Guid)elementValue;
                    break;

                case "Subscribe":
                    Subscribe = (bool)elementValue;
                    break;

                case "LocalOnly":
                    LocalOnly = (bool)elementValue;
                    break;

                default:
                    base.SetElement(elementName, elementValue);
                    break;
            }

            return true;
        }
    }
}
