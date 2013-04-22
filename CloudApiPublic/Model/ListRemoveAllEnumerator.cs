//
//  ListRemoveAllEnumerator.cs
//  Cloud SDK Windows
//
//  Created by DavidBruck.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Cloud.Model
{
    internal sealed class ListRemoveAllEnumerator<T> : IEnumerator<T>
    {
        private bool Disposed = false;
        private bool block = true;
        private readonly object blocker = new object();
        private bool endReached = false;

        private readonly List<T> toEnumerate;
        private readonly Predicate<T> whenToRemove;
        private readonly Thread ProcessorThread;

        public ListRemoveAllEnumerator(List<T> toEnumerate, Predicate<T> whenToRemove)
        {
            this.toEnumerate = toEnumerate;
            this.whenToRemove = whenToRemove;
            ProcessorThread = new Thread(new ThreadStart(RemoveProcessor));
            ProcessorThread.Start();
        }

        private void RemoveProcessor()
        {
            try
            {
                lock (blocker)
                {
                    if (block)
                    {
                        Monitor.Wait(blocker);
                    }
                }

                if (toEnumerate != null)
                {
                    toEnumerate.RemoveAll(new Predicate<T>(ShouldRemoveItem));
                }

                endReached = true;
                _current = default(T);

                lock (blocker)
                {
                    block = true;
                    Monitor.Pulse(blocker);
                }
            }
            catch (ThreadAbortException ex)
            {
            }
        }

        private bool ShouldRemoveItem(T currentItem)
        {
            if (whenToRemove == null
                || !whenToRemove(currentItem))
            {
                return false;
            }

            _current = currentItem;

            lock (blocker)
            {
                block = true;
                Monitor.Pulse(blocker);
            }

            lock (blocker)
            {
                if (block)
                {
                    Monitor.Wait(blocker);
                }
            }

            return true;
        }

        #region IEnumerator of T members

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>The element in the collection at the current position of the enumerator.</returns>
        public T Current
        {
            get
            {
                if (Disposed)
                {
                    throw new ObjectDisposedException("This ListRemoveAllEnumerator has already been disposed");
                }

                return _current;
            }
            private set
            {
                _current = value;
            }
        }
        private T _current;

        #endregion

        #region IDisposable members

        // Standard IDisposable implementation based on MSDN System.IDisposable
        ~ListRemoveAllEnumerator()
        {
            Dispose(false);
        }
        // Standard IDisposable implementation based on MSDN System.IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region IEnumerator

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>The current element in the collection.</returns>
        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("This ListRemoveAllEnumerator has already been disposed");
            }

            if (endReached)
            {
                return false;
            }
            else
            {
                lock (blocker)
                {
                    block = false;
                    Monitor.Pulse(blocker);
                }

                lock (blocker)
                {
                    if (!block)
                    {
                        Monitor.Wait(blocker);
                    }
                }

                return !endReached;
            }
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <exception cref="System.NotSupportedException">Always throws this exception</exception>
        public void Reset()
        {
            throw new NotSupportedException("Reset not supported");
        }

        #endregion

        // Standard IDisposable implementation based on MSDN System.IDisposable
        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                //if (disposing)
                //{
                //// dispose managed objects in here
                //}

                // dispose remove all thread here if it hasn't already fully enumerated
                if (!endReached)
                {
                    try
                    {
                        ProcessorThread.Abort();
                    }
                    catch
                    {
                    }
                }

                Disposed = true;
            }
        }
    }
}