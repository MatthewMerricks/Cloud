//
//  DelayChangeObservableCollection.cs
//  Cloud Windows
//
//  Created by DavidBruck
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace CloudApiPrivate.EventMessageReceiver
{
    public class DelayChangeObservableCollection<T> : ObservableCollection<T>
    {
        int delayLocked = 0;

        public void LockCollectionChanged()
        {
            delayLocked++;
        }

        public void UnlockCollectionChanged()
        {
            bool needRefresh = false;
            if (delayLocked == 1)
            {
                needRefresh = true;
            }
            delayLocked--;
            if (needRefresh)
            {
                OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Collections.ObjectModel.ObservableCollection`1.CollectionChanged"/> event with the provided event data.
        /// </summary>
        /// <param name="e">The event data to report in the event.</param>
        protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (delayLocked == 0)
            {
                base.OnCollectionChanged(e);
            }
        }
    }
}