//
//  ValidatingViewModelBase.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.Generic;
using GalaSoft.MvvmLight;
using win_client.ViewModels;

namespace MVVMProductsDemo.ViewModels
{
#if SILVERLIGHT
    public class    ValidatingViewModelBase : ViewModelBase, INotifyDataErrorInfo
    {
        //Key is propertyName, value is list of validation errors
        protected Dictionary<string, List<string>> errorList = new Dictionary<string, List<string>>();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// Property that indicates whether any validation errors exist.
        /// </summary>
        public bool HasErrors
        {
            get
            {
                // Determine whether we have a validation error.
                foreach(KeyValuePair<String,List<String>> entry in errorList) 
                {
                    if(entry.Value.Count > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the list of validation errors for a property
        /// </summary>
        /// <param name="propertyName">Name of the propety</param>
        public System.Collections.IEnumerable GetErrors(string propertyName)
        {
            CheckErrorCollectionForProperty(propertyName);

            //return related validation errors for selected property.
            return errorList[propertyName];
        }

        protected void CheckErrorCollectionForProperty(string propertyName)
        {
            //First time to get the validation errors of this property.
            if (!errorList.ContainsKey(propertyName))
            {
                errorList[propertyName] = new List<string>();
            }
        }

        /// <summary>
        /// Add a validation error to a property name.
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="error">The validation error itself</param>
        protected void AddError(string propertyName, string error)
        {
            CheckErrorCollectionForProperty(propertyName);

            errorList[propertyName].Add(error);
            RaiseErrorsChanged(propertyName);
        }

        /// <summary>
        /// Remove a specific validation error registered to a property name.
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="error">The validation error itself</param>
        protected void RemoveError(string propertyName, string error)
        {
            if (errorList[propertyName].Contains(error))
            {
                errorList[propertyName].Remove(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        /// <summary>
        /// Remove all errors for a specific property name.
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        public void RemoveAllErrorsForPropertyName(string propertyName)
        {
                errorList.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
        }

        /// <summary>
        /// Notifies the UI of validation error state change
        /// </summary>
        /// <param name="propertyName">The name of the property that is validated</param>
        protected void RaiseErrorsChanged(string propertyName)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }
    }
#else  // !SILVERLIGHT
    public class ValidatingViewModelBase : ViewModelBase, IDataErrorInfo
    {
        //Key is propertyName, value is list of validation errors
        protected Dictionary<string, List<string>> errorList = new Dictionary<string, List<string>>();

       /// <summary>
        /// Property that returns an error message for this object,
        /// </summary>
        public string Error
        { 
            get
            {
                return "";
            }
        }

        /// <summary>
        /// Property that returns the first error associated with a column.
        /// </summary>
        public string this[string columnName]
        { 
            get
            {
                if (errorList.ContainsKey(columnName) && errorList[columnName][0] != null)
                {
                    return errorList[columnName][0];
                }
                return "";
            }
        }

        /// <summary>
        /// Property that indicates whether any validation errors exist.
        /// </summary>
        public bool HasErrors
        {
            get
            {
                // Determine whether we have a validation error.
                foreach(KeyValuePair<String,List<String>> entry in errorList) 
                {
                    if(entry.Value.Count > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the list of validation errors for a property
        /// </summary>
        /// <param name="propertyName">Name of the propety</param>
        public System.Collections.IEnumerable GetErrors(string propertyName)
        {
            CheckErrorCollectionForProperty(propertyName);

            //return related validation errors for selected property.
            return errorList[propertyName];
        }

        protected void CheckErrorCollectionForProperty(string propertyName)
        {
            //First time to get the validation errors of this property.
            if (!errorList.ContainsKey(propertyName))
            {
                errorList[propertyName] = new List<string>();
            }
        }

        /// <summary>
        /// Add a validation error to a property name.
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="error">The validation error itself</param>
        protected void AddError(string propertyName, string error)
        {
            CheckErrorCollectionForProperty(propertyName);

            errorList[propertyName].Add(error);
            RaiseErrorsChanged(propertyName);
        }

        /// <summary>
        /// Remove a specific validation error registered to a property name.
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="error">The validation error itself</param>
        protected void RemoveError(string propertyName, string error)
        {
            if (errorList[propertyName].Contains(error))
            {
                errorList[propertyName].Remove(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        /// <summary>
        /// Remove all errors for a specific property name.
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        public void RemoveAllErrorsForPropertyName(string propertyName)
        {
                errorList.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
        }

        /// <summary>
        /// Notifies the UI of validation error state change
        /// </summary>
        /// <param name="propertyName">The name of the property that is validated</param>
        protected void RaiseErrorsChanged(string propertyName)
        {
        }
    }
#endif  // !SILVERLIGHT
}
