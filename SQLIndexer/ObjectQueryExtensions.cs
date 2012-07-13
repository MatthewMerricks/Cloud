//
// ObjectQueryExtensions.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SQLIndexer
{
    public static class ObjectQueryExtensions
    {
        public static ObjectQuery<T> Include<T>(this ObjectQuery<T> includeQuery, Expression<Func<T, object>> propertySelector)
        {
            if (propertySelector == null)
            {
                return includeQuery;
            }
            MemberExpression propertyMember = propertySelector.Body as MemberExpression;
            if (propertyMember == null)
            {
                UnaryExpression propertyUnary = propertySelector.Body as UnaryExpression;
                if (propertyUnary != null)
                {
                    propertyMember = propertyUnary.Operand as MemberExpression;
                }
            }
            if (propertyMember == null)
            {
                return includeQuery;
            }
            return includeQuery.Include(propertyMember.Member.Name);
        }
    }
}
