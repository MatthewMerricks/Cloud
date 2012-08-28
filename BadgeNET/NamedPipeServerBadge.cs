//
// NamedPipeServerBadge.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using CloudApiPublic.Support;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace BadgeNET
{
    public class NamedPipeServerBadge_UserState
    {
        public cloudAppIconBadgeType BadgeType;
        public Func<cloudAppIconBadgeType, string, bool> ShouldIconBeBadged;
        public FilePath FilePathCloudDirectory;
    }

    public class NamedPipeServerBadge : NamedPipeServer
    {
        CLTrace _trace = CLTrace.Instance;

        public override void ProcessClientCommunication(NamedPipeServerStream pipeStream, object userState)
        {
            // try/catch which silences errors and stops badging functionality (should never error here)
            try
            {
                NamedPipeServerBadge_UserState UserState = userState as NamedPipeServerBadge_UserState;
                if (UserState != null)
                {
                    // expect exactly 20 bytes from client (packetId<10> + filepath byte length<10>)
                    _trace.writeToLog(9, "IconOverlay: NamedPipeServerBadge. Data ready to read.");
                    byte[] pipeBuffer = new byte[20];
                    // read from client into buffer
                    pipeStream.Read(pipeBuffer,
                        0,
                        20);

                    // pull out badgeId from first ten bytes (each byte is an ASCII character)
                    string badgeId = new string(pipeBuffer.Take(10).Select(currentCharByte => (char)currentCharByte).ToArray());
                    // pull out filepath byte length from last ten bytes (each byte is an ASCII character)
                    string pathSize = new string(pipeBuffer.Skip(10).Take(10).Select(currentCharByte => (char)currentCharByte).ToArray());

                    // ensure data from client was readable by checking if the filepath byte length is parsable to int
                    int pathSizeParsed;
                    if (int.TryParse(pathSize, out pathSizeParsed))
                    {
                        // create buffer for second read from client with dynamic size equal to the filepath byte length
                        pipeBuffer = new byte[int.Parse(pathSize)];
                        // read filepath from client into buffer
                        pipeStream.Read(pipeBuffer,
                            0,
                            pipeBuffer.Length);

                        // convert unicode bytes from buffer into string
                        string filePath = Encoding.Unicode.GetString(pipeBuffer);

                        // define bool to send back to client:
                        // --true means use overlay
                        // --false means don't use overlay
                        bool setOverlay;

                        // lock on internal list so it is not modified while being read
                        _trace.writeToLog(9, "IconOverlay: NamedPipeServerBadge. Call ShouldIconBeBadged. Path: {0}, type: {1}.", filePath, UserState.BadgeType.ToString());
                        setOverlay = UserState.ShouldIconBeBadged(UserState.BadgeType, filePath);
                        _trace.writeToLog(9, "IconOverlay: NamedPipeServerBadge. Back from ShouldIconBeBadged. WillBadge: {0}.", setOverlay);

                        // Send the result back to the client on the pipe.
                        pipeStream.WriteByte(setOverlay ? (byte) 1 : (byte) 0);
                    }
                    else
                    {
                        _trace.writeToLog(9, "IconOverlay: NamedPipeServerBadge. ERROR: pathSize not parsed.");
                    }
                }
                else
                {
                    throw new NullReferenceException("userState must be castable to NamedPipeServerBadge_UserState");
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "IconOverlay: NamedPipeServerBadge: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
            }
            _trace.writeToLog(9, "IconOverlay: NamedPipeServerBadge. Return.  Done processing this client communication.");
        }
    }
}