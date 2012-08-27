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
        public FilePathDictionary<GenericHolder<cloudAppIconBadgeType>> AllBadges;
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
                        setOverlay = ShouldIconBeBadged(UserState.BadgeType, filePath, UserState.AllBadges, UserState.FilePathCloudDirectory);
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

        /// <summary>
        /// Determine whether this icon should be badged by this badge handler.
        /// </summary>
        /// <param name="pipeParams"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool ShouldIconBeBadged(
                                    cloudAppIconBadgeType badgeType, 
                                    string filePath, 
                                    FilePathDictionary<GenericHolder<cloudAppIconBadgeType>> AllBadges,
                                    FilePath FilePathCloudDirectory)
        {
            try
            {
                _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Entry.");
                // Convert the badgetype and filepath to objects.
                FilePath objFilePath = filePath;
                GenericHolder<cloudAppIconBadgeType> objBadgeType = new GenericHolder<cloudAppIconBadgeType>(badgeType);

                // Lock and query the in-memory database.
                lock (AllBadges)
                {
                    // There will be no badge if the path doesn't contain Cloud root
                    _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Locked.");
                    if (objFilePath.Contains(FilePathCloudDirectory))
                    {
                        // If the value at this path is set and it is our type, then badge.
                        _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Contains Cloud root.");
                        GenericHolder<cloudAppIconBadgeType> tempBadgeType;
                        bool success = AllBadges.TryGetValue(objFilePath, out tempBadgeType);
                        if (success)
                        {
                            bool rc = (tempBadgeType.Value == objBadgeType.Value);
                            _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Return: {0}.", rc);
                            return rc;
                        }
                        else
                        {
                            // If an item is marked selective, then none of its children (whole hierarchy of children) should be badged.
                            _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. TryGetValue not successful.");
                            if (!FilePathComparer.Instance.Equals(objFilePath, FilePathCloudDirectory))
                            {
                                // Recurse through parents of this node up to and including the CloudPath.
                                _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Recurse thru parents.");
                                FilePath node = objFilePath;
                                while (node != null)
                                {
                                    // Return false if the value of this node is not null, and is marked SyncSelective
                                    _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Get the type for path: {0}.", node.ToString());
                                    success = AllBadges.TryGetValue(node, out tempBadgeType);
                                    if (success && tempBadgeType != null)
                                    {
                                        // Got the badge type at this level.
                                        _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Got type {0}.", tempBadgeType.Value.ToString());
                                        if (tempBadgeType.Value == cloudAppIconBadgeType.cloudAppBadgeSyncSelective)
                                        {
                                            _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Return false.");
                                            return false;
                                        }
                                    }

                                    // Quit if we notified the Cloud directory root
                                    _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Have we reached the Cloud root?");
                                    if (FilePathComparer.Instance.Equals(node, FilePathCloudDirectory))
                                    {
                                        _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Break to determine the badge status from the children of this node.");
                                        break;
                                    }

                                    // Chain up
                                    _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Chain up.");
                                    node = node.Parent;
                                }
                            }

                            // Determine the badge type from the hierarchy at this path
                            return DetermineBadgeStatusFromHierarchyOfChildrenOfThisNode(badgeType, AllBadges, objFilePath);
                        }
                    }
                    else
                    {
                        // This path is not in the Cloud folder.  Don't badge.
                        _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Not in the Cloud folder.  Don't badge.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Exception.  Normal? Msg: {0}, Code: (1).", error.errorDescription, error.errorCode);
                return false;
            }
        }

        /// <summary>
        /// Determine whether we should badge with this badge type at this path.
        /// </summary>
        /// <param name="badgeType">The badge type.</param>
        /// <param name="AllBadges">The current badge dictionary.</param>
        /// <param name="objFilePath">The path to test.</param>
        /// <returns></returns>
        private bool DetermineBadgeStatusFromHierarchyOfChildrenOfThisNode(cloudAppIconBadgeType badgeType, FilePathDictionary<GenericHolder<cloudAppIconBadgeType>> AllBadges, FilePath objFilePath)
        {
            try
            {
                // Get the hierarchy of children of this node.
                _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Get the hierarchy for path: {0}.", objFilePath.ToString());
                FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> tree;
                CLError error = AllBadges.GrabHierarchyForPath(objFilePath, out tree);
                if (error == null)
                {
                    _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Successful getting the hierarcy.  Call GetDesiredBadgeTypeViaRecursivePostorderTraversal.");
                    // Chase the children hierarchy using recursive postorder traversal to determine the desired badge type.
                    cloudAppIconBadgeType desiredBadgeType = GetDesiredBadgeTypeViaRecursivePostorderTraversal(tree);
                    bool rc = (badgeType == desiredBadgeType);
                    _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Return(2): {0}.", rc);
                    return rc;
                }
                else
                {
                    bool rc = (badgeType == cloudAppIconBadgeType.cloudAppBadgeSynced);
                    _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Return(3): {0}.", rc);
                    return rc;
                }
            }
            catch
            {
                bool rc = (badgeType == cloudAppIconBadgeType.cloudAppBadgeSynced);
                _trace.writeToLog(9, "IconOverlay: ShouldIconBeBadged. Return(4): {0}.", rc);
                return rc;
            }
        }

        /// <summary>
        /// Determine the desired badge type of a node based on the badging state of its children.
        /// </summary>
        /// <param name="node">The selected node.</param>
        /// <returns>cloudAddIconBadgeType: The desired badge type.</returns>
        private cloudAppIconBadgeType GetDesiredBadgeTypeViaRecursivePostorderTraversal(FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> node)
        {
            // If the node doesn't exist, that means synced
            if (node == null)
            {
                return cloudAppIconBadgeType.cloudAppBadgeSynced;
            }

            // Loop through all of the node's children
            foreach (FilePathHierarchicalNode<GenericHolder<cloudAppIconBadgeType>> child in node.Children)
            {
                cloudAppIconBadgeType returnBadgeType = GetDesiredBadgeTypeViaRecursivePostorderTraversal(child);
                if (returnBadgeType != cloudAppIconBadgeType.cloudAppBadgeSynced)
                {
                    return returnBadgeType;
                }
            }

            // Process by whether the node has a value.  If not, it is synced.
            if (node.HasValue)
            {
                switch (node.Value.Value.Value)
                {
                    case cloudAppIconBadgeType.cloudAppBadgeSynced:
                        return cloudAppIconBadgeType.cloudAppBadgeSynced;
                    case cloudAppIconBadgeType.cloudAppBadgeSyncing:
                        return cloudAppIconBadgeType.cloudAppBadgeSyncing;
                    case cloudAppIconBadgeType.cloudAppBadgeFailed:
                        return cloudAppIconBadgeType.cloudAppBadgeSyncing;
                    case cloudAppIconBadgeType.cloudAppBadgeSyncSelective:
                        return cloudAppIconBadgeType.cloudAppBadgeSynced;
                }
            }
            else
            {
                return cloudAppIconBadgeType.cloudAppBadgeSynced;
            }

            return cloudAppIconBadgeType.cloudAppBadgeSynced;
        }
    }
}
