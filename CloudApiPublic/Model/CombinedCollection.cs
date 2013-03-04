// CombinedCollection.cs
// Cloud Windows
//
// Created by DavidBruck.
//
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloud.Model
{
    /// <summary>
    /// Used to represent a new collection from two composite collections
    /// </summary>
    /// <typeparam name="T">Generic type for both composite collections</typeparam>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class CombinedCollection<T> : ICollection<T>
    {
        private ICollection<T> collectionOne;
        private ICollection<T> collectionTwo;

        public CombinedCollection(ICollection<T> collectionOne, ICollection<T> collectionTwo)
        {
            if (collectionOne == null)
            {
                throw new ArgumentException("First collection must not be null");
            }
            if (collectionTwo == null)
            {
                throw new ArgumentException("Second collection must not be null");
            }
            this.collectionOne = collectionOne;
            this.collectionTwo = collectionTwo;
        }

        #region ICollection<T> members
        public int Count
        {
            get
            {
                return collectionOne.Count + collectionTwo.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public void Add(T item)
        {
            throw new NotSupportedException("Cannot add to read-only collection");
        }

        public void Clear()
        {
            throw new NotSupportedException("Cannot clear read-only collection");
        }

        public bool Contains(T item)
        {
            return collectionOne.Contains(item)
                || collectionTwo.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
            {
                throw new ArgumentException("arrayIndex must be non-negative");
            }
            if ((array.Length - arrayIndex) < this.Count)
            {
                throw new ArgumentException("Not enough room to copy into array");
            }
            int currentTargetIndex = arrayIndex;
            foreach (T currentCollectionOneItem in collectionOne)
            {
                array[currentTargetIndex] = currentCollectionOneItem;
                currentTargetIndex++;
            }
            foreach (T currentCollectionTwoItem in collectionTwo)
            {
                array[currentTargetIndex] = currentCollectionTwoItem;
                currentTargetIndex++;
            }
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException("Cannot remove from read-only collection");
        }
        #endregion

        private IEnumerable<T> combineEnumerables()
        {
            int storeCount = this.Count;
            int collectionOneCount = this.collectionOne.Count;
            IEnumerator<T> collectionOneEnumerator = this.collectionOne.GetEnumerator();
            IEnumerator<T> collectionTwoEnumerator = this.collectionTwo.GetEnumerator();
            int currentIndex = 0;
            while (currentIndex < storeCount)
            {
                T toReturn;
                if (currentIndex < collectionOneCount)
                {
                    collectionOneEnumerator.MoveNext();
                    toReturn = collectionOneEnumerator.Current;
                }
                else
                {
                    collectionTwoEnumerator.MoveNext();
                    toReturn = collectionTwoEnumerator.Current;
                }
                currentIndex++;
                yield return toReturn;
            }
        }
        #region IEnumerable<T> members
        public IEnumerator<T> GetEnumerator()
        {
            return combineEnumerables().GetEnumerator();
        }
        #endregion 

        #region IEnumerable members
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
    }
}
