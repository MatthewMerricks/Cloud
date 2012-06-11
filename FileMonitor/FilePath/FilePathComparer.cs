using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileMonitor
{
    public class FilePathComparer : EqualityComparer<FilePath>
    {
        public override bool Equals(FilePath x, FilePath y)
        {
            return x.Name == y.Name
                && ((x.Parent == null
                        && y.Parent == null)
                    || (x.Parent != null
                        && y.Parent != null
                        && Equals((FilePath)x.Parent, (FilePath)y.Parent)));
        }
        public override int GetHashCode(FilePath obj)
        {
            return obj.ToString().GetHashCode();
        }
        public static FilePathComparer Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new FilePathComparer();
                    }
                    return _instance;
                }
            }
        }
        private static FilePathComparer _instance = null;
        private static object InstanceLocker = new object();
        private FilePathComparer() { }
    }
}
