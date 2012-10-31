using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.WebSolutionsPlatform.Event.PubSubManager
{
    /// <summary>
    /// The exception that is thrown when the process cannot connect to the event system queue. Check 
    /// to see that the event system service is running.
    /// </summary>
    [Serializable()]
    public class PubSubConnectionFailedException : SystemException
    {
        /// <summary>
        /// Base constructor to create a new PubSubConnectionFailedException
        /// </summary>
        public PubSubConnectionFailedException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubConnectionFailedException
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PubSubConnectionFailedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubConnectionFailedException
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public PubSubConnectionFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubConnectionFailedException
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object buffer about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected PubSubConnectionFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
