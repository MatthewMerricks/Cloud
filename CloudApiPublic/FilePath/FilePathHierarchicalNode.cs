//
// FilePathHierarchicalNode.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiPublic.Model
{
    public class FilePathHierarchicalNode<T> : IFilePathHierarchicalNode<T> where T : class
    {
        public IEnumerable<FilePathHierarchicalNode<T>> Children { get; set; }

        #region IFilePathHierarchicalNode<T> members
        public bool HasValue
        {
            get
            {
                return this is FilePathHierarchicalNodeWithValue<T>;
            }
        }

        public KeyValuePair<FilePath, T> Value
        {
            get
            {
                if (!this.HasValue)
                    throw new NullReferenceException("Not created with value");
                return ((FilePathHierarchicalNodeWithValue<T>)this).Value;
            }
        }
        #endregion
    }

    public interface IFilePathHierarchicalNode<T> where T : class
    {
        bool HasValue { get; }
        KeyValuePair<FilePath, T> Value { get; }
    }

    public class FilePathHierarchicalNodeWithValue<T> : FilePathHierarchicalNode<T> where T : class
    {
        protected KeyValuePair<FilePath, T> Value { get; set; }
        public FilePathHierarchicalNodeWithValue(KeyValuePair<FilePath, T> value)
        {
            this.Value = value;
        }
    }
}
