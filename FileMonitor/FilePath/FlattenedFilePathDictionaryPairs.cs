using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileMonitor
{
    public partial class FilePathDictionary<T>
    {
        private class FlattenedFilePathDictionaryPairs<InnerT> : ICollection<KeyValuePair<FilePath, InnerT>> where InnerT : class
        {
            ICollection<FilePathDictionary<InnerT>> unflattenedCollection;
            public FlattenedFilePathDictionaryPairs(ICollection<FilePathDictionary<InnerT>> unflattenedCollection)
            {
                if (unflattenedCollection == null)
                {
                    throw new Exception("unflattenedCollection parameter cannot be null");
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
                throw new Exception("Cannot add to read-only collection");
            }

            public void Clear()
            {
                throw new Exception("Cannot clear read-only collection");
            }

            public bool Contains(KeyValuePair<FilePath, InnerT> item)
            {
                return this.unflattenedCollection.Any(currentInnerCollection => currentInnerCollection.Contains(item));
            }

            public void CopyTo(KeyValuePair<FilePath, InnerT>[] array, int arrayIndex)
            {
                if (arrayIndex < 0)
                {
                    throw new Exception("arrayIndex must be non-negative");
                }
                if ((array.Length - arrayIndex) < this.Count)
                {
                    throw new Exception("Not enough room to copy into array");
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
                throw new Exception("Cannot remove from read-only collection");
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
