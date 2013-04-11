//
// FilePathHierarchicalNode.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Cloud.Model
{
    /// <summary>
    /// Node to represent a hierarchical representation of FilePaths and their generic-typed values
    /// </summary>
    /// <typeparam name="T">Generic-typed value</typeparam>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class FilePathHierarchicalNode<T> where T : class// : IFilePathHierarchicalNode<T> where T : class
    {
        /// <summary>
        /// Children hierarchical nodes
        /// </summary>
        public IEnumerable<FilePathHierarchicalNode<T>> Children { get; set; }

        internal FilePathHierarchicalNode() { }

        #region IFilePathHierarchicalNode<T> members
        /// <summary>
        /// True iff the current node has a value,
        /// always check before grabbing the value
        /// </summary>
        public bool HasValue
        {
            get
            {
                Type thisType = this.GetType();

                lock (hasValueLookup)
                {
                    bool toReturn;
                    if (hasValueLookup.TryGetValue(thisType, out toReturn))
                    {
                        return toReturn;
                    }

                    hasValueLookup[thisType] = toReturn =
                        thisType
                            .GetProperty(
                                ((MemberExpression)((Expression<Func<FilePathHierarchicalNode<T>, KeyValuePair<FilePath, T>>>)(member => member.Value)).Body).Member.Name)
                            .DeclaringType
                            .GetGenericTypeDefinition() != typeof(FilePathHierarchicalNode<>);
                    return toReturn;
                }
            }
        }
        private static readonly Dictionary<Type, bool> hasValueLookup = new Dictionary<Type, bool>();

        /// <summary>
        /// Value for the current node,
        /// always check HasValue to be true first to prevent an exception;
        /// must be set by creating a new generic-typed FilePathHierarchicalNodeWithValue
        /// </summary>
        public virtual KeyValuePair<FilePath, T> Value
        {
            get
            {
                // must override Value to not throw exception

                //if (!this.HasValue)
                    throw new NullReferenceException("Not created with value");
                //return ((FilePathHierarchicalNodeWithValue<T>)this).Value;
            }
        }
        #endregion
    }

    /// <summary>
    /// Extends the generic-typed FilePathHierarchicalNode to store the node Value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class FilePathHierarchicalNodeWithValue<T> : FilePathHierarchicalNode<T> where T : class
    {
        /// <summary>
        /// Value for the current node
        /// </summary>
        public override KeyValuePair<FilePath, T> Value
        {
            get
            {
                return _value;
            }
        }
        private readonly KeyValuePair<FilePath, T> _value;
        /// <summary>
        /// Creates the FilePathHierarchical node so that it has a Value
        /// </summary>
        /// <param name="value"></param>
        internal FilePathHierarchicalNodeWithValue(KeyValuePair<FilePath, T> value)
        {
            this._value = value;
        }
    }
}
