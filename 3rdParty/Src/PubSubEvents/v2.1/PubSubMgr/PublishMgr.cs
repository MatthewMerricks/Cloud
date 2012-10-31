using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Resources;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.WebSolutionsPlatform.Common;

[assembly: CLSCompliant(true)]
namespace Microsoft.WebSolutionsPlatform.Event.PubSubManager
{
    /// <summary>
    /// PublishManager is used by applcations to publish events.
    /// <code>
    /// class WorkerClass
    /// {
    ///     private static PublishManager pubMgr;
    ///
    ///     public WorkerClass()
    ///     {
    ///         pubMgr = new PublishManager(10000);
    ///     }
    ///
    ///     public void DoWork()
    ///     {
    ///         ...
    ///
    ///         WebpageEvent localEvent = new WebpageEvent();
    ///         localEvent.EventName = @"Test Event";
    ///
    ///         ...
    ///
    ///         pubMgr.Publish(localEvent.Serialize());
    ///
    ///         ...
    ///     }
    /// }
    /// </code>
    /// </summary>
    public class PublishManager : IDisposable
    {
        private static UInt32 defaultEventTimeout = 10000;

        private string eventQueueName = @"WspEventQueue";
        private SharedQueue eventQueue;

        private bool disposed;

        private int retryAttempts;
        /// <summary>
        /// Number of times to retry a failed enqueue request before returning a fail to the application
        /// </summary>
        public int RetryAttempts
        {
            get
            {
                return retryAttempts;
            }
            set
            {
                retryAttempts = value;
            }
        }

        private int retryPause;
        /// <summary>
        /// Number of milliseconds to wait before retrying an enqueue request
        /// </summary>
        public int RetryPause
        {
            get
            {
                return retryPause;
            }
            set
            {
                retryPause = value;
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
        public PublishManager()
            : this(defaultEventTimeout)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="timeout">Timeout for publishing an event</param>
        [CLSCompliant(false)]
        public PublishManager(UInt32 timeout)
        {
            this.timeout = timeout;
            this.retryAttempts = 3;
            this.retryPause = 1000;

            try
            {
                eventQueue = new SharedQueue(eventQueueName, 100);

                if (eventQueue == null)
                {
                    ResourceManager rm = new ResourceManager("PubSubMgr.PubSubMgr", Assembly.GetExecutingAssembly());

                    throw new PubSubConnectionFailedException(rm.GetString("ConnectionFailed"));
                }
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
        /// Publishes an event to the event service
        /// </summary>
        /// <param name="serializedEvent">Serialized version of the event</param>
        public void Publish(byte[] serializedEvent)
        {
            int tries = 0;

            if (serializedEvent == null || serializedEvent.Length == 0)
            {
                ResourceManager rm = new ResourceManager("PubSubMgr.PubSubMgr", Assembly.GetExecutingAssembly());

                throw new ArgumentException(rm.GetString("InvalidArgumentValue"), "serializedEvent");
            }

            while (true)
            {
                try
                {
                    eventQueue.Enqueue(ref serializedEvent, timeout);

                    tries = 0;

                    break;
                }
                catch (TimeoutException)
                {
                    tries++;

                    if (tries > retryAttempts)
                    {
                        throw;
                    }

                    Thread.Sleep(retryPause);
                }
                catch (SharedQueueFullException e)
                {
                    throw new PubSubQueueFullException(e.Message, e.InnerException);
                }
                catch (SharedQueueException e)
                {
                    throw new PubSubException(e.Message, e.InnerException);
                }
            }
        }

        /// <summary>
        /// Dispose the object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dispose the object
        /// </summary>
        /// <param name="disposing">True if disposing</param>
        protected virtual void Dispose(bool disposing) 
        {
            if (!disposed)
            {
                try
                {
                    if (eventQueue != null)
                    {
                        eventQueue.Dispose();
                    }
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
        ~PublishManager()
        {
            Dispose(false);
        }
    }
}
