namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System.Configuration;
    using System.IO;
    using Microsoft.ServiceModel.Configuration;

    public class TestjQueryConfigurationSection : ConfigurationSection
    {
        public TestjQueryConfigurationSection()
        {
        }

        [ConfigurationProperty("webHttp3")]
        public WebHttpElement3 WebHttpElement3
        {
            get { return (WebHttpElement3)this["webHttp3"]; }
        }

        internal static TestjQueryConfigurationSection GetWebHttpBehavior3Section(string configXml)
        {
            string configPath = Path.GetTempFileName();
            File.WriteAllText(configPath, configXml);

            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
            fileMap.ExeConfigFilename = configPath;
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

            TestjQueryConfigurationSection connectionInfoSection = (TestjQueryConfigurationSection)config.GetSection("jQueryConfigurationSection");
            File.Delete(configPath);

            return connectionInfoSection;
        }
    }
}
