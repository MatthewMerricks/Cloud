using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloud.Model
{
    public sealed partial class FilePathDictionary<T>
    {
        /// <summary>
        /// Used to flatten the values from each FilePathDictionary in a collection of FilePathDictionaries into a collection of all values
        /// </summary>
        /// <typeparam name="InnerT">Generic type of the FilePathDictionary to flatten</typeparam>
        private sealed class FlattenedFilePathDictionaryValues<InnerT> : ICollection<InnerT> where InnerT : class
        {
            ICollection<FilePathDictionary<InnerT>> unflattenedCollection;
            public FlattenedFilePathDictionaryValues(ICollection<FilePathDictionary<InnerT>> unflattenedCollection)
            {
                if (unflattenedCollection == null)
                {
                    throw new ArgumentException("unflattenedCollection parameter cannot be null");
                }
                this.unflattenedCollection = unflattenedCollection;
            }

            #region ICollection<InnerT> members
            public int Count
            {
                get
                {
                    return this.unflattenedCollection.Sum(currentInnerCollection => currentInnerCollection == null ? 0 : currentInnerCollection.Count);
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public void Add(InnerT item)
            {
                throw new NotSupportedException("Cannot add to read-only collection");
            }

            public void Clear()
            {
                throw new NotSupportedException("Cannot clear read-only collection");
            }

            public bool Contains(InnerT item)
            {
                return this.unflattenedCollection.Any(currentInnerCollection => currentInnerCollection.Values.Contains(item));
            }

            public void CopyTo(InnerT[] array, int arrayIndex)
            {
                if (arrayIndex < 0)
                {
                    throw new ArgumentException("arrayIndex must be non-negative");
                }
                if ((array.Length - arrayIndex) < this.Count)
                {
                    throw new ArgumentException("Not enough room to copy into array");
                }
                int currentDestinationIndex = arrayIndex;
                foreach (FilePathDictionary<InnerT> currentInnerCollection in unflattenedCollection)
                {
                    if (currentInnerCollection != null)
                    {
                        foreach (InnerT itemFromInnerCollection in currentInnerCollection.Values)
                        {
                            array[currentDestinationIndex] = itemFromInnerCollection;
                            currentDestinationIndex++;
                        }
                    }
                }
            }

            public bool Remove(InnerT item)
            {
                throw new NotSupportedException("Cannot remove from read-only collection");
            }
            #endregion

            private IEnumerable<InnerT> getFlattenedEnumerable()
            {
                foreach (FilePathDictionary<InnerT> currentInnerCollection in unflattenedCollection)
                {
                    if (currentInnerCollection != null)
                    {
                        foreach (InnerT itemFromInnerCollection in currentInnerCollection.Values)
                        {
                            yield return itemFromInnerCollection;
                        }
                    }
                }
            }
            #region IEnumerable<InnerT> members
            public IEnumerator<InnerT> GetEnumerator()
            {
                return getFlattenedEnumerable().GetEnumerator();
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
}
