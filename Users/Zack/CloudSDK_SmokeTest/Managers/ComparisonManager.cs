using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public sealed class ComparisonManager
    {
        public bool MoveOn { get; set; }
        public long LastSyncBoxID { get; set; }

        private static readonly GenericHolder<ComparisonManager> _instance = new GenericHolder<ComparisonManager>(null);
        public static ComparisonManager GetInstance()
        {
            lock (_instance)
            {
                if (_instance.Value == null)
                {
                    _instance.Value = new ComparisonManager();
                }
                return _instance.Value;
            }

        }

        private ComparisonManager()
        {
            MoveOn = false;
        }

    }
}
