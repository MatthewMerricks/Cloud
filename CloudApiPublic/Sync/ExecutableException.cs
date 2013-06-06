//
// ExecutableException.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model.EventMessages.ErrorInfo;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Cloud.Sync
{
    /// <summary>
    /// Generic-typed executable exception which can be handled via an action passed as a construction parameter (along with the typed userstate)
    /// </summary>
    /// <typeparam name="T">Type of input for the handler action as well as the userstate to pass along</typeparam>
    internal sealed class ExecutableException<T> : Exception, IExecutableException
    {
        /// <summary>
        /// User state passed to the handler action when it is fired
        /// </summary>
        public T ExecutionState { get; private set; }
        // handler action
        private Action<T, AggregateException> ExecutionAction;

        #region constructor overloads to match the constructors of the base type Exception
        public ExecutableException(Action<T, AggregateException> executionAction, T executionState)
            : base()
        {
            LocalPropertiesSetter(executionAction, executionState);
        }

        public ExecutableException(Action<T, AggregateException> executionAction, T executionState, string message)
            : base(message)
        {
            LocalPropertiesSetter(executionAction, executionState);
        }

        public ExecutableException(Action<T, AggregateException> executionAction, T executionState, string message, Exception innerException)
            : base(message, innerException)
        {
            LocalPropertiesSetter(executionAction, executionState);
        }

        public ExecutableException(Action<T, AggregateException> executionAction, T executionState, SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            LocalPropertiesSetter(executionAction, executionState);
        }
        #endregion

        #region IExecutableException member
        /// <summary>
        /// Executes code to handle an exception which was provided as an Action on construction along with original userstate
        /// </summary>
        /// <param name="originalException">Passes through the original exception which required handling</param>
        /// <returns>Returns an exception that occurred handling the exception, if any</returns>
        public Exception ExecuteException(AggregateException originalException)
        {
            // pull whether execution is needed under a lock based on whether execution already occurred;
            // mark execution as occurred if it is not marked already
            bool needsExecution;
            lock (this)
            {
                needsExecution = !AlreadyExecuted;
                if (!AlreadyExecuted)
                {
                    AlreadyExecuted = true;
                }
            }

            // if execution has not already occurred,
            // then process execution
            if (needsExecution)
            {
                // if there is an ExecutionAction,
                // then execute it
                if (ExecutionAction != null)
                {
                    try
                    {
                        // execute the handler action with the provided exception and the original userstate
                        ExecutionAction(ExecutionState, originalException);
                    }
                    catch (Exception ex)
                    {
                        return ex;
                    }
                }
            }
            return null;
        }
        private bool AlreadyExecuted = false;
        #endregion

        #region private methods
        // sets the local properties from all constructors
        private void LocalPropertiesSetter(Action<T, AggregateException> executionAction, T executionState)
        {
            if (executionAction == null)
            {
                MessageEvents.FireNewEventMessage(
                    "¡¡ExecutableException should NEVER be instantiated with a null executionAction!!",
                    EventMessageLevel.Important,
                    new HaltAllOfCloudSDKErrorInfo());
            }

            this.ExecutionAction = executionAction;
            this.ExecutionState = executionState;
        }
        #endregion
    }
}
