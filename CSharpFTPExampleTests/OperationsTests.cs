using System;
using System.Threading;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CSharpFTPExample;
using System.Net;
using WinSCP;
using Moq;
using Moq.Protected;

namespace CSharpFTPExampleTests
{
    [TestClass]
    public class OperationsTests
    {
        private Mock<Operations> mockOperations;
        private Operations operations;
        private Mock<FtpOperations> mockFtpOperations;
        private Mock<IWebClient> mockWebClient;
        private IWebClient client;

        private string username = "TestKey";
        private string password = "e261742d-fe2f-4569-95e6-312689d04903";
        private string host = "bacon";
        private int port = 9871;
        private int pollEvery = 1;
        private string protocol = "http";
        private string notify = "test@test.com";

        [TestInitialize]
        public void Setup()
        {
            mockOperations = new Mock<Operations>(username, password, port, host, pollEvery, protocol, notify);
            mockOperations.CallBase = true;
            operations = mockOperations.Object;

            mockWebClient = new Mock<IWebClient>();
            client = mockWebClient.Object;

            mockFtpOperations = new Mock<FtpOperations>();

            operations.Init();

            operations.ftpOperations = mockFtpOperations.Object;
        }

        [TestMethod]
        public void Instantiation_AllVariables_SetsValues()
        {
            Assert.AreEqual(operations.pollEvery, pollEvery);
            Assert.AreEqual(operations.protocol, protocol);
            Assert.AreEqual(operations.notify, notify);

            var details = operations.GetConnectionDetails();
            Assert.AreEqual(details["username"], username);
            Assert.AreEqual(details["password"], password);
            Assert.AreEqual(details["host"], host);
            Assert.AreEqual(details["port"], port);
        }

        [TestMethod]
        public void Instantiation_DefaultValues_SetsValues()
        {
            Operations operations = new Operations(username, password, port);

            Assert.AreEqual(operations.pollEvery, 300);
            Assert.AreEqual(operations.protocol, "http");
            Assert.AreEqual(operations.notify, null);

            var details = operations.GetConnectionDetails();
            Assert.AreEqual(details["host"], "localhost");
        }

        // Init

        [TestMethod]
        public void Init_Default_CallsFtp()
        {
            mockFtpOperations.Setup(m => m.Init(It.IsAny<ISession>(), username, password, host, port))
                             .Returns(new Tuple<bool, string>(true, "A Message."));

            Assert.AreEqual(operations.Init(), new Tuple<bool, string>(true, "A Message."));
        }

        // Upload

        [TestMethod]
        public void Upload_Default_CallsFtp()
        {
            mockFtpOperations.Setup(m => m.Upload("test.csv", false))
                             .Returns(new Tuple<bool, string>(true, "A Message."));

            Assert.AreEqual(operations.Upload("test.csv", false), new Tuple<bool, string>(true, "A Message."));
        }

        // Download

        [TestMethod]
        public void Download_Default_CallsFtp()
        {
            mockFtpOperations.Setup(m => m.Download("C:\\TEMP", pollEvery, true, It.IsAny<Action<bool, string>>()));

            operations.Download("C:\\TEMP", true, delegate(bool noError, string message){});

            mockFtpOperations.VerifyAll();
        }

        // Remove

        [TestMethod]
        public void Remove_Default_CallsFtp()
        {
            mockFtpOperations.Setup(m => m.Remove())
                             .Returns(new Tuple<bool, string>(true, "A Message."));

            Assert.AreEqual(operations.Remove(), new Tuple<bool, string>(true, "A Message."));
        }
    }
}

