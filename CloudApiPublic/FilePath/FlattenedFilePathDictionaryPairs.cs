using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiPublic.Model
{
    public partial class FilePathDictionary<T>
    {
        /// <summary>
        /// Used to flatten the key value pairs from each FilePathDictionary in a collection of FilePathDictionaries into a collection of all pairs
        /// </summary>
        /// <typeparam name="InnerT">Generic type of the FilePathDictionary to flatten</typeparam>
        private class FlattenedFilePathDictionaryPairs<InnerT> : ICollection<KeyValuePair<FilePath, InnerT>> where InnerT : class
        {
            ICollection<FilePathDictionary<InnerT>> unflattenedCollection;
            public FlattenedFilePathDictionaryPairs(ICollection<FilePathDictionary<InnerT>> unflattenedCollection)
            {
                if (unflattenedCollection == null)
                {
                    throw new ArgumentException("unflattenedCollection parameter cannot be null");
                }
                this.unflattenedCollection = unflattenedCollection;
            }

            #region ICollection<KeyValuePair<FilePath, InnerT>> members
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

            public void Add(KeyValuePair<FilePath, InnerT> item)
            {
                throw new NotSupportedException("Cannot add to read-only collection");
            }

            public void Clear()
            {
                throw new NotSupportedException("Cannot clear read-only collection");
            }

            public bool Contains(KeyValuePair<FilePath, InnerT> item)
            {
                return this.unflattenedCollection.Any(currentInnerCollection => currentInnerCollection.Contains(item));
            }

            public void CopyTo(KeyValuePair<FilePath, InnerT>[] array, int arrayIndex)
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
                        foreach (KeyValuePair<FilePath, InnerT> itemFromInnerCollection in currentInnerCollection)
                        {
                            array[currentDestinationIndex] = itemFromInnerCollection;
                            currentDestinationIndex++;
                        }
                    }
                }
            }

            public bool Remove(KeyValuePair<FilePath, InnerT> item)
            {
                throw new NotSupportedException("Cannot remove from read-only collection");
            }
            #endregion

            private IEnumerable<KeyValuePair<FilePath, InnerT>> getFlattenedEnumerable()
            {
                foreach (FilePathDictionary<InnerT> currentInnerCollection in unflattenedCollection)
                {
                    if (currentInnerCollection != null)
                    {
                        foreach (KeyValuePair<FilePath, InnerT> itemFromInnerCollection in currentInnerCollection)
                        {
                            yield return itemFromInnerCollection;
                        }
                    }
                }
            }
            #region IEnumerable<KeyValuePair<FilePath, InnerT>> members
            public IEnumerator<KeyValuePair<FilePath, InnerT>> GetEnumerator()
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
