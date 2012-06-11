using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudApiPrivate.Common;
using CloudApiPublic.Model;

namespace FileMonitor
{
    public partial class FilePathDictionary<T> : IDictionary<FilePath, T> where T : class
    {
        #region private members
        private FilePath CurrentFilePath;
        private T CurrentValue;
        private Action<FilePath, T> recursiveDeleteCallback;
        private Action<FilePath, FilePath, T> recursiveRenameCallback;
        #endregion

        #region private collection members
        private Dictionary<FilePath, FilePathDictionary<T>> innerFolders = null;
        private Dictionary<FilePath, T> pathsAtCurrentLevel = null;
        private int _count;
        #endregion

        private FilePathDictionary() { }

        #region non-interface public methods
        public static CLError CreateAndInitialize(FilePath rootPath,
            out FilePathDictionary<T> pathDictionary,
            Action<FilePath, T> recursiveDeleteCallback = null,
            Action<FilePath, FilePath, T> recursiveRenameCallback = null,
            T valueAtFolder = null)
        {
            try
            {
                pathDictionary = new FilePathDictionary<T>();
            }
            catch (Exception ex)
            {
                pathDictionary = null;
                return ex;
            }
            try
            {
                if (rootPath == null)
                {
                    throw new Exception("rootPath cannot be null");
                }
                pathDictionary.CurrentFilePath = rootPath;
                pathDictionary.CurrentValue = valueAtFolder;
                if (pathDictionary.CurrentValue == null)
                {
                    pathDictionary._count = 0;
                }
                else
                {
                    pathDictionary._count = 1;
                }
                pathDictionary.recursiveDeleteCallback = recursiveDeleteCallback;
                pathDictionary.recursiveRenameCallback = recursiveRenameCallback;
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        public CLError Rename(FilePath oldPath, FilePath newPath)
        {
            try
            {
                if (oldPath == null)
                {
                    throw new Exception("oldPath cannot be null");
                }
                if (newPath == null)
                {
                    throw new Exception("newPath cannot be null");
                }
                if (FilePathComparer.Instance.Equals(oldPath, newPath))
                {
                    throw new Exception("oldPath and newPath cannot be the same");
                }

                Func<FilePath, FilePath, FilePath> findOverlappingPath = (anonOldPath, anonNewPath) =>
                    {
                        FilePath recurseOldPath;
                        FilePath recurseNewPath = anonNewPath;
                        while (recurseNewPath != null)
                        {
                            recurseOldPath = anonOldPath;
                            while (recurseOldPath != null)
                            {
                                if (FilePathComparer.Instance.Equals(recurseOldPath, recurseNewPath))
                                {
                                    return recurseNewPath;
                                }

                                recurseOldPath = recurseOldPath.Parent;
                            }

                            recurseNewPath = recurseNewPath.Parent;
                        }
                        return null;
                    };

                FilePath overlappingPath = findOverlappingPath(oldPath, newPath);

                if (FilePathComparer.Instance.Equals(overlappingPath, this.CurrentFilePath))
                {
                    Rename(oldPath, newPath, null, this, this);
                }
                else
                {
                    Rename(oldPath, newPath, overlappingPath, null, this);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        #endregion

        #region interface implementors

        #region IDictionary<FilePath, T> members
        public ICollection<FilePath> Keys
        {
            get
            {
                FilePath[] currentKeyAsArray = this.CurrentValue == null
                    ? new FilePath[0]
                    : new FilePath[] { this.CurrentFilePath };
                if (innerFolders == null)
                {
                    if (pathsAtCurrentLevel == null)
                    {
                        return currentKeyAsArray;
                    }
                    else
                    {
                        return new CombinedCollection<FilePath>(pathsAtCurrentLevel.Keys,
                            currentKeyAsArray);
                    }
                }
                else if (pathsAtCurrentLevel == null)
                {
                    return new CombinedCollection<FilePath>(new FlattenedFilePathDictionaryKeys<T>(innerFolders.Values),
                        currentKeyAsArray);
                }
                else
                {
                    return new CombinedCollection<FilePath>(new CombinedCollection<FilePath>(new FlattenedFilePathDictionaryKeys<T>(innerFolders.Values),
                            pathsAtCurrentLevel.Keys),
                        currentKeyAsArray);
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
                Func<Exception> notFoundException = () => new Exception("Previous value not found for provided FilePath key");
                if (pathsAtCurrentLevel != null && pathsAtCurrentLevel.ContainsKey(key))
                {
                    pathsAtCurrentLevel[key] = value;
                }
                else if (innerFolders == null)
                {
                    throw notFoundException();
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
                        throw notFoundException();
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
            Func<Exception> alreadyExistsException = () => new Exception("FilePath key already exists in collection");
            if (!rootSearchStarted)
            {
                throw alreadyExistsException();
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
                        FilePathDictionary<T> innerFolder;
                        CLError innerFolderError = FilePathDictionary<T>.CreateAndInitialize(rootChild,
                            out innerFolder,
                            this.recursiveDeleteCallback,
                            this.recursiveRenameCallback);
                        if (innerFolderError != null)
                        {
                            throw (Exception)innerFolderError.errorInfo[CLError.ErrorInfo_Exception];
                        }
                        innerFolder.Add(key, value);
                        _count++;
                        innerFolders.Add(rootChild, innerFolder);
                    }
                }
                else if (FilePathComparer.Instance.Equals(existingCurrentPath, key))
                {
                    throw alreadyExistsException();
                }
                else
                {
                    if (innerFolders == null)
                    {
                        innerFolders = new Dictionary<FilePath, FilePathDictionary<T>>(FilePathComparer.Instance);
                    }
                    FilePathDictionary<T> innerFolder;
                    CLError innerFolderError = FilePathDictionary<T>.CreateAndInitialize(rootChild,
                        out innerFolder,
                        this.recursiveDeleteCallback,
                        this.recursiveRenameCallback,
                        pathsAtCurrentLevel[existingCurrentPath]);
                    if (innerFolderError != null)
                    {
                        throw (Exception)innerFolderError.errorInfo[CLError.ErrorInfo_Exception];
                    }
                    _count++;
                    innerFolder.Add(key, value);
                    innerFolders.Add(rootChild, innerFolder);
                    pathsAtCurrentLevel.Remove(rootChild);
                }
            }
            else
            {
                FilePathDictionary<T> checkCurrentValue = innerFolders[(FilePath)existingInnerFolder];
                if (FilePathComparer.Instance.Equals((FilePath)existingInnerFolder, key))
                {
                    if (checkCurrentValue.CurrentValue == null)
                    {
                        _count++;
                        checkCurrentValue._count++;
                        checkCurrentValue.CurrentValue = value;
                    }
                    else
                    {
                        throw alreadyExistsException();
                    }
                }
                else
                {
                    _count++;
                    checkCurrentValue.Add(key, value);
                }
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

        private void Rename(FilePath oldPath, FilePath newPath, FilePath overlappingPath, FilePathDictionary<T> overlappingRoot, FilePathDictionary<T> globalRoot)
        {
            Func<Exception> oldPathNotFound = () => new Exception("oldPath not found in dictionary");
            if (overlappingPath == null)
            {
                if (overlappingRoot == null)
                {
                    if (FilePathComparer.Instance.Equals(oldPath, this.CurrentFilePath))
                    {
                        //Todo: add support for a rename of the root path
                        throw new NotSupportedException("Moving or renaming root directly not supported");
                    }
                    else
                    {
                        throw new Exception("Cannot move file or folder outside root path");
                    }
                }
                else if (pathsAtCurrentLevel != null && pathsAtCurrentLevel.ContainsKey(oldPath))
                {
                    if (overlappingRoot.ContainsKey(newPath))
                    {
                        throw new Exception("Item already exists at newPath");
                    }
                    T previousValue = pathsAtCurrentLevel[oldPath];

                    // Need to manually decrease the _count variable from the overlappiongRoot on down since we cannot remove
                    FilePathDictionary<T> manualCountDecrement = overlappingRoot;
                    while (manualCountDecrement != this)
                    {
                        if (manualCountDecrement.innerFolders == null)
                        {
                            throw new Exception("Internal dictionary error, ensure synchronized access");
                        }
                        manualCountDecrement._count--;

                        FilePath recurseCountDecrement = oldPath.Parent;
                        while (recurseCountDecrement != null)
                        {
                            if (manualCountDecrement.innerFolders.ContainsKey(recurseCountDecrement))
                            {
                                manualCountDecrement = manualCountDecrement.innerFolders[recurseCountDecrement];
                                break;
                            }

                            recurseCountDecrement = recurseCountDecrement.Parent;
                        }
                    }
                    this._count--;
                    pathsAtCurrentLevel.Remove(oldPath);

                    globalRoot.Add(newPath, previousValue);
                }
                else if (innerFolders == null)
                {
                    throw oldPathNotFound();
                }
                else if (innerFolders.ContainsKey(oldPath))
                {
                    FilePathDictionary<T> innerFolder = innerFolders[oldPath];
                    Action<FilePathDictionary<T>, FilePathDictionary<T>, FilePathDictionary<T>, KeyValuePair<FilePath, T>, FilePath, FilePath, Action<FilePath, FilePath, T>> processInnerRename =
                        (renameRoot, renameFolder, renameGlobal, renamePair, renameNewPath, renameOldPath, renameCallback) =>
                        {
                            FilePath rebuiltNewPath, oldPathChild;
                            rebuiltNewPath = oldPathChild = renamePair.Key.Copy();
                            while (oldPathChild.Parent != null)
                            {
                                if (FilePathComparer.Instance.Equals(renameOldPath, oldPathChild.Parent))
                                {
                                    break;
                                }

                                oldPathChild = oldPathChild.Parent;
                            }
                            if (oldPathChild.Parent == null)
                            {
                                throw new Exception("Internal dictionary error, ensure synchronized access");
                            }
                            oldPathChild.Parent = renameNewPath;

                            renameFolder.Rename(renamePair.Key,
                                rebuiltNewPath,
                                null,
                                renameRoot,
                                renameGlobal);

                            if (renameCallback != null
                                && renamePair.Value != null)
                            {
                                renameCallback(renamePair.Key, rebuiltNewPath, renamePair.Value);
                            }
                        };
                    if (innerFolder.innerFolders != null)
                    {
                        foreach (KeyValuePair<FilePath, FilePathDictionary<T>> recurseRename in innerFolder.innerFolders.ToArray())
                        {
                            processInnerRename(overlappingRoot,
                                innerFolder,
                                globalRoot,
                                new KeyValuePair<FilePath, T>(recurseRename.Key, recurseRename.Value.CurrentValue),
                                newPath,
                                oldPath,
                                recursiveRenameCallback);
                        }
                    }
                    if (innerFolder.pathsAtCurrentLevel != null)
                    {
                        foreach (KeyValuePair<FilePath, T> recurseRename in innerFolder.pathsAtCurrentLevel.ToArray())
                        {
                            processInnerRename(overlappingRoot,
                                innerFolder,
                                globalRoot,
                                recurseRename,
                                newPath,
                                oldPath,
                                recursiveRenameCallback);
                        }
                    }
                    if (innerFolder.CurrentValue != null)
                    {
                        T storeCurrentValue = innerFolder.CurrentValue;
                        if (!globalRoot.Remove(oldPath))
                        {
                            throw new Exception("Internal dictionary error, ensure synchronized access");
                        }
                        globalRoot.Add(newPath, storeCurrentValue);
                    }
                }
                else
                {
                    FilePath recurseOldPath = oldPath.Parent;
                    while (recurseOldPath != null)
                    {
                        if (innerFolders.ContainsKey(recurseOldPath))
                        {
                            innerFolders[recurseOldPath].Rename(oldPath,
                                newPath,
                                null,
                                overlappingRoot,
                                globalRoot);
                            break;
                        }

                        recurseOldPath = recurseOldPath.Parent;
                    }
                    if (recurseOldPath == null)
                    {
                        throw oldPathNotFound();
                    }
                }
            }
            else if (this.innerFolders == null)
            {
                throw oldPathNotFound();
            }
            else
            {
                bool innerFolderIsOverlappingRoot = true;
                FilePath recurseOverlappingPath = overlappingPath;
                while (recurseOverlappingPath != null)
                {
                    if (this.innerFolders.ContainsKey(recurseOverlappingPath))
                    {
                        FilePathDictionary<T> newOverlappingRoot = this.innerFolders[recurseOverlappingPath];
                        if (innerFolderIsOverlappingRoot)
                        {
                            newOverlappingRoot.Rename(oldPath,
                                newPath,
                                null,
                                newOverlappingRoot,
                                globalRoot);
                        }
                        else
                        {
                            newOverlappingRoot.Rename(oldPath,
                                newPath,
                                overlappingPath,
                                null,
                                globalRoot);
                        }
                    }
                    innerFolderIsOverlappingRoot = false;

                    recurseOverlappingPath = recurseOverlappingPath.Parent;
                }
                if (recurseOverlappingPath == null)
                {
                    throw oldPathNotFound();
                }
            }
        }
        #endregion
    }
}