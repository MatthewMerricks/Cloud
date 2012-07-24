namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Json;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class JsonTypeTest
    {
        [TestMethod]
        public void JsonTypeValues()
        {
            string[] allJsonTypeExpectedValues = new string[] { "Array", "Boolean", "Default", "Number", "Object", "String" };
            JsonType[] allJsonTypeActualValues = (JsonType[])Enum.GetValues(typeof(JsonType));

            Assert.AreEqual(allJsonTypeExpectedValues.Length, allJsonTypeActualValues.Length);

            List<string> allJsonTypeActualStringValues = new List<string>(allJsonTypeActualValues.Select((x) => x.ToString()));
            allJsonTypeActualStringValues.Sort(StringComparer.Ordinal);

            for (int i = 0; i < allJsonTypeExpectedValues.Length; i++)
            {
                Assert.AreEqual(allJsonTypeExpectedValues[i], allJsonTypeActualStringValues[i]);
            }
        }
    }
}
