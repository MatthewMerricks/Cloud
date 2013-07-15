using Cloud;
using Cloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallingAllPublicMethods.ViewModels
{
    public sealed class SyncboxViewModel : NotifiableObject<SyncboxViewModel>
    {
        public CLSyncbox SelectedSyncbox
        {
            get
            {
                return _selectedSyncbox;
            }
            set
            {
                if (_selectedSyncbox != value)
                {
                    _selectedSyncbox = value;
                    base.NotifyPropertyChanged(parent => parent.SelectedSyncbox);
                }
            }
        }
        private CLSyncbox _selectedSyncbox = null;
    }
}