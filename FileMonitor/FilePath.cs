//
// MonitorAgent.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudApiPrivate.Common;

namespace FileMonitor
{
    public class FilePath
    {
        public string Name
        {
            get
            {
                return _name;
            }
        }
        private string _name;
        public FilePath Parent
        {
            get
            {
                return _parent;
            }
        }
        private FilePath _parent;

        public FilePath(string name, FilePath parent = null)
        {
            this._name = name;
            this._parent = parent;
        }

        public override string ToString()
        {
            if (_parent == null)
            {
                return Name;
            }
            else
            {
                return ((FilePath)_parent).ToString() + "\\" + Name;
            }
        }

        public static implicit operator FilePath(DirectoryInfo directory)
        {
            if (directory == null)
            {
                return null;
            }

            return new FilePath(directory.Name, directory.Parent);
        }

        public static implicit operator FilePath(FileInfo file)
        {
            if (file == null)
            {
                return null;
            }

            return new FilePath(file.Name, file.Directory);
        }
    }
    public class FilePathComparer : EqualityComparer<FilePath>
    {
        public override bool Equals(FilePath x, FilePath y)
        {
            return x.Name == y.Name
                && ((x.Parent == null
                        && y.Parent == null)
                    || (x.Parent != null
                        && y.Parent != null
                        && Equals((FilePath)x.Parent, (FilePath)y.Parent)));
        }
        public override int GetHashCode(FilePath obj)
        {
            return obj.ToString().GetHashCode();
        }
    }
    public class FilePathDictionary<T> : IDictionary<FilePath, T> where T : class
    {
        private FilePath CurrentFilePath;
        private T CurrentValue;
        private Action<FilePath, T> recursiveDeleteCallback;
        public FilePathDictionary(FilePath rootPath, Action<FilePath, T> recursiveDeleteCallback = null, T valueAtFolder = null)
        {
            this.CurrentFilePath = rootPath;
            this.CurrentValue = valueAtFolder;
            this.recursiveDeleteCallback = recursiveDeleteCallback;
        }

        public static FilePathComparer Comparer = new FilePathComparer();
        private Dictionary<FilePath, FilePathDictionary<T>> innerFolders = null;
        private Dictionary<FilePath, T> pathsAtCurrentLevel = null;
        private int _count = 0;

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
                    return new CombinedCollection<FilePath>(new FlattenedFilePathDictionaryKeys<T>(innerFolders.Values),
                        innerFolders.Keys);
                }
                else
                {
                    return new CombinedCollection<FilePath>(new CombinedCollection<FilePath>(new FlattenedFilePathDictionaryKeys<T>(innerFolders.Values),
                            innerFolders.Keys),
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
                    innerFolders[key].CurrentValue = value;
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
            while (!Comparer.Equals(rootSearch, this.CurrentFilePath))
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
                            pathsAtCurrentLevel = new Dictionary<FilePath, T>(Comparer);
                        }
                        _count++;
                        pathsAtCurrentLevel.Add(key, value);
                    }
                    else
                    {
                        if (innerFolders == null)
                        {
                            innerFolders = new Dictionary<FilePath, FilePathDictionary<T>>(Comparer);
                        }
                        FilePathDictionary<T> innerFolder = new FilePathDictionary<T>(rootChild,
                            this.recursiveDeleteCallback);
                        innerFolder.Add(key, value);
                        _count++;
                        innerFolders.Add(rootChild, innerFolder);
                    }
                }
                else if (Comparer.Equals((FilePath)existingCurrentPath, key))
                {
                    throw alreadyExistsException;
                }
                else
                {
                    if (innerFolders == null)
                    {
                        innerFolders = new Dictionary<FilePath, FilePathDictionary<T>>(Comparer);
                    }
                    FilePathDictionary<T> innerFolder = new FilePathDictionary<T>(rootChild,
                        this.recursiveDeleteCallback,
                        pathsAtCurrentLevel[(FilePath)existingCurrentPath]);
                    innerFolder.Add(key, value);
                    innerFolders.Add(rootChild, innerFolder);
                    pathsAtCurrentLevel.Remove(rootChild);
                }
            }
            else if (Comparer.Equals((FilePath)existingInnerFolder, key))
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
            if (Comparer.Equals(CurrentFilePath, key))
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
                    recursiveDeleteCallback(currentLevelPath.Key, currentLevelPath.Value);
                }
                pathsAtCurrentLevel.Clear();
            }
            if (innerFolders != null)
            {
                foreach (FilePathDictionary<T> currentInnerFolder in innerFolders.Values)
                {
                    if (currentInnerFolder.CurrentValue != null)
                    {
                        recursiveDeleteCallback(currentInnerFolder.CurrentFilePath, currentInnerFolder.CurrentValue);
                    }
                    currentInnerFolder.Clear();
                }
            }
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