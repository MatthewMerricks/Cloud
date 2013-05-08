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
using Cloud.Model;
using Cloud.Static;

namespace Cloud.Model
{
    /// <summary>
    /// Extension of FileChange to add a property for Dependencies
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class FileChangeWithDependencies : FileChange
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
        internal static CLError CreateAndInitialize(FileChange baseChange, IEnumerable<FileChange> initialDependencies, out FileChangeWithDependencies rebuiltFileChange, object DelayCompletedLocker = null, object fileDownloadMoveLocker = null)
        {
            try
            {
                // The base FileChange has two constructors so we have to split our construction accordingly
                rebuiltFileChange = new FileChangeWithDependencies(baseChange, initialDependencies, DelayCompletedLocker, fileDownloadMoveLocker);
            }
            catch (Exception ex)
            {
                rebuiltFileChange = Helpers.DefaultForType<FileChangeWithDependencies>();
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Creates a new FileChangeWithDependencies from a base change and initial dependencies
        /// </summary>
        /// <param name="baseChange">Base change to copy</param>
        /// <param name="initialDependencies">Dependencies for starting initial list</param>
        /// <param name="rebuiltFileChange">Output change with dependencies</param>
        /// <returns>Returns an error in constructing the FileChangeWithDependencies, if any</returns>
        public static CLError CreateAndInitialize(FileChange baseChange, IEnumerable<FileChange> initialDependencies, out FileChangeWithDependencies rebuiltFileChange)
        {
            return CreateAndInitialize(baseChange, initialDependencies, out rebuiltFileChange, DelayCompletedLocker: null, fileDownloadMoveLocker: null);
        }

        // Base constructor with locker for DelayProcessesable methods followed by copy of parameters from baseChange
        private FileChangeWithDependencies(FileChange baseChange, IEnumerable<FileChange> initialDependencies, object DelayCompletedLocker, object fileDownloadMoveLocker)
            : base(DelayCompletedLocker, fileDownloadMoveLocker)
        {
            FillInObjectFromConstructionParameters(baseChange, initialDependencies);
        }

        // Copies parameters from baseChange, sets initial dependency list
        private void FillInObjectFromConstructionParameters(FileChange baseChange, IEnumerable<FileChange> initialDependencies)
        {
            if (baseChange == null)
            {
                throw new NullReferenceException(Resources.FileChangeWithDependenciesBaseChangeCannotBeNull);
            }

            base.Direction = baseChange.Direction;
            base.DoNotAddToSQLIndex = baseChange.DoNotAddToSQLIndex;
            base.EventId = baseChange.EventId;
            base.FailureCounter = baseChange.FailureCounter;
            base.NotFoundForStreamCounter = baseChange.NotFoundForStreamCounter;
            base.Metadata = baseChange.Metadata;
            base.NewPath = baseChange.NewPath;
            base.OldPath = baseChange.OldPath;
            byte[] previousMD5Bytes = baseChange.MD5;
            CLError setMD5Error = base.SetMD5(previousMD5Bytes);
            if (setMD5Error != null)
            {
                throw new CLException(CLExceptionCode.Syncing_Model, Resources.FileChangeWithDependenciesErrorSettingMD5FromBaseChangeToNewFileChangeWithDependencies, setMD5Error.Exceptions);
            }
            base.Type = baseChange.Type;

            this._dependencies = (initialDependencies == null ? new List<FileChange>() : new List<FileChange>(initialDependencies));
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