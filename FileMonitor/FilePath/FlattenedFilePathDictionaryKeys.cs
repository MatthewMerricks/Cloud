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
        private class FlattenedFilePathDictionaryKeys<InnerT> : ICollection<FilePath> where InnerT : class
        {
            ICollection<FilePathDictionary<InnerT>> unflattenedCollection;
            public FlattenedFilePathDictionaryKeys(ICollection<FilePathDictionary<InnerT>> unflattenedCollection)
            {
                if (unflattenedCollection == null)
                {
                    throw new Exception("unflattenedCollection parameter cannot be null");
                }
                this.unflattenedCollection = unflattenedCollection;
            }

            #region ICollection<FilePath> members
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

            public void Add(FilePath item)
            {
                throw new Exception("Cannot add to read-only collection");
            }

            public void Clear()
            {
                throw new Exception("Cannot clear read-only collection");
            }

            public bool Contains(FilePath item)
            {
                return this.unflattenedCollection.Any(currentInnerCollection => currentInnerCollection.Keys.Contains(item));
            }

            public void CopyTo(FilePath[] array, int arrayIndex)
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
                        foreach (FilePath itemFromInnerCollection in currentInnerCollection.Keys)
                        {
                            array[currentDestinationIndex] = itemFromInnerCollection;
                            currentDestinationIndex++;
                        }
                        if (currentInnerCollection.CurrentValue != null)
                        {
                            array[currentDestinationIndex] = currentInnerCollection.CurrentFilePath;
                            currentDestinationIndex++;
                        }
                    }
                }
            }

            public bool Remove(FilePath item)
            {
                throw new Exception("Cannot remove from read-only collection");
            }
            #endregion

            private IEnumerable<FilePath> getFlattenedEnumerable()
            {
                foreach (FilePathDictionary<InnerT> currentInnerCollection in unflattenedCollection)
                {
                    if (currentInnerCollection != null)
                    {
                        foreach (FilePath itemFromInnerCollection in currentInnerCollection.Keys)
                        {
                            yield return itemFromInnerCollection;
                        }
                    }
                }
            }
            #region IEnumerable<FilePath> members
            public IEnumerator<FilePath> GetEnumerator()
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
