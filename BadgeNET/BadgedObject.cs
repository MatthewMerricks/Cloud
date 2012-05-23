//
// BadgedObject.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Linq.Expressions;

namespace BadgeNET
{
    public class BadgedObject : INotifyPropertyChanged
    {
        #region Properties
        /// <summary>
        /// Does not notify on property changed
        /// </summary>
        public string FilePath { get; private set; }

        public BadgeType Type
        {
            get
            {
                return _type;
            }
            set
            {
                if (!_type.Equals(value))
                {
                    _type = value;
                    this.NotifyPropertyChanged((property) => property.Type);
                }
            }
        }
        private BadgeType _type = BadgeType.Syncing;
        #endregion

        #region INotifyPropertyChanged members
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(Expression<Func<BadgedObject, object>> property)
        {
            //I do not check if PropertyChanged is null because the constructor asserts for an initial changeHandler

            MemberExpression propertyMember = property.Body as MemberExpression;
            if (propertyMember == null)
                propertyMember = (MemberExpression)((UnaryExpression)property.Body).Operand;
            PropertyChanged(this, new PropertyChangedEventArgs(propertyMember.Member.Name));
        }
        #endregion

        public BadgedObject(string filePath, BadgeType initialType, PropertyChangedEventHandler changeHandler)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new Exception("filePath cannot be null nor empty");
            if (changeHandler == null)
                throw new Exception("changeHandler cannot be null");

            this.FilePath = filePath;
            this._type = initialType;
            this.PropertyChanged += changeHandler;
        }
    }
}
