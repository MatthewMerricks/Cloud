//
// FileChangeWithDependencies.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Model;
using CloudApiPublic.Static;

namespace Sync
{
    /// <summary>
    /// Extension of FileChange to add a property for Dependencies
    /// </summary>
    internal sealed class FileChangeWithDependencies : FileChange
    {
        /// <summary>
        /// Array copy of the current list of dependencies
        /// ¡¡ Do not call this just to get a count, instead use DependenciesCount !!
        /// </summary>
        public FileChange[] Dependencies
        {
            get
            {
                return _dependencies.ToArray();
            }
        }
        private List<FileChange> _dependencies;
        /// <summary>
        /// Count of current list of dependencies
        /// </summary>
        public int DependenciesCount
        {
            get
            {
                return _dependencies.Count;
            }
        }

        /// <summary>
        /// Creates a new FileChangeWithDependencies from a base change and initial dependencies
        /// </summary>
        /// <param name="baseChange">Base change to copy</param>
        /// <param name="initialDependencies">Dependencies for starting initial list</param>
        /// <param name="rebuiltFileChange">Output change with dependencies</param>
        /// <param name="DelayCompletedLocker">Optional DelayCompletedLocker required for DelayProcessable methods</param>
        /// <returns>Returns an error in constructing the FileChangeWithDependencies, if any</returns>
        public CLError CreateAndInitialize(FileChange baseChange, IEnumerable<FileChange> initialDependencies, out FileChangeWithDependencies rebuiltFileChange, object DelayCompletedLocker = null)
        {
            try
            {
                // The base FileChange has two constructors so we have to split our construction accordingly
                if (DelayCompletedLocker == null)
                {
                    rebuiltFileChange = new FileChangeWithDependencies(baseChange, initialDependencies);
                }
                else
                {
                    rebuiltFileChange = new FileChangeWithDependencies(baseChange, initialDependencies, DelayCompletedLocker);
                }
            }
            catch (Exception ex)
            {
                rebuiltFileChange = Helpers.DefaultForType<FileChangeWithDependencies>();
                return ex;
            }
            return null;
        }

        // Base constructor with locker for DelayProcessesable methods followed by copy of parameters from baseChange
        private FileChangeWithDependencies(FileChange baseChange, IEnumerable<FileChange> initialDependencies, object DelayCompletedLocker)
            : base(DelayCompletedLocker)
        {
            FillInObjectFromConstructionParameters(baseChange, initialDependencies);
        }

        // Base constructor without locker for DelayProcessesable methods followed by copy of parameters from baseChange
        private FileChangeWithDependencies(FileChange baseChange, IEnumerable<FileChange> initialDependencies)
            : base()
        {
            FillInObjectFromConstructionParameters(baseChange, initialDependencies);
        }

        // Copies parameters from baseChange, sets initial dependency list
        private void FillInObjectFromConstructionParameters(FileChange baseChange, IEnumerable<FileChange> initialDependencies)
        {
            if (baseChange == null)
            {
                throw new NullReferenceException("baseChange cannot be null");
            }

            base.Direction = baseChange.Direction;
            base.DoNotAddToSQLIndex = baseChange.DoNotAddToSQLIndex;
            base.EventId = baseChange.EventId;
            base.Metadata = baseChange.Metadata;
            base.NewPath = baseChange.NewPath;
            base.OldPath = baseChange.OldPath;
            byte[] previousMD5Bytes;
            CLError retrievePreviousMD5Bytes = baseChange.GetMD5Bytes(out previousMD5Bytes);
            if (retrievePreviousMD5Bytes != null)
            {
                throw retrievePreviousMD5Bytes.GrabFirstException();
            }
            base.SetMD5(previousMD5Bytes);
            base.FailureCounter = baseChange.FailureCounter;

            this._dependencies = new List<FileChange>(initialDependencies ?? new FileChange[0]);
        }

        /// <summary>
        /// Removes a dependency from the internal list
        /// </summary>
        /// <param name="toRemove">Dependency to remove</param>
        /// <returns>Returns true if dependency was found and removed, otherwise false</returns>
        public bool RemoveDependency(FileChange toRemove)
        {
            return this._dependencies.Remove(toRemove);
        }

        /// <summary>
        /// Adds a dependency to the internal list
        /// </summary>
        /// <param name="toAdd">Dependency to add</param>
        /// <returns>Returns true if the dependency did not already exist and was added, otherwise false</returns>
        public bool AddDependency(FileChange toAdd)
        {
            if (this._dependencies.Contains(toAdd))
            {
                return false;
            }
            this._dependencies.Add(toAdd);
            return true;
        }
    }
}