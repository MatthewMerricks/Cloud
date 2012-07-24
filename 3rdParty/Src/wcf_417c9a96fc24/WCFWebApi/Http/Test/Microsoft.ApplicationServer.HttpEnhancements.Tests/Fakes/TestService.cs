using System.ServiceModel;
using System.ServiceModel.Web;

namespace Microsoft.ApplicationServer.HttpEnhancements.Tests
{
    [ServiceContract]
    public class TestService
    {
        [WebGet()]
        public void Operation()
        {
            
        }
    }
}