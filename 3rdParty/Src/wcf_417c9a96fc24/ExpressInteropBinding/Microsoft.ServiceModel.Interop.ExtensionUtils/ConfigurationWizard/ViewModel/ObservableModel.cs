// <copyright file="ObservableModel.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard.ViewModel
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Base class for view models
    /// </summary>
    public abstract class ObservableModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the ObservableModel class
        /// </summary>
        protected ObservableModel()
        {
        }

        /// <summary>
        /// Event to notify changes in the properties
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event infering the property name from an expression tree representing the property
        /// </summary>
        /// <typeparam name="T">Type containing the property</typeparam>
        /// <param name="propertyExpresion">Expression representing the property</param>
        protected void OnPropertyChanged<T>(Expression<Func<T>> propertyExpresion)
        {
            if (propertyExpresion == null)
            {
                throw new ArgumentNullException("propertyExpresion");
            }

            var property = (MemberExpression)propertyExpresion.Body;
            VerifyPropertyExpression<T>(propertyExpresion, property);
            this.OnPropertyChanged(property.Member.Name);
        }

        /// <summary>
        /// Raises the PropertyChanged event with the property name passed as argument
        /// </summary>
        /// <param name="propertyName">Property name</param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Sets a property with an specific value and raises the PropertyChanged event after that
        /// </summary>
        /// <typeparam name="T">Type containing the property</typeparam>
        /// <param name="refValue">Current property value</param>
        /// <param name="newValue">New property value</param>
        /// <param name="propertyExpresion">Expression tree representing the property</param>
        protected void SetValue<T>(ref T refValue, T newValue, Expression<Func<T>> propertyExpresion)
        {
            if (!object.Equals(refValue, newValue))
            {
                refValue = newValue;
                this.OnPropertyChanged(propertyExpresion);
            }
        }

        /// <summary>
        /// Sets a property with an specific value and calls a callback after that
        /// </summary>
        /// <typeparam name="T">Type containing the property</typeparam>
        /// <param name="refValue">Current property value</param>
        /// <param name="newValue">New property value</param>
        /// <param name="valueChanged">Callback to be called after setting the property value</param>
        protected void SetValue<T>(ref T refValue, T newValue, Action valueChanged)
        {
            if (valueChanged == null)
            {
                throw new ArgumentNullException("valueChanged");
            }

            if (!object.Equals(refValue, newValue))
            {
                refValue = newValue;
                valueChanged();
            }
        }

        [Conditional("DEBUG")]
        private void VerifyPropertyExpression<T>(Expression<Func<T>> propertyExpresion, MemberExpression property)
        {
            if (property.Member.GetType().IsAssignableFrom(typeof(PropertyInfo)))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Invalid Property Expression {0}", propertyExpresion));
            }

            var instance = property.Expression as ConstantExpression;
            if (instance.Value != this)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Invalid Property Expression {0}", propertyExpresion));
            }
        }
    }
}
