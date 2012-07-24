//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using Microsoft.ApplicationServer.Common.Interop;
    using System.Runtime.Versioning;
    using System.Security;
    using Microsoft.ApplicationServer.Common.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    class ExceptionTrace
    {
        private string eventSourceName;
        const ushort FailFastEventLogCategory = 6;

        public ExceptionTrace(string eventSourceName)
        {
            this.eventSourceName = eventSourceName;
        }

        [SuppressMessage(FxCop.Category.Performance, FxCop.Rule.MarkMembersAsStatic, Justification = "CSDMain #183668")]
        public void AsInformation(Exception exception)
        {
        }

        [SuppressMessage(FxCop.Category.Performance, FxCop.Rule.MarkMembersAsStatic, Justification = "CSDMain #183668")]
        public void AsWarning(Exception exception)
        {
        }

        
        public Exception AsError(Exception exception)
        {
            return TraceException<Exception>(exception);
        }

        public Exception AsError(Exception exception, string eventSource)
        {
            return TraceException<Exception>(exception, eventSource);
        }

        public ArgumentException Argument(string paramName, string message)
        {
            return TraceException<ArgumentException>(new ArgumentException(message, paramName));
        }

        public ArgumentNullException ArgumentNull(string paramName)
        {
            return TraceException<ArgumentNullException>(new ArgumentNullException(paramName));
        }

        public ArgumentNullException ArgumentNull(string paramName, string message)
        {
            return TraceException<ArgumentNullException>(new ArgumentNullException(paramName, message));
        }

        public ArgumentException ArgumentNullOrEmpty(string paramName)
        {
            return this.Argument(paramName, SRCore.ArgumentNullOrEmpty(paramName));
        }

        public ArgumentOutOfRangeException ArgumentOutOfRange(string paramName, object actualValue, string message)
        {
            return TraceException<ArgumentOutOfRangeException>(new ArgumentOutOfRangeException(paramName, actualValue, message));
        }

        // When throwing ObjectDisposedException, it is highly recommended that you use this ctor
        // [C#]
        // public ObjectDisposedException(string objectName, string message);
        // And provide null for objectName but meaningful and relevant message for message. 
        // It is recommended because end user really does not care or can do anything on the disposed object, commonly an internal or private object.
        public ObjectDisposedException ObjectDisposed(string message)
        {
            // pass in null, not disposedObject.GetType().FullName as per the above guideline
            return TraceException<ObjectDisposedException>(new ObjectDisposedException(null, message));
        }

        [SuppressMessage(FxCop.Category.Performance, FxCop.Rule.MarkMembersAsStatic, Justification = "CSDMain #183668")]
        public void TraceUnhandledException(Exception exception)
        {

        }

        TException TraceException<TException>(TException exception)
            where TException : Exception
        {
            return TraceException<TException>(exception, this.eventSourceName);
        }

        [ResourceConsumption(ResourceScope.Process)]
        [Fx.Tag.SecurityNote(Critical = "Calls 'System.Runtime.Interop.UnsafeNativeMethods.IsDebuggerPresent()' which is a P/Invoke method",
            Safe = "Does not leak any resource, needed for debugging")]
        [SecuritySafeCritical]
        TException TraceException<TException>(TException exception, string eventSource)
            where TException : Exception
        {

            BreakOnException(exception);

            return exception;
        }

        [SuppressMessage(FxCop.Category.Performance, FxCop.Rule.MarkMembersAsStatic, Justification = "CSDMain #183668")]
        [Fx.Tag.SecurityNote(Critical = "Calls into critical method UnsafeNativeMethods.IsDebuggerPresent and UnsafeNativeMethods.DebugBreak",
        Safe = "Safe because it's a no-op in retail builds.")]
        [SecuritySafeCritical]
        void BreakOnException(Exception exception)
        {
#if DEBUG
            if (Fx.BreakOnExceptionTypes != null)
            {
                foreach (Type breakType in Fx.BreakOnExceptionTypes)
                {
                    if (breakType.IsAssignableFrom(exception.GetType()))
                    {
                        // This is intended to "crash" the process so that a debugger can be attached.  If a managed
                        // debugger is already attached, it will already be able to hook these exceptions.  We don't
                        // want to simulate an unmanaged crash (DebugBreak) in that case.
                        if (!Debugger.IsAttached && !UnsafeNativeMethods.IsDebuggerPresent())
                        {
                            UnsafeNativeMethods.DebugBreak();
                        }
                    }
                }
            }
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void TraceFailFast(string message)
        {
            EventLogger logger = null;
#pragma warning disable 618
            logger = new EventLogger(this.eventSourceName, Fx.Trace);
#pragma warning restore 618
            TraceFailFast(message, logger);
        }

        // Generate an event Log entry for failfast purposes
        // To force a Watson on a dev machine, do the following:
        // 1. Set \HKLM\SOFTWARE\Microsoft\PCHealth\ErrorReporting ForceQueueMode = 0 
        // 2. In the command environment, set COMPLUS_DbgJitDebugLaunchSetting=0
        [SuppressMessage(FxCop.Category.Performance, FxCop.Rule.MarkMembersAsStatic, Justification = "CSDMain #183668")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void TraceFailFast(string message, EventLogger logger)
        {
            if (logger != null)
            {
                try
                {
                    string stackTrace = null;
                    try
                    {
                        stackTrace = new StackTrace().ToString();
                    }
                    catch (Exception exception)
                    {
                        stackTrace = exception.Message;
                        if (Fx.IsFatal(exception))
                        {
                            throw;
                        }
                    }
                    finally
                    {
                        logger.LogEvent(TraceEventType.Critical,
                            FailFastEventLogCategory,
                            (uint)EventLogEventId.FailFast,
                            message,
                            stackTrace);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogEvent(TraceEventType.Critical,
                        FailFastEventLogCategory,
                        (uint)EventLogEventId.FailFastException,
                        ex.ToString());
                    if (Fx.IsFatal(ex))
                    {
                        throw;
                    }
                }
            }
        }
    }
}
