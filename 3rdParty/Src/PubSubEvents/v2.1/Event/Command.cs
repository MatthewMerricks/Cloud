using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Soap;
using System.Security.Cryptography;

namespace Microsoft.WebSolutionsPlatform.Event
{
    /// <summary>
    /// The Command class defines the command request object to execute
    /// commands on remote systems
    /// </summary>
    public class CommandRequest : Event
    {
        private Guid eventIdForResponse = Guid.Empty;
        /// <summary>
        /// This is the event ID which should be used by the response event when responding to this command
        /// </summary>
        public Guid EventIdForResponse
        {
            get
            {
                if (eventIdForResponse == Guid.Empty)
                {
                    eventIdForResponse = Guid.NewGuid();
                }

                return eventIdForResponse;
            }
            set
            {
                eventIdForResponse = value;
            }
        }

        private Guid correlationID = Guid.Empty;
        /// <summary>
        /// ID to correlate command with response
        /// </summary>
        public Guid CorrelationID
        {
            get
            {
                if (correlationID == Guid.Empty)
                {
                    correlationID = Guid.NewGuid();
                }

                return correlationID;
            }
            set
            {
                correlationID = value;
            }
        }

        private string targetMachineFilter = string.Empty;
        /// <summary>
        /// A regex filter of target machines
        /// </summary>
        public string TargetMachineFilter
        {
            get
            {
                return targetMachineFilter;
            }
            set
            {
                targetMachineFilter = value;
            }
        }

        private string targetRoleFilter = string.Empty;
        /// <summary>
        /// A regex filter of target roles
        /// </summary>
        public string TargetRoleFilter
        {
            get
            {
                return targetRoleFilter;
            }
            set
            {
                targetRoleFilter = value;
            }
        }

        private string command = string.Empty;
        /// <summary>
        /// Command to execute on target machine
        /// </summary>
        public string Command
        {
            get
            {
                return command;
            }
            set
            {
                command = value;
            }
        }

        private List<object> arguments;
        /// <summary>
        /// Arguments associated with the command
        /// </summary>
        public List<object> Arguments
        {
            get
            {
                return arguments;
            }
            set
            {
                arguments = value;
            }
        }

        private Int32 timeToLive = 0;
        /// <summary>
        /// Time to live for command
        /// </summary>
        public Int32 TimeToLive
        {
            get
            {
                return timeToLive;
            }
            set
            {
                timeToLive = value;
            }
        }

        /// <summary>
        /// Base constructor to create a new subscription event
        /// </summary>
        public CommandRequest() :
            base()
        {
            EventVersion = new Version(@"2.0.0.0");
            Arguments = new List<object>();
        }

        /// <summary>
        /// Base constructor to create a new web page event from a serialized event
        /// </summary>
        /// <param name="serializationData">Serialized event buffer</param>
        public CommandRequest(byte[] serializationData) :
            base(serializationData)
        {
        }

        /// <summary>
        /// Used to create a new CommandResponse object which corresponds to this CommandRequest.
        /// </summary>
        /// <returns>CommandResponse object</returns>
        public CommandResponse GetResponse()
        {
            return new CommandResponse(this);
        }

        /// <summary>
        /// Used for event serialization.
        /// </summary>
        /// <param name="buffer">SerializationData object passed to store serialized object</param>
        public override void GetObjectData(WspBuffer buffer)
        {
            buffer.AddElement(@"EventIdForResponse", EventIdForResponse);
            buffer.AddElement(@"CorrelationID", CorrelationID);
            buffer.AddElement(@"TargetMachineFilter", TargetMachineFilter);
            buffer.AddElement(@"TargetRoleFilter", TargetRoleFilter);
            buffer.AddElement(@"Command", Command);
            buffer.AddElement(@"Arguments", Arguments);
            buffer.AddElement(@"TimeToLive", TimeToLive);
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
                case "EventIdForResponse":
                    EventIdForResponse = (Guid)elementValue;
                    break;

                case "CorrelationID":
                    CorrelationID = (Guid)elementValue;
                    break;

                case "TargetMachineFilter":
                    TargetMachineFilter = (string)elementValue;
                    break;

                case "TargetRoleFilter":
                    TargetRoleFilter = (string)elementValue;
                    break;

                case "Command":
                    Command = (string)elementValue;
                    break;

                case "Arguments":
                    Arguments = (List<object>)elementValue;
                    break;

                case "TimeToLive":
                    TimeToLive = (Int32)elementValue;
                    break;

                default:
                    base.SetElement(elementName, elementValue);
                    break;
            }

            return true;
        }
    }

    /// <summary>
    /// The Command class defines the command response object to execute
    /// commands on remote systems
    /// </summary>
    public class CommandResponse : Event
    {
        private Guid correlationID = Guid.Empty;
        /// <summary>
        /// ID to correlate command with response
        /// </summary>
        public Guid CorrelationID
        {
            get
            {
                if (correlationID == Guid.Empty)
                {
                    correlationID = Guid.NewGuid();
                }

                return correlationID;
            }
            set
            {
                correlationID = value;
            }
        }

        private Dictionary<string, object> results;
        /// <summary>
        /// Results of the command
        /// </summary>
        public Dictionary<string, object> Results
        {
            get
            {
                return results;
            }
            set
            {
                results = value;
            }
        }

        private Int32 returnCode;
        /// <summary>
        /// Return code of command
        /// </summary>
        public Int32 ReturnCode
        {
            get
            {
                return returnCode;
            }
            set
            {
                returnCode = value;
            }
        }

        private string message;
        /// <summary>
        /// Response message
        /// </summary>
        public string Message
        {
            get
            {
                return message;
            }
            set
            {
                message = value;
            }
        }

        private Exception responseException = null;
        /// <summary>
        /// If an exception occurred, this dictionary should contain the Exception properties
        /// </summary>
        public Exception ResponseException
        {
            get
            {
                return responseException;
            }
            set
            {
                responseException = value;
            }
        }

        /// <summary>
        /// Base constructor to create a new CommandResponse event
        /// </summary>
        public CommandResponse() :
            base()
        {
            EventVersion = new Version(@"2.0.0.0");
            Results = new Dictionary<string, object>();
            ResponseException = null;
            ReturnCode = 0;
            Message = string.Empty;
        }

        /// <summary>
        /// Base constructor to create a new CommandResponse event from a CommandRequest object
        /// </summary>
        public CommandResponse(CommandRequest commandRequest) :
            base()
        {
            EventVersion = new Version(@"2.0.0.0");
            Results = new Dictionary<string, object>();
            ResponseException = null;
            ReturnCode = 0;
            Message = string.Empty;
            EventType = commandRequest.EventIdForResponse;
            CorrelationID = commandRequest.CorrelationID;
        }

        /// <summary>
        /// Base constructor to create a new CommandResponse from a serialized event
        /// </summary>
        /// <param name="serializationData">Serialized event buffer</param>
        public CommandResponse(byte[] serializationData) : 
            base(serializationData)
        {
        }

        /// <summary>
        /// Used for event serialization.
        /// </summary>
        /// <param name="buffer">SerializationData object passed to store serialized object</param>
        public override void GetObjectData(WspBuffer buffer)
        {
            buffer.AddElement(@"CorrelationID", CorrelationID);
            buffer.AddElement(@"Results", Results);
            buffer.AddElement(@"ReturnCode", ReturnCode);
            buffer.AddElement(@"Message", Message);

            if (ResponseException != null)
            {
                byte[] responseExceptionSerialized = SerializeException(ResponseException);

                if (responseExceptionSerialized != null)
                {
                    buffer.AddElement(@"ResponseExceptionSerialized", responseExceptionSerialized);
                }
            }
        }

        /// <summary>
        /// Serialize an exception to a binary array
        /// </summary>
        /// <param name="exception">Exception object to serialize</param>
        /// <returns>Byte array of serialized exception</returns>
        public byte[] SerializeException(Exception exception)
        {
            MemoryStream ms = new MemoryStream();

            try
            {
                SoapFormatter formatter = new SoapFormatter(null, new StreamingContext(StreamingContextStates.File));
                formatter.Serialize(ms, exception);

                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Deerialize an exception from a binary array
        /// </summary>
        /// <param name="exceptionSerialized">Serialized exception object</param>
        /// <returns>Exception object</returns>
        public Exception DeserializeException(byte[] exceptionSerialized)
        {
            try
            {
                MemoryStream ms = new MemoryStream(exceptionSerialized);

                SoapFormatter formatter = new SoapFormatter(null, new StreamingContext(StreamingContextStates.File));

                Exception exception = (Exception)formatter.Deserialize(ms);

                return exception;
            }
            catch
            {
                return null;
            }
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
                case "CorrelationID":
                    CorrelationID = (Guid)elementValue;
                    break;

                case "Results":
                    Results = (Dictionary<string, object>)elementValue;
                    break;

                case "ReturnCode":
                    ReturnCode = (Int32)elementValue;
                    break;

                case "Message":
                    Message = (string)elementValue;
                    break;

                case "ResponseExceptionSerialized":
                    ResponseException = DeserializeException((byte[])elementValue);
                    break;

                default:
                    base.SetElement(elementName, elementValue);
                    break;
            }

            return true;
        }
    }
}
