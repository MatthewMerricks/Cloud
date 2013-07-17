using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallingAllPublicMethods.Static
{
    public static class DesignTimeData
    {
        #region staging
        public const string ConfigurationNameStaging = "staging";
        public const string ConfigurationKeyStaging = "00d86d2ef9971f5f410480e6f82b49095ec11daa81e5702448f624d53460f42f";
        public const string ConfigurationSecretStaging = "ad4b65f0d89ced24a9feaa5c93e970b8e1a43008d69ad20a6809ab18f6abb5dd";
        public const string ConfigurationDeviceIdStaging = "DesignMode";
        public const long ConfigurationSyncboxIdStaging = 8341;
        #endregion

        #region fake
        public const string ConfigurationKeyFake = "beefbeefbeefbeefbeefbeefbeefbeefbeefbeefbeefbeefbeefbeefbeefbeef";
        public const string ConfigurationSecretFake = "feedfeedfeedfeedfeedfeedfeedfeedfeedfeedfeedfeedfeedfeedfeedfeed";
        public const string ConfigurationTokenFake = "fadefadefadefadefadefadefadefadefadefadefadefadefadefadefadefade";
        public const string ConfigurationDeviceIdFake = "{null}";
        public const long ConfigurationSyncboxIdFake = 1;
        #endregion
    }
}