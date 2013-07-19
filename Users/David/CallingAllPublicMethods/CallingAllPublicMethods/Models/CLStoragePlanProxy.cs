using Cloud.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallingAllPublicMethods.Models
{
    public sealed class CLStoragePlanProxy
    {
        public CLStoragePlan StoragePlan
        {
            get
            {
                return _storagePlan;
            }
        }
        private readonly CLStoragePlan _storagePlan;

        public long PlanId
        {
            get
            {
                return (_storagePlan == null
                    ? 1
                    : _storagePlan.PlanId);
            }
        }

        public string Name
        {
            get
            {
                return ((_storagePlan == null
                        || _storagePlan.Name == null)
                    ? "{null}"
                    : _storagePlan.Name);
            }
        }

        public bool IsDefaultPlan
        {
            get
            {
                return (_storagePlan == null
                    ? false
                    : _storagePlan.IsDefaultPlan);
            }
        }

        public Nullable<long> BandwidthQuota
        {
            get
            {
                return (_storagePlan == null
                    ? null
                    : _storagePlan.BandwidthQuota);
            }
        }

        public Nullable<long> StorageQuota
        {
            get
            {
                return (_storagePlan == null
                    ? null
                    : _storagePlan.StorageQuota);
            }
        }

        public CLStoragePlanProxy(CLStoragePlan storagePlan)
        {
            Debug.Assert(
                storagePlan != null
                    || DesignDependencyObject.IsInDesignTool,
                "storagePlan cannot be null");

            this._storagePlan = storagePlan;
        }
    }
}