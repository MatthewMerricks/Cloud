using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloud.Model
{
    /// <summary>
    /// Comparer to be used in all dictionaries or hashsets on FilePath objects,
    /// also used to perform a deep compare between FilePath objects manually
    /// </summary>
    public sealed class FilePathComparer : EqualityComparer<FilePath>
    {
        /// <summary>
        /// Overridden Equals for comparing FilePaths by deep compare
        /// </summary>
        /// <param name="x">First FilePath to compare</param>
        /// <param name="y">Second FilePath to compare</param>
        /// <returns>Returns true for equality, otherwise false</returns>
        public override bool Equals(FilePath x, FilePath y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return RecursiveEqualityCheck(x, y, false);
        }
        public bool CaseInsensitiveEquals(FilePath x, FilePath y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return RecursiveEqualityCheck(x, y, true);
        }
        private static bool RecursiveEqualityCheck(FilePath x, FilePath y, bool insensitiveNameSearch)
        {
            // check local Name property first
            // if Parents are null then both are roots and along with equal name represents equality
            // otherwise if both Parents are not null but running Equals recursively returns true then FilePaths are also equal
            return x.Equals(y)// if object references are equal we're sure the FilePaths match, otherwise continue on to deep level compare
                || (string.Equals(x.Name, y.Name, (insensitiveNameSearch ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture))
                    && ((x.Parent == null
                            && y.Parent == null)
                        || (x.Parent != null
                            && y.Parent != null
                            && RecursiveEqualityCheck((FilePath)x.Parent, (FilePath)y.Parent, insensitiveNameSearch))));
        }
        /// <summary>
        /// Overridden GetHashCode that gets a hash from the underlying full path string,
        /// could be improved for efficiency
        /// </summary>
        /// <param name="obj">FilePath to hash</param>
        /// <returns>Returns hashcode of underlying full path string</returns>
        public override int GetHashCode(FilePath obj)
        {
            // Grabs the full path string representing a FilePath object and uses that to return a hashcode
            return obj.ToString().GetHashCode();
        }
        /// <summary>
        /// Public static instance to be used everywhere the FilePathComparer is needed
        /// </summary>
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
        /// <summary>
        /// Private constructor to ensure other classes only use the public static Instance
        /// </summary>
        private FilePathComparer() { }
    }
}
