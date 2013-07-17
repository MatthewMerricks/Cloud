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
            CLError testCredentialsError = CLCredentials.AllocAndInit(
                copyKey,
                copySecret,
                out testCredentials,
                copyToken);

            if (testCredentialsError != null)
            {
                MessageBox.Show(
                    string.Format(
                        "Key, secret, and/or token are invalid for CLCredentials. ExceptionCode: {0}. Error message: {1}.",
                        testCredentialsError.PrimaryException.Code,
                        testCredentialsError.PrimaryException.Message));
            }
            else
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

        public ConfigurationsViewModel()
        {
            Configurations initialConfigurations = ReadInitialConfigurations();
            _configurations = new ObservableCollection<ConfigurationType>(initialConfigurations.Items);
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
                MessageBox.Show("Saving configurations is disabled in design tool");
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