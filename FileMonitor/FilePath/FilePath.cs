using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileMonitor
{
    /// <summary>
    /// Self-referencing file path storage,
    /// each instance contains a name and a link to it's parent,
    /// ToString returns a combined representation (i.e. "C:\A\B\C\D.txt"),
    /// Implicitly converted from FileInfo and DirectoryInfo
    /// </summary>
    public class FilePath
    {
        /// <summary>
        /// File or folder name
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }
        private string _name;
        /// <summary>
        /// Link to instance of parent directory, null for root; set this to reparent a subtree
        /// </summary>
        public FilePath Parent { get; set; }

        /// <summary>
        /// Construct file path with name of item and optionally a reference to the parent directory
        /// </summary>
        /// <param name="name">Name of file or folder</param>
        /// <param name="parent">(optional) Reference to parent directory</param>
        public FilePath(string name, FilePath parent = null)
        {
            this._name = name;
            this.Parent = parent;
        }

        /// <summary>
        /// Copies a FilePath structure (recurvisely includes parent directories) such as for modifications
        /// </summary>
        /// <returns></returns>
        public FilePath Copy()
        {
            if (this.Parent == null)
            {
                return new FilePath(this.Name);
            }
            else
            {
                return new FilePath(this.Name,
                    this.Parent.Copy());
            }
        }

        /// <summary>
        /// Overridden ToString returns a full path representation of the current instance (i.e. "C:\A\B\C\D.txt")
        /// </summary>
        /// <returns>Full path string</returns>
        public override string ToString()
        {
            if (this.Parent == null)
            {
                return Name;
            }
            else
            {
                return ((FilePath)this.Parent).ToString() + "\\" + Name;
            }
        }

        // Implicitly converts DirectoryInfo to FilePath, loses all data except the path itself
        public static implicit operator FilePath(DirectoryInfo directory)
        {
            if (directory == null)
            {
                return null;
            }

            return new FilePath(directory.Name, directory.Parent);
        }

        // Implicitly converts FileInfo to FilePath, loses all data except the path itself
        public static implicit operator FilePath(FileInfo file)
        {
            if (file == null)
            {
                return null;
            }

            return new FilePath(file.Name, file.Directory);
        }

        // Implicitly converts a full path string to FilePath by first creating a DirectoryInfo
        // (which is then implicitly converted)
        public static implicit operator FilePath(string fullPath)
        {
            // Must use DirectoryInfo implicit converter instead of FileInfo because
            // "C:\\" produces a FileInfo without a name
            return new DirectoryInfo(fullPath);
        }
    }
}
