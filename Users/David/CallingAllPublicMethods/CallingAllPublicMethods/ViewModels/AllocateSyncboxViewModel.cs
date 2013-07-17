using CallingAllPublicMethods.Models;
using CallingAllPublicMethods.Models.AllocateSyncboxActions;
using CallingAllPublicMethods.Static;
using Cloud;
using Cloud.Model;
using Cloud.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CallingAllPublicMethods.ViewModels
{
    public sealed class AllocateSyncboxViewModel : NotifiableObject<AllocateSyncboxViewModel>
    {
        private CLCredentials credentials = null;

        public void OnCLCredentialsChanged(CLCredentials credentials)
        {
            this.credentials = credentials;
        }

        public event EventHandler<CLSyncboxPickedEventArgs> CLSyncboxPicked;
        private void FireCLSyncboxPicked(CLSyncboxProxy Syncbox)
        {
            if (CLSyncboxPicked != null)
            {
                CLSyncboxPicked(this, new CLSyncboxPickedEventArgs(Syncbox));
            }
        }

        public AllocateSyncboxAction[] AllocateSyncboxActions
        {
            get
            {
                return _allocateSyncboxActions;
            }
        }
        private static readonly AllocateSyncboxAction[] _allocateSyncboxActions = new[]
        {
            ListSyncboxesAction.Instance
        };

        public AllocateSyncboxAction SelectedAllocateSyncboxAction
        {
            get
            {
                return _selectedAllocateSyncboxAction;
            }
            set
            {
                if (_selectedAllocateSyncboxAction != value)
                {
                    _selectedAllocateSyncboxAction = value;
                    base.NotifyPropertyChanged(parent => parent.SelectedAllocateSyncboxAction);
                }
            }
        }
        private AllocateSyncboxAction _selectedAllocateSyncboxAction = null;

        public ICommand AllocateSyncboxActions_SelectionChanged
        {
            get
            {
                return _allocateSyncboxActions_SelectionChanged;
            }
        }
        private readonly ICommand _allocateSyncboxActions_SelectionChanged;
        private void AllocateSyncboxActions_SelectionChangedHandler(SelectionChangedEventArgs e)
        {
            AllocateSyncboxAction selectedAllocateSyncboxAction;
            if (e.AddedItems != null && e.AddedItems.Count == 1 && (selectedAllocateSyncboxAction = e.AddedItems[0] as AllocateSyncboxAction) != null)
            {
                selectedAllocateSyncboxAction.Process(this, this.credentials);
            }
            SelectedAllocateSyncboxAction = null; // reset action selector for next process
        }

        public ObservableCollection<CLSyncboxProxy> KnownCLSyncboxes
        {
            get
            {
                return _knownCLSyncboxes;
            }
        }
        private readonly ObservableCollection<CLSyncboxProxy> _knownCLSyncboxes;
        private readonly Dictionary<long, CLSyncboxProxy> _knownCLSyncboxesDictionary = new Dictionary<long, CLSyncboxProxy>();
        public readonly ReadOnlyDictionary<long, CLSyncboxProxy> KnownCLSyncboxesDictionary;
        private void _knownCLSyncboxes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    _knownCLSyncboxesDictionary.Clear();
                    AddSyncboxesToKnownCLSyncboxesDictionary(_knownCLSyncboxes);
                    break;

                case NotifyCollectionChangedAction.Add:
                    AddSyncboxesToKnownCLSyncboxesDictionary(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    RemoveSyncboxesFromKnownCLSyncboxesDictionary(e.OldItems);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    RemoveSyncboxesFromKnownCLSyncboxesDictionary(e.OldItems);
                    AddSyncboxesToKnownCLSyncboxesDictionary(e.NewItems);
                    break;

                //// no need to handle move case since only new/old items need to be remapped in the other dictionary
                //case NotifyCollectionChangedAction.Move:
            }
        }
        private void AddSyncboxesToKnownCLSyncboxesDictionary(IEnumerable syncboxes)
        {
            foreach (CLSyncboxProxy currentKnownSyncbox in syncboxes)
            {
                _knownCLSyncboxesDictionary[currentKnownSyncbox.SyncboxId] = currentKnownSyncbox;
            }
        }
        private void RemoveSyncboxesFromKnownCLSyncboxesDictionary(IEnumerable syncboxes)
        {
            foreach (CLSyncboxProxy currentKnownSyncbox in syncboxes)
            {
                _knownCLSyncboxesDictionary.Remove(currentKnownSyncbox.SyncboxId);
            }
        }

        public string ModifyableDeviceId
        {
            get
            {
                return _modifyableDeviceId;
            }
            set
            {
                if (_modifyableDeviceId != value)
                {
                    _modifyableDeviceId = value;

                    DeviceIdInvalid = string.IsNullOrEmpty(value);

                    base.NotifyPropertyChanged(parent => parent.ModifyableDeviceId);
                }
            }
        }
        private string _modifyableDeviceId = null;

        public Nullable<bool> ListPopupDialogResult
        {
            get
            {
                return _listPopupDialogResult;
            }
            set
            {
                if (_listPopupDialogResult != value)
                {
                    _listPopupDialogResult = value;
                    base.NotifyPropertyChanged(parent => parent.ListPopupDialogResult);
                }
            }
        }
        private Nullable<bool> _listPopupDialogResult = null;

        public ICommand ListPopup_SyncboxSelected
        {
            get
            {
                return _listPopup_SyncboxSelected;
            }
        }
        private readonly ICommand _listPopup_SyncboxSelected;
        private void ListPopup_SyncboxSelectedHandler(CLSyncboxProxy parameter)
        {
            FireCLSyncboxPicked(parameter);

            ListPopupDialogResult = true; // triggers [Window].Close() and sets a successful DialogResult
        }
        private bool ListPopup_SyncboxSelectedCanHandle(CLSyncboxProxy parameter)
        {
            return parameter != null;
        }

        public ICommand ListPopup_Cancelled
        {
            get
            {
                return _listPopup_Cancelled;
            }
        }
        private readonly ICommand _listPopup_Cancelled;
        private void ListPopup_CancelledHandler(object parameter)
        {
            ListPopupDialogResult = false; // triggers [Window].Close() and sets an unsuccessful DialogResult
        }

        public ICommand ListPopup_Refresh
        {
            get
            {
                return _listPopup_Refresh;
            }
        }
        private readonly ICommand _listPopup_Refresh;
        private void ListPopup_RefreshHandler(object parameter)
        {
            ListSyncboxesAction.RefreshList(this, this.credentials);
        }
        private bool ListPopup_RefreshCanHandle(object parameter)
        {
            return !_deviceIdInvalid;
        }

        public bool DeviceIdInvalid
        {
            get
            {
                return _deviceIdInvalid;
            }
            set
            {
                if (_deviceIdInvalid != value)
                {
                    _deviceIdInvalid = value;
                    base.NotifyPropertyChanged(parent => parent.DeviceIdInvalid);
                }
            }
        }
        private bool _deviceIdInvalid = true;

        public ICommand DeleteSyncbox
        {
            get
            {
                return _deleteSyncbox;
            }
        }
        private readonly ICommand _deleteSyncbox;
        private void DeleteSyncboxHandler(CLSyncboxProxy parameter)
        {
            CLError deleteSyncboxError = CLSyncbox.DeleteSyncbox(
                parameter.SyncboxId,
                credentials,
                (parameter == null
                    ? new CLSyncSettings("{null}")
                    : (Cloud.Interfaces.ICLSyncSettings)parameter.Syncbox.CopiedSettings));

            if (deleteSyncboxError == null)
            {
                _knownCLSyncboxes.Remove(parameter);
            }
            else
            {
                MessageBox.Show(string.Format("An error occurred deleting a syncbox. SyncboxId: {0}. Exception code: {1}. Error message: {2}.",
                    parameter.SyncboxId,
                    deleteSyncboxError.PrimaryException.Code,
                    deleteSyncboxError.PrimaryException.Message));
            }
        }

        public AllocateSyncboxViewModel()
        {
            this._allocateSyncboxActions_SelectionChanged = new RelayCommand<SelectionChangedEventArgs>(AllocateSyncboxActions_SelectionChangedHandler);
            this._knownCLSyncboxes = new ObservableCollection<CLSyncboxProxy>();
            this._knownCLSyncboxes.CollectionChanged += _knownCLSyncboxes_CollectionChanged;
            this.KnownCLSyncboxesDictionary = new ReadOnlyDictionary<long, CLSyncboxProxy>(_knownCLSyncboxesDictionary);
            this._listPopup_SyncboxSelected = new RelayCommand<CLSyncboxProxy>(
                ListPopup_SyncboxSelectedHandler,
                ListPopup_SyncboxSelectedCanHandle);
            this._listPopup_Cancelled = new RelayCommand<object>(ListPopup_CancelledHandler);
            this._listPopup_Refresh = new RelayCommand<object>(
                ListPopup_RefreshHandler,
                ListPopup_RefreshCanHandle);
            this._deleteSyncbox = new RelayCommand<CLSyncboxProxy>(DeleteSyncboxHandler);

            if (DesignDependencyObject.IsInDesignTool)
            {
                this._knownCLSyncboxes.Add(new CLSyncboxProxy(syncbox: null));
            }
        }
    }
}