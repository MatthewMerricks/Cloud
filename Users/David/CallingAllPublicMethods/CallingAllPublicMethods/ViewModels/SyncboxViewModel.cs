using CallingAllPublicMethods.Models;
using CallingAllPublicMethods.Models.CLSyncboxActions;
using Cloud.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallingAllPublicMethods.ViewModels
{
    public sealed class SyncboxViewModel : NotifiableObject<SyncboxViewModel>
    {
        public CLSyncboxProxy SelectedSyncbox
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
        private CLSyncboxProxy _selectedSyncbox = 
            (DesignDependencyObject.IsInDesignTool
                ? new CLSyncboxProxy(syncbox: null)
                : null);

        public ObservableCollection<CLSyncboxAction> SyncboxActions
        {
            get
            {
                return _syncboxActions;
            }
        }
        private readonly ObservableCollection<CLSyncboxAction> _syncboxActions = new ObservableCollection<CLSyncboxAction>(
            new []
            {
                // todo: remove debug code:
                (CLSyncboxAction)(new object())
            });
    }
}