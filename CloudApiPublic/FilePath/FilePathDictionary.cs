using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudApiPublic.Model;
using CloudApiPublic.Static;

namespace CloudApiPublic.Model
{
    /// <summary>
    /// Generic-typed dictionary by FilePath ¡¡Not thread-safe, must be locked for synchronized use!!
    /// recursively stores directory structure to produce same time order operations as a Dictionary,
    /// but allows recursive renaming and deletion operations of everything contained below a given path
    /// (use with callbacks to retrieve subsequent renames or deletions)
    /// </summary>
    /// <typeparam name="T">Any reference type</typeparam>
    public sealed partial class FilePathDictionary<T> : IDictionary<FilePath, T> where T : class
    {
        #region private members
        /// <summary>
        /// Represents the path of the directory for this dictionary
        /// </summary>
        private FilePath CurrentFilePath;
        /// <summary>
        /// Value at current directory, if any (represents a Key/Value in parent Dictionaries only if it exists)
        /// </summary>
        private T CurrentValue;
        /// <summary>
        /// Store callback to fire on subsequent deletions;
        /// fire with path of subsequent change with its value
        /// </summary>
        private Action<FilePath, T, FilePath> recursiveDeleteCallback;
        /// <summary>
        /// Store callback to fire oon subsequent renames;
        /// fire with old path of subsequent change, new path of subsequent change, and its value
        /// </summary>
        private Action<FilePath, FilePath, T, FilePath, FilePath> recursiveRenameCallback;
        #endregion

        #region private collection members
        /// <summary>
        /// Storage of non-empty folders below the current path (they may become empty later);
        /// may be null if it was not yet needed
        /// </summary>
        private Dictionary<FilePath, FilePathDictionary<T>> innerFolders = null;
        /// <summary>
        /// Storage of empty folders and files within the current path;
        /// may be null if it was not yet needed
        /// </summary>
        private Dictionary<FilePath, T> pathsAtCurrentLevel = null;
        /// <summary>
        /// Storage of the count of all items contained at the current path or below
        /// (excludes inner folders that do not have a value)
        /// </summary>
        private int _count;
        #endregion

        /// <summary>
        /// Private constructor, create an instance with public static CreateAndInitialize
        /// </summary>
        private FilePathDictionary() { }

        #region non-interface public methods
        /// <summary>
        /// Used to construct a new FilePathDictionary,
        /// returns a CLError if an exception occurred during creation,
        /// optionally takes callbacks to fire upon subsequent deletions or renames
        /// as well as an optional value in order to set the info for the current path
        /// </summary>
        /// <param name="rootPath">Root path of the dictionary (such as the path of a watched directory)</param>
        /// <param name="pathDictionary">(out) FilePathDictionary created by calling this method</param>
        /// <param name="recursiveDeleteCallback">(optional) Callback fired upon subsequent deletions</param>
        /// <param name="recursiveRenameCallback">(optional) Callback fired upon subsequent renames</param>
        /// <param name="valueAtFolder">(optional) Item for current path, if any</param>
        /// <returns>Returns a CLError if an exception occurred, otherwise returns null</returns>
        public static CLError CreateAndInitialize(FilePath rootPath,
            out FilePathDictionary<T> pathDictionary,
            Action<FilePath, T, FilePath> recursiveDeleteCallback = null,
            Action<FilePath, FilePath, T, FilePath, FilePath> recursiveRenameCallback = null,
            T valueAtFolder = null)
        {
            // First create the dictionary in its own block so that if a subsequent exception is thrown,
            // at least an object will be returned in the out parameter pathDictionary
            try
            {
                pathDictionary = new FilePathDictionary<T>();
            }
            catch (Exception ex)
            {
                pathDictionary = Helpers.DefaultForType<FilePathDictionary<T>>();
                return ex;
            }
            // Take the created dictionary and initialize it, any exception will be returned as a CLError
            try
            {
                // Dictionary requires a root path for normal usage
                if (rootPath == null)
                {
                    throw new ArgumentException("rootPath cannot be null");
                }
                // Current path of the dictionary is the root used to create it
                pathDictionary.CurrentFilePath = rootPath;
                // Store a value passed for the dictionary in case it exists
                pathDictionary.CurrentValue = valueAtFolder;
                // Start the count of items in the dictionary based on whether a value was passed for the current path
                if (pathDictionary.CurrentValue == null)
                {
                    pathDictionary._count = 0;
                }
                else
                {
                    pathDictionary._count = 1;
                }
                // Store the callbacks for subsequent deletions or renames
                pathDictionary.recursiveDeleteCallback = recursiveDeleteCallback;
                pathDictionary.recursiveRenameCallback = recursiveRenameCallback;
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Rename files or folders with recursive updates of all items below the changed item (if item is a non-empty folder);
        /// oldPath must not be null, newPath must not be null, and the two paths must not be the same;
        /// also, an item must exist at the oldPath and must not exist at the newPath
        /// </summary>
        /// <param name="oldPath">Previous path for file or folder</param>
        /// <param name="newPath">New path for file or folder</param>
        /// <returns>Returns a CLError for an exception if one occurred, otherwise null</returns>
        public CLError Rename(FilePath oldPath, FilePath newPath)
        {
            try
            {
                // Assert parameter restrictions
                if (oldPath == null)
                {
                    throw new ArgumentException("oldPath cannot be null");
                }
                if (newPath == null)
                {
                    throw new ArgumentException("newPath cannot be null");
                }
                if (FilePathComparer.Instance.Equals(oldPath, newPath))
                {
                    throw new ArgumentException("oldPath and newPath cannot be the same");
                }

                // Run function to retrieve overlapping path
                FilePath overlappingPath = oldPath.FindOverlappingPath(newPath);

                // If the current dictionary already represents the overlapping path,
                // then call the private Rename function using this as the overlapping root,
                // otherwise call the private Rename function without the overlapping root
                // (this will recursively call rename on inner folders until the overlapping root is found)
                if (FilePathComparer.Instance.Equals(overlappingPath, this.CurrentFilePath))
                {
                    Rename(oldPath, newPath, null, this, this, oldPath, newPath);
                }
                else
                {
                    Rename(oldPath, newPath, overlappingPath, null, this, oldPath, newPath);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Finds the paths starting at the given key value and builds a recursive structure including all children
        /// </summary>
        /// <param name="key">Key to search</param>
        /// <param name="outputNode">Outputs heirarchical structure of the highest node and its recursive children</param>
        /// <returns>Returns an error that occurred retrieving the heirarchical structure, if any</returns>
        public CLError GrabHierarchyForPath(FilePath key, out FilePathHierarchicalNode<T> outputNode)
        {
            try
            {
                // define exception thrown if the key is not found
                Func<Exception> getKeyNotFound = () => new KeyNotFoundException("Key not found in dictionary");

                // if the key represents the current file path of this dictionary or null (which means the same)
                if (key == null
                    || FilePathComparer.Instance.Equals(key, this.CurrentFilePath))
                {
                    // define dictionary for nodes directly below the current dictionary
                    List<FilePathHierarchicalNode<T>> childNodes = new List<FilePathHierarchicalNode<T>>();

                    // if the current level contains folders to check,
                    // then they must be recursed for inner children
                    if (this.innerFolders != null)
                    {
                        // loop through each inner folder
                        foreach (FilePathDictionary<T> currentInnerFolder in this.innerFolders.Values)
                        {
                            // declare hierarchical structure of recursed inner folder
                            FilePathHierarchicalNode<T> innerFolderNode;

                            // recurse inner folder, storing any returned error to rethrow
                            CLError grabRecursionError = currentInnerFolder.GrabHierarchyForPath(null, out innerFolderNode);
                            if (grabRecursionError != null)
                            {
                                throw grabRecursionError.GrabFirstException();
                            }

                            // if the inner folder has a value,
                            // then add its hierarchical structure as a child node
                            if (innerFolderNode.HasValue)
                            {
                                childNodes.Add(innerFolderNode);
                            }
                            // else the inner folder has no values,
                            // then add the inner folders children as child nodes (skips over the missing value node)
                            else
                            {
                                childNodes.AddRange(innerFolderNode.Children);
                            }
                        }
                    }

                    // if the current level contains files or empty folders to check,
                    // then they must be added to the child nodes
                    if (this.pathsAtCurrentLevel != null)
                    {
                        // loop through each inner file or empty folder
                        foreach (KeyValuePair<FilePath, T> currentLevelPath in this.pathsAtCurrentLevel)
                        {
                            // add the current inner file or empty folder as a child node
                            childNodes.Add(new FilePathHierarchicalNodeWithValue<T>(new KeyValuePair<FilePath, T>(currentLevelPath.Key, currentLevelPath.Value))
                            {
                                Children = new FilePathHierarchicalNode<T>[0]
                            });
                        }
                    }

                    // if the current dictionary has a value,
                    // then this dictionary's hierarchical structure is output including that value (including all child nodes)
                    if (this.CurrentValue != null)
                    {
                        outputNode = new FilePathHierarchicalNodeWithValue<T>(new KeyValuePair<FilePath, T>(this.CurrentFilePath, this.CurrentValue))
                        {
                            Children = childNodes
                        };
                    }
                    // else if the current dictionary does not have a value,
                    // then this dictionary's hierarchical structure is output without a value (but with all child nodes)
                    else
                    {
                        outputNode = new FilePathHierarchicalNode<T>()
                        {
                            Children = childNodes
                        };
                    }
                }
                // else if the key does not represent the current dictionary,
                // then search inner files/folders
                else
                {
                    // if the key matches an inner file or inner empty folder,
                    // then output the inner file or empty folder as a valued hierarchical structure without children
                    if (this.pathsAtCurrentLevel != null
                        && this.pathsAtCurrentLevel.ContainsKey(key))
                    {
                        outputNode = new FilePathHierarchicalNodeWithValue<T>(new KeyValuePair<FilePath, T>(key, this.pathsAtCurrentLevel[key]))
                            {
                                Children = new FilePathHierarchicalNode<T>[0]
                            };
                    }
                    // else if the key does not match an inner file or empty folder
                    // and there are inner folders to check,
                    // then recursively check inner folders for the key
                    else if (innerFolders != null)
                    {
                        // iterate through parent paths of the key,
                        // checking if the recursed parent path is contained in inner folders
                        FilePath recursePath = key;
                        while (recursePath != null)
                        {
                            if (innerFolders.ContainsKey(recursePath))
                            {
                                break;
                            }

                            recursePath = recursePath.Parent;
                        }

                        // if inner folders didn't contain any recursed parents of the key,
                        // then throw the not found exception
                        if (recursePath == null)
                        {
                            throw getKeyNotFound();
                        }
                        // else if inner folders did contain a recursed parent of the key,
                        // then recursively grab the hierarchical structure of the inner folder for the key to output
                        else
                        {
                            CLError recurseHeirarchyError = innerFolders[recursePath].GrabHierarchyForPath(key, out outputNode);
                            if (recurseHeirarchyError != null)
                            {
                                throw recurseHeirarchyError.GrabFirstException();
                            }
                        }
                    }
                    // else if the key does not match an inner file or empty folder
                    // and there were not inner folders to check,
                    // then throw the not found exception
                    else
                    {
                        throw getKeyNotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                outputNode = Helpers.DefaultForType<FilePathHierarchicalNode<T>>();
                return ex;
            }
            return null;
        }
        #endregion

        // Interface members do not have comment summaries,
        // they provide the common functionality described by the interface they implement
        #region interface implementors

        #region IDictionary<FilePath, T> members
        public ICollection<FilePath> Keys
        {
            get
            {
                // FilePath keys include any of the following if they exist:
                // Current path if there is a matching value (currentKeysArray)
                // Keys from non-empty folders below this path (innerFolders.Values flattened to retrieve all keys)
                // Empty folders and files (pathsAtCurrentLevel.Keys)

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
                // Generic-typed values include any of the following if they exist:
                // Value at current path
                // Values from non-empty folders below this path (innerFolders.Values flattened to retrieve all values)
                // Empty folders and files (pathsAtCurrentLevel.Values) 

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
                // Search for value by key, if it is not found throw KeyNotFoundException;
                // A null key refers to the current value if it exists
                // Otherwise start search in pathsAtCurrentLevel then innerFolders
                // Last, see if inner folders contains any recursed parents of the key's path and call the indexer on it

                Func<KeyNotFoundException> getNotFound = () => new KeyNotFoundException("Value not found for provided FilePath key");
                if (key == null)
                {
                    if (CurrentValue != null)
                    {
                        return CurrentValue;
                    }
                }
                else
                {
                    if (pathsAtCurrentLevel != null && pathsAtCurrentLevel.ContainsKey(key))
                    {
                        return pathsAtCurrentLevel[key];
                    }
                    if (innerFolders != null)
                    {
                        if (innerFolders.ContainsKey(key))
                        {
                            return innerFolders[key][null];
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
                }
                throw getNotFound();
            }
            set
            {
                // Search for where to put the value by key;
                // A null key refers to current value, if it is changed increment/decrement the count appropriately;
                // Search where to put the value starting with pathsAtCurrentLevel, followed by innerFolders;
                // If innerFolders exists, but does not contain the key,
                // check if innerFolders contains a recursed parent path of the key and call its indexer to set the value;
                // If a place to put the value is found, set it at that location, otherwise add it as a new key/value

                if (key == null)
                {
                    if (CurrentValue == null)
                    {
                        if (value != null)
                        {
                            _count++;
                        }
                    }
                    else if (CurrentValue != null && value == null)
                    {
                        _count--;
                    }
                    CurrentValue = value;
                }
                else
                {
                    if (pathsAtCurrentLevel != null && pathsAtCurrentLevel.ContainsKey(key))
                    {
                        if (value == null)
                        {
                            _count--;
                        }
                        else
                        {
                            pathsAtCurrentLevel[key] = value;
                        }
                    }
                    else if (innerFolders == null)
                    {
                        if (value != null)
                        {
                            Add(key, value);
                        }
                    }
                    else if (innerFolders.ContainsKey(key))
                    {
                        if (innerFolders[key].CurrentValue != null
                            && value == null)
                        {
                            _count--;
                        }

                        innerFolders[key][null] = value;
                    }
                    else
                    {
                        FilePath recurseParent = key;
                        while ((recurseParent = recurseParent.Parent) != null)
                        {
                            if (innerFolders.ContainsKey(recurseParent))
                            {
                                if (value == null)
                                {
                                    int previousCount = innerFolders[recurseParent].Count;
                                    innerFolders[recurseParent][key] = value;
                                    if (innerFolders[recurseParent].Count < previousCount)
                                    {
                                        _count--;
                                    }
                                }
                                else
                                {
                                    innerFolders[recurseParent][key] = value;
                                }
                                break;
                            }
                        }
                        if (recurseParent == null
                            && value != null)
                        {
                            Add(key, value);
                        }
                    }
                }
            }
        }

        public void Add(FilePath key, T value)
        {
            // define function for the exception to throw when a value is already found for the key
            Func<ArgumentException> alreadyExistsException = () => new ArgumentException("FilePath key already exists in collection");

            // Recurse the parent path structure of the key until it matches the current path;
            // if the key is null or matches immediately,
            // then it represents the current dictionary path so try to set the current value;
            // During the search,
            // check if the searched path is found in innerFolders or in pathsAtCurrentLevel and store the path accordingly;
            bool rootSearchStarted = false;
            FilePath rootSearch = key;
            FilePath rootChild = key;
            FilePath existingInnerFolder = null;
            FilePath existingCurrentPath = null;
            if (key != null)
            {
                while (!FilePathComparer.Instance.Equals(rootSearch, this.CurrentFilePath))
                {
                    if (rootSearch.Parent == null)
                    {
                        throw new ArgumentException("FilePath key to add does not belong in current FilePath root");
                    }
                    if (innerFolders != null && innerFolders.ContainsKey(rootSearch))
                    {
                        existingInnerFolder = rootSearch;
                    }
                    else if (pathsAtCurrentLevel != null && pathsAtCurrentLevel.ContainsKey(rootSearch))
                    {
                        existingCurrentPath = rootSearch;
                    }
                    rootSearchStarted = true;
                    rootChild = rootSearch;
                    rootSearch = rootSearch.Parent;
                }
            }
            if (!rootSearchStarted)
            {
                if (CurrentValue == null)
                {
                    _count++;
                    CurrentValue = value;
                }
                else
                {
                    throw alreadyExistsException();
                }
            }

            // if key paths did not match any inner folders
            if (existingInnerFolder == null)
            {
                // if key paths did not match any inner folders and the key was not in pathsAtCurrentLevel
                if (existingCurrentPath == null)
                {
                    // a new current level path or an inner folder needs to be created to store the new value
                    
                    // if the child to be placed under the current level is the key itself,
                    // then the key belongs directly under the current level as a new current level path
                    if (rootChild.Equals(key))
                    {
                        // create pathsAtCurrentLevel as needed
                        if (pathsAtCurrentLevel == null)
                        {
                            pathsAtCurrentLevel = new Dictionary<FilePath, T>(FilePathComparer.Instance);
                        }
                        _count++;
                        pathsAtCurrentLevel.Add(key, value);
                    }
                    // else if the key is more than one level below the current path,
                    // then create an innerFolder to recursively create the child directory structure
                    else
                    {
                        // create innerFolders as needed
                        if (innerFolders == null)
                        {
                            innerFolders = new Dictionary<FilePath, FilePathDictionary<T>>(FilePathComparer.Instance);
                        }

                        // create the FilePathDictionary to use as innerFolder
                        FilePathDictionary<T> innerFolder;
                        CLError innerFolderError = FilePathDictionary<T>.CreateAndInitialize(rootChild,
                            out innerFolder,
                            this.recursiveDeleteCallback,
                            this.recursiveRenameCallback);
                        if (innerFolderError != null)
                        {
                            throw innerFolderError.GrabFirstException();
                        }

                        // recursively call Add on innerFolder to build child structure and eventually add the value
                        _count++;
                        innerFolder.Add(key, value);

                        // add the new innerFolder to innerFolders at the path directly below the current path
                        innerFolders.Add(rootChild, innerFolder);
                    }
                }
                // else if a path exists in pathsAtCurrentLevel for the key,
                // then throw alreadyExistsException
                else if (FilePathComparer.Instance.Equals(existingCurrentPath, key))
                {
                    throw alreadyExistsException();
                }
                // else if a path exists at pathsAtCurrentLevel for a recursed parent of a key,
                // then the pathAtCurrentLevel needs to be made into an innerFolder to which the new key/value will be added
                else
                {
                    // create innerFolders as necessary
                    if (innerFolders == null)
                    {
                        innerFolders = new Dictionary<FilePath, FilePathDictionary<T>>(FilePathComparer.Instance);
                    }

                    // create the FilePathDictionary to use as innerFolder,
                    // pass the optional parameter for its new CurrentValue from pathsAtCurrentLevel
                    FilePathDictionary<T> innerFolder;
                    CLError innerFolderError = FilePathDictionary<T>.CreateAndInitialize(rootChild,
                        out innerFolder,
                        this.recursiveDeleteCallback,
                        this.recursiveRenameCallback,
                        pathsAtCurrentLevel[existingCurrentPath]);
                    if (innerFolderError != null)
                    {
                        throw innerFolderError.GrabFirstException();
                    }

                    // recursively call Add on innerFolder to build child structure and eventually add the value 
                    _count++;
                    innerFolder.Add(key, value);

                    // add the new innerFolder to innerFolders at the path directly below the current path
                    innerFolders.Add(rootChild, innerFolder);

                    // remove the pathAtCurrentLevel that was converted to an innerFolder
                    pathsAtCurrentLevel.Remove(rootChild);
                }
            }
            // else if key paths did match an innerFolder
            else
            {
                // store the innerFolder found for the key paths out of innerFolders
                FilePathDictionary<T> innerFolder = innerFolders[(FilePath)existingInnerFolder];

                // if the innerFolder has a path matching the key,
                // then only add the value if it does not have a current value;
                // if the innerFolder path matched and has a current value,
                // throw alreadyExistsException
                if (FilePathComparer.Instance.Equals((FilePath)existingInnerFolder, key))
                {
                    if (innerFolder.CurrentValue == null)
                    {
                        _count++;
                        innerFolder._count++;
                        innerFolder.CurrentValue = value;
                    }
                    else
                    {
                        throw alreadyExistsException();
                    }
                }
                // else if innerFolder path does not match key (instead matches a recursed parent of the key),
                // then recursively call Add on the innerFolder to add the key/value
                else
                {
                    _count++;
                    innerFolder.Add(key, value);
                }
            }
        }

        public bool ContainsKey(FilePath key)
        {
            // if the key is null or matches the current path,
            // then return true for existance of key only if value exists
            if (key == null
                || FilePathComparer.Instance.Equals(key, this.CurrentFilePath))
            {
                return CurrentValue != null;
            }
            // else if key does not match the current path,
            // then search for key first in pathsAtCurrentLevel then in innerFolders
            // (recursively calling ContainsKey on recursed path-matched innerFolder)
            else
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
        }

        public bool Remove(FilePath key)
        {
            // Forward call to private method which keeps track of the original change root for callbacks even through recursion
            return this.Remove(key, key);
        }

        public bool TryGetValue(FilePath key, out T value)
        {
            // if the key is null or matches the current path,
            // then if also the current value exists output it and return true
            if (key == null
                || FilePathComparer.Instance.Equals(key, CurrentFilePath))
            {
                if (CurrentValue != null)
                {
                    value = CurrentValue;
                    return true;
                }
            }
            // else if key does not match the current path,
            // then search pathsAtCurrentLevel first by key then innerFolders
            else
            {
                // if pathsAtCurrentLevel contains the key,
                // then output its value at the key location and return true
                if (pathsAtCurrentLevel != null && pathsAtCurrentLevel.ContainsKey(key))
                {
                    value = pathsAtCurrentLevel[key];
                    return true;
                }
                // if innerFolders exists,
                // then search it for the key
                if (innerFolders != null)
                {
                    // if innerFolders contains the key,
                    // then recursively return TryGetValue on the innerFolder with null
                    // (null will be faster for this case so it just checks if the current value exists)
                    if (innerFolders.ContainsKey(key))
                    {
                        return innerFolders[key].TryGetValue(null, out value);
                    }
                    // else if innerFolders does not contain the key,
                    // then check if innerFolders contains a recursed parent key path
                    else
                    {
                        // if an innerFolder is found for a recursed parent key path,
                        // then recursively return TryGetValue on the innerFolder with the same key/value
                        FilePath recurseParent = key;
                        while ((recurseParent = recurseParent.Parent) != null)
                        {
                            if (innerFolders.ContainsKey(recurseParent))
                            {
                                return innerFolders[recurseParent].TryGetValue(key, out value);
                            }
                        }
                    }
                }
            }
            // fallthrough if key not found,
            // output default and return false
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
            // Forward call to existing, implemented add method
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            // Forward call to private clear method that takes a change root path to return on callbacks
            Clear(this.CurrentFilePath);
        }

        public bool Contains(KeyValuePair<FilePath, T> item)
        {
            // use TryGetValue to both determine if the key exists and to return the item;
            // if the item is returned and is equal to the value in the parameter pair return true,
            // otherwise return false
            T toCompare;
            if (TryGetValue(item.Key, out toCompare))
            {
                return item.Value.Equals(toCompare);
            }
            return false;
        }

        public void CopyTo(KeyValuePair<FilePath, T>[] array, int arrayIndex)
        {
            // use enumeration from getPairs to add all pairs to the output array

            if (arrayIndex < 0)
            {
                throw new ArgumentException("arrayIndex must be non-negative");
            }
            if ((array.Length - arrayIndex) < this.Count)
            {
                throw new ArgumentException("Not enough room to copy into array");
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
            // convert remove call for pair to remove call for key,
            // but first check that the item at the key exists
            // and matches the value from the parameter pair

            T toCompare;
            if (TryGetValue(item.Key, out toCompare))
            {
                if (item.Value.Equals(toCompare))
                {
                    return Remove(item.Key);
                }
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
        /// <summary>
        /// Combines collections from current value, pathsAtCurrentLevel,
        /// and innerFolders and returns the combination
        /// (retains ICollection interface instead of just IEnumerable)
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Private overload for Rename that performs the bulk of the renaming operation
        /// </summary>
        /// <param name="oldPath">Previous path of file or folder</param>
        /// <param name="newPath">New path of file or folder</param>
        /// <param name="overlappingPath">The part of the new and old path that overlaps, null if the overlapping root is already found</param>
        /// <param name="overlappingRoot">The FilePathDictionary at the root of the overlap between the old path and new path</param>
        /// <param name="globalRoot">The FilePathDictionary passed in representing the instance from the original public Rename overload</param>
        /// <param name="changeRootOld">The original previous path that triggered recursive changes</param>
        /// <param name="changeRootNew">The original new path that triggered recursive changes</param>
        private void Rename(FilePath oldPath, FilePath newPath, FilePath overlappingPath, FilePathDictionary<T> overlappingRoot, FilePathDictionary<T> globalRoot, FilePath changeRootOld, FilePath changeRootNew)
        {
            // Function to return path not found exception if it needs to be thrown
            Func<ArgumentException> oldPathNotFound = () => new ArgumentException("oldPath not found in dictionary");

            // Function to return synchronization exception if it needs to be thrown;
            // should only be thrown when an error that should never occur under normal use needs to be thrown
            Func<System.Threading.SynchronizationLockException> synchronizationError = () => new System.Threading.SynchronizationLockException("Internal dictionary error, ensure synchronized access");

            // if overlappingPath is null
            // (used when the overlappingRoot was already found;
            //   if it was not already found and the path matches the current path then it is a global rename)
            if (overlappingPath == null)
            {
                // if overlapping root was not set
                if (overlappingRoot == null)
                {
                    // if oldPath matches current path,
                    // then it represents a global rename which still needs to be handled (Todo)
                    if (FilePathComparer.Instance.Equals(oldPath, this.CurrentFilePath))
                    {
                        //Todo: add support for a rename of the root path
                        throw new NotSupportedException("Moving or renaming root directly not supported");
                    }
                    // oldPath is not contained underneath the root,
                    // throw exception for renaming outside the root
                    else
                    {
                        throw new ArgumentException("Cannot move file or folder outside root path");
                    }
                }
                // else if overlapping root was set and pathsAtCurrentLevel contains the oldPath
                else if (pathsAtCurrentLevel != null && pathsAtCurrentLevel.ContainsKey(oldPath))
                {
                    // search to ensure the overlappingRoot does not already contain the new path,
                    // if so throw already exists exception
                    if (overlappingRoot.ContainsKey(newPath))
                    {
                        throw new ArgumentException("Item already exists at newPath");
                    }

                    // store previous value at oldPath
                    T previousValue = pathsAtCurrentLevel[oldPath];

                    // Need to manually decrease the _count variable from the overlappiongRoot on down since we are not removing from the global root;
                    // We start at the overlapping root and trace down through the paths to recursive innerFolders until we find the current dictionary
                    // At each level we decrement the count
                    FilePathDictionary<T> manualCountDecrement = overlappingRoot;
                    while (manualCountDecrement != this)
                    {
                        if (manualCountDecrement.innerFolders == null)
                        {
                            throw synchronizationError();
                        }

                        // decrement recursed child starting at overlapping root
                        manualCountDecrement._count--;

                        // start at oldPath.Parent and work up to find the Dictionary
                        // between the current dictionary and the overlapping root
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
                        if (manualCountDecrement == null)
                        {
                            throw synchronizationError();
                        }
                    }
                    // After decrementing the counts for dictionaries from the overlapping root to above the current dictionary,
                    // still need to decrement the current dictionary
                    this._count--;

                    // Remove old path
                    pathsAtCurrentLevel.Remove(oldPath);

                    // Add new path with stored previous value
                    overlappingRoot.Add(newPath, previousValue);
                }
                // else if overlapping root was set and pathsAtCurrentLevel did not contain the old item but inner folders does not exist,
                // then old path not found
                else if (innerFolders == null)
                {
                    throw oldPathNotFound();
                }
                // else if overlapping root was set and pathsAtCurrentLevel did not contain the old item and innerFolders contains the old path,
                // then all items inside the innerFolder need to be renamed and have their rename callbacks called
                // before moving the inner folder to its new path
                else if (innerFolders.ContainsKey(oldPath))
                {
                    // store innerFolder
                    FilePathDictionary<T> innerFolder = innerFolders[oldPath];

                    // if innerFolders exists within the innerFolder,
                    // then run the helper function to process renaming on each inner inner folder
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
                                recursiveRenameCallback,
                                changeRootOld,
                                changeRootNew);
                        }
                    }
                    // if pathsAtCurrentLevel exists within the innerFolder
                    // then run the helper function to process renaming on each file/empty folder in the inner folder
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
                                recursiveRenameCallback,
                                changeRootOld,
                                changeRootNew);
                        }
                    }
                    // if the inner folder has a current value:
                    // then store the old current value,
                    // remove the inner folder at the old path,
                    // and add the stored value at the new path
                    if (innerFolder.CurrentValue != null)
                    {
                        T storeCurrentValue = innerFolder.CurrentValue;
                        if (!globalRoot.Remove(oldPath))
                        {
                            // synchronization exception should only be thrown when an error that should never occur under normal use needs to be thrown
                            throw new System.Threading.SynchronizationLockException("Internal dictionary error, ensure synchronized access");
                        }
                        globalRoot.Add(newPath, storeCurrentValue);
                    }
                }
                // else if the overlapping root exists and pathsAtCurrentLevel did not contain the old path and innerPaths did not contain the old path,
                // then recursively search innerFolders for the recursed parents of the old path to recursively call the private Rename overload
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
                                globalRoot,
                                changeRootOld,
                                changeRootNew);
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
            // if overlappingPath is not null
            // (occurs when the overlapping root dictionary has not been found yet)
            // and innerFolders is null,
            // then old path wasn't found
            else if (this.innerFolders == null)
            {
                throw oldPathNotFound();
            }
            // if overlappingPath is not null
            // (occurs when the overlapping root dictionary has not been found yet)
            // and innerFolders exists,
            // then recursively call the private Rename overload until the overlapping root is found
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
                                globalRoot,
                                changeRootOld,
                                changeRootNew);
                        }
                        else
                        {
                            newOverlappingRoot.Rename(oldPath,
                                newPath,
                                overlappingPath,
                                null,
                                globalRoot,
                                changeRootOld,
                                changeRootNew);
                        }
                        break;
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
        /// <summary>
        /// Method extracted from the private Rename overload into a helper method;
        /// it builds a new path to use in the renaming process,
        /// recursively calls the private Rename overload,
        /// and call the rename callback if it exists
        /// </summary>
        /// <param name="renameRoot">Common root dictionary at the original overlapping path location</param>
        /// <param name="renameFolder">Folder containing an item at the old path</param>
        /// <param name="renameGlobal">Root dictionary from the original public Rename overload call</param>
        /// <param name="renamePair">The key value pair from renameFolder containing the old item</param>
        /// <param name="renameNewPath">The new path</param>
        /// <param name="renameOldPath">The previous path</param>
        /// <param name="renameCallback">The callback to fire if it exists</param>
        private static void processInnerRename(FilePathDictionary<T> renameRoot,
            FilePathDictionary<T> renameFolder,
            FilePathDictionary<T> renameGlobal,
            KeyValuePair<FilePath, T> renamePair,
            FilePath renameNewPath,
            FilePath renameOldPath,
            Action<FilePath, FilePath, T, FilePath, FilePath> renameCallback,
            FilePath changeRootOld,
            FilePath changeRootNew)
        {
            // make a copy of the key in the old pair so that it can have one of
            // its recursed parents replaced with the newly named one
            FilePath rebuiltNewPath, oldPathChild;
            rebuiltNewPath = oldPathChild = renamePair.Key.Copy();

            // find the current item pair's key path which matches the old path,
            // but only store its child as oldPathChild
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
                // synchronization exception should only be thrown when an error that should never occur under normal use needs to be thrown
                throw new System.Threading.SynchronizationLockException("Internal dictionary error, ensure synchronized access");
            }
            // reparent the child of the old path with the new path
            oldPathChild.Parent = renameNewPath;

            // recursively fire rename on the folder containing the item
            // at its old path with the rebuilt new path
            renameFolder.Rename(renamePair.Key,
                rebuiltNewPath,
                null,
                renameRoot,
                renameGlobal,
                changeRootOld,
                changeRootNew);

            // fire the callback for rename if it exists and if the item pair has a value
            if (renameCallback != null
                && renamePair.Value != null)
            {
                renameCallback(renamePair.Key, rebuiltNewPath, renamePair.Value, changeRootOld, changeRootNew);
            }
        }
        
        /// <summary>
        /// Private method to implement the action of the public Remove call,
        /// recurses on itself as needed for inner paths
        /// </summary>
        /// <param name="key">Current FilePath to remove for this recursion</param>
        /// <param name="changeRoot">Original FilePath removed that triggered recursions</param>
        /// <returns></returns>
        private bool Remove(FilePath key, FilePath changeRoot)
        {
            // if the key to remove matches the current path (or is null),
            // then Clear this dictionary and return true
            if (key == null
                || FilePathComparer.Instance.Equals(CurrentFilePath, key))
            {
                Clear(changeRoot);
                return true;
            }
            // if pathsAtCurrentLevel contains the key,
            // then if pathAtCurrentLevel can remove the key
            // decrement counter and return true
            // if not returned true, return false
            if (pathsAtCurrentLevel != null && pathsAtCurrentLevel.ContainsKey(key))
            {
                if (pathsAtCurrentLevel.Remove(key))
                {
                    _count--;
                    return true;
                }
                return false;
            }
            // check innerFolders next if it exists
            if (innerFolders != null)
            {
                // recurse key paths to find parent contained in innerFolders;
                // if an innerFolder is found attempt to recursively call Remove at the recursed path with the key
                // for succesful recursive Remove, decrement the count and return true
                // if inner folder is found but returned false on Remove, return false
                FilePath recursePath = key;
                while (recursePath != null)
                {
                    if (innerFolders.ContainsKey(recursePath))
                    {
                        FilePathDictionary<T> innerFolder = innerFolders[recursePath];
                        int previousInnerCount = innerFolder._count;
                        if (innerFolder.Remove(key, changeRoot))
                        {
                            _count -= (previousInnerCount - innerFolder._count);
                            return true;
                        }
                        return false;
                    }

                    recursePath = recursePath.Parent;
                }
            }
            // for fallthrough if an innerFolder was not found on the recursed parent key paths,
            // return false
            return false;
        }

        /// <summary>
        /// Private method to implement public Clear,
        /// recurses on itself for clearing inner paths
        /// </summary>
        /// <param name="changeRoot">Original FilePath removed or cleared that triggered recursion</param>
        private void Clear(FilePath changeRoot)
        {
            // loop through all inner objects and recursively call delete callbacks on each item;
            // for each innerFolder, also recursively call Clear
            // nullify the current value and set count to zero

            if (pathsAtCurrentLevel != null)
            {
                foreach (KeyValuePair<FilePath, T> currentLevelPath in pathsAtCurrentLevel)
                {
                    if (recursiveDeleteCallback != null)
                    {
                        recursiveDeleteCallback(currentLevelPath.Key, currentLevelPath.Value, changeRoot);
                    }
                }
                pathsAtCurrentLevel.Clear();
            }
            if (innerFolders != null)
            {
                foreach (FilePathDictionary<T> currentInnerFolder in innerFolders.Values)
                {
                    if (currentInnerFolder.CurrentValue != null)
                    {
                        recursiveDeleteCallback(currentInnerFolder.CurrentFilePath, currentInnerFolder.CurrentValue, changeRoot);
                    }
                    currentInnerFolder.Clear();
                }
            }
            this.CurrentValue = null;
            _count = 0;
        }
        #endregion
    }
}