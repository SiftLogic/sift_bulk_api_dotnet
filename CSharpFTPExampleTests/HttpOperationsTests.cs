using System;
using System.IO;
using System.Threading;
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
        private int pollEvery = 1;

        private AutoResetEvent resetEvent;

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

        // Download

        [TestMethod]
        public void Download_Default_AnErrorReturnsFalse()
        {
            httpOperations.statusUrl = "http://bacon:80/status";

            mockOperations.Setup(m => m.GetRawResponse(It.IsAny<HttpResponse>()))
                          .Throws(new Exception("An Error"));

            // Setup waiting
            this.resetEvent = new AutoResetEvent(false);

            httpOperations.Download("test.csv", pollEvery, false, delegate(bool noError, string message)
            {
                Assert.IsFalse(noError);
                Assert.AreEqual(message, "An Error");

                // Stop waiting
                this.resetEvent.Set();
            });

            var calls = httpOperations.http.LastCalls;
            Assert.AreEqual(calls[0][0], "http://bacon:80/status");

            // Do not pass this statement until the waiting is done
            Assert.IsTrue(this.resetEvent.WaitOne());
            mockOperations.VerifyAll();
        }

        [TestMethod]
        public void Download_Default_FileDoesNotExistReturnsFalse()
        {
            mockOperations.Setup(m => m.GetRawResponse(It.IsAny<HttpResponse>()))
                          .Returns("{\"status\": \"error\", \"msg\": \"An Error\"}");

            // Setup waiting
            this.resetEvent = new AutoResetEvent(false);

            httpOperations.Download("test.csv", pollEvery, false, delegate(bool noError, string message)
            {
                Assert.IsFalse(noError);
                Assert.AreEqual(message, "An Error");

                // Stop waiting
                this.resetEvent.Set();
            });

            // Do not pass this statement until the waiting is done
            Assert.IsTrue(this.resetEvent.WaitOne());
            mockOperations.VerifyAll();
        }

        [TestMethod]
        public void Download_Default_FileNotFound()
        {
            mockOperations.Setup(m => m.GetRawResponse(It.IsAny<HttpResponse>()))
                          .Returns("{\"status\": \"active\", \"job\": \"a_job\"}");

            mockOperations.Setup(m => m.WaitAndDownload("a_job", It.IsAny<System.Timers.Timer>(), It.IsAny<Action>()))
                            .Callback((string name, System.Timers.Timer timer, Action callback) =>
                            {
                                mockOperations.Setup(m =>
                                    m.Download("test.csv", pollEvery, true, It.IsAny<Action<bool, string>>()));

                                Assert.AreEqual(name, "a_job");
                                Assert.AreEqual(timer.Interval, pollEvery * 1000);

                                callback();

                                mockOperations.Verify(m =>
                                    m.Download("test.csv", pollEvery, true, It.IsAny<Action<bool, string>>()));
                            });

            httpOperations.Download("test.csv", pollEvery, true, delegate(bool noError, string message) { });

            mockOperations.VerifyAll();
        }

        [TestMethod]
        public void Download_Default_FileFoundError()
        {
            var returns = new Queue<string>();
            returns.Enqueue("{\"status\": \"completed\", \"download_url\": \"a_url\", \"job\": \"a_job\"}");
            returns.Enqueue("{\"status\": \"error\", \"msg\": \"An Error\"}");

            mockOperations.Setup(m => m.GetRawResponse(It.IsAny<HttpResponse>()))
                          .Returns(returns.Dequeue);

            // Setup waiting
            this.resetEvent = new AutoResetEvent(false);

            httpOperations.Download("\test", pollEvery, false, delegate(bool noError, string message)
            {
                Assert.IsFalse(noError);
                Assert.AreEqual(message, "An Error");

                // Stop waiting
                this.resetEvent.Set();
            });

            // Do not pass this statement until the waiting is done
            Assert.IsTrue(this.resetEvent.WaitOne());

            mockOperations.VerifyAll();
        }

        [TestMethod]
        public void Download_Default_FileFoundAndDownloads()
        {
            var returns = new Queue<string>();
            returns.Enqueue("{\"status\": \"completed\", \"download_url\": \"a_url\", \"job\": \"a_job\"}");
            returns.Enqueue("{\"status\": \"other\"}");

            mockOperations.Setup(m => m.GetRawResponse(It.IsAny<HttpResponse>()))
                          .Returns(returns.Dequeue);

            // Setup waiting
            this.resetEvent = new AutoResetEvent(false);

            httpOperations.Download("\test", pollEvery, false, delegate(bool noError, string message)
            {
                Assert.IsTrue(noError);
                Assert.AreEqual(message, "a_job.zip downloaded to \test");

                // Stop waiting
                this.resetEvent.Set();
            });

            // Do not pass this statement until the waiting is done
            Assert.IsTrue(this.resetEvent.WaitOne());

            mockOperations.VerifyAll();
        }

        // WaitsForDownload

        [TestMethod]
        public void WaitsForDownload_Default_PrintsSleepsAndCreatesTimer()
        {
            var time = 2;
            Mock<System.Timers.Timer> mockTimer = new Mock<System.Timers.Timer>(time * 1000);

            using (StringWriter sw = new StringWriter())
            {
                Console.SetOut(sw);

                var callCount = 0;
                mockOperations.CallBase = true;
                mockOperations.Object.WaitAndDownload("test.csv", mockTimer.Object, delegate()
                {
                    callCount += 1;
                });

                Assert.AreEqual("Waiting for results file test.csv", sw.ToString().Trim());
            }

            // Unfortunately, short of defining a new timer interface for the main code base, this is the most I can test.
            mockTimer.Object.Stop();
            Assert.AreEqual(mockTimer.Object.AutoReset, false);
            mockTimer.VerifyAll();

            // Restore the Console
            StreamWriter standardOut = new StreamWriter(Console.OpenStandardOutput());
            standardOut.AutoFlush = true;
            Console.SetOut(standardOut);
        }
    }
}
