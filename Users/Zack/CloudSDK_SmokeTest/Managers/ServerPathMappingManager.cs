using Cloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    /// <summary>
    /// Incomplete 
    /// </summary>
    public sealed class ServerPathMappingManager
    {
        public readonly FilePathDictionary<FilePath> ClientToServer;
        public readonly FilePathDictionary<FilePath> ServerToClient;

        public static ServerPathMappingManager GetInstance(FilePath path)
        {
            lock (_instance)
            {
                if (_instance.Value == null)
                {
                    _instance.Value = new ServerPathMappingManager(path);
                }
                return _instance.Value;
            }

        }

        private static readonly GenericHolder<ServerPathMappingManager> _instance = new GenericHolder<ServerPathMappingManager>(null);
        private ServerPathMappingManager(FilePath path)
        { 
            //
            CLError clientToServerError = FilePathDictionary<FilePath>.CreateAndInitialize(path, out ClientToServer, null, OnRecursivePathRenameClientToServer, null);
            // check the error
            // create the other direction
        }

        private void OnRecursivePathRenameClientToServer(FilePath recursiveOldPath, FilePath recursiveRebuiltNewPath,
            FilePath mappedValuePath, FilePath originalOldPath, FilePath originalNewPath)
        {
            OnRecursivePathRename(recursiveOldPath, recursiveRebuiltNewPath, mappedValuePath, originalOldPath, originalNewPath, true);
        }

        private void OnRecursivePathRenameServerToClient(FilePath recursiveOldPath, FilePath recursiveRebuiltNewPath,
            FilePath mappedValuePath, FilePath originalOldPath, FilePath originalNewPath)
        {
            OnRecursivePathRename(recursiveOldPath, recursiveRebuiltNewPath, mappedValuePath, originalOldPath, originalNewPath, false);
        }

        private void OnRecursivePathRename(FilePath recursiveOldPath, FilePath recursiveRebuiltNewPath, 
            FilePath mappedValuePath, FilePath originalOldPath, FilePath originalNewPath, bool clientToServer)
        { 
            //// The following would work most of the time for renames, unless a parent path along the original paths had itself been remapped in the same dictionary
            //// so first check all names at all levels on both originals
            //
            ////Take the originalOldPath 
            //FilePath.ApplyRename(mappedValuePath, originalOldPath, originalNewPath);
        }
    }
}
