using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiPublic.Model
{
    /// <summary>
    /// Self-referencing file path storage,
    /// each instance contains a name and a link to it's parent,
    /// ToString returns a combined representation (i.e. "C:\A\B\C\D.txt"),
    /// Implicitly converted from FileInfo and DirectoryInfo
    /// </summary>
    public sealed class FilePath
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
        /// Character length of path
        /// </summary>
        public int Length
        {
            get
            {
                return (this.Name == null
                    ? (this.Parent == null
                        ? 0
                        : this.Parent.Length)
                    : this.Name.Length +
                        (this.Parent == null
                            ? 0
                            : 1 + this.Parent.Length));//+1 is for the slash path seperator
            }
        }

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
                string parentPath = ((FilePath)this.Parent).ToString();
                return (!string.IsNullOrEmpty(parentPath)
                        && parentPath[parentPath.Length - 1] == '\\'
                    ? parentPath + Name
                    : parentPath + "\\" + Name);
            }
        }

        // Implicitly converts DirectoryInfo to FilePath, loses all data except the path itself
        public static implicit operator FilePath(DirectoryInfo directory)
        {
            // Null check and return for nulls
            if (directory == null)
            {
                return null;
            }

            return new FilePath(directory.Name, directory.Parent);
        }

        // Implicitly converts FileInfo to FilePath, loses all data except the path itself
        public static implicit operator FilePath(FileInfo file)
        {
            // Null check and return for nulls
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
            // Null check and return for nulls
            if (fullPath == null)
            {
                return null;
            }
            // Must use DirectoryInfo implicit converter instead of FileInfo because
            // "C:\\" produces a FileInfo without a name
            return new DirectoryInfo(fullPath);
        }

        /// <summary>
        /// Returns the overlap of two paths, if any
        /// </summary>
        /// <param name="firstPath">First path to attempt overlap</param>
        /// <param name="secondPath">Second path to attempt overlap</param>
        /// <returns>Returns overlapped path, if any</returns>
        public static FilePath FindOverlappingPath(FilePath firstPath, FilePath secondPath)
        {
            // Function to find the common root path between the old and new paths.
            // This was created as an seperate function to provide a clean way to
            // break out of a double while loop when the overlapping path was found.
            // Example of function:
            // For an oldPath of "C:\A\B\C\D.txt"
            // and a newPath of "C:\A\B\E\F.txt",
            // the common root is "C:\A\B"

            FilePath recurseOldPath;
            FilePath recurseNewPath = secondPath;
            while (recurseNewPath != null)
            {
                recurseOldPath = firstPath;
                while (recurseOldPath != null)
                {
                    if (FilePathComparer.Instance.Equals(recurseOldPath, recurseNewPath))
                    {
                        return recurseNewPath;
                    }

                    recurseOldPath = recurseOldPath.Parent;
                }

                recurseNewPath = recurseNewPath.Parent;
            }
            return null;
        }

        public static string GetRelativePath(FilePath fullPath, FilePath relativeRoot, bool replaceWithForwardSlashes)
        {
            if (fullPath == null)
            {
                return null;
            }
            FilePath overlappingRoot = FilePath.FindOverlappingPath(fullPath, relativeRoot);
            if (overlappingRoot == null
                || !FilePathComparer.Instance.Equals(relativeRoot, overlappingRoot))
            {
                return fullPath.ToString();
            }
            
            string relativePath = fullPath.ToString().Substring(overlappingRoot.ToString().Length);

            if (replaceWithForwardSlashes)
            {
                return relativePath.Replace('\\', '/');
            }
            return relativePath;
        }

        public string GetRelativePath(FilePath relativeRoot, bool replaceWithForwardSlashes)
        {
            return GetRelativePath(this, relativeRoot, replaceWithForwardSlashes);
        }

        /// <summary>
        /// Returns the overlap of current path with another path, if any
        /// </summary>
        /// <param name="otherPath">Another path to attempt overlap</param>
        /// <returns>Returns overlapped path, if any</returns>
        public FilePath FindOverlappingPath(FilePath otherPath)
        {
            return FindOverlappingPath(this, otherPath);
        }
    }
}
