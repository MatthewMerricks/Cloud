//
//  GenericHolder.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.using System;
//
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Cloud.Model
{
    [DataContract]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class GenericHolder<T>
    {
        [DataMember]
        public T Value { get; set; }

        public static readonly PropertyInfo ValueInfo = typeof(GenericHolder<T>)
            .GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);

        /// <summary>
        /// Attempts to cast the first parameter as the current generic typed GenericHolder and sets Value to the second parameter
        /// </summary>
        /// <param name="toSet">Generic-typed GenericHolder for setting Value</param>
        /// <param name="value">Generic-type instance to put in Value</param>
        public static void GenericSet(object toSet, T value)
        {
            GenericSet(toSet as GenericHolder<T>, value);
        }

        /// <summary>
        /// Sets Value of the first parameter to the second parameter
        /// </summary>
        /// <param name="toSet">Holder of Value</param>
        /// <param name="value">Instance to put in Value</param>
        public static void GenericSet(GenericHolder<T> toSet, T value)
        {
            if (toSet != null)
            {
                toSet.Value = value;
            }
        }

        public GenericHolder() { }
        public GenericHolder(T defaultValue)
        {
            this.Value = defaultValue;
        }

        public override bool Equals(object obj)
        {
            GenericHolder<T> castObj = obj as GenericHolder<T>;
            if (((object)castObj) == null)
            {
                return false;
            }

            if (base.Equals(obj))
            {
                return true;
            }

            if (Value == null && castObj.Value == null)
            {
                return true;
            }

            if (Value == null || castObj.Value == null)
            {
                return false;
            }

            return Value.Equals(castObj.Value);
        }

        public override int GetHashCode()
        {
            return (((object)Value) == null ? 0 : Value.GetHashCode());
        }

        private static Func<T, T, bool> _runEquality = null;

        private static Func<T, T, bool> RunEquality
        {
            get
            {
                lock (RunEqualityLocker)
                {
                    if (_runEquality == null)
                    {
                        ParameterExpression firstExpression = Expression.Parameter(typeof(T), "first");
                        ParameterExpression secondExpression = Expression.Parameter(typeof(T), "second");

                        BinaryExpression equalityExpression = Expression.Equal(firstExpression, secondExpression);

                        _runEquality = (Func<T, T, bool>)Expression.Lambda(equalityExpression,
                            new ParameterExpression[]
                            {
                                firstExpression,
                                secondExpression
                            }).Compile();
                    }
                    return _runEquality;
                }
            }
        }
        private static object RunEqualityLocker = new object();

        public static bool operator ==(GenericHolder<T> first, GenericHolder<T> second)
        {
            if (((object)first) == ((object)second))
            {
                return true;
            }

            if (((object)first) == null || ((object)second) == null)
            {
                return false;
            }

            return (bool)RunEquality.DynamicInvoke(first.Value, second.Value);
        }

        public static bool operator !=(GenericHolder<T> first, GenericHolder<T> second)
        {
            if (((object)first) == ((object)second))
            {
                return false;
            }

            if (((object)first) == null || ((object)second) == null)
            {
                return true;
            }

            return !((bool)RunEquality.DynamicInvoke(first.Value, second.Value));
        }
    }
}