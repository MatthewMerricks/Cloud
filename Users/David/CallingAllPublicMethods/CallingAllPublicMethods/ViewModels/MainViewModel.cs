using Cloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallingAllPublicMethods.ViewModels
{
    public sealed class MainViewModel : NotifiableObject<MainViewModel>
    {
        public SyncboxViewModel SyncboxViewModel
        {
            get
            {
                return _syncboxViewModel;
            }
        }
        private readonly SyncboxViewModel _syncboxViewModel = new SyncboxViewModel();
    }
}