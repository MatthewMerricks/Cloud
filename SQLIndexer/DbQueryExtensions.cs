//
// ObjectQueryExtensions.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SQLIndexer
{
    public static class DbQueryExtensions
    {
        public static DbQuery<T> Include<T>(this DbQuery<T> includeQuery, Expression<Func<T, object>> propertySelector)
        {
            // return unmodified input on null parameter
            if (propertySelector == null
                || includeQuery == null)
            {
                return includeQuery;
            }
            // try cast member expression
            MemberExpression propertyMember = propertySelector.Body as MemberExpression;
            // if try cast failed,
            // then it may be a unary expression containing a member expresion operand
            if (propertyMember == null)
            {
                // try cast unary expression
                UnaryExpression propertyUnary = propertySelector.Body as UnaryExpression;
                // if try cast did not fail,
                // then try cast the operand as member expression
                if (propertyUnary != null)
                {
                    propertyMember = propertyUnary.Operand as MemberExpression;
                }
            }
            // if both attempts to try cast as member expression failed,
            // return unmodified input
            if (propertyMember == null)
            {
                return includeQuery;
            }
            // return input after running the base Include with the property name
            return includeQuery.Include(propertyMember.Member.Name);
        }
    }
}
