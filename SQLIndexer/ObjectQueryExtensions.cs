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
        /// <summary>
        /// Custom extension method to run refactorable includes on an ObjectQuery
        /// </summary>
        /// <typeparam name="T">Generic type of input ObjectQuery</typeparam>
        /// <param name="includeQuery">Input ObjectQueryt to run Include</param>
        /// <param name="propertySelector">Member expression returning the property to include (i.e. "parent -> parent.[Inner Property]")</param>
        /// <returns></returns>
        public static ObjectQuery<T> Include<T>(this ObjectQuery<T> includeQuery, Expression<Func<T, object>> propertySelector)
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
