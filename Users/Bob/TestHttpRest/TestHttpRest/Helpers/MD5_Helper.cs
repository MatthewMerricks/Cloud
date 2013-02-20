using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CloudSDK_SmokeTest.Helpers
{
    public static class MD5_Helper
    {
        #region constants 
        public const int BufferSize = 4096;
        public static byte[] EmptyBuffer = new byte[0];
        #endregion 

        public static byte[] GetHashOfStream(System.IO.Stream inputStream, out long fileSize)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] fileBuffer = new byte[BufferSize];
            int fileReadBytes;
            long countFileSize = 0;

            while ((fileReadBytes = inputStream.Read(fileBuffer, 0, BufferSize)) > 0)
            {
                countFileSize += fileReadBytes;
                md5Hasher.TransformBlock(fileBuffer, 0, fileReadBytes, fileBuffer, 0);
            }

            md5Hasher.TransformFinalBlock(EmptyBuffer, 0, 0);
            fileSize = countFileSize;
            return md5Hasher.Hash;            
        }
    }
}
