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
        private class FlattenedFilePathDictionaryValues<InnerT> : ICollection<InnerT> where InnerT : class
        {
            ICollection<FilePathDictionary<InnerT>> unflattenedCollection;
            public FlattenedFilePathDictionaryValues(ICollection<FilePathDictionary<InnerT>> unflattenedCollection)
            {
                if (unflattenedCollection == null)
                {
                    throw new Exception("unflattenedCollection parameter cannot be null");
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
                throw new Exception("Cannot add to read-only collection");
            }

            public void Clear()
            {
                throw new Exception("Cannot clear read-only collection");
            }

            public bool Contains(InnerT item)
            {
                return this.unflattenedCollection.Any(currentInnerCollection => currentInnerCollection.Values.Contains(item));
            }

            public void CopyTo(InnerT[] array, int arrayIndex)
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
                throw new Exception("Cannot remove from read-only collection");
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
