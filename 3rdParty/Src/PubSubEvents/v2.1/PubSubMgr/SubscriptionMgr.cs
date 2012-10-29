using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Net;
using System.Resources;
using System.Reflection;
using Microsoft.WebSolutionsPlatform.Common;
using Microsoft.WebSolutionsPlatform.Event;

namespace Microsoft.WebSolutionsPlatform.Event.PubSubManager
{
    /// <summary>
    /// The ISubscriptionCallback interface is implemented by an event subscriber class. The
    /// SubscriptionCallback method is then called to deliver events to the application.
    /// </summary>
    public interface ISubscriptionCallback
    {
        /// <summary>
        /// This method is passed to the SubscriptionManager as the callback for delivering events.
        /// </summary>
        /// <param name="eventType">Event type for the event being passed.</param>
        /// <param name="serializedEvent">The serialized version of the event.</param>
        void SubscriptionCallback(Guid eventType, byte[] serializedEvent);
    }

    internal struct StateInfo
    {
        public object callBack;
        public Guid eventType;
        public byte[] serializedEvent;
    }

    /// <summary>
    /// This class is used to subscribe to events.
    /// </summary>
    public class SubscriptionManager : IDisposable
    {
        private bool disposed;

        private static UInt32 defaultEventTimeout = 10000;
        private static uint subscriptionRefreshIncrement = 3; // in minutes

        private static PublishManager publishMgr;
        private static int publishMgrRefCount = 0;

        private static object lockObject = new object();

        private string eventQueueName = @"WspEventQueue";
        private SharedQueue eventQueue;

        private Dictionary<Guid, Subscription> subscriptions;

        private Thread listenThread;

        /// <summary>
        /// Defines the callback method for delivering events to an application.
        /// </summary>
        /// <param name="eventType">Event type for the event being passed.</param>
        /// <param name="serializedEvent">The serialized version of the event.</param>
        public delegate void Callback(Guid eventType, byte[] serializedEvent);

        private bool listenForEvents;
        /// <summary>
        /// Starts and stops listening for events
        /// </summary>
        public bool ListenForEvents
        {
            get
            {
                return listenForEvents;
            }
            set
            {
                if(value == true)
                {
                    if(listenForEvents == false)
                    {
                        StartListening();
                    }
                }
                else
                {
                    StopListening();
                }

                listenForEvents = value;
            }
        }

        private UInt32 timeout;
        /// <summary>
        /// Timeout for publishing events
        /// </summary>
        [CLSCompliant(false)]
        public UInt32 Timeout
        {
            get
            {
                return timeout;
            }
            set
            {
                timeout = value;
            }
        }

        private Callback callbackMethod;
        /// <summary>
        /// Callback delegate
        /// </summary>
        public Callback CallbackMethod
        {
            get
            {
                return callbackMethod;
            }
        }

        /// <summary>
        /// Size in bytes of the SharedQueue
        /// </summary>
        [CLSCompliant(false)]
        public UInt32 QueueSize
        {
            get
            {
                if (eventQueue == null)
                {
                    return 0;
                }

                return eventQueue.QueueSize;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="subscriptionCallback">Handle to the SubscriptionCallback method</param>
        public SubscriptionManager(object subscriptionCallback)
            : this(defaultEventTimeout, subscriptionCallback)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="timeout">Timeout for publishing an event</param>
        /// <param name="subscriptionCallback">Handle to the SubscriptionCallback method</param>
        [CLSCompliant(false)]
        public SubscriptionManager(UInt32 timeout, object subscriptionCallback)
        {
            this.timeout = timeout;
            this.callbackMethod = new Callback((Callback)subscriptionCallback);

            try
            {
                lock (lockObject)
                {
                    publishMgrRefCount++;

                    if (publishMgr == null)
                    {
                        publishMgr = new PublishManager(timeout);
                    }
                }

                subscriptions = new Dictionary<Guid, Subscription>();

                eventQueue = new SharedQueue(eventQueueName, 100);

                if (eventQueue == null)
                {
                    ResourceManager rm = new ResourceManager("PubSubMgr.PubSubMgr", Assembly.GetExecutingAssembly());

                    throw new PubSubConnectionFailedException(rm.GetString("ConnectionFailed"));
                }

                listenForEvents = true;
                StartListening();
            }
            catch (SharedQueueDoesNotExistException e)
            {
                throw new PubSubQueueDoesNotExistException(e.Message, e.InnerException);
            }
            catch (SharedQueueInsufficientMemoryException e)
            {
                throw new PubSubInsufficientMemoryException(e.Message, e.InnerException);
            }
            catch (SharedQueueInitializationException e)
            {
                throw new PubSubInitializationException(e.Message, e.InnerException);
            }
        }

        /// <summary>
        /// Dispose for SubscriptionManager
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dispose for SubscriptionManager
        /// </summary>
        /// <param name="disposing">True if disposing.</param>
        protected virtual void Dispose(bool disposing) 
        {
            if (!disposed)
            {
                try
                {
                    this.StopListening();

                    Guid[] keys = GetSubscriptions();

                    foreach (Guid subId in keys)
                    {
                        RemoveSubscription(subId);
                    }

                    eventQueue.Dispose();
                    eventQueue = null;


                    lock (lockObject)
                    {
                        publishMgrRefCount--;

                        if (publishMgrRefCount == 0)
                        {
                            publishMgr.Dispose();
                            publishMgr = null;
                        }
                    }
                }

                catch
                {
                    // intentionally left empty
                }

                finally
                {
                    disposed = true;

                    if (disposing)
                    {
                        GC.SuppressFinalize(this);
                    }
                }
            }
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~SubscriptionManager()
        {
            Dispose(false);
        }

        /// <summary>
        /// Add a subscription for a specific event
        /// </summary>
        /// <param name="eventType">EventType being subscribed to</param>
        /// <param name="localOnly">Specifies if subscription is only for local machine or global</param>
        public void AddSubscription(Guid eventType, bool localOnly)
        {
            Subscription subscription = new Subscription();
            subscription.SubscriptionEventType = eventType;
            subscription.SubscriptionId = Guid.NewGuid();
            subscription.Subscribe = true;
            subscription.LocalOnly = localOnly;

            publishMgr.Publish(subscription.Serialize());

            subscriptions[eventType] = subscription;
        }

        /// <summary>
        /// Remove a subscription for a specific event
        /// </summary>
        /// <param name="eventType">EventType being unsubscribed to</param>
        /// <returns>True if successful</returns>
        public bool RemoveSubscription(Guid eventType)
        {
            Subscription subscription = null;

            if (subscriptions.TryGetValue(eventType, out subscription) == true)
            {
                subscription.Subscribe = false;
                publishMgr.Publish(subscription.Serialize());
                return subscriptions.Remove(eventType);
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Returns a list of EventTypes being subscribed to
        /// </summary>
        /// <returns>An array of EventTypes</returns>
        public Guid[] GetSubscriptions()
        {
            Guid[] keys = new Guid[subscriptions.Count];
            subscriptions.Keys.CopyTo(keys, 0);

            return keys;
        }

        /// <summary>
        /// Starts a thread to listen to events
        /// </summary>
        private void StartListening()
        {
            listenThread = new Thread(new ThreadStart(Listen));

            listenThread.Start();
        }

        /// <summary>
        /// Stops the thread listening to events
        /// </summary>
        private void StopListening()
        {
            if (listenThread != null)
            {
                try
                {
                    listenThread.Abort();
                }
                catch
                {
                }

                listenThread = null;
            }
        }

        /// <summary>
        /// Listening thread
        /// </summary>
        private void Listen()
        {
            Guid eventType;
            string originatingRouterName;
            string localRouterName;
            string inRouterName;
            bool elementRetrieved;
            DateTime nextPushSubscriptions = DateTime.UtcNow.AddMinutes(subscriptionRefreshIncrement);
            Subscription subscription;

            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                byte[] buffer = null;

                localRouterName = Dns.GetHostName();

                while(true)
                {
                    buffer = eventQueue.Dequeue(Timeout);

                    if (buffer == null)
                    {
                        elementRetrieved = false;
                    }
                    else
                    {
                        elementRetrieved = true;
                    }

                    if (elementRetrieved == true)
                    {
                        Event.GetHeader(buffer, out originatingRouterName, out inRouterName, out eventType);

                        if (subscriptions.TryGetValue(eventType, out subscription) == true)
                        {
                            if (subscription.LocalOnly == false || string.Compare(localRouterName, originatingRouterName,true) == 0)
                            {
                                StateInfo stateInfo = new StateInfo();

                                stateInfo.callBack = callbackMethod;
                                stateInfo.eventType = eventType;
                                stateInfo.serializedEvent = buffer;

                                ThreadPool.QueueUserWorkItem(new WaitCallback(CallSubscriptionCallback), stateInfo);
                            }
                        }
                    }

                    if (DateTime.UtcNow > nextPushSubscriptions)
                    {
                        foreach (Guid subId in subscriptions.Keys)
                        {
                            try
                            {
                                publishMgr.Publish(subscriptions[subId].Serialize());
                            }
                            catch
                            {
                                // intentionally left blank
                                // it will retry next time
                            }
                        }

                        nextPushSubscriptions = DateTime.UtcNow.AddMinutes(subscriptionRefreshIncrement);
                    }
                }
            }
            catch(ThreadAbortException)
            {
                // Another thread has signalled that this worker
                // thread must terminate.  Typically, this occurs when
                // the main service thread receives a service stop 
                // command.
            }
            catch (AccessViolationException)
            {
                // This can occur after the thread has been stopped and the runtime is doing GC.
                // Just let the thread quit to end listening.
            }
            catch (SharedQueueException e)
            {
                throw new PubSubException(e.Message, e.InnerException);
            }
        }

        /// <summary>
        /// Call the SubscriptionCallback with event
        /// </summary>
        private static void CallSubscriptionCallback(object stateInfo)
        {
            ((Callback)((StateInfo)stateInfo).callBack)(
                ((StateInfo)stateInfo).eventType, ((StateInfo)stateInfo).serializedEvent);
        }
    }
}