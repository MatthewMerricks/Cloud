namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Json;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    [TestClass]
    public class JsonValueLinqExtensionsTest
    {
        [TestMethod]
        public void ToJsonArrayTest()
        {
            var target = (new List<int>(new[] {1, 2, 3}).Select(i => (JsonValue)i).ToJsonArray());
            Assert.AreEqual("[1,2,3]", target.ToString());
        }

        [TestMethod]
        public void ToJsonObjectTest()
        {
            JsonValue jv = new JsonObject { { "one", 1 }, { "two", 2 }, {"three", 3} };

            var result = from n in jv
                         where n.Value.ReadAs<int>() > 1
                         select n;
            Assert.AreEqual("{\"two\":2,\"three\":3}", result.ToJsonObject().ToString());
        }

        [TestMethod]
        public void ToJsonObjectFromArray()
        { 
            JsonArray ja = new JsonArray("first", "second");
            JsonObject jo = ja.ToJsonObject();
            Assert.AreEqual("{\"0\":\"first\",\"1\":\"second\"}", jo.ToString());
        }
    }
}
