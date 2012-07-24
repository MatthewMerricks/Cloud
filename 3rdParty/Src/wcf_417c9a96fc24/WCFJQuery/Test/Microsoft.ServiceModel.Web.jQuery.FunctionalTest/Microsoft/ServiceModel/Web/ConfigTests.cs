namespace Microsoft.ServiceModel.Web.Test
{
    using System;
    using System.Configuration;
    using System.Json;
    using System.Reflection;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using System.ServiceModel.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [ServiceContract]
    public interface IJQueryWCF2
    {
        [WebInvoke]
        JsonValue Echo(JsonValue input);
    }

    public class JQueryWCF2 : IJQueryWCF2
    {
        public JsonValue Echo(JsonValue input)
        {
            return input;
        }
    }

    [TestClass]
    public class ConfigTests
    {
        [TestMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Variable host does not expose a Dispose method.")]
        public void ApplyConfigTest()
        {
            string configFile = Assembly.GetExecutingAssembly().Location + ".config";
            Console.WriteLine("Config File location: {0}", configFile);

            // Setting APP_CONFIG_FILE value on the AppDomain forcibly assign a config file when none was assigned before.
            // This can only be done once in the lifetime of the AppDomain.
            AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", configFile);
            ConfigurationManager.RefreshSection("configuration");

            using (ServiceHost host = new ServiceHost(typeof(JQueryWCF2), new Uri[] { new Uri("http://localhost:55552") }))
            {
                foreach (ServiceEndpoint endpoint in host.Description.Endpoints)
                {
                    Console.WriteLine("Verifying endpoint '{0}'.", endpoint.Name);

                    WebHttpBehavior3 webHttpBehavior3 = null;
                    foreach (IEndpointBehavior behavior in endpoint.Behaviors)
                    {
                        if (behavior is WebHttpBehavior3)
                        {
                            Console.WriteLine("Found WebHttpBehavior3 on the endpoint.");
                            webHttpBehavior3 = (WebHttpBehavior3)endpoint.Behaviors[0];
                            break;
                        }
                    }

                    if (webHttpBehavior3 != null)
                    {
                        WebHttp3Values expectedValues = null;
                        switch (endpoint.Name)
                        {
                            case "implicitDefaults":
                            case "explicitDefaults":
                                expectedValues = new WebHttp3Values(true, WebMessageBodyStyle.Bare, WebMessageFormat.Xml, false, true);
                                break;
                            case "noDefaults":
                                expectedValues = new WebHttp3Values(false, WebMessageBodyStyle.WrappedResponse, WebMessageFormat.Json, true, false);
                                break;
                            default:
                                Assert.Fail("Endpoint '{0}' could not be verified as it was not expected in the test.", endpoint.Name);
                                break;
                        }

                        Assert.AreEqual(webHttpBehavior3.AutomaticFormatSelectionEnabled, expectedValues.AutomaticFormatSelectionEnabledValue, "AutomaticFormatSelectionEnabled is not of the expected value.");
                        Assert.AreEqual(webHttpBehavior3.DefaultBodyStyle, expectedValues.DefaultBodyStyleValue, "DefaultBodyStyle is not equal.");
                        Assert.AreEqual(webHttpBehavior3.DefaultOutgoingResponseFormat, expectedValues.DefaultOutgoingResponseFormatValue, "DefaultOutgoingResponseFormat is not of the expected value.");
                        Assert.AreEqual(webHttpBehavior3.FaultExceptionEnabled, expectedValues.FaultExceptionEnabledValue, "FaultExceptionEnabled is not of the expected value.");
                        Assert.AreEqual(webHttpBehavior3.HelpEnabled, expectedValues.HelpEnabledValue, "HelpEnabled is not of the expected value.");
                    }
                    else
                    {
                        Assert.Fail("No WebHttpBehavior3 was found on the endpoint.");
                    }
                }
            }
        }

        class WebHttp3Values
        {
            public WebHttp3Values(bool automaticFormatSelectionEnabledValue, WebMessageBodyStyle defaultBodyStyleValue, WebMessageFormat defaultOutgoingResponseFormatValue, bool faultExceptionEnabledValue, bool helpEnabledValue)
            {
                this.AutomaticFormatSelectionEnabledValue = automaticFormatSelectionEnabledValue;
                this.DefaultBodyStyleValue = defaultBodyStyleValue;
                this.DefaultOutgoingResponseFormatValue = defaultOutgoingResponseFormatValue;
                this.FaultExceptionEnabledValue = faultExceptionEnabledValue;
                this.HelpEnabledValue = helpEnabledValue;
            }

            internal bool AutomaticFormatSelectionEnabledValue { get; private set; }

            internal WebMessageBodyStyle DefaultBodyStyleValue { get; private set; }

            internal WebMessageFormat DefaultOutgoingResponseFormatValue { get; private set; }

            internal bool FaultExceptionEnabledValue { get; private set; }

            internal bool HelpEnabledValue { get; private set; }
        }
    }
}
