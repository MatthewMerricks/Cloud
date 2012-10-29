using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncTestServer
{
    public interface IMetadataProvider
    {
        void InitializeProvider(IServerStorage storage, IEnumerable<InitialMetadata> initialData = null);
        bool AddFolderMetadata(int userId, FilePath relativePathFromRoot, FileMetadata metadata);
        bool AddFileMetadata(int userId, Guid deviceId, FilePath relativePathFromRoot, FileMetadata metadata, out bool isPending, out bool newUpload, byte[] MD5);
        bool TryGetMetadata(int userId, FilePath relativePathFromRoot, out FileMetadata metadata);
        bool RecursivelyRemoveMetadata(int userId, FilePath relativePathFromRoot);
        bool RecursivelyRenameMetadata(int userId, FilePath relativePathFrom, FilePath relativePathTo);
        bool UpdateMetadata(int userId, Guid deviceId, string revision, FilePath relativePathFromRoot, FileMetadata metadata, out bool isPending, out bool newUpload, out bool conflict, byte[] MD5 = null);
    }
}