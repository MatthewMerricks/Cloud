//
// GlobalSyncEvents.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Model;

namespace CloudApiPublic.Sync
{
    /// <summary>
    /// Handler delegate type for GlobalSyncEvents.FileChangeSyncStatusChanged event
    /// </summary>
    /// <param name="sender">FileChange which changed during sync</param>
    /// <param name="aggregateErrors">Aggregated CLError to append upon error, if any error occurs</param>
    public delegate void FileChangeSyncStatusChangedHandler(FileChange sender, CLError aggregateErrors);

    /// <summary>
    /// Static class containing events for responding to changes to FileChange instances during sync
    /// </summary>
    public static class GlobalSyncEvents
    {
        /// <summary>
        /// Event for status changes to FileChange objects that occur during sync
        /// </summary>
        public static event FileChangeSyncStatusChangedHandler FileChangeSyncStatusChanged;

        /// <summary>
        /// Notify all listeners that a FileChange has changed during sync
        /// </summary>
        /// <param name="changed">FileChange which changed during sync</param>
        /// <param name="aggregateErrors">(optional) Aggregated CLError to append upon error, if any error occurs</param>
        /// <returns>Returns all errors that occurred in firing the event appended to the optional input parameter, if any ¡¡Use this to replace the pointer used on input!!</returns>
        public static CLError NotifyFileChangeSyncStatusChanged(FileChange changed, CLError aggregateErrors = null)
        {
            // if any event handlers have been attached,
            // then fire the event
            if (FileChangeSyncStatusChanged != null)
            {
                try
                {
                    // event handlers must share an instance of CLError to append,
                    // so if it is null then put in a placeholder exception that will be removed later
                    bool aggregateErrorsNull = aggregateErrors == null;
                    Exception placeHolderException = null;
                    if (aggregateErrorsNull)
                    {
                        placeHolderException = new Exception("Placeholder exception");
                        aggregateErrors = placeHolderException;
                    }
                    
                    // fire the event
                    FileChangeSyncStatusChanged(changed, aggregateErrors);

                    // if aggregateErrors was null earlier,
                    // then take out the exceptions except for the placeholder to readd to the returned error
                    if (aggregateErrorsNull)
                    {
                        Exception[] eventExceptions = (aggregateErrors.GrabExceptions() ?? Enumerable.Empty<Exception>()).ToArray();

                        if (eventExceptions.Length > 1)
                        {
                            aggregateErrors = null;
                            foreach (Exception currentEventException in eventExceptions)
                            {
                                if (currentEventException != placeHolderException)
                                {
                                    aggregateErrors += currentEventException;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // if an error occurred firing the event then it is recorded here
                    aggregateErrors += ex;
                }
            }
            return aggregateErrors;
        }
    }
}