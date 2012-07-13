﻿//
// FileMetadata.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Static;

namespace CloudApiPublic.Model
{
    /// <summary>
    /// Contains data used to compare files or folders along with an MD5 checksum to establish identity
    /// </summary>
    public class FileMetadata
    {
        /// <summary>
        /// Section of comparable properties used to determine uniqueness of a file change
        /// </summary>
        public FileMetadataHashableProperties HashableProperties { get; set; }
    }
    /// <summary>
    /// Comparable properties used to determine uniqueness of a file change
    /// </summary>
    public struct FileMetadataHashableProperties
    {
        /// <summary>
        /// True if file change is a folder, otherwise false for files
        /// </summary>
        public bool IsFolder
        {
            get
            {
                return _isFolder;
            }
        }
        private bool _isFolder;

        /// <summary>
        /// Time file or folder was last accessed or written to
        /// </summary>
        public DateTime LastTime
        {
            get
            {
                return _lastTime;
            }
        }
        private DateTime _lastTime;

        /// <summary>
        /// Time file or folder was created
        /// </summary>
        public DateTime CreationTime
        {
            get
            {
                return _creationTime;
            }
        }
        private DateTime _creationTime;

        /// <summary>
        /// The byte size of a file (null for folders)
        /// </summary>
        public Nullable<long> Size
        {
            get
            {
                return _size;
            }
        }
        private Nullable<long> _size;

        /// <summary>
        /// Primary constructor for file metadata
        /// </summary>
        /// <param name="isFolder">True if file change is a folder, otherwise false for files</param>
        /// <param name="lastTime">Time file or folder was last accessed or written to</param>
        /// <param name="creationTime">Time file or folder was created</param>
        /// <param name="size">The byte size of a file (null for folders)</param>
        public FileMetadataHashableProperties(bool isFolder, Nullable<DateTime> lastTime, Nullable<DateTime> creationTime, Nullable<long> size)
        {
            this._isFolder = isFolder;
            this._lastTime = lastTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc);
            this._creationTime = creationTime ?? new DateTime(FileConstants.InvalidUtcTimeTicks, DateTimeKind.Utc);
            this._size = size;
        }
    }
    /// <summary>
    /// Custom comparer for FileMetadataHashableProperties which generates a better than
    /// default hash code for reduced hash conflicts when used in a Dictionary/Hashset
    /// </summary>
    public class FileMetadataHashableComparer : EqualityComparer<FileMetadataHashableProperties>
    {
        /// <summary>
        /// Override for equality comparison of FileMetadataHashableProperties
        /// </summary>
        /// <param name="x">First FileMetadataHashableProperties to compare</param>
        /// <param name="y">Second FileMetadataHashableProperties to compare</param>
        /// <returns>Returns true if FileMetadataHashableProperties inputs are equal, otherwise false</returns>
        public override bool Equals(FileMetadataHashableProperties x, FileMetadataHashableProperties y)
        {
            return x.IsFolder == y.IsFolder
                && x.LastTime == y.LastTime
                && x.CreationTime == y.CreationTime
                && x.Size == y.Size;
        }

        /// <summary>
        /// Improved hash code generation over the default,
        /// takes bits from all properties to fill a new Int32 to return
        /// </summary>
        /// <param name="obj">FileMetadataHashableProperties for generating hash</param>
        /// <returns>Returns hash code</returns>
        public override int GetHashCode(FileMetadataHashableProperties obj)
        {
            //IsFolder 1 bit max, take 1, 31 left for return
            //LastTime 64 bits max, take 10, 21 left for return
            //CreationTime 64 bits max, take 10, 11 left for return
            //Size 65 bits max, take 1 for null and another 10, 0 left for return
            byte[] returnBytes = new byte[4];
            if (obj.IsFolder)
                returnBytes[0] |= 0x01;// set first bit if IsFolder

            // Now we have 1 bit from IsFolder
            // Bits filled up to bit 1 in first byte of return

            byte[] lastTimeBytes = BitConverter.GetBytes(obj.LastTime.Ticks);
            // Take 7 bits from last byte of LastTime by bitwise and of binary 01111111
            // Shift those 7 bits left by 1 and bitwise or it to the first return byte
            returnBytes[0] |= (byte)((lastTimeBytes[BitConverter.IsLittleEndian ? 0 : 7] & 0x7F) << 1);

            // First return byte now filled

            // Take next bit from last byte of LastTime by bitwise and of binary 10000000
            // Shift that bit right (logical) by 7 and bitwise or it to the second return byte
            returnBytes[1] |= (byte)((uint)(lastTimeBytes[BitConverter.IsLittleEndian ? 0 : 7] & 0x80) >> 7);

            // Last byte of LastTime depleted

            // Take 2 bits from second to last byte of LastTime by bitwise and of binary 00000011
            // Shift those 2 bits left by 1 and bitwise or it to the second return byte
            returnBytes[1] |= (byte)((lastTimeBytes[BitConverter.IsLittleEndian ? 1 : 6] & 0x03) << 1);

            // Now we have 10 bits from LastTime
            // Bits filled up to bit 3 in second byte of return

            byte[] creationTimeBytes = BitConverter.GetBytes(obj.CreationTime.Ticks);
            // Take 5 bits from last byte of CreationType by bitwise and of binary 00011111
            // Shift those bits left by 3 and bitwise or it to the second return byte
            returnBytes[1] |= (byte)((creationTimeBytes[BitConverter.IsLittleEndian ? 0 : 7] & 0x1F) << 3);

            // Second return byte now filled

            // Take next 3 bits from last byte of CreationType by bitwise and of binary 11100000
            // Shift that bit right (logical) by 5 and bitwise or it to the third return byte
            returnBytes[2] |= (byte)((uint)(creationTimeBytes[BitConverter.IsLittleEndian ? 0 : 7] & 0xE0) >> 5);

            // Last byte of CreationTime depleted

            // Take 2 bits from second to last byte of CreationTime by bitwise and of binary 00000011
            // Shift those 2 bits left by 3 and bitwise or it to the third return byte
            returnBytes[2] |= (byte)((creationTimeBytes[BitConverter.IsLittleEndian ? 1 : 6] & 0x03) << 3);

            // Now we have 10 bits from CreationTime
            // Bits filled up to bit 5 in third byte of return

            if (obj.Size != null)
            {
                // If Size is not null, set bit accordingly via bitwise or of binary 00100000
                returnBytes[2] |= 0x20;

                // Now we have a bit for the non-null Size
                // Bits filled up to bit 6 in third byte of return

                byte[] sizeBytes = BitConverter.GetBytes((long)obj.Size);
                // Take 2 bits from last byte of Size by bitwise and of binary 00000011
                // Shift those 2 bits left by 6 and bitwise or it to the third return byte
                returnBytes[2] |= (byte)((sizeBytes[BitConverter.IsLittleEndian ? 0 : 7] & 0x03) << 6);

                // Third return byte now filled

                // Take next 6 bits from last byte of Size by bitwise and of binary 11111100
                // Shift those 6 bits right (logical) by 2 and bitwise or it to the last return byte
                returnBytes[3] |= (byte)((uint)(sizeBytes[BitConverter.IsLittleEndian ? 0 : 7] & 0xFC) >> 2);

                // Last byte of Size depleted

                // Take two bits from second to last byte of Size by bitwise or of binary 00000011
                // Shift those 2 bits left by 6 and bitwise or it to the last return byte
                returnBytes[3] |= (byte)((sizeBytes[BitConverter.IsLittleEndian ? 1 : 6] & 0x03) << 6);

                // Now we have 10 bits from Size
            }

            // Return filled bytes as int
            return BitConverter.ToInt32(returnBytes, 0);
        }
    }
}