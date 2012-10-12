using CloudApiPrivate.Model;
using RateBar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Windows;

namespace win_client.Model
{
    public abstract class CLStatusFileTransferBase<T> : NotifiableObject<T> where T : CLStatusFileTransferBase<T>
    {
        public virtual string CloudRelativePath { get { return string.Empty; } set { throw new NotSupportedException("Readonly"); } }

        /// <summary>
        /// Sets and gets the Visibility property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public abstract Visibility Visibility { get; }

        /// <summary>
        /// Sets and gets the DisplayRateAtCurrentSample property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public virtual Double DisplayRateAtCurrentSample
        {
            get { return 0d; }
            set
            {
                throw new NotSupportedException("Readonly");
            }
        }

        /// <summary>
        /// Sets and gets the StatusGraph property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public virtual RateGraph StatusGraph
        {
            get { return null; }
            set
            {
                throw new NotSupportedException("Readonly");
            }
        }

        /// <summary>
        /// Sets and gets the PercentComplete property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public virtual Double PercentComplete
        {
            get { return 0d; }
            set
            {
                throw new NotSupportedException("Readonly");
            }
        }
        protected static string PercentCompleteName = ((MemberExpression)((Expression<Func<CLStatusFileTransferBase<T>, double>>)(parent => parent.PercentComplete)).Body).Member.Name;

        /// <summary>
        /// Sets and gets the DisplayFileSize property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public virtual string DisplayFileSize
        {
            get { return string.Empty; }
            set
            {
                throw new NotSupportedException("Readonly");
            }
        }

        /// <summary>
        /// Sets and gets the DisplayTimeLeft property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public virtual string DisplayTimeLeft
        {
            get { return string.Empty; }
            set
            {
                throw new NotSupportedException("Readonly");
            }
        }

        /// <summary>
        /// Sets and gets the DisplayElapsedTime property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public virtual string DisplayElapsedTime
        {
            get { return string.Empty; }
            set
            {
                throw new NotSupportedException("Readonly");
            }
        }
    }
}