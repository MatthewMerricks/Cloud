//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Diagnostics
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Diagnostics.Eventing;
    using System.Globalization;
    using System.IO;
    using System.Security;
    using System.Text;
    using System.Xml;
    using System.Xml.XPath;
    using System.Diagnostics.CodeAnalysis;
    using System.Security.Permissions;
    //using Microsoft.AppFabric.Tracing;

    [SuppressMessage(FxCop.Category.Design, FxCop.Rule.TypesThatOwnDisposableFieldsShouldBeDisposable, Justification = "This class uses it's own shutdown pattern that is not compatible with IDisposable.")]
    sealed class DiagnosticTrace
    {
        //Diagnostics trace
        const string DefaultTraceListenerName = "Default";
        const string TraceRecordVersion = "http://schemas.microsoft.com/2004/10/E2ETraceEvent/TraceRecord";
        const int WindowsVistaMajorNumber = 6;
        const string EventSourceVersion = "4.0.0.0";
        const ushort TracingEventLogCategory = 4;

        [Fx.Tag.SecurityNote(Critical = "provider Id to create EtwProvider, which is SecurityCritical")]
        [SecurityCritical]
        static Guid defaultEtwProviderId = new Guid("{c651f5f6-1c0d-492e-8ae1-b4efd7c9d503}");
        static Hashtable etwProviderCache = new Hashtable();
        static bool isVistaOrGreater = Environment.OSVersion.Version.Major >= WindowsVistaMajorNumber;
        static string appDomainFriendlyName = AppDomain.CurrentDomain.FriendlyName;
        static Func<string> traceAnnotation;

        bool calledShutdown;
        bool haveListeners;
        object thisLock;
        SourceLevels level;
        DiagnosticTraceSource traceSource;
        [Fx.Tag.SecurityNote(Critical = "Stores object created by a critical c'tor")]
        [SecurityCritical]
        EtwProvider etwProvider;
        string TraceSourceName;
        [Fx.Tag.SecurityNote(Critical = "Usage of EventDescriptor, which is protected by a LinkDemand")]
        [SecurityCritical]
        static EventDescriptor transferEventDescriptor = new EventDescriptor(499, 0, (byte)TraceChannel.Analytic, (byte)TraceEventLevel.LogAlways, (byte)TraceEventOpcode.Info, 0x0, 0x20000000001A0065);
        [Fx.Tag.SecurityNote(Critical = "This determines the event source name.")]
        [SecurityCritical]
        string eventSourceName;

        //Compiler will add all static initializers into the static constructor.  Adding an explicit one to mark SecurityCritical.
        [Fx.Tag.SecurityNote(Critical = "setting critical field defaultEtwProviderId")]
        [SecurityCritical]
        [SuppressMessage(FxCop.Category.Performance, FxCop.Rule.InitializeReferenceTypeStaticFieldsInline,
                        Justification = "SecurityCriticial method")]
        static DiagnosticTrace()
        {
        }

        [SuppressMessage(FxCop.Category.Performance, FxCop.Rule.DoNotInitializeUnnecessarily, Justification = "Setting etwProvider to null is valid as the field may have been assigned previously.", Scope = "Member", Target = "Microsoft.ApplicationServer.Common.Diagnostics.DiagnosticTrace.#.ctor(System.String,System.Guid)")]
        [Fx.Tag.SecurityNote(Critical = "Access critical etwProvider, eventSourceName field",
            Safe = "Doesn't leak info\\resources")]
        [SecuritySafeCritical]
        public DiagnosticTrace(string traceSourceName, Guid etwProviderId)
        {
            try
            {
                this.thisLock = new object();
                this.TraceSourceName = traceSourceName;
                this.eventSourceName = string.Concat(this.TraceSourceName, " ", EventSourceVersion);
                this.LastFailure = DateTime.MinValue;

                CreateTraceSource();
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

#pragma warning disable 618
                EventLogger logger = new EventLogger(this.eventSourceName, null);
                logger.LogEvent(TraceEventType.Error, TracingEventLogCategory, (uint)EventLogEventId.FailedToSetupTracing, false,
                    exception.ToString());
#pragma warning restore 618
            }

            try
            {
                CreateEtwProvider(etwProviderId);
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                this.etwProvider = null;
#pragma warning disable 618
                EventLogger logger = new EventLogger(this.eventSourceName, null);
                logger.LogEvent(TraceEventType.Error, TracingEventLogCategory, (uint)EventLogEventId.FailedToSetupTracing, false,
                    exception.ToString());
#pragma warning restore 618

            }

            if (this.TracingEnabled || this.EtwTracingEnabled)
            {
#pragma warning disable 618
                this.AddDomainEventHandlersForCleanup();
#pragma warning restore 618
            }
        }

        static public Guid DefaultEtwProviderId
        {
            [Fx.Tag.SecurityNote(Critical = "reading critical field defaultEtwProviderId", Safe = "Doesn't leak info\\resources")]
            [SecuritySafeCritical]
            [SuppressMessage(FxCop.Category.Security, FxCop.Rule.DoNotIndirectlyExposeMethodsWithLinkDemands,
                Justification = "SecuritySafeCriticial method")]
            get
            {
                return DiagnosticTrace.defaultEtwProviderId;
            }
            [Fx.Tag.SecurityNote(Critical = "setting critical field defaultEtwProviderId")]
            [SecurityCritical]
            [SuppressMessage(FxCop.Category.Security, FxCop.Rule.DoNotIndirectlyExposeMethodsWithLinkDemands,
                Justification = "SecurityCriticial method")]
            set
            {
                DiagnosticTrace.defaultEtwProviderId = value;
            }
        }

        DateTime LastFailure { get; set; }

        public DiagnosticTraceSource TraceSource
        {
            get
            {
                return this.traceSource;
            }
        }

        public EtwProvider EtwProvider
        {
            [Fx.Tag.SecurityNote(Critical = "Exposes the critical etwProvider field")]
            [SecurityCritical]
            get
            {
                return this.etwProvider;
            }
        }

        public bool IsEtwProviderEnabled
        {
            [Fx.Tag.SecurityNote(Critical = "Access critical etwProvider field",
                Safe = "Doesn't leak info\\resources")]
            [SecuritySafeCritical]
            get
            {
                return (this.EtwTracingEnabled && this.etwProvider.IsEnabled());
            }
        }

        public bool HaveListeners
        {
            get
            {
                return this.haveListeners;
            }
        }

        public static Guid ActivityId
        {
            [Fx.Tag.SecurityNote(Critical = "gets the CorrelationManager, which does a LinkDemand for UnmanagedCode",
                Safe = "only uses the CM to get the ActivityId, which is not protected data, doesn't leak the CM")]
            [SecuritySafeCritical]
            [SuppressMessage(FxCop.Category.Security, FxCop.Rule.DoNotIndirectlyExposeMethodsWithLinkDemands,
                Justification = "SecuritySafeCriticial method")]
            get
            {
                object id = Trace.CorrelationManager.ActivityId;
                return id == null ? Guid.Empty : (Guid)id;
            }

            [Fx.Tag.SecurityNote(Critical = "gets the CorrelationManager, which does a LinkDemand for UnmanagedCode",
                Safe = "only uses the CM to get the ActivityId, which is not protected data, doesn't leak the CM")]
            [SecuritySafeCritical]
            set
            {
                Trace.CorrelationManager.ActivityId = value;
            }
        }

        public Action RefreshState
        {
            [Fx.Tag.SecurityNote(Critical = "Access critical etwProvider field",
            Safe = "Doesn't leak resources or information")]
            [SecuritySafeCritical]
            get
            {
                return this.EtwProvider.ControllerCallBack;
            }

            [Fx.Tag.SecurityNote(Critical = "Access critical etwProvider field",
            Safe = "Doesn't leak resources or information")]
            [SecuritySafeCritical]
            set
            {
                this.EtwProvider.ControllerCallBack = value;
            }
        }

        public SourceLevels Level
        {
            get
            {
                if (this.TraceSource != null)
                {
                    this.level = this.TraceSource.Switch.Level;
                }

                return this.level;
            }
        }

        public bool TracingEnabled
        {
            get
            {
                return (this.traceSource != null);
            }
        }

        bool EtwTracingEnabled
        {
            [Fx.Tag.SecurityNote(Critical = "Access critical etwProvider field",
                Safe = "Doesn't leak info\\resources")]
            [SecuritySafeCritical]
            get
            {
                return (this.etwProvider != null);
            }
        }

        static string ProcessName
        {
            [Fx.Tag.SecurityNote(Critical = "Satisfies a LinkDemand for 'PermissionSetAttribute' on type 'Process' when calling method GetCurrentProcess",
            Safe = "Does not leak any resource and has been reviewed")]
            [SecuritySafeCritical]
            get
            {
                string retval = null;
                using (Process process = Process.GetCurrentProcess())
                {
                    retval = process.ProcessName;
                }
                return retval;
            }
        }

        static int ProcessId
        {
            [Fx.Tag.SecurityNote(Critical = "Satisfies a LinkDemand for 'PermissionSetAttribute' on type 'Process' when calling method GetCurrentProcess",
            Safe = "Does not leak any resource and has been reviewed")]
            [SecuritySafeCritical]
            get
            {
                int retval = -1;
                using (Process process = Process.GetCurrentProcess())
                {
                    retval = process.Id;
                }
                return retval;
            }
        }

        public static void SetAnnotation(Func<string> annotation)
        {
            DiagnosticTrace.traceAnnotation = annotation;
        }

        public bool ShouldTrace(TraceEventLevel eventLevel)
        {
            return ShouldTraceToTraceSource(eventLevel) || ShouldTraceToEtw(eventLevel);
        }

        public bool ShouldTraceToTraceSource(TraceEventLevel eventLevel)
        {
            return (this.HaveListeners && this.TraceSource != null &&
                0 != ((int)TraceLevelHelper.GetTraceEventType(eventLevel) & (int)this.Level));
        }

        [Fx.Tag.SecurityNote(Critical = "Access critical etwProvider field",
            Safe = "Doesn't leak information\\resources")]
        [SecuritySafeCritical]
        public bool ShouldTraceToEtw(TraceEventLevel traceEventLevel)
        {
            return (this.EtwProvider != null && this.EtwProvider.IsEnabled((byte)traceEventLevel, 0));
        }

        [Fx.Tag.SecurityNote(Critical = "Usage of EventDescriptor, which is protected by a LinkDemand",
            Safe = "Doesn't leak information\\resources")]
        [SecuritySafeCritical]
        public void Event(int eventId, TraceEventLevel traceEventLevel, TraceChannel channel, string description)
        {
            if (this.TracingEnabled)
            {
                EventDescriptor eventDescriptor = DiagnosticTrace.GetEventDescriptor(eventId, channel, traceEventLevel);
                this.Event(ref eventDescriptor, description);
            }
        }

        [Fx.Tag.SecurityNote(Critical = "Usage of EventDescriptor, which is protected by a LinkDemand")]
        [SecurityCritical]
        public void Event(ref EventDescriptor eventDescriptor, string description)
        {
            if (this.TracingEnabled)
            {
                TracePayload tracePayload = DiagnosticTrace.GetSerializedPayload(null, null, null);
                this.WriteTraceSource(ref eventDescriptor, description, tracePayload);
            }
        }

        public void SetAndTraceTransfer(Guid newId, bool emitTransfer)
        {
            if (emitTransfer)
            {
                TraceTransfer(newId);
            }
            DiagnosticTrace.ActivityId = newId;
        }

        [Fx.Tag.SecurityNote(Critical = "Access critical transferEventDescriptor field, as well as other critical methods",
            Safe = "Doesn't leak information or resources")]
        [SecuritySafeCritical]
        public void TraceTransfer(Guid newId)
        {
            Guid oldId = DiagnosticTrace.ActivityId;
            if (newId != oldId)
            {
                try
                {
                    if (this.HaveListeners)
                    {
                        this.TraceSource.TraceTransfer(0, null, newId);
                    }
                    //also emit to ETW
                    if (this.IsEtwEventEnabled(ref DiagnosticTrace.transferEventDescriptor))
                    {
                        this.etwProvider.WriteTransferEvent(ref DiagnosticTrace.transferEventDescriptor, newId,
                            DiagnosticTrace.traceAnnotation == null ? string.Empty : DiagnosticTrace.traceAnnotation(),
                            DiagnosticTrace.appDomainFriendlyName);
                    }
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    LogTraceFailure(null, e);
                }
            }
        }

        [Fx.Tag.SecurityNote(Critical = "Usage of EventDescriptor, which is protected by a LinkDemand")]
        [SecurityCritical]
        public void WriteTraceSource(ref EventDescriptor eventDescriptor, string description, TracePayload payload)
        {
            if (this.TracingEnabled)
            {
                XPathNavigator navigator = null;
                try
                {
                    string traceString = BuildTrace(ref eventDescriptor, description, payload);
                    XmlDocument traceDocument = new XmlDocument();
                    traceDocument.LoadXml(traceString);
                    navigator = traceDocument.CreateNavigator();
                    this.TraceSource.TraceData(TraceLevelHelper.GetTraceEventType(eventDescriptor.Level, eventDescriptor.Opcode), (int)eventDescriptor.EventId, navigator);

                    if (this.calledShutdown)
                    {
                        this.TraceSource.Flush();
                    }
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }

                    LogTraceFailure(navigator == null ? string.Empty : navigator.ToString(), exception);
                }
            }
        }

        [Fx.Tag.SecurityNote(Critical = "Usage of EventDescriptor, which is protected by a LinkDemand")]
        [SecurityCritical]
        static string BuildTrace(ref EventDescriptor eventDescriptor, string description, TracePayload payload)
        {
            StringBuilder sb = new StringBuilder();
            XmlTextWriter writer = new XmlTextWriter(new StringWriter(sb, CultureInfo.CurrentCulture));

            writer.WriteStartElement(DiagnosticStrings.TraceRecordTag);
            writer.WriteAttributeString(DiagnosticStrings.NamespaceTag, DiagnosticTrace.TraceRecordVersion);
            writer.WriteAttributeString(DiagnosticStrings.SeverityTag,
                TraceLevelHelper.LookupSeverity((TraceEventLevel)eventDescriptor.Level, (TraceEventOpcode)eventDescriptor.Opcode));
            writer.WriteAttributeString(DiagnosticStrings.ChannelTag, DiagnosticTrace.LookupChannel((TraceChannel)eventDescriptor.Channel));

            writer.WriteElementString(DiagnosticStrings.TraceCodeTag, DiagnosticTrace.GenerateTraceCode(ref eventDescriptor));
            writer.WriteElementString(DiagnosticStrings.DescriptionTag, description);
            writer.WriteElementString(DiagnosticStrings.AppDomain, payload.AppDomainFriendlyName);

            if (!string.IsNullOrEmpty(payload.EventSource))
            {
                writer.WriteElementString(DiagnosticStrings.SourceTag, payload.EventSource);
            }

            if (!string.IsNullOrEmpty(payload.ExtendedData))
            {
                writer.WriteRaw(payload.ExtendedData);
            }

            if (!string.IsNullOrEmpty(payload.SerializedException))
            {
                writer.WriteRaw(payload.SerializedException);
            }

            writer.WriteEndElement();

            return sb.ToString();
        }

        [Fx.Tag.SecurityNote(Critical = "Usage of EventDescriptor, which is protected by a LinkDemand")]
        [SecurityCritical]
        static string GenerateTraceCode(ref EventDescriptor eventDescriptor)
        {
            return eventDescriptor.EventId.ToString(CultureInfo.InvariantCulture);
        }

        static string LookupChannel(TraceChannel traceChannel)
        {
            string channelName;
            switch (traceChannel)
            {
                case TraceChannel.Admin:
                    channelName = "Admin";
                    break;
                case TraceChannel.Analytic:
                    channelName = "Analytic";
                    break;
                case TraceChannel.Application:
                    channelName = "Application";
                    break;
                case TraceChannel.Debug:
                    channelName = "Debug";
                    break;
                case TraceChannel.Operational:
                    channelName = "Operational";
                    break;
                case TraceChannel.Perf:
                    channelName = "Perf";
                    break;
                default:
                    channelName = traceChannel.ToString();
                    break;
            }

            return channelName;
        }

        public static TracePayload GetSerializedPayload(object source, TraceRecord traceRecord, Exception exception)
        {
            return DiagnosticTrace.GetSerializedPayload(source, traceRecord, exception, false);
        }

        public static TracePayload GetSerializedPayload(object source, TraceRecord traceRecord, Exception exception, bool getServiceReference)
        {
            string eventSource = null;
            string extendedData = null;
            string serializedException = null;

            if (source != null)
            {
                eventSource = CreateSourceString(source);
            }

            if (traceRecord != null)
            {
                StringBuilder sb = new StringBuilder();
                XmlTextWriter writer = new XmlTextWriter(new StringWriter(sb, CultureInfo.CurrentCulture));

                writer.WriteStartElement(DiagnosticStrings.ExtendedDataTag);
                traceRecord.WriteTo(writer);
                writer.WriteEndElement();

                extendedData = sb.ToString();
            }

            if (exception != null)
            {
                serializedException = DiagnosticTrace.ExceptionToTraceString(exception);
            }

            if (getServiceReference && (DiagnosticTrace.traceAnnotation != null))
            {
                return new TracePayload(serializedException, eventSource, DiagnosticTrace.appDomainFriendlyName, extendedData, DiagnosticTrace.traceAnnotation());
            }

            return new TracePayload(serializedException, eventSource, DiagnosticTrace.appDomainFriendlyName, extendedData, string.Empty);
        }

        [Fx.Tag.SecurityNote(Critical = "Usage of EventDescriptor, which is protected by a LinkDemand",
            Safe = "Only queries the status of the provider - does not modify the state")]
        [SecuritySafeCritical]
        public bool IsEtwEventEnabled(ref EventDescriptor eventDescriptor)
        {
            return (this.EtwTracingEnabled && this.etwProvider.IsEnabled(eventDescriptor.Level, eventDescriptor.Keywords));
        }

        //only used for exceptions, perf is not important
        public static string XmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            int len = text.Length;
            StringBuilder encodedText = new StringBuilder(len + 8); //perf optimization, expecting no more than 2 > characters

            for (int i = 0; i < len; ++i)
            {
                char ch = text[i];
                switch (ch)
                {
                    case '<':
                        encodedText.Append("&lt;");
                        break;
                    case '>':
                        encodedText.Append("&gt;");
                        break;
                    case '&':
                        encodedText.Append("&amp;");
                        break;
                    default:
                        encodedText.Append(ch);
                        break;
                }
            }
            return encodedText.ToString();
        }

        [Fx.Tag.SecurityNote(Critical = "Access the critical Listeners property",
            Safe = "Only Removes the default listener of the local source")]
        [SecuritySafeCritical]
        [SuppressMessage(FxCop.Category.Security, FxCop.Rule.DoNotIndirectlyExposeMethodsWithLinkDemands,
            Justification = "SecuritySafeCriticial method")]
        void CreateTraceSource()
        {
            if (!string.IsNullOrEmpty(this.TraceSourceName))
            {
                this.traceSource = new DiagnosticTraceSource(this.TraceSourceName);
                if (this.traceSource != null)
                {
                    this.traceSource.Listeners.Remove(DiagnosticTrace.DefaultTraceListenerName);
                    this.haveListeners = this.traceSource.Listeners.Count > 0;
                    this.level = this.traceSource.Switch.Level;
                }
            }
        }

        [Fx.Tag.SecurityNote(Critical = "Sets global event handlers for the AppDomain",
            Safe = "Doesn't leak resources\\Information")]
        [SecuritySafeCritical]
        [Obsolete("For SMDiagnostics.dll use only")]
        void AddDomainEventHandlersForCleanup()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            if (this.TracingEnabled)
            {
                currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);
                currentDomain.DomainUnload += new EventHandler(ExitOrUnloadEventHandler);
                currentDomain.ProcessExit += new EventHandler(ExitOrUnloadEventHandler);
            }
        }

        [Fx.Tag.SecurityNote(Critical = "Usage of EventDescriptor, which is protected by a LinkDemand",
            Safe = "Doesn't leak resources or information")]
        [SecuritySafeCritical]
        void CreateEtwProvider(Guid etwProviderId)
        {
            if (etwProviderId != Guid.Empty && DiagnosticTrace.isVistaOrGreater)
            {
                //Pick EtwProvider from cache, add to cache if not found
                this.etwProvider = (EtwProvider)etwProviderCache[etwProviderId];
                if (this.etwProvider == null)
                {
                    lock (etwProviderCache)
                    {
                        this.etwProvider = (EtwProvider)etwProviderCache[etwProviderId];
                        if (this.etwProvider == null)
                        {
                            this.etwProvider = new EtwProvider(etwProviderId);
                            etwProviderCache.Add(etwProviderId, this.etwProvider);
                        }
                    }
                }
            }
        }

        void ExitOrUnloadEventHandler(object sender, EventArgs e)
        {
            ShutdownTracing();
        }

        void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            /*
            Exception e = (Exception)args.ExceptionObject;
            TraceCore.UnhandledException(this, e);
            ShutdownTracing();
             */
        }

        static string CreateSourceString(object source)
        {
            return source.GetType().ToString() + "/" + source.GetHashCode().ToString(CultureInfo.CurrentCulture);
        }

        [Fx.Tag.SecurityNote(Critical = "Usage of EventDescriptor, which is protected by a LinkDemand")]
        [SecurityCritical]
        static EventDescriptor GetEventDescriptor(int eventId, TraceChannel channel, TraceEventLevel traceEventLevel)
        {
            unchecked
            {
                //map channel to keywords
                long keyword = (long)0x0;
                if (channel == TraceChannel.Admin)
                {
                    keyword = keyword | (long)0x8000000000000000;
                }
                else if (channel == TraceChannel.Operational)
                {
                    keyword = keyword | 0x4000000000000000;
                }
                else if (channel == TraceChannel.Analytic)
                {
                    keyword = keyword | 0x2000000000000000;
                }
                else if (channel == TraceChannel.Debug)
                {
                    keyword = keyword | 0x100000000000000;
                }
                else if (channel == TraceChannel.Perf)
                {
                    keyword = keyword | 0x0800000000000000;
                }
                return new EventDescriptor(eventId, 0x0, (byte)channel, (byte)traceEventLevel, 0x0, 0x0, (long)keyword);
            }
        }

        static string ExceptionToTraceString(Exception exception)
        {
            StringBuilder sb = new StringBuilder();
            XmlTextWriter xml = new XmlTextWriter(new StringWriter(sb, CultureInfo.CurrentCulture));

            xml.WriteStartElement(DiagnosticStrings.ExceptionTag);
            xml.WriteElementString(DiagnosticStrings.ExceptionTypeTag, DiagnosticTrace.XmlEncode(exception.GetType().AssemblyQualifiedName));
            xml.WriteElementString(DiagnosticStrings.MessageTag, DiagnosticTrace.XmlEncode(exception.Message));
            xml.WriteElementString(DiagnosticStrings.StackTraceTag, DiagnosticTrace.XmlEncode(StackTraceString(exception)));
            xml.WriteElementString(DiagnosticStrings.ExceptionStringTag, DiagnosticTrace.XmlEncode(exception.ToString()));
            System.ComponentModel.Win32Exception win32Exception = exception as System.ComponentModel.Win32Exception;
            if (win32Exception != null)
            {
                xml.WriteElementString(DiagnosticStrings.NativeErrorCodeTag, win32Exception.NativeErrorCode.ToString("X", CultureInfo.InvariantCulture));
            }

            if (exception.Data != null && exception.Data.Count > 0)
            {
                xml.WriteStartElement(DiagnosticStrings.DataItemsTag);
                foreach (object dataItem in exception.Data.Keys)
                {
                    xml.WriteStartElement(DiagnosticStrings.DataTag);
                    xml.WriteElementString(DiagnosticStrings.KeyTag, DiagnosticTrace.XmlEncode(dataItem.ToString()));
                    xml.WriteElementString(DiagnosticStrings.ValueTag, DiagnosticTrace.XmlEncode(exception.Data[dataItem].ToString()));
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }
            if (exception.InnerException != null)
            {
                xml.WriteStartElement(DiagnosticStrings.InnerExceptionTag);
                xml.WriteRaw(ExceptionToTraceString(exception.InnerException));
                xml.WriteEndElement();
            }
            xml.WriteEndElement();

            return sb.ToString();
        }

        static string StackTraceString(Exception exception)
        {
            string retval = exception.StackTrace;
            if (string.IsNullOrEmpty(retval))
            {
                // This means that the exception hasn't been thrown yet. We need to manufacture the stack then.
                StackTrace stackTrace = new StackTrace(false);
                // Figure out how many frames should be throw away
                System.Diagnostics.StackFrame[] stackFrames = stackTrace.GetFrames();

                int frameCount = 0;
                bool breakLoop = false;
                foreach (StackFrame frame in stackFrames)
                {
                    string methodName = frame.GetMethod().Name;
                    switch (methodName)
                    {
                        case "StackTraceString":
                        case "AddExceptionToTraceString":
                        case "GetAdditionalPayload":
                            ++frameCount;
                            break;
                        default:
                            if (methodName.StartsWith("ThrowHelper", StringComparison.Ordinal))
                            {
                                ++frameCount;
                            }
                            else
                            {
                                breakLoop = true;
                            }
                            break;
                    }
                    if (breakLoop)
                    {
                        break;
                    }
                }

                stackTrace = new StackTrace(frameCount, false);
                retval = stackTrace.ToString();
            }
            return retval;
        }

        //CSDMain:109153, Duplicate code from System.ServiceModel.Diagnostics
        [Fx.Tag.SecurityNote(Critical = "Calls unsafe methods, UnsafeCreateEventLogger and UnsafeLogEvent.",
            Safe = "Event identities cannot be spoofed as they are constants determined inside the method, Demands the same permission that is asserted by the unsafe method.")]
        [SecuritySafeCritical]
        [SuppressMessage(FxCop.Category.Security, FxCop.Rule.SecureAsserts,
            Justification = "Should not demand permission that is asserted by the EtwProvider ctor.")]
        void LogTraceFailure(string traceString, Exception exception)
        {
            const int FailureBlackoutDuration = 10;
            TimeSpan FailureBlackout = TimeSpan.FromMinutes(FailureBlackoutDuration);
            try
            {
                lock (this.thisLock)
                {
                    if (DateTime.UtcNow.Subtract(this.LastFailure) >= FailureBlackout)
                    {
                        this.LastFailure = DateTime.UtcNow;
#pragma warning disable 618
                        EventLogger logger = EventLogger.UnsafeCreateEventLogger(this.eventSourceName, this);
#pragma warning restore 618
                        if (exception == null)
                        {
                            logger.UnsafeLogEvent(TraceEventType.Error, TracingEventLogCategory, (uint)EventLogEventId.FailedToTraceEvent, false,
                                traceString);
                        }
                        else
                        {
                            logger.UnsafeLogEvent(TraceEventType.Error, TracingEventLogCategory, (uint)EventLogEventId.FailedToTraceEventWithException, false,
                                traceString, exception.ToString());
                        }
                    }
                }
            }
            catch (Exception eventLoggerException)
            {
                if (Fx.IsFatal(eventLoggerException))
                {
                    throw;
                }
            }
        }
        
        void ShutdownTracing()
        {
            if (!this.calledShutdown)
            {
                this.calledShutdown = true;
                ShutdownTraceSource();
                ShutdownEtwProvider();             
            }
        }

        void ShutdownTraceSource()
        {            
            try
            {
                //MessagingEtwProvider.Provider.EventWriteAppDomainUnload(AppDomain.CurrentDomain.FriendlyName, DiagnosticTrace.ProcessName, DiagnosticTrace.ProcessId);
                this.TraceSource.Flush();
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                //log failure
                LogTraceFailure(null, exception);
            }         
        }

        [Fx.Tag.SecurityNote(Critical = "Access critical etwProvider field",
            Safe = "Doesn't leak info\\resources")]
        [SecuritySafeCritical]
        void ShutdownEtwProvider()
        {            
            try
            {                
                if (this.etwProvider != null)
                {
                    this.etwProvider.Dispose();
                    //no need to set this.etwProvider as null as Dispose() provides the necessary guard
                    //leaving it non-null protects trace calls from NullReferenceEx, CSDMain Bug 136228
                }                
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                //log failure
                LogTraceFailure(null, exception);
            }            
        }
    }
}
