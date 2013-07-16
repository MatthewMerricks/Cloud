using CallingAllPublicMethods.Models;
using Cloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        public ConfigurationsViewModel ConfigurationsViewModel
        {
            get
            {
                return _configurationsViewModel;
            }
        }
        private readonly ConfigurationsViewModel _configurationsViewModel = new ConfigurationsViewModel();

        public MainViewModel()
        {
            _configurationsViewModel.PropertyChanged += _configurationsViewModel_PropertyChanged;
            _syncboxViewModel.PropertyChanged += _syncboxViewModel_PropertyChanged;
        }

        private void _syncboxViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == ((MemberExpression)((Expression<Func<SyncboxViewModel, CLSyncboxProxy>>)(parent => parent.SelectedSyncbox)).Body).Member.Name)
            {

            }
        }

        private void _configurationsViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == ((MemberExpression)((Expression<Func<ConfigurationsViewModel, ConfigurationType>>)(parent => parent.SelectedConfiguration)).Body).Member.Name)
            {
                _syncboxViewModel.OnConfigurationChanged(
                    (_configurationsViewModel.SelectedConfiguration.Credentials == null
                        ? null
                        : _configurationsViewModel.SelectedConfiguration.Credentials.Key),
                    (_configurationsViewModel.SelectedConfiguration.Credentials == null
                        ? null
                        : _configurationsViewModel.SelectedConfiguration.Credentials.Secret),
                    (_configurationsViewModel.SelectedConfiguration.Credentials == null
                        ? null
                        : _configurationsViewModel.SelectedConfiguration.Credentials.Token),
                    (_configurationsViewModel.SelectedConfiguration.SelectedSyncbox == null
                        ? (Nullable<long>)null
                        : _configurationsViewModel.SelectedConfiguration.SelectedSyncbox.SyncboxId),
                    (_configurationsViewModel.SelectedConfiguration.SelectedSyncbox == null
                        ? null
                        : _configurationsViewModel.SelectedConfiguration.SelectedSyncbox.DeviceId));
            }
        }
    }
}