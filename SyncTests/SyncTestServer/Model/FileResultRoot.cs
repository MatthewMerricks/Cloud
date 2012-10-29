using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncTestServer.Model
{
    public sealed class FileResultRoot : IFileResultParent
    {
        private string RootName;

        public string FullName
        {
            get
            {
                return RootName;
            }
        }

        public FileResultRoot(string rootName)
        {
            this.RootName = rootName;
        }
    }
}