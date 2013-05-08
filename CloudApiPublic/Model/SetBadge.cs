//
// SetBadge.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.Static;

namespace Cloud.Model
{
    internal struct SetBadge
    {
        public FilePath PathToBadge
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException(Resources.SetBadgeCannotRetrievePropertyValuesOnAnInvalidSetBadge);
                }
                return _pathToBadge;
            }
        }
        private FilePath _pathToBadge;

        public PathState BadgeState
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException(Resources.SetBadgeCannotRetrievePropertyValuesOnAnInvalidSetBadge);
                }
                return _badgeState;
            }
        }
        private PathState _badgeState;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public SetBadge(PathState badgeType, FilePath pathToBadge)
        {
            // Ensure input variables have proper references set
            if (pathToBadge == null)
            {
                throw new NullReferenceException(Resources.SetBadgePathToBadgeCannotBeNull);
            }
            else if ((badgeType < Enum.GetValues(typeof(PathState)).Cast<PathState>().Min()) || (badgeType > Enum.GetValues(typeof(PathState)).Cast<PathState>().Max()))
            {
                throw new NullReferenceException(Resources.SetBadgeBadgeTypeIsNotWithinTheAcceptableRange);
            }

            this._badgeState = badgeType;
            this._pathToBadge = pathToBadge;
            this._isValid = true;
        }
    }
}