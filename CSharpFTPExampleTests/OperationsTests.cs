using System;
using System.Threading;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CSharpFTPExample;
using System.Net;
using Moq;
using Moq.Protected;

namespace CSharpFTPExampleTests
{
    [TestClass]
    public class OperationsTests
    {
        private Mock<Operations> mockOperations;
        private Operations operations;
        private Mock<IWebClient> mockWebClient;
        private IWebClient client;

        private string username = "TestKey";
        private string password = "e261742d-fe2f-4569-95e6-312689d04903";
        private string host = "bacon";
        private int port = 9871;
        private int pollEvery = 1;

        private string file = @"C:\WINDOWS\Temp\test.csv";
        private string directory;

        private AutoResetEvent resetEvent;

        [TestInitialize]
        public void Setup()
        {
            mockOperations = new Mock<Operations>(username, password, host, port, pollEvery);
            operations = mockOperations.Object;

            mockWebClient = new Mock<IWebClient>();
            client = mockWebClient.Object;

            directory = "ftp://" + host + ':' + port + "/complete";
        }

        [TestMethod]
        public void Instantiation_AllVariables_SetsValues()
        {
            Assert.AreEqual(operations.pollEvery, pollEvery);

            var details = operations.GetConnectionDetails();
            Assert.AreEqual(details["username"], username);
            Assert.AreEqual(details["password"], password);
            Assert.AreEqual(details["host"], host);
            Assert.AreEqual(details["port"], port);
        }

        [TestMethod]
        public void Instantiation_DefaultValues_SetsValues()
        {
            Operations operations = new Operations(username, password);

            Assert.AreEqual(operations.pollEvery, 300);

            var details = operations.GetConnectionDetails();
            Assert.AreEqual(details["host"], "localhost");
            Assert.AreEqual(details["port"], 21);
        }

        // Init

        [TestMethod]
        public void Init_Default_SetsFTPAndCredentializes()
        {
            Assert.IsTrue(operations.Init());

            var credentials = operations.ftp.Credentials.GetCredential(null, "");
            Assert.AreEqual(credentials.Password, password);
            Assert.AreEqual(credentials.UserName, username);

            Assert.IsTrue(operations.ftp is WebClient);
        }

        // Upload

        [TestMethod]
        public void Upload_BadStatusCode_SplitFileNameIsUploaded()
        {
            var ftpMessage = "500 The command was not accepted.";
            mockWebClient.Setup(m => m.UploadFile(null, null));
            mockOperations.Setup(m => m.GetStatusDescription(client)).Returns(new Tuple<int, string>(500, ftpMessage));

            operations.Init();
            operations.ftp = client;

            Assert.AreEqual(operations.Upload(file), new Tuple<bool, string>(false, "Failed to extract filename from: " + ftpMessage));
            Assert.AreEqual(operations.uploadFileName, null);
        }

        [TestMethod]
        public void Upload_SplitFile_SplitFileNameIsUploaded()
        {
            var ftpMessage = "226 closing data connection; File upload success; source.csv";
            mockWebClient.Setup(m => m.UploadFile("ftp://bacon:9871/import_TestKey_splitfile_config/test.csv", file));
            mockOperations.Setup(m => m.GetStatusDescription(client)).Returns(new Tuple<int, string>(226, ftpMessage));

            operations.Init();
            operations.ftp = client;

            Assert.AreEqual(operations.Upload(file), new Tuple<bool, string>(true, "test.csv has been uploaded as source.csv"));

            mockWebClient.VerifyAll();
            mockOperations.VerifyAll();
            Assert.AreEqual(operations.uploadFileName, "source.csv");
        }

        [TestMethod]
        public void Upload_SingleFile_SingleFileNameIsUploaded()
        {
            var ftpMessage = "226 closing data connection; File upload success; source.csv";
            mockWebClient.Setup(m => m.UploadFile("ftp://bacon:9871/import_TestKey_default_config/test.csv", file));
            mockOperations.Setup(m => m.GetStatusDescription(client)).Returns(new Tuple<int, string>(226, ftpMessage));

            operations.Init();
            operations.ftp = client;

            Assert.AreEqual(operations.Upload(file, true), new Tuple<bool, string>(true, "test.csv has been uploaded as source.csv"));

            mockWebClient.VerifyAll();
            mockOperations.VerifyAll();
            Assert.AreEqual(operations.uploadFileName, "source.csv");
        }

        // GetDownloadFileName

        [TestMethod]
        public void GetDownloadFileName_NoModify_ReturnsSentIn()
        {
            var operations = new Operations(username, password);

            Assert.AreEqual(operations.GetDownloadFileName(), operations.uploadFileName);

            operations.uploadFileName = "";
            Assert.AreEqual(operations.GetDownloadFileName(), "");

            operations.uploadFileName = "test_test.doc";
            Assert.AreEqual(operations.GetDownloadFileName(), "test_test.doc");
        }

        [TestMethod]
        public void GetDownloadFileName_Modify_ReturnsModified()
        {
            var operations = new Operations(username, password);

            operations.uploadFileName = "source_test.doc";
            Assert.AreEqual(operations.GetDownloadFileName(), "archive_test.doc");

            operations.uploadFileName = "source_source_test.csv.csv";
            Assert.AreEqual(operations.GetDownloadFileName(), "archive_source_test.csv.zip");

            operations.uploadFileName = "source_source_test.txt.txt";
            Assert.AreEqual(operations.GetDownloadFileName(), "archive_source_test.txt.zip");

            operations.uploadFileName = "source_source_test.csv.txt";
            Assert.AreEqual(operations.GetDownloadFileName(), "archive_source_test.csv.zip");
        }

        // Download

        [TestMethod]
        public void Download_ListingErrors_DownloadListError()
        {
            mockOperations.Setup(m => m.GetDownloadFileName()).Returns("test.csv");
            mockOperations.Setup(m => m.GetDirectoryListing(directory)).Returns(new Tuple<bool, string>(false, "error"));
            mockOperations.CallBase = true;
 
            // Setup waiting
            this.resetEvent = new AutoResetEvent(false);

            operations.Download("test.csv", delegate(bool noError, string message)
            {
                Assert.IsFalse(noError);
                Assert.AreEqual(message, "error");

                // Stop waiting
                this.resetEvent.Set();
            });

            // Do not pass this statement until the waiting is done
            Assert.IsTrue(this.resetEvent.WaitOne());
            mockOperations.VerifyAll();
        }

        [TestMethod]
        public void Download_ListingDoesNotContainFile_CallsWaitForDownload()
        {
            mockOperations.Setup(m => m.GetDownloadFileName()).Returns("test.csv");
            mockOperations.Setup(m => m.GetDirectoryListing(directory)).Returns(new Tuple<bool, string>(true, "not here"));
            mockOperations.Setup(m => m.WaitAndDownload("test.csv", It.IsAny<System.Timers.Timer>(), It.IsAny<Action>()))
                          .Callback((string name, System.Timers.Timer timer, Action callback) =>
                          {
                              mockOperations.Setup(m => m.Download("test.csv", It.IsAny<Action<bool, string>>()));

                              Assert.AreEqual(name, "test.csv");
                              Assert.AreEqual(timer.Interval, pollEvery * 1000);

                              callback();

                              mockOperations.Verify(m => m.Download("test.csv", It.IsAny<Action<bool, string>>()));
                          });
            mockOperations.CallBase = true;

            operations.Download("test.csv", delegate(bool noError, string message) { });

            mockOperations.VerifyAll();
        }

        [TestMethod]
        public void Download_ListingDoesContainFile_DownloadsFile()
        {
            var location = @"C:\WINDOWS\";
            var fileName = "test.csv";
            var listing = @"
                 -rw-r---- 1000 test1.csv\n
                 -rw-r---- 1000 test.csv\n
                 -rw-r---- 1000 test2.csv\n
                 -rw-r---- 1000 test3.csv\n
            ";

            mockOperations.Setup(m => m.GetDownloadFileName()).Returns(fileName);
            mockOperations.Setup(m => m.GetDirectoryListing(directory)).Returns(new Tuple<bool, string>(true, listing));
            mockOperations.CallBase = true;

            this.resetEvent = new AutoResetEvent(false);

            mockWebClient.Setup(m => m.DownloadFile(directory + "/" + fileName, location + "/" + fileName));
            operations.Init();
            operations.ftp = client;

            operations.Download(@"C:\WINDOWS\", delegate(bool noError, string message)
            {
                Assert.IsTrue(noError);
                Assert.AreEqual(message, fileName + @" downloaded to C:\WINDOWS\");

                this.resetEvent.Set();
            });

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
                operations.WaitAndDownload("test.csv", mockTimer.Object, delegate()
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

