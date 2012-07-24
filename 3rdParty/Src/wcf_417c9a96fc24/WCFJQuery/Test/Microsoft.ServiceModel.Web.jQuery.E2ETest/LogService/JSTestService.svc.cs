namespace Microsoft.ServiceModel.Web.E2ETest.LogService
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.ServiceModel.Web;
    using System.Text;

    public class JSTestService : IJSTestService
    {
        static Dictionary<string, TestResult> testResults = new Dictionary<string, TestResult>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "00000000-0000-0000-0000-000000000000", new TestResult("00000000-0000-0000-0000-000000000000", "Test") },
        };

        public Stream StartTest(string testName)
        {
            Guid guid = Guid.NewGuid();
            string testId = guid.ToString("D", CultureInfo.InvariantCulture);
            testResults.Add(testId, new TestResult(testId, testName));
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
            return new MemoryStream(Encoding.UTF8.GetBytes(testId));
        }

        public void Log(string testId, string value)
        {
            TestResult testResult;
            if (testResults.TryGetValue(testId, out testResult))
            {
                testResult.LogLines.Add(SanitizeInput(value.Replace("\r\n", "\n")));
            }
            else
            {
                WebOperationContext.Current.OutgoingResponse.Headers["X-Test"] = "test id not found; running on VS only?";
            }
        }

        public void SetTestResult(string testId, int passed, int failed)
        {
            TestResult testResult;
            if (testResults.TryGetValue(testId, out testResult))
            {
                testResult.Passed = passed;
                testResult.Failed = failed;
            }
            else
            {
                WebOperationContext.Current.OutgoingResponse.Headers["X-Test"] = "test id not found; running on VS only?";
            }
        }

        public Stream AbortTest(string testId)
        {
            TestResult testResult = testResults[testId];
            testResult.Passed = 0;
            testResult.Failed = 1;
            testResult.LogLines.Add(" ***** TEST ABORTED!!! ***** ");
            Stream result = GetTextFromTestResult(testResult);
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
            testResults.Remove(testId); // not needed anymore
            return result;
        }

        public Stream GetTestResult(string testId)
        {
            TestResult testResult = testResults[testId];
            Stream result = GetTextFromTestResult(testResult);
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
            testResults.Remove(testId); // not needed anymore
            return result;
        }

        public Stream IsTestFinished(string testId)
        {
            TestResult testResult = testResults[testId];
            bool testFinished = testResult.Passed > 0 || testResult.Failed > 0;
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
            return new MemoryStream(Encoding.UTF8.GetBytes(testFinished.ToString().ToLowerInvariant()));
        }

        static string SanitizeInput(string logInput)
        {
            StringBuilder sb = new StringBuilder();
            if (logInput == null)
            {
                sb.Append("<<null>>");
            }
            else
            {
                for (int i = 0; i < logInput.Length; i++)
                {
                    char c = logInput[i];
                    if (c != '\t' && c != '\n' && c != '\r' && (c < ' ' || c > '~'))
                    {
                        sb.AppendFormat("\\u{0:X4}", (int)c);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            return sb.ToString();
        }

        static Stream GetTextFromTestResult(TestResult testResult)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Test {0} ({1})\n", testResult.TestId, testResult.TestName);
            sb.Append("==============\n");
            foreach (string logLine in testResult.LogLines)
            {
                sb.AppendFormat("{0}\n", logLine);
            }

            sb.Append("==============\n");
            sb.AppendFormat("Failures: {0}\n", testResult.Failed);
            sb.AppendFormat("Pass:     {0}\n", testResult.Passed);
            sb.AppendFormat("Total:    {0}\n", testResult.Failed + testResult.Passed);
            return new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        class TestResult
        {
            public TestResult(string testId, string testName)
            {
                this.TestId = testId;
                this.TestName = testName;
                this.LogLines = new List<string>();
                this.Failed = 0;
                this.Passed = 0;
            }

            public string TestId { get; private set; }

            public string TestName { get; private set; }
            
            public List<string> LogLines { get; private set; }
            
            public int Passed { get; set; }
            
            public int Failed { get; set; }
        }
    }
}
