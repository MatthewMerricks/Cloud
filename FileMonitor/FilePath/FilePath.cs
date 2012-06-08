using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileMonitor
{
    public class FilePath
    {
        public string Name
        {
            get
            {
                return _name;
            }
        }
        private string _name;
        public FilePath Parent
        {
            get
            {
                return _parent;
            }
        }
        private FilePath _parent;

        public FilePath(string name, FilePath parent = null)
        {
            this._name = name;
            this._parent = parent;
        }

        public override string ToString()
        {
            if (_parent == null)
            {
                return Name;
            }
            else
            {
                return ((FilePath)_parent).ToString() + "\\" + Name;
            }
        }

        public static implicit operator FilePath(DirectoryInfo directory)
        {
            if (directory == null)
            {
                return null;
            }

            return new FilePath(directory.Name, directory.Parent);
        }

        public static implicit operator FilePath(FileInfo file)
        {
            if (file == null)
            {
                return null;
            }

            return new FilePath(file.Name, file.Directory);
        }
    }
}
