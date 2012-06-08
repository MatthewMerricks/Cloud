using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudApiPrivate.Common;

namespace FileMonitor
{
    public partial class FilePathDictionary<T> : IDictionary<FilePath, T> where T : class
    {
        #region private members
        private FilePath CurrentFilePath;
        private T CurrentValue;
        private Action<FilePath, RecursiveDeleteArgs<T>> recursiveDeleteCallback;
        private Action<FilePath, RecursiveRenameArgs<T>> recursiveRenameCallback;
        #endregion

        #region private collection members
        private Dictionary<FilePath, FilePathDictionary<T>> innerFolders = null;
        private Dictionary<FilePath, T> pathsAtCurrentLevel = null;
        private int _count;
        #endregion

        public FilePathDictionary(FilePath rootPath,
            Action<FilePath, RecursiveDeleteArgs<T>> recursiveDeleteCallback = null,
            Action<FilePath, RecursiveRenameArgs<T>> recursiveRenameCallback = null,
            T valueAtFolder = null)
        {
            this.CurrentFilePath = rootPath;
            this.CurrentValue = valueAtFolder;
            if (this.CurrentValue == null)
            {
                _count = 0;
            }
            else
            {
                _count = 1;
            }
            this.recursiveDeleteCallback = recursiveDeleteCallback;
            this.recursiveRenameCallback = recursiveRenameCallback;
        }

        #region non-interface public methods
        public void Rename(FilePath oldPath, FilePath newPath)
        {
            if (FilePathComparer.Instance.Equals(oldPath, newPath))
            {
                throw new Exception("oldPath and newPath cannot be the same");
            }
            if (FilePathComparer.Instance.Equals(CurrentFilePath, oldPath))
            {
                CurrentFilePath = newPath;

                if (pathsAtCurrentLevel != null)
                {
                    foreach (KeyValuePair<FilePath, T> currentLevelPath in pathsAtCurrentLevel.ToArray())
                    {
                        recursiveRenameCallback(currentLevelPath.Key, currentLevelPath.Value);
                        if (pathsAtCurrentLevel.Remove(currentLevelPath))
                        {
                            
                        }
                        else
                        {
                        }
                    }
                }
            }
            else if (pathsAtCurrentLevel != null && pathsAtCurrentLevel.ContainsKey(oldPath))
            {
                T oldValue = pathsAtCurrentLevel[oldPath];
                if (pathsAtCurrentLevel.Remove(oldPath))
                {
                    pathsAtCurrentLevel.Add(newPath, oldValue);
                }
                else
                {
                    throw new Exception("Removal of path at current level by key returned false");
                }
            }
            else if (innerFolders != null)
            {
                if (innerFolders.ContainsKey(oldPath))
                {
                    FilePathDictionary<T> oldValue = innerFolders[oldPath];
                    if (recursiveRenameCallback != null)
                    {
                        foreach (FilePath innerPath in oldValue.Keys)
                        {
                            
                        }
                        
                        foreach (KeyValuePair<FilePath, T> innerPath in oldValue)
                        {
                            recursiveRenameCallback(innerPath.Key, new RecursiveRenameArgs<T>(innerPath.Value));
                        }
                    }
                    if (innerFolders.Remove(oldPath))
                    {
                        innerFolders.Add(newPath, oldValue);
                    }
                    else
                    {
                        throw new Exception("Removal of inner folder path by key returned false");
                    }
                }
                else
                {
                }
            }
            else
            {
                throw new Exception("oldPath not found in dictionary");
            }
        }
        #endregion

        #region interface implementors

        #region IDictionary<FilePath, T> members
        public ICollection<FilePath> Keys
        {
            get
            {
                if (innerFolders == null)
                {
                    if (pathsAtCurrentLevel == null)
                    {
                        return new FilePath[0];
                    }
                    else
                    {
                        return pathsAtCurrentLevel.Keys;
                    }
                }
                else if (pathsAtCurrentLevel == null)
                {
                    return new FlattenedFilePathDictionaryKeys<T>(innerFolders.Values);
                }
                else
                {
                    return new CombinedCollection<FilePath>(new FlattenedFilePathDictionaryKeys<T>(innerFolders.Values),
                        pathsAtCurrentLevel.Keys);
                }
            }
        }


        public ICollection<T> Values
        {
            get
            {
                T[] currentValueAsArray = this.CurrentValue == null
                    ? new T[0]
                    : new T[] { this.CurrentValue };
                if (innerFolders == null)
                {
                    if (pathsAtCurrentLevel == null)
                    {
                        return currentValueAsArray;
                    }
                    else
                    {
                        return new CombinedCollection<T>(pathsAtCurrentLevel.Values,
                            currentValueAsArray);
                    }
                }
                else if (pathsAtCurrentLevel == null)
                {
                    return new CombinedCollection<T>(new FlattenedFilePathDictionaryValues<T>(innerFolders.Values),
                        currentValueAsArray);
                }
                else
                {
                    return new CombinedCollection<T>(new CombinedCollection<T>(new FlattenedFilePathDictionaryValues<T>(innerFolders.Values),
                            pathsAtCurrentLevel.Values),
                        currentValueAsArray);
                }
            }
        }

        public T this[FilePath key]
        {
            get
            {
                if (pathsAtCurrentLevel != null && pathsAtCurrentLevel.ContainsKey(key))
                {
                    return pathsAtCurrentLevel[key];
                }
                if (innerFolders != null)
                {
                    if (innerFolders.ContainsKey(key))
                    {
                        T storeCurrentValue = innerFolders[key].CurrentValue;
                        if (storeCurrentValue != null)
                        {
                            return innerFolders[key].CurrentValue;
                        }
                    }
                    else
                    {
                        FilePath recurseParent = key;
                        while ((recurseParent = recurseParent.Parent) != null)
                        {
                            if (innerFolders.ContainsKey(recurseParent))
                            {
                                return innerFolders[recurseParent][key];
                            }
                        }
                    }
                }
                throw new Exception("Value not found for provided FilePath key");
            }
            set
            {
                if (value == null)
                {
                    throw new Exception("Cannot set index to null value");
                }
                Exception notFoundException = new Exception("Previous value not found for provided FilePath key");
                if (pathsAtCurrentLevel != null && pathsAtCurrentLevel.ContainsKey(key))
                {
                    pathsAtCurrentLevel[key] = value;
                }
                else if (innerFolders == null)
                {
                    throw notFoundException;
                }
                else if (innerFolders.ContainsKey(key))
                {
                    FilePathDictionary<T> innerFolder = innerFolders[key];
                    if (innerFolder.CurrentValue == null)
                    {
                        innerFolder._count++;
                    }
                    innerFolder.CurrentValue = value;
                }
                else
                {
                    FilePath recurseParent = key;
                    while ((recurseParent = recurseParent.Parent) != null)
                    {
                        if (innerFolders.ContainsKey(recurseParent))
                        {
                            innerFolders[recurseParent][key] = value;
                            break;
                        }
                    }
                    if (recurseParent == null)
                    {
                        throw notFoundException;
                    }
                }
            }
        }

        public void Add(FilePath key, T value)
        {
            FilePath rootSearch = key;
            FilePath rootChild = key;
            FilePath existingInnerFolder = null;
            FilePath existingCurrentPath = null;
            bool rootSearchStarted = false;
            while (!FilePathComparer.Instance.Equals(rootSearch, this.CurrentFilePath))
            {
                rootSearchStarted = true;
                if (rootSearch.Parent == null)
                {
                    throw new Exception("FilePath key to add does not belong in current FilePath root");
                }
                if (innerFolders != null && innerFolders.ContainsKey(rootSearch))
                {
                    existingInnerFolder = rootSearch;
                }
                else if (pathsAtCurrentLevel != null && pathsAtCurrentLevel.ContainsKey(rootSearch))
                {
                    existingCurrentPath = rootSearch;
                }
                rootChild = rootSearch;
                rootSearch = rootSearch.Parent;
            }
            Exception alreadyExistsException = new Exception("FilePath key already exists in collection");
            if (!rootSearchStarted)
            {
                throw alreadyExistsException;
            }
            if (existingInnerFolder == null)
            {
                if (existingCurrentPath == null)
                {
                    if (rootChild.Equals(key))
                    {
                        if (pathsAtCurrentLevel == null)
                        {
                            pathsAtCurrentLevel = new Dictionary<FilePath, T>(FilePathComparer.Instance);
                        }
                        _count++;
                        pathsAtCurrentLevel.Add(key, value);
                    }
                    else
                    {
                        if (innerFolders == null)
                        {
                            innerFolders = new Dictionary<FilePath, FilePathDictionary<T>>(FilePathComparer.Instance);
                        }
                        FilePathDictionary<T> innerFolder = new FilePathDictionary<T>(rootChild,
                            this.recursiveDeleteCallback);
                        innerFolder.Add(key, value);
                        _count++;
                        innerFolders.Add(rootChild, innerFolder);
                    }
                }
                else if (FilePathComparer.Instance.Equals(existingCurrentPath, key))
                {
                    throw alreadyExistsException;
                }
                else
                {
                    if (innerFolders == null)
                    {
                        innerFolders = new Dictionary<FilePath, FilePathDictionary<T>>(FilePathComparer.Instance);
                    }
                    FilePathDictionary<T> innerFolder = new FilePathDictionary<T>(rootChild,
                        this.recursiveDeleteCallback,
                        this.recursiveRenameCallback,
                        pathsAtCurrentLevel[existingCurrentPath]);
                    innerFolder.Add(key, value);
                    innerFolders.Add(rootChild, innerFolder);
                    pathsAtCurrentLevel.Remove(rootChild);
                }
            }
            else if (FilePathComparer.Instance.Equals((FilePath)existingInnerFolder, key))
            {
                throw alreadyExistsException;
            }
            else
            {
                _count++;
                innerFolders[(FilePath)existingInnerFolder].Add(key, value);
            }
        }

        public bool ContainsKey(FilePath key)
        {
            if (pathsAtCurrentLevel != null && pathsAtCurrentLevel.ContainsKey(key))
            {
                return true;
            }
            if (innerFolders != null)
            {
                FilePath recursePath = key;
                while (recursePath != null)
                {
                    if (innerFolders.ContainsKey(recursePath))
                    {
                        return innerFolders[recursePath].ContainsKey(key);
                    }

                    recursePath = recursePath.Parent;
                }
            }
            return false;
        }

        public bool Remove(FilePath key)
        {
            if (FilePathComparer.Instance.Equals(CurrentFilePath, key))
            {
                Clear();
                return true;
            }
            if (pathsAtCurrentLevel != null && pathsAtCurrentLevel.ContainsKey(key))
            {
                if (pathsAtCurrentLevel.Remove(key))
                {
                    _count--;
                    return true;
                }
                return false;
            }
            if (innerFolders != null)
            {
                FilePath recursePath = key;
                while (recursePath != null)
                {
                    if (innerFolders.ContainsKey(recursePath))
                    {
                        if (innerFolders[recursePath].Remove(key))
                        {
                            _count--;
                            return true;
                        }
                        return false;
                    }

                    recursePath = recursePath.Parent;
                }
            }
            return false;
        }

        public bool TryGetValue(FilePath key, out T value)
        {
            if (pathsAtCurrentLevel != null && pathsAtCurrentLevel.ContainsKey(key))
            {
                value = pathsAtCurrentLevel[key];
                return true;
            }
            if (innerFolders != null)
            {
                if (innerFolders.ContainsKey(key))
                {
                    T storeCurrentValue = innerFolders[key].CurrentValue;
                    if (storeCurrentValue != null)
                    {
                        value = storeCurrentValue;
                        return true;
                    }
                }
                else
                {
                    FilePath recurseParent = key;
                    while ((recurseParent = recurseParent.Parent) != null)
                    {
                        if (innerFolders.ContainsKey(recurseParent))
                        {
                            value = innerFolders[recurseParent][key];
                            return true;
                        }
                    }
                }
            }
            value = default(T);
            return false;
        }
        #endregion

        #region ICollection<KeyValuePair<FilePath, T>> members
        public int Count
        {
            get
            {
                return _count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public void Add(KeyValuePair<FilePath, T> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            if (pathsAtCurrentLevel != null)
            {
                foreach (KeyValuePair<FilePath, T> currentLevelPath in pathsAtCurrentLevel)
                {
                    recursiveDeleteCallback(currentLevelPath.Key, new RecursiveDeleteArgs<T>(currentLevelPath.Value));
                }
                pathsAtCurrentLevel.Clear();
            }
            if (innerFolders != null)
            {
                foreach (FilePathDictionary<T> currentInnerFolder in innerFolders.Values)
                {
                    if (currentInnerFolder.CurrentValue != null)
                    {
                        recursiveDeleteCallback(currentInnerFolder.CurrentFilePath, new RecursiveDeleteArgs<T>(currentInnerFolder.CurrentValue));
                    }
                    currentInnerFolder.Clear();
                }
            }
            this.CurrentValue = null;
            _count = 0;
        }

        public bool Contains(KeyValuePair<FilePath, T> item)
        {
            return ContainsKey(item.Key)
                && this[item.Key].Equals(item.Value);
        }

        public void CopyTo(KeyValuePair<FilePath, T>[] array, int arrayIndex)
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
            foreach (KeyValuePair<FilePath, T> currentPair in getPairs())
            {
                array[currentDestinationIndex] = currentPair;
                currentDestinationIndex++;
            }
        }

        public bool Remove(KeyValuePair<FilePath, T> item)
        {
            if (Contains(item))
            {
                return Remove(item.Key);
            }
            return false;
        }
        #endregion

        #region IEnumerable<KeyValuePair<FilePath, T>> members
        public IEnumerator<KeyValuePair<FilePath, T>> GetEnumerator()
        {
            return getPairs().GetEnumerator();
        }
        #endregion

        #region IEnumerable members
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #endregion

        #region private methods
        private ICollection<KeyValuePair<FilePath, T>> getPairs()
        {
            KeyValuePair<FilePath, T>[] currentValueAsArray = this.CurrentValue == null
                ? new KeyValuePair<FilePath, T>[0]
                : new KeyValuePair<FilePath, T>[] { new KeyValuePair<FilePath, T>(this.CurrentFilePath, this.CurrentValue) };
            if (innerFolders == null)
            {
                if (pathsAtCurrentLevel == null)
                {
                    return currentValueAsArray;
                }
                else
                {
                    return new CombinedCollection<KeyValuePair<FilePath, T>>(pathsAtCurrentLevel,
                        currentValueAsArray);
                }
            }
            else if (pathsAtCurrentLevel == null)
            {
                return new CombinedCollection<KeyValuePair<FilePath, T>>(new FlattenedFilePathDictionaryPairs<T>(innerFolders.Values),
                    currentValueAsArray);
            }
            else
            {
                return new CombinedCollection<KeyValuePair<FilePath, T>>(new CombinedCollection<KeyValuePair<FilePath, T>>(new FlattenedFilePathDictionaryPairs<T>(innerFolders.Values),
                        pathsAtCurrentLevel),
                    currentValueAsArray);
            }
        }
        #endregion
    }
}
