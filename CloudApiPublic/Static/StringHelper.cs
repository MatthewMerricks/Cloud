using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Static
{
    public static class StringHelper
    {
        public static bool PathEndsWithSlash(this string path)
        {
            bool _endsWithSlash;
            if (path.LastIndexOf('\\') == (path.Count() - 1))
            {
                _endsWithSlash = true;
            }
            else
            {
                _endsWithSlash = false;
            }

            return _endsWithSlash;
        }

        public static string TrimTrailingSlash(this string path)
        {
            string _returnValue;

            if (PathEndsWithSlash(path))
            {
                _returnValue = path.Remove(path.Count() - 1, 1);
            }
            else
            {
                _returnValue = path;
            }

            return _returnValue;
        }
    }
}
