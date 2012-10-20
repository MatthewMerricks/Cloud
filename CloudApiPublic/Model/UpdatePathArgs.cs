//
// UpdatePathArgs.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Model
{
    public sealed class UpdatePathArgs : HandleableEventArgs
    {
        public PathState State
        {
            get
            {
                return _state;
            }
        }
        private PathState _state;

        public FilePath Path
        {
            get
            {
                return _path;
            }
        }
        private FilePath _path;

        public UpdatePathArgs(PathState State, FilePath Path)
        {
            if (Path == null)
            {
                throw new NullReferenceException("Path cannot be null");
            }

            this._state = State;
            this._path = Path;
        }
    }
}