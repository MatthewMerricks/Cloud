namespace Microsoft.ServiceModel.Web.E2ETest.LogService
{
    using System.IO;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract]
    public interface IJSTestService
    {
        [OperationContract, WebGet(UriTemplate = "/StartTest?name={testName}")]
        Stream StartTest(string testName);
        [OperationContract, WebInvoke(UriTemplate = "/Log/{testId}")]
        void Log(string testId, string value);
        [OperationContract, WebInvoke(UriTemplate = "/TestResult/{testId}", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        void SetTestResult(string testId, int passed, int failed);
        [OperationContract, WebInvoke(UriTemplate = "/AbortTest/{testId}")]
        Stream AbortTest(string testId);
        [OperationContract, WebGet(UriTemplate = "/GetTestResult?testId={testId}")]
        Stream GetTestResult(string testId);
        [OperationContract, WebGet(UriTemplate = "/IsTestFinished?testId={testId}")]
        Stream IsTestFinished(string testId);
    }
}