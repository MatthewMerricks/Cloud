using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.Model;
using System.Windows;
using System.Windows.Data;
using System.Linq.Expressions;

namespace CloudSdkSyncSample.EventMessageReceiver
{
    public sealed class CLStatusMessage : NotifiableObject<CLStatusMessage> 
    {
        #region Variable bindable properties

        /// <summary>
        /// Sets and gets the MessageText property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string MessageText
        {
            get { return _messageText; }
            set
            {
                if (_messageText != value)
                {
                    _messageText = value;
                    NotifyPropertyChanged(parent => parent.MessageText);
                }
            }
        }
        private static string MessageTextName = ((MemberExpression)((Expression<Func<CLStatusMessage, string>>)(parent => parent.MessageText)).Body).Member.Name;
        private string _messageText = String.Empty;

        #endregion
    }
}