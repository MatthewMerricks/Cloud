using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model
{
    internal sealed class FileChangeMergeToStateArgs : HandleableEventArgs
    {
        public FileChangeMerge MergedFileChanges
        {
            get
            {
                return _mergedFileChanges;
            }
        }
        private FileChangeMerge _mergedFileChanges;

        public FileChangeMergeToStateArgs(FileChangeMerge mergedFileChanges)
        {
            this._mergedFileChanges = mergedFileChanges;
        }
    }
}