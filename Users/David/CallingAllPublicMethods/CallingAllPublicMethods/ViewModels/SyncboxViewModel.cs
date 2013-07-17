using CallingAllPublicMethods.Models;
using CallingAllPublicMethods.Models.CLSyncboxActions;
using CallingAllPublicMethods.Static;
using Cloud;
using Cloud.Model;
using Cloud.Support;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CallingAllPublicMethods.ViewModels
{
    public sealed class SyncboxViewModel : NotifiableObject<SyncboxViewModel>
    {
        public string SyncboxActionsFilterRegex
        {
            get
            {
                return _syncboxActionsFilterRegex;
            }
            set
            {
                if (_syncboxActionsFilterRegex != value)
                {
                    _syncboxActionsFilterRegex = value;

                    bool tempSyncboxActionsFilterRegexInvalid;
                    SyncboxActionsFilterRegexInvalid = tempSyncboxActionsFilterRegexInvalid = !string.IsNullOrEmpty(value)
                        && !Helpers.IsValidRegex(value);

                    base.NotifyPropertyChanged(parent => parent.SyncboxActionsFilterRegex);

                    _syncboxActions.Clear();
                    if (tempSyncboxActionsFilterRegexInvalid
                        || string.IsNullOrEmpty(value))
                    {
                        Array.ForEach(AllSyncboxActions, currentSyncboxAction => _syncboxActions.Add(currentSyncboxAction));
                    }
                    else
                    {
                        foreach (CLSyncboxAction currentSyncboxAction in AllSyncboxActions)
                        {
                            if (Regex.IsMatch(currentSyncboxAction.Name, value, RegexOptions.IgnoreCase))
                            {
                                _syncboxActions.Add(currentSyncboxAction);
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Default action regex filter: filters out async methods, property get/set methods, base methods (inherited from object or implemented for IDisposable), initializers, and live-syncing methods
        /// </summary>
        private const string InitialSyncboxActionsFilterRegex = "^(?!(Begin|End|get_|set_|add_|remove_|Dispose|Equals|InitWithPath|GetType|GetCurrentSyncStatus|StartLiveSync|StopLiveSync|ToString)).+";
        private string _syncboxActionsFilterRegex = InitialSyncboxActionsFilterRegex;

        public bool SyncboxActionsFilterRegexInvalid
        {
            get
            {
                return _syncboxActionsFilterRegexInvalid;
            }
            private set
            {
                if (_syncboxActionsFilterRegexInvalid != value)
                {
                    _syncboxActionsFilterRegexInvalid = value;
                    base.NotifyPropertyChanged(parent => parent.SyncboxActionsFilterRegexInvalid);
                }
            }
        }
        private bool _syncboxActionsFilterRegexInvalid = false;

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

        public ICommand SyncboxActions_SelectionChanged
        {
            get
            {
                return _syncboxActions_SelectionChanged;
            }
        }
        private readonly ICommand _syncboxActions_SelectionChanged;
        private void SyncboxActions_SelectionChangedHandler(SelectionChangedEventArgs e)
        {
            CLSyncboxAction selectedCLSyncboxAction;
            if (e.AddedItems != null && e.AddedItems.Count == 1 && (selectedCLSyncboxAction = e.AddedItems[0] as CLSyncboxAction) != null)
            {
                MessageBox.Show(string.Format("Not implemented yet: need to process action with name {0}", selectedCLSyncboxAction.Name));
            }
        }

        public CLCredentials CLCredentials
        {
            get
            {
                return _cLCredentials;
            }
            set
            {
                if (_cLCredentials != value)
                {
                    _cLCredentials = value;
                    base.NotifyPropertyChanged(parent => parent.CLCredentials);
                }
            }
        }
        private CLCredentials _cLCredentials = null;

        public void OnConfigurationChanged(string key, string secret, string token, Nullable<long> syncboxId, string deviceId)
        {
            CLCredentials tempCLCredentials;
            CLCredentials.AllocAndInit(key, secret, out tempCLCredentials, token);
            this.CLCredentials = tempCLCredentials;

            CLSyncbox tempCLSyncbox;
            if (tempCLCredentials != null
                && syncboxId != null
                && !string.IsNullOrEmpty(deviceId))
            {
                CLSyncbox.AllocAndInit(
                    (long)syncboxId,
                    tempCLCredentials,
                    out tempCLSyncbox,
                    settings: new CLSyncSettings(deviceId));
            }
            else
            {
                tempCLSyncbox = null;
            }

            SelectedSyncbox = (tempCLSyncbox == null
                ? null
                : new CLSyncboxProxy(tempCLSyncbox));
        }

        public ObservableCollection<CLSyncboxAction> SyncboxActions
        {
            get
            {
                return _syncboxActions;
            }
        }
        private static CLSyncboxAction[] AllSyncboxActions =
            typeof(CLSyncbox)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Select(currentSyncboxMethod => new CLSyncboxAction(currentSyncboxMethod))
                .OrderBy(currentSyncboxMethod => currentSyncboxMethod.Name, StringComparer.Ordinal)
                .ToArray();
        private readonly ObservableCollection<CLSyncboxAction> _syncboxActions = new ObservableCollection<CLSyncboxAction>(
            AllSyncboxActions.Where(
                currentSyncboxAction => Regex.IsMatch(currentSyncboxAction.Name, InitialSyncboxActionsFilterRegex, RegexOptions.IgnoreCase)));

        public SyncboxViewModel()
        {
            _syncboxActions_SelectionChanged = new RelayCommand<SelectionChangedEventArgs>(SyncboxActions_SelectionChangedHandler);
        }
    }
}