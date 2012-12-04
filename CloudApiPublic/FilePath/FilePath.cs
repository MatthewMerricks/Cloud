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

        
        /// <summary>
        /// Implicitly converts DirectoryInfo to FilePath, loses all data except the path itself
        /// </summary>
        public static implicit operator FilePath(DirectoryInfo directory)
        {
            // Null check and return for nulls
            if (directory == null)
            {
                return null;
            }

            return new FilePath(directory.Name, directory.Parent);
        }

        /// <summary>
        /// Implicitly converts FileInfo to FilePath, loses all data except the path itself
        /// </summary>
        public static implicit operator FilePath(FileInfo file)
        {
            // Null check and return for nulls
            if (file == null)
            {
                return null;
            }

            return new FilePath(file.Name, file.Directory);
        }
        
        /// <summary>
        /// Implicitly converts a full path string to FilePath by first creating a DirectoryInfo
        /// (which is then implicitly converted)
        /// </summary>
        public static implicit operator FilePath(string fullPath)
        {
            // Null check and return for nulls
            if (fullPath == null)
            {
                return null;
            }

            ////// Cannot use System.IO for paths due to size limitation
            //// Must use DirectoryInfo implicit converter instead of FileInfo because
            //// "C:\\" produces a FileInfo without a name
            //return new DirectoryInfo(fullPath);

            if (fullPath.StartsWith("\\\\?\\"))
            {
                return fullPath.Substring(4);
            }

            int lastSlash;
            if ((lastSlash = fullPath.LastIndexOf("\\")) < -1)
            {
                throw new ArgumentException("fullPath is not a properly formatted file or folder absolute path");
            }

            int firstSlash;
            if ((firstSlash = fullPath.IndexOf("\\")) == lastSlash)
            {
                if (fullPath.EndsWith(":\\"))
                {
                    return new FilePath(fullPath);
                }

                return new FilePath(fullPath.Substring(lastSlash + 1),
                    fullPath.Substring(0, lastSlash + 1));
            }
            else
            {
                return new FilePath(fullPath.Substring(lastSlash + 1),
                    fullPath.Substring(0, lastSlash));
            }
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

        /// <summary>
        /// Builds a string portion of a path after a specified relative root to the end of the given full path, possibly swapping the slashes for communication
        /// </summary>
        /// <param name="fullPath">Path to make relative</param>
        /// <param name="relativeRoot">Full path to the root base of the relative path to return</param>
        /// <param name="replaceWithForwardSlashes">Whether to replace the default Windows backslash in the resulting path with forward slash for directory seperation</param>
        /// <returns>Returns the relative path</returns>
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

        /// <summary>
        /// Builds a string portion of a path after a specified relative root to the end of this FilePath, possibly swapping the slashes for communication
        /// </summary>
        /// <param name="relativeRoot">Full path to the root base of the relative path to return</param>
        /// <param name="replaceWithForwardSlashes">Whether to replace the default Windows backslash in the resulting path with forward slash for directory seperation</param>
        /// <returns>Returns the relative path</returns>
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

        /// <summary>
        /// Determines whether or not the second path is contained within the first path (including if both are perfectly equal)
        /// </summary>
        /// <param name="outerPath">First path that may contain the second path</param>
        /// <param name="innerPath">Second path which may be contained in the first path</param>
        /// <returns>Returns true if the second path is contained within the first path, otherwise false</returns>
        public static bool Contains(FilePath outerPath, FilePath innerPath)
        {
            if (innerPath == null)
            {
                return false;// I do not know if this logically makes sense since anything and nothing contains nothing? Still, null should never be used as a second input.
            }
            while (outerPath != null)
            {
                if (FilePathComparer.Instance.Equals(outerPath, innerPath))
                {
                    return true;
                }

                outerPath = outerPath.Parent;
            }
            return false;
        }

        /// <summary>
        /// Determines whether or not an inner path is contained within the current path (including if both are perfectly equal)
        /// </summary>
        /// <param name="innerPath">The inner path which may be contained in the current path</param>
        /// <returns>Returns true if the inner path is contained within the current path, otherwise false</returns>
        public bool Contains(FilePath innerPath)
        {
            return Contains(this, innerPath);
        }

        /// <summary>
        /// Modifies a FilePath to swap a parent structure at a certain point to effectively rename it via an exact path rename or a rename of a parent directory;
        /// be careful because it modifies the actual FilePath which may be referenced elsewhere and not meant to be changed
        /// </summary>
        /// <param name="toModify">The path to apply a rename which will be modified</param>
        /// <param name="rootOldPath">Previous path which was renamed, either the exact path to rename or a path to a parent directory</param>
        /// <param name="rootNewPath">The new path of the rename after the change from the rootOldPath name</param>
        public static void ApplyRename(FilePath toModify, FilePath rootOldPath, FilePath rootNewPath)
        {
            if (toModify == null)
            {
                throw new NullReferenceException("toModify cannot be null");
            }
            if (rootOldPath == null)
            {
                throw new NullReferenceException("rootOldPath cannot be null");
            }
            if (rootNewPath == null)
            {
                throw new NullReferenceException("rootNewPath cannot be null");
            }

            if (FilePathComparer.Instance.Equals(toModify, rootOldPath))
            {
                toModify._name = rootNewPath._name;
                toModify.Parent = rootNewPath.Parent;
            }
            else
            {
                while (toModify.Parent != null)
                {
                    if (FilePathComparer.Instance.Equals(toModify.Parent, rootOldPath))
                    {
                        toModify.Parent = rootNewPath;
                        return;
                    }

                    toModify = toModify.Parent;
                }
                throw new KeyNotFoundException("Unable to find overlap between renamed paths or unable to rename");
            }
        }
    }
}