//
// UpdatePathArgs.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model
{
    internal sealed class UpdatePathArgs : HandleableEventArgs
    {
        public PathState State
        {
            get
            {
                return _state;
            }
        }
        private readonly PathState _state;

        public FilePath Path
        {
            get
            {
                return _path;
            }
        }
        private readonly FilePath _path;

        public UpdatePathArgs(SetBadge badgeChange)
        {
            if (badgeChange.PathToBadge == null)
            {
                throw new NullReferenceException(Resources.UpdatePathArgsPathToBadgeCannotBeNull);
            }

            this._state = State;
            this._path = Path;
        }
    }
}