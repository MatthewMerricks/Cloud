//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Diagnostics;

    class TraceLevelHelper
    {
        static TraceEventType[] EtwLevelToTraceEventType = { TraceEventType.Critical, TraceEventType.Critical, TraceEventType.Error,
                TraceEventType.Warning, TraceEventType.Information, TraceEventType.Verbose
            }
            ;

        static TraceEventType EtwOpcodeToTraceEventType(TraceEventOpcode opcode)
        {
            if (opcode == TraceEventOpcode.Start)
            {
                return TraceEventType.Start;
            }
            if (opcode == TraceEventOpcode.Stop)
            {
                return TraceEventType.Stop;
            }
            if (opcode == TraceEventOpcode.Suspend)
            {
                return TraceEventType.Suspend;
            }
            if (opcode == TraceEventOpcode.Resume)
            {
                return TraceEventType.Resume;
            }

            return TraceEventType.Information;
        }

        internal static TraceEventType GetTraceEventType(byte level, byte opcode)
        {
            if (opcode == (byte)TraceEventOpcode.Info)
            {
                return EtwLevelToTraceEventType[(int)level];
            }
            else
            {
                return EtwOpcodeToTraceEventType((TraceEventOpcode)opcode);
            }
        }

        internal static TraceEventType GetTraceEventType(TraceEventLevel level)
        {
            return EtwLevelToTraceEventType[(int)level];
        }

        internal static TraceEventType GetTraceEventType(byte level)
        {
            return EtwLevelToTraceEventType[(int)level];
        }

        internal static string LookupSeverity(TraceEventLevel level, TraceEventOpcode opcode)
        {
            string severity;
            if (opcode == TraceEventOpcode.Info)
            {
                switch (level)
                {
                    case TraceEventLevel.Critical:
                        severity = "Critical";
                        break;
                    case TraceEventLevel.Error:
                        severity = "Error";
                        break;
                    case TraceEventLevel.Warning:
                        severity = "Warning";
                        break;
                    case TraceEventLevel.Informational:
                        severity = "Information";
                        break;
                    case TraceEventLevel.Verbose:
                        severity = "Verbose";
                        break;
                    default:
                        severity = level.ToString();
                        break;
                }
            }
            else
            {
                switch (opcode)
                {
                    case TraceEventOpcode.Start:
                        severity = "Start";
                        break;
                    case TraceEventOpcode.Stop:
                        severity = "Stop";
                        break;
                    case TraceEventOpcode.Suspend:
                        severity = "Suspend";
                        break;
                    case TraceEventOpcode.Resume:
                        severity = "Resume";
                        break;
                    default:
                        severity = opcode.ToString();
                        break;
                }
            }
            return severity;
        }
    }
}
