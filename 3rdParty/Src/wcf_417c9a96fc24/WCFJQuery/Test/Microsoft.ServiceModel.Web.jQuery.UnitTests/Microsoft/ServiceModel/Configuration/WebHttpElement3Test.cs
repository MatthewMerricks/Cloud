namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System.Configuration;
    using System.ServiceModel.Web;
    using System.Text;
    using Microsoft.ServiceModel.Configuration;
    using Microsoft.ServiceModel.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebHttpElement3Test
    {
        [TestMethod]
        public void DefaultsTest()
        {
            string configXml = CreateConfig(null, null, null, null, null);

            TestjQueryConfigurationSection configSection = TestjQueryConfigurationSection.GetWebHttpBehavior3Section(configXml);
            WebHttpElement3 element = configSection.WebHttpElement3;
            Assert.AreEqual(true, element.AutomaticFormatSelectionEnabled);
            Assert.AreEqual(typeof(WebHttpBehavior3), element.BehaviorType);
            Assert.AreEqual(WebMessageBodyStyle.Bare, element.DefaultBodyStyle);
            Assert.AreEqual(WebMessageFormat.Xml, element.DefaultOutgoingResponseFormat);
            Assert.AreEqual(false, element.FaultExceptionEnabled);
            Assert.AreEqual(true, element.HelpEnabled);
        }

        [TestMethod]
        public void ValuesTest()
        {
            bool anyBool1 = AnyInstance.AnyBool;
            bool anyBool2 = !AnyInstance.AnyBool;
            bool anyBool3 = AnyInstance.AnyBool;
            WebMessageBodyStyle anyBodyStyle = WebMessageBodyStyle.Bare;
            WebMessageFormat anyMessageFormat = WebMessageFormat.Xml;

            string configXml = CreateConfig(anyBool1, anyBodyStyle, anyMessageFormat, anyBool2, anyBool3);

            TestjQueryConfigurationSection configSection = TestjQueryConfigurationSection.GetWebHttpBehavior3Section(configXml);
            WebHttpElement3 element = configSection.WebHttpElement3;
            Assert.AreEqual(typeof(WebHttpBehavior3), element.BehaviorType);

            Assert.AreEqual(anyBool1, element.AutomaticFormatSelectionEnabled);
            Assert.AreEqual(anyBodyStyle, element.DefaultBodyStyle);
            Assert.AreEqual(anyMessageFormat, element.DefaultOutgoingResponseFormat);
            Assert.AreEqual(anyBool2, element.FaultExceptionEnabled);
            Assert.AreEqual(anyBool3, element.HelpEnabled);
        }

        [TestMethod]
        public void PropertiesTest()
        {
            WebHttpElement3 target = new WebHttpElement3();

            bool anyBool1 = AnyInstance.AnyBool;
            bool anyBool2 = !AnyInstance.AnyBool;
            bool anyBool3 = AnyInstance.AnyBool;
            WebMessageBodyStyle anyBodyStyle = WebMessageBodyStyle.Bare;
            WebMessageFormat anyMessageFormat = WebMessageFormat.Xml;

            target.AutomaticFormatSelectionEnabled = anyBool1;
            Assert.AreEqual(anyBool1, target.AutomaticFormatSelectionEnabled);

            target.DefaultBodyStyle = anyBodyStyle;
            Assert.AreEqual(anyBodyStyle, target.DefaultBodyStyle);

            target.DefaultOutgoingResponseFormat = anyMessageFormat;
            Assert.AreEqual(anyMessageFormat, target.DefaultOutgoingResponseFormat);

            target.FaultExceptionEnabled = anyBool2;
            Assert.AreEqual(anyBool2, target.FaultExceptionEnabled);

            target.HelpEnabled = anyBool3;
            Assert.AreEqual(anyBool3, target.HelpEnabled);
        }

        [TestMethod]
        public void InvalidConfigTest()
        {
            string configXml = CreateConfig(null, WebMessageBodyStyle.Bare, null, null, null).Replace("'Bare'", "'notAStyle'");
            ExceptionTestHelper.ExpectException<ConfigurationException>(delegate { var b = TestjQueryConfigurationSection.GetWebHttpBehavior3Section(configXml).WebHttpElement3.DefaultBodyStyle; });

            configXml = CreateConfig(null, null, WebMessageFormat.Xml, null, null).Replace("'Xml'", "'notAFormat'");
            ExceptionTestHelper.ExpectException<ConfigurationException>(delegate { var f = TestjQueryConfigurationSection.GetWebHttpBehavior3Section(configXml).WebHttpElement3.DefaultOutgoingResponseFormat; });
        }

        static string CreateConfig(bool? automaticFormatSelectionEnabled, WebMessageBodyStyle? defaultBodyStyle, WebMessageFormat? defaultOutgoingResponseFormat, bool? faultExceptionEnabled, bool? helpEnabled)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
  <configSections>
    <section name='jQueryConfigurationSection' type='Microsoft.ServiceModel.Web.UnitTests.TestjQueryConfigurationSection, Microsoft.ServiceModel.Web.jQuery.UnitTests' />
  </configSections>
  <jQueryConfigurationSection>
    <webHttp3 ");

            if (automaticFormatSelectionEnabled.HasValue)
            {
                sb.AppendFormat("automaticFormatSelectionEnabled='{0}' ", automaticFormatSelectionEnabled.Value);
            }

            if (defaultBodyStyle.HasValue)
            {
                sb.AppendFormat("defaultBodyStyle='{0}' ", defaultBodyStyle.Value);
            }

            if (defaultOutgoingResponseFormat.HasValue)
            {
                sb.AppendFormat("defaultOutgoingResponseFormat='{0}' ", defaultOutgoingResponseFormat.Value);
            }

            if (faultExceptionEnabled.HasValue)
            {
                sb.AppendFormat("faultExceptionEnabled='{0}' ", faultExceptionEnabled.Value);
            }

            if (helpEnabled.HasValue)
            {
                sb.AppendFormat("helpEnabled='{0}' ", helpEnabled.Value);
            }

            sb.Append(@"/>
  </jQueryConfigurationSection>
</configuration>");

            return sb.ToString();
        }
    }
}
