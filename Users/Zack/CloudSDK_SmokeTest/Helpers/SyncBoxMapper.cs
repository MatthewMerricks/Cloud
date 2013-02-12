using CloudApiPublic.Model;
using CloudSDK_SmopkeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Helpers

{
    public static class SyncBoxMapper
    {
        public static readonly Dictionary<int, long> SyncBoxes = new Dictionary<int, long>();
        public static readonly Dictionary<string, string> MappedPaths = new Dictionary<string, string>();

        /// <summary>
        ///     Will Load Contents of Local File Mapping Info from local file.
        /// </summary>
        /// <returns>
        ///     returns a Dictionary Where Key is a string of the Server Id and Value is the Local File Path 
        /// </returns>
        public static AllMappings GetMappings(string mappedItemsFilePath, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            return XMLHelper.GetMappingItems(mappedItemsFilePath, ref ProcessingErrorHolder);
        }
    }
}
