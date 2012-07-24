namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System;
    using System.ServiceModel.Description;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebServiceHost3Test
    {
        [TestMethod]
        public void WebServiceHost3OpenTest()
        {
            Type serviceType = typeof(TestService);
            Uri baseAddress = new Uri(TestService.BaseAddress);
            using (WebServiceHost3 target = new WebServiceHost3(serviceType, baseAddress))
            {
                target.Open();

                Assert.AreEqual(1, target.Description.Endpoints.Count);
                foreach (ServiceEndpoint endpoint in target.Description.Endpoints)
                {
                    Assert.AreEqual(typeof(ITestService).FullName, endpoint.Contract.ContractType.FullName);
                    Assert.IsNotNull(endpoint.Behaviors.Find<WebHttpBehavior3>());
                }
            }
        }
    }
}
