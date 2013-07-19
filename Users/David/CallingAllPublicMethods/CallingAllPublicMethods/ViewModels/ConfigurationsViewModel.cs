using CallingAllPublicMethods.Models;
using CallingAllPublicMethods.Static;
using Cloud;
using Cloud.Model;
using Cloud.Support;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;

namespace CallingAllPublicMethods.ViewModels
{
    public sealed class ConfigurationsViewModel : NotifiableObject<ConfigurationsViewModel>
    {
        private static readonly XmlSerializer ConfigurationsSerializer = new XmlSerializer(typeof(Configurations));
        private static readonly string ConfigurationsParentFolderLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create) +
            "\\CloudCAPM";
        private static readonly string ConfigurationsLocation = ConfigurationsParentFolderLocation + "\\Configurations.xml";

        public string ModifyableKey
        {
            get
            {
                return _modifyableKey;
            }
            set
            {
                if (_modifyableKey != value)
                {
                    _modifyableKey = value;
                    base.NotifyPropertyChanged(parent => parent.ModifyableKey);
                }
            }
        }
        private string _modifyableKey = null;

        public string ModifyableSecret
        {
            get
            {
                return _modifyableSecret;
            }
            set
            {
                if (_modifyableSecret != value)
                {
                    _modifyableSecret = value;
                    base.NotifyPropertyChanged(parent => parent.ModifyableSecret);
                }
            }
        }
        private string _modifyableSecret = null;

        public string ModifyableToken
        {
            get
            {
                return _modifyableToken;
            }
            set
            {
                if (_modifyableToken != value)
                {
                    _modifyableToken = value;
                    base.NotifyPropertyChanged(parent => parent.ModifyableToken);
                }
            }
        }
        private string _modifyableToken = null;

        public ICommand SaveCredentials
        {
            get
            {
                return _saveCredentials;
            }
        }
        private readonly ICommand _saveCredentials;
        private void SaveCredentialsHandler(object parameter)
        {
            string copyKey = _modifyableKey;
            if (copyKey == string.Empty)
            {
                copyKey = null;
            }
            string copySecret = _modifyableSecret;
            if (copySecret == string.Empty)
            {
                copySecret = null;
            }
            string copyToken = _modifyableToken;
            if (copyToken == string.Empty)
            {
                copyToken = null;
            }

            CLCredentials testCredentials;
            if (Helpers.TryAllocCLCredentials(copyKey, copySecret, copyToken, out testCredentials))
            {
                _selectedConfiguration.Credentials = new CredentialsType()
                {
                    Key = copyKey,
                    Secret = copySecret,
                    Token = copyToken
                };
                RevertCredentialsHandler(parameter: null);
                SaveConfigurations(_configurations);
                base.NotifyPropertyChanged(parent => parent.SelectedConfiguration);
            }
        }

        public ICommand RevertCredentials
        {
            get
            {
                return _revertCredentials;
            }
        }
        private readonly ICommand _revertCredentials;
        private void RevertCredentialsHandler(object parameter)
        {
            ModifyableKey = ((_selectedConfiguration == null
                    || _selectedConfiguration.Credentials == null
                    || _selectedConfiguration.Credentials.Key == null)
                ? null
                : _selectedConfiguration.Credentials.Key);
            ModifyableSecret = ((_selectedConfiguration == null
                    || _selectedConfiguration.Credentials == null
                    || _selectedConfiguration.Credentials.Secret == null)
                ? null
                : _selectedConfiguration.Credentials.Secret);
            ModifyableToken = ((_selectedConfiguration == null
                    || _selectedConfiguration.Credentials == null
                    || _selectedConfiguration.Credentials.Token == null)
                ? null
                : _selectedConfiguration.Credentials.Token);
        }

        public ConfigurationType SelectedConfiguration
        {
            get
            {
                return _selectedConfiguration;
            }
            set
            {
                if (_selectedConfiguration != value)
                {
                    _selectedConfiguration = value;
                    if (value != null)
                    {
                        lastNonNullConfiguration = value;
                    }

                    RevertCredentialsHandler(parameter: null);

                    base.NotifyPropertyChanged(parent => parent.SelectedConfiguration);
                }
            }
        }
        private ConfigurationType _selectedConfiguration;
        private ConfigurationType lastNonNullConfiguration;

        public void UpdateSelectedSyncboxIdAndDeviceId(Nullable<long> syncboxId, string deviceId)
        {
            bool changeDetected;

            if (syncboxId != null
                && !string.IsNullOrEmpty(deviceId))
            {
                if (_selectedConfiguration.SelectedSyncbox == null
                    || _selectedConfiguration.SelectedSyncbox.SyncboxId != ((long)syncboxId)
                    || !string.Equals(_selectedConfiguration.SelectedSyncbox.DeviceId, deviceId, StringComparison.Ordinal))
                {
                    _selectedConfiguration.SelectedSyncbox = new SyncboxType()
                    {
                        DeviceId = deviceId,
                        SyncboxId = (long)syncboxId
                    };
                    changeDetected = true;
                }
                else
                {
                    changeDetected = false;
                }
            }
            else
            {
                if (_selectedConfiguration.SelectedSyncbox == null)
                {
                    changeDetected = false;
                }
                else
                {
                    _selectedConfiguration.SelectedSyncbox = null;
                    changeDetected = true;
                }
            }

            if (changeDetected)
            {
                SaveConfigurations(_configurations);
            }
        }

        public ObservableCollection<ConfigurationType> Configurations
        {
            get
            {
                return _configurations;
            }
        }
        private readonly ObservableCollection<ConfigurationType> _configurations;

        public ICommand AddConfiguration
        {
            get
            {
                return _addConfiguration;
            }
        }
        private readonly ICommand _addConfiguration;
        private void AddConfigurationHandler(object parameter)
        {
            ConfigurationType lastConfiguration = _configurations[_configurations.Count - 1];
            ConfigurationType newConfiguration = new ConfigurationType()
            {
                Id = lastConfiguration.Id + 1
            };
            _configurations.Add(newConfiguration);
            SelectedConfiguration = newConfiguration;
            SaveConfigurations(_configurations);
        }

        public ICommand RemoveConfiguration
        {
            get
            {
                return _removeConfiguration;
            }
        }
        private readonly ICommand _removeConfiguration;
        private void RemoveConfigurationHandler(object parameter)
        {
            if (_configurations.Count == 1)
            {
                MessageBox.Show("Cannot remove last remaining configuration");
            }
            else
            {
                for (int configurationIndex = 0; configurationIndex < _configurations.Count; configurationIndex++)
                {
                    if (_configurations[configurationIndex] == _selectedConfiguration)
                    {
                        if (configurationIndex == 0)
                        {
                            SelectedConfiguration = _configurations[1];
                        }
                        else
                        {
                            SelectedConfiguration = _configurations[configurationIndex - 1];
                        }

                        _configurations.RemoveAt(configurationIndex);

                        SaveConfigurations(_configurations);

                        break;
                    }
                }
            }
        }

        public ICommand Configurations_LostFocus
        {
            get
            {
                return _configurations_LostFocus;
            }
        }
        private readonly ICommand _configurations_LostFocus;
        private void Configurations_LostFocusHandler(ComboBox parameter)
        {
            if (parameter.SelectedIndex == -1)
            {
                lastNonNullConfiguration.Name = parameter.Text;
                ConfigurationType[] storeConfigurations = _configurations.OrderBy(currentConfiguration => currentConfiguration.Name, StringComparer.Ordinal).ToArray();
                _configurations.Clear();
                for (int configurationIndex = 0; configurationIndex < storeConfigurations.Length; configurationIndex++)
                {
                    _configurations.Add(storeConfigurations[configurationIndex]);
                }
                SelectedConfiguration = lastNonNullConfiguration;
                SaveConfigurations(_configurations);
            }
        }

        public ICommand CreateSession
        {
            get
            {
                return _createSession;
            }
        }
        private readonly ICommand _createSession;
        private void CreateSessionHandler(KeyValuePair<string, string> parameter)
        {
            CLCredentials testCredentials;
            if (Helpers.TryAllocCLCredentials(
                (_selectedConfiguration.Credentials == null
                    ? null
                    : _selectedConfiguration.Credentials.Key),
                (_selectedConfiguration.Credentials == null
                    ? null
                    : _selectedConfiguration.Credentials.Secret),
                (_selectedConfiguration.Credentials == null
                    ? null
                    : _selectedConfiguration.Credentials.Token),
                out testCredentials))
            {
                Nullable<long> storeFirstSyncboxId = null;
                HashSet<long> syncboxIds = null;
                if (!string.IsNullOrEmpty(parameter.Key))
                {
                    foreach (long currentSyncboxId in parameter.Key.Split(',').Select(currentKeySplit => long.Parse(currentKeySplit)))
                    {
                        if (syncboxIds == null)
                        {
                            syncboxIds = new HashSet<long>(Cloud.Static.Helpers.EnumerateSingleItem(currentSyncboxId));
                            if (storeFirstSyncboxId == null)
                            {
                                storeFirstSyncboxId = currentSyncboxId;
                            }
                        }
                        else
                        {
                            syncboxIds.Add(currentSyncboxId);
                        }
                    }
                }

                Nullable<long> sessionMinutes;
                if (string.IsNullOrEmpty(parameter.Value))
                {
                    sessionMinutes = null;
                }
                else
                {
                    sessionMinutes = long.Parse(parameter.Value);
                }

                string sessionKey;
                string sessionSecret;
                string sessionToken;
                CLError createSessionError = testCredentials.CreateSessionCredentialsForSyncboxIds(
                    out sessionKey,
                    out sessionSecret,
                    out sessionToken,
                    syncboxIds,
                    sessionMinutes);

                if (createSessionError == null)
                {
                    _configurations.Add(
                        SelectedConfiguration = new ConfigurationType()
                        {
                            Id = (_configurations[_configurations.Count - 1].Id + 1),
                            Name = _selectedConfiguration.ToString() + " Session",
                            Credentials = new CredentialsType()
                            {
                                Key = sessionKey,
                                Secret = sessionSecret,
                                Token = sessionToken
                            },
                            SelectedSyncbox = ((_selectedConfiguration.SelectedSyncbox == null || storeFirstSyncboxId == null)
                                ? null
                                : new SyncboxType()
                                {
                                    DeviceId = _selectedConfiguration.SelectedSyncbox.DeviceId,
                                    SyncboxId = (long)storeFirstSyncboxId
                                })
                        });

                    SaveConfigurations(_configurations);
                }
                else
                {
                    MessageBox.Show(string.Format("An error occurred creating session. Exception code: {0}. Error message: {1}.",
                        createSessionError.PrimaryException.Code,
                        createSessionError.PrimaryException.Message));
                }
            }
        }
        private bool CreateSessionCanHandle(KeyValuePair<string, string> parameter)
        {
            long testParse;
            return
                // check for valid configuration credentials
                _selectedConfiguration.Credentials != null
                && !string.IsNullOrEmpty(_selectedConfiguration.Credentials.Key)
                && !string.IsNullOrEmpty(_selectedConfiguration.Credentials.Secret)
                && string.IsNullOrEmpty(_selectedConfiguration.Credentials.Token)

                // make sure credentials are not currently modified (but not saved)
                && string.Equals(_selectedConfiguration.Credentials.Key, _modifyableKey, StringComparison.Ordinal)
                && string.Equals(_selectedConfiguration.Credentials.Secret, _modifyableSecret, StringComparison.Ordinal)
                && string.IsNullOrEmpty(_modifyableToken)

                // check for valid syncbox ids
                && (string.IsNullOrEmpty(parameter.Key)
                    || parameter.Key.Split(',')
                        .All(currentSyncboxId => long.TryParse(currentSyncboxId.Trim(), out testParse)))

                // check for valid session minutes
                && (string.IsNullOrEmpty(parameter.Value)
                    || long.TryParse(parameter.Value.Trim(), out testParse));
        }

        public ConfigurationsViewModel()
        {
            Configurations initialConfigurations = ReadInitialConfigurations();
            _configurations = new ObservableCollection<ConfigurationType>(initialConfigurations.Items.OrderBy(initialConfiguration => initialConfiguration.ToString(), StringComparer.Ordinal));
            if (initialConfigurations.Items.Length > 0)
            {
                lastNonNullConfiguration = _selectedConfiguration = initialConfigurations.Items[0];
                RevertCredentialsHandler(parameter: null);
            }
            _saveCredentials = new RelayCommand<object>(SaveCredentialsHandler);
            _revertCredentials = new RelayCommand<object>(RevertCredentialsHandler);
            _addConfiguration = new RelayCommand<object>(AddConfigurationHandler);
            _removeConfiguration = new RelayCommand<object>(RemoveConfigurationHandler);
            _configurations_LostFocus = new RelayCommand<ComboBox>(Configurations_LostFocusHandler);
            _createSession = new RelayCommand<KeyValuePair<string, string>>(
                CreateSessionHandler,
                CreateSessionCanHandle);
        }

        private static readonly object ConfigurationsLocker = new object();
        private static Configurations ReadInitialConfigurations()
        {
            if (DesignDependencyObject.IsInDesignTool)
            {
                return new Configurations()
                {
                    Items = new[]
                    {
                        new ConfigurationType()
                        {
                            Id = 1,

                            //// staging:
                            //
                            Name = DesignTimeData.ConfigurationNameStaging,

                            Credentials = new CredentialsType()
                            {
                                //// staging:
                                //
                                Key = DesignTimeData.ConfigurationKeyStaging,
                                Secret = DesignTimeData.ConfigurationSecretStaging

                                //// fake:
                                //
                                //Key = DesignTimeData.ConfigurationKeyFake,
                                //Secret = DesignTimeData.ConfigurationSecretFake,
                                //Token = DesignTimeData.ConfigurationTokenFake
                            },
                            SelectedSyncbox = new SyncboxType()
                            {
                                //// staging:
                                //
                                DeviceId = DesignTimeData.ConfigurationDeviceIdStaging,
                                SyncboxId = DesignTimeData.ConfigurationSyncboxIdStaging

                                //// fake:
                                //
                                //DeviceId = DesignTimeData.ConfigurationDeviceIdFake,
                                //SyncboxId = DesignTimeData.ConfigurationSyncboxIdFake
                            }
                        }
                    }
                };
            }
            else
            {
                lock (ConfigurationsLocker)
                {
                    try
                    {
                        if (Directory.Exists(ConfigurationsParentFolderLocation))
                        {
                            if (File.Exists(ConfigurationsLocation))
                            {
                                using (FileStream configurationsReadStream = File.OpenRead(ConfigurationsLocation))
                                {
                                    return (Configurations)ConfigurationsSerializer.Deserialize(configurationsReadStream);
                                }
                            }
                        }
                        else
                        {
                            Directory.CreateDirectory(ConfigurationsParentFolderLocation);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format("Error reading initial configurations. Configurations will be recreated as new. Error message: {0}", ex.Message));
                    }

                    Configurations toReturn = new Configurations()
                    {
                        Items = new[]
                        {
                            new ConfigurationType()
                            {
                                Id = 1
                            }
                        }
                    };

                    try
                    {
                        if (File.Exists(ConfigurationsLocation))
                        {
                            File.Delete(ConfigurationsLocation);
                        }

                        using (FileStream configurationsWriteStream = File.OpenWrite(ConfigurationsLocation))
                        {
                            ConfigurationsSerializer.Serialize(configurationsWriteStream, toReturn);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format("Error writing initial configurations. Error message: {0}", ex.Message));
                    }

                    return toReturn;
                }
            }
        }
        private static void SaveConfigurations(IEnumerable<ConfigurationType> configurationTypes)
        {
            ConfigurationType[] configurationTypesArray = null;
            if (DesignDependencyObject.IsInDesignTool)
            {
                MessageBox.Show(string.Format("Saving configurations is disabled in design tool. Stacktrace:{0}{1}",
                    Environment.NewLine,
                    Environment.StackTrace));
            }
            else if (configurationTypes != null
                && (configurationTypesArray = configurationTypes.ToArray()).Length == 0)
            {
                MessageBox.Show("Unable to save configurations without any configurations");
            }
            else
            {
                lock (ConfigurationsLocker)
                {
                    try
                    {
                        if (!Directory.Exists(ConfigurationsParentFolderLocation))
                        {
                            Directory.CreateDirectory(ConfigurationsParentFolderLocation);
                        }

                        if (File.Exists(ConfigurationsLocation))
                        {
                            File.Delete(ConfigurationsLocation);
                        }

                        Configurations toWrite = new Configurations()
                        {
                            Items = configurationTypesArray
                        };

                        using (FileStream configurationsWriteStream = File.OpenWrite(ConfigurationsLocation))
                        {
                            ConfigurationsSerializer.Serialize(configurationsWriteStream, toWrite);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format("Error saving configurations. Error message: {0}", ex.Message));
                    }
                }
            }
        }
    }
}