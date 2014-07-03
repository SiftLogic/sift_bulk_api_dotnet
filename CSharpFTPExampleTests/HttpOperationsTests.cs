using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CSharpFTPExample;
using EasyHttp.Http;
using EasyHttp.Infrastructure;
using Moq;
using Moq.Protected;

namespace CSharpFTPExampleTests
{
    [TestClass]
    public class HttpOperationsTests
    {
        private Mock<HttpOperations> mockOperations;
        private HttpOperations httpOperations;
        private Mock<WrappedHttpClient> mockHttp;

        private string apikey = "12345";

        [TestInitialize]
        public void Setup()
        {
            mockOperations = new Mock<HttpOperations>();
            mockOperations.CallBase = true;
            httpOperations = mockOperations.Object;

            httpOperations.Init(apikey, "bacon");

            var mockHttp = new Mock<WrappedHttpClient>(true);
            mockHttp.CallBase = true;
            httpOperations.http = mockHttp.Object;

            httpOperations.http.MockedResponse = new HttpResponse(null);
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
        public void Init_Default_SetsUpHttp()
        {
            httpOperations.Init(apikey, "bacon", 82);

            Assert.IsTrue(httpOperations.http is IHttpClient);
            Assert.AreEqual(httpOperations.http.Request.RawHeaders["x-authorization"], apikey);
        }

        [TestMethod]
        public void Init_NoPort_SetsApiKeyAndBaseUrl()
        {
            Assert.AreEqual(httpOperations.baseUrl, "http://bacon:80/api/live/bulk/");
        }

        // Upload

        [TestMethod]
        public void Upload_Default_SendsMultiAndNoNotifyAndSetsStatusUrl()
        {
            mockOperations.Setup(m => m.GetRawResponse(It.IsAny<HttpResponse>()))
                          .Returns("{\"status\": \"success\", \"status_url\": \"http://localhost:80/status\"}");

            var result = new Tuple<bool, string>(true, "test.csv was uploaded.");
            Assert.AreEqual(httpOperations.Upload("test.csv"), result);

            var calls = httpOperations.http.LastCalls;
            Assert.AreEqual(calls[0][0], "http://bacon:80/api/live/bulk/");

            IDictionary<string, object> newData = (IDictionary<string, object>)calls[0][1];
            Assert.AreEqual(newData["export_type"], "multi");
            Assert.AreEqual(newData["notify_email"], null);

            IList<FileData> newFiles = (IList<FileData>)calls[0][2];
            Assert.AreEqual(newFiles[0].FieldName, "file");
            Assert.AreEqual(newFiles[0].ContentType, "text/csv");
            Assert.AreEqual(newFiles[0].Filename, "test.csv");

            Assert.AreEqual(httpOperations.statusUrl, "http://localhost:80/status");
        }

        [TestMethod]
        public void Upload_SingleFile_SendsSingleAndNoNotify()
        {
            mockOperations.Setup(m => m.GetRawResponse(It.IsAny<HttpResponse>()))
                          .Returns("{\"status\": \"error\", \"msg\": \"An Error\"}");

            var result = new Tuple<bool, string>(false, "An Error");
            Assert.AreEqual(httpOperations.Upload("test.csv", true), result);

            var calls = httpOperations.http.LastCalls;
            IDictionary<string, object> newData = (IDictionary<string, object>) calls[0][1];
            Assert.AreEqual(newData["export_type"], "single");
            Assert.AreEqual(newData["notify_email"], null);
        }

        [TestMethod]
        public void Upload_SingleFileAndNotify_SendsSingleAndNotify()
        {
            mockOperations.Setup(m => m.GetRawResponse(It.IsAny<HttpResponse>()))
              .Returns("{\"status\": \"error\", \"msg\": \"An Error\"}");

            var result = new Tuple<bool, string>(false, "An Error");
            Assert.AreEqual(httpOperations.Upload("test.csv", true, "test@test.com"), result);

            var calls = httpOperations.http.LastCalls;
            IDictionary<string, object> newData = (IDictionary<string, object>)calls[0][1];
            Assert.AreEqual(newData["export_type"], "single");
            Assert.AreEqual(newData["notify_email"], "test@test.com");
        }

        [TestMethod]
        public void Upload_SingleFileAndNotify_ThrowsError()
        {
            mockOperations.Setup(m => m.GetRawResponse(It.IsAny<HttpResponse>()))
                          .Throws(new Exception("An Error"));

            var result = new Tuple<bool, string>(false, "An Error");
            Assert.AreEqual(httpOperations.Upload("test.csv", true, "test@test.com"), result);
        }
    }
}
