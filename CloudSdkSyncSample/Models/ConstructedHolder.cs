using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSdkSyncSample.Model
{
    /// <summary>
    /// Marker for whether partial-class specific construction methods are fired
    /// </summary>
    internal sealed class ConstructedHolder
    {
        public bool IsConstructed
        {
            get
            {
                lock (IsConstructedLocker)
                {
                    return _isConstructed;
                }
            }
        }
        private bool _isConstructed = false;
        private readonly object IsConstructedLocker = new object();

        public void MarkConstructed(Action constructionSetters = null)
        {
            lock (IsConstructedLocker)
            {
                if (_isConstructed)
                {
                    throw new Exception("Already marked constructed");
                }

                if (constructionSetters != null)
                {
                    constructionSetters();
                }

                _isConstructed = true;
            }
        }

        public void MarkConstructed<T>(Action<T> constructionSetters, T constructionState)
        {
            lock (IsConstructedLocker)
            {
                if (_isConstructed)
                {
                    throw new Exception("Already marked constructed");
                }

                if (constructionSetters != null)
                {
                    constructionSetters(constructionState);
                }

                _isConstructed = true;
            }
        }
    }
}