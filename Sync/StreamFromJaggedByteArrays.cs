//
// StreamFromJaggedByteArray.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;

//namespace Sync
//{
//    public class StreamFromJaggedByteArrays : Stream
//    {
//        /* sample usage */
//        //string pushResponseString = null;
//        //// Bug in MDS: ContentLength is not set so I cannot read the stream to compare against it
//        //if (pushResponse.ContentLength > 0)
//        //{
//        //    // code to allow reading an array of bytes larger than int.MaxValue while checking size against ContentLength

//        //    int lastBucketSize = (int)(pushResponse.ContentLength % int.MaxValue);
//        //    int numberOfEarlierBuckets = (int)((pushResponse.ContentLength - lastBucketSize) / int.MaxValue);
//        //    byte[][] pushResponseBytes = new byte[numberOfEarlierBuckets + 1][];
//        //    int currentBucketNumber = -1;
//        //    int spaceLeftInBucket = 0;
//        //    using (Stream pushResponseStream = pushResponse.GetResponseStream())
//        //    {
//        //        byte[] data = new byte[CLDefinitions.SyncConstantsResponseBufferSize];
//        //        int read;
//        //        while ((read = pushResponseStream.Read(data, 0, data.Length)) > 0)
//        //        {
//        //            if (read > spaceLeftInBucket)
//        //            {
//        //                currentBucketNumber++;
//        //                if (currentBucketNumber + 1 == pushResponseBytes.Length)
//        //                {
//        //                    if (read - spaceLeftInBucket > lastBucketSize)
//        //                    {
//        //                        throw new ArgumentOutOfRangeException("Number of bytes received exceeded content-length");
//        //                    }

//        //                    pushResponseBytes[currentBucketNumber] = new byte[lastBucketSize];
//        //                }
//        //                else
//        //                {
//        //                    pushResponseBytes[currentBucketNumber] = new byte[int.MaxValue];
//        //                }

//        //                if (spaceLeftInBucket == 0)
//        //                {
//        //                    Buffer.BlockCopy(data, 0, pushResponseBytes[currentBucketNumber], 0, read);
//        //                }
//        //                else
//        //                {
//        //                    Buffer.BlockCopy(data, 0, pushResponseBytes[currentBucketNumber - 1], int.MaxValue - spaceLeftInBucket, spaceLeftInBucket);
//        //                    Buffer.BlockCopy(data, read - spaceLeftInBucket, pushResponseBytes[currentBucketNumber], 0, read - spaceLeftInBucket);
//        //                }
//        //                spaceLeftInBucket = pushResponseBytes[currentBucketNumber].Length - read + spaceLeftInBucket;
//        //            }
//        //            else
//        //            {
//        //                Buffer.BlockCopy(data, 0, pushResponseBytes[currentBucketNumber], int.MaxValue - spaceLeftInBucket, read);
                                    
//        //                spaceLeftInBucket -= read;
//        //            }
//        //        }
//        //        if (currentBucketNumber != numberOfEarlierBuckets
//        //            || spaceLeftInBucket != 0)
//        //        {
//        //            throw new ArgumentOutOfRangeException("Number of bytes received is less than content-length");
//        //        }
//        //    }
//        //    using (Stream pushResponseStream = new StreamFromJaggedByteArrays(pushResponseBytes))
//        //    {
//        //        using (StreamReader pushResponseStreamReader = new StreamReader(pushResponseStream, Encoding.UTF8))
//        //        {
//        //            pushResponseString = pushResponseStreamReader.ReadToEnd();
//        //        }
//        //    }
//        //}
//        //else
//        //{
//        //    // this should be the condition for no response body,
//        //    // but ContentLength is not set due to an MDS Bug,
//        //    // try to read the response anyways

//        //    try
//        //    {
//        //        using (Stream pushResponseStream = pushResponse.GetResponseStream())
//        //        {
//        //            using (StreamReader pushResponseStreamReader = new StreamReader(pushResponseStream, Encoding.UTF8))
//        //            {
//        //                pushResponseString = pushResponseStreamReader.ReadToEnd();
//        //            }
//        //        }
//        //    }
//        //    catch
//        //    {
//        //    }
//        //}



//        private byte[][] data;
//        private long streamPosition = 0;
//        private readonly long streamLength;
//        private readonly int bufferSize;

//        private const int DEFAULT_BUFFER_SIZE = 1048576;
//        public StreamFromJaggedByteArrays(byte[][] data) : this(data, DEFAULT_BUFFER_SIZE) { }
//        public StreamFromJaggedByteArrays(byte[][] data, int bufferSize)
//        {
//            if (data == null)
//            {
//                throw new NullReferenceException("data cannot be null");
//            }
//            if (bufferSize <= 0)
//            {
//                throw new ArgumentOutOfRangeException("bufferSize must be greater than zero");
//            }

//            this.data = data;
//            this.streamLength = ((long)(((long)(data.Length - 1)) * ((long)int.MaxValue))) + ((long)data[data.Length - 1].Length);
//            this.bufferSize = bufferSize;
//        }

//        public override bool CanRead { get { return true; } }
//        public override bool CanWrite { get { return false; } }
//        public override bool CanSeek { get { return true; } }
//        public override long Length { get { return streamLength; } }
//        public override long Position
//        {
//            get { return streamPosition; }
//            set
//            {
//                if (value == Position)
//                {
//                    return;
//                }
//                if (value < 0 || value >= streamLength)
//                {
//                    throw new ArgumentOutOfRangeException("Position must be between zero and Length");
//                }
//                streamPosition = value;
//            }
//        }
//        public override long Seek(long offset, SeekOrigin origin)
//        {
//            switch (origin)
//            {
//                case SeekOrigin.Begin:
//                    Position = offset;
//                    break;
//                case SeekOrigin.End:
//                    Position = streamLength + offset;
//                    break;
//                case SeekOrigin.Current:
//                    Position += offset;
//                    break;
//                default:
//                    throw new ArgumentException("Unknown value for origin: " + origin.ToString());
//            }
//            return streamLength;
//        }
//        public override void SetLength(long value)
//        {
//            throw new NotSupportedException("StreamFromJaggedByteArray is not writable");
//        }
//        public override int Read(byte[] buffer, int offset, int count)
//        {
//            if (data == null)
//            {
//                throw new ObjectDisposedException(typeof(StreamFromJaggedByteArrays).Name);
//            }
//            if (buffer == null)
//            {
//                throw new NullReferenceException("buffer cannot be null");
//            }
//            long maxRead = streamLength - streamPosition;
//            if (count > maxRead)
//            {
//                count = (int)maxRead;
//            }

//            if (count > buffer.Length - offset)
//            {
//                throw new ArgumentOutOfRangeException("Length of buffer is not large enough to copy into");
//            }

//            int bucketOffset = (int)(streamPosition % int.MaxValue);
//            int bucketNumber = (int)((streamPosition - ((long)bucketOffset)) / int.MaxValue);

//            if (int.MaxValue - bucketOffset < count)
//            {
//                Buffer.BlockCopy(data[bucketNumber], bucketOffset, buffer, offset, int.MaxValue - bucketOffset);
//                Buffer.BlockCopy(data[bucketNumber + 1], 0, buffer, (int.MaxValue - bucketOffset) + offset, count - (int.MaxValue - bucketOffset));
//            }
//            else
//            {
//                Buffer.BlockCopy(data[bucketNumber], bucketOffset, buffer, offset, count);
//            }

//            streamPosition += count;
//            return count;
//        }
//        public override void Write(byte[] buffer, int offset, int count)
//        {
//            throw new NotSupportedException("StreamFromJaggedByteArray is not writable");
//        }

//        protected override void Dispose(bool disposing)
//        {
//            if (disposing)
//            {
//                data = null;
//            }
//            base.Dispose(disposing);
//        }

//        public override void Flush() { }
//    }
//}