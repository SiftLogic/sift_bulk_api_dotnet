using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CSharpFTPExample;
using Moq;
using Moq.Protected;

namespace CSharpFTPExampleTests
{
    [TestClass]
    public class HttpOperationsTests
    {
        private Mock<HttpOperations> mockOperations;
        private HttpOperations httpOperations;

        private string apikey = "12345";

        [TestInitialize]
        public void Setup()
        {
            mockOperations = new Mock<HttpOperations>();
            mockOperations.CallBase = true;
            httpOperations = mockOperations.Object;
        }

        // Init

        [TestMethod]
        public void Init_Default_SetsApiKeyAndBaseUrl()
        {
            httpOperations.Init(apikey, "bacon", 82);

            Assert.AreEqual(httpOperations.apikey, apikey);
            Assert.AreEqual(httpOperations.baseUrl, "http://bacon:82/api/live/bulk/");
        }

        [TestMethod]
        public void Init_NoPort_SetsApiKeyAndBaseUrl()
        {
            httpOperations.Init(apikey, "bacon");

            Assert.AreEqual(httpOperations.baseUrl, "http://bacon:80/api/live/bulk/");
        }
    }
}
