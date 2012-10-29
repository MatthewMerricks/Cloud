using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SyncTestServer.Model
{
    public abstract class NotifiableObject<T> : INotifyPropertyChanged where T : NotifiableObject<T>
    {
        #region INotifyPropertyChanged member
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region private methods
        protected void NotifyPropertyChanged(Expression<Func<T, object>> notifyExpression)
        {
            if (PropertyChanged != null
                && notifyExpression != null)
            {
                MemberExpression notifyMember = notifyExpression.Body as MemberExpression;
                if (notifyMember == null)
                {
                    UnaryExpression notifyConvert = notifyExpression.Body as UnaryExpression;
                    if (notifyConvert != null)
                    {
                        notifyMember = notifyConvert.Operand as MemberExpression;
                    }
                }

                if (notifyMember != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(notifyMember.Member.Name));
                }
            }
        }
        #endregion
    }
}