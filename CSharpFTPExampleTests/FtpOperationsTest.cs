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
    public class FtpOperationsTest
    {
        private Mock<FtpOperations> mockOperations;
        private FtpOperations operations;
        private Mock<IWebClient> mockWebClient;
        private IWebClient client;
        private Mock<ISession> mockSession;
        private ISession session;

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
            mockOperations = new Mock<FtpOperations>();
            mockOperations.CallBase = true;
            operations = mockOperations.Object;

            mockWebClient = new Mock<IWebClient>();
            client = mockWebClient.Object;

            mockSession = new Mock<ISession>();
            session = mockSession.Object;
            operations.Init(session, username, password, host, port);

            directory = "ftp://" + host + ':' + port + "/complete";
        }

        // Init

        [TestMethod]
        public void Init_Default_LogsInAndConnects()
        {
            mockSession.Setup(m => m.Open(It.Is<SessionOptions>(
                o => o.Protocol == Protocol.Ftp &&
                     o.HostName == host &&
                     o.UserName == username &&
                     o.Password == password
            )));

            var result = operations.Init(session, username, password, host, port);
            Assert.AreEqual(result, new Tuple<bool, string>(true, "Initialization succeeded."));
            Assert.AreEqual(operations.ftp, session);
            mockSession.VerifyAll();
        }

        [TestMethod]
        public void Init_Default_SetsUpWebClientNoConnect()
        {
            mockSession.Setup(m => m.Open(It.IsAny<SessionOptions>()));

            operations.Init(session, username, password, host);

            var credentials = operations.ftpOther.Credentials.GetCredential(null, "");
            Assert.AreEqual(credentials.Password, password);
            Assert.AreEqual(credentials.UserName, username);

            mockSession.VerifyAll();
            Assert.IsTrue(operations.ftpOther is WebClient);
        }

        [TestMethod]
        public void Init_Default_SessionCausesError()
        {
            mockSession.Setup(m => m.Open(It.IsAny<SessionOptions>()))
                       .Throws(new IOException("An Error"));

            var result = operations.Init(session, username, password, host);
            Assert.AreEqual(result, new Tuple<bool, string>(false, "An Error"));
            mockSession.VerifyAll();
        }

        // Upload

        [TestMethod]
        public void Upload_BadStatusCode_SplitFileNameIsUploaded()
        {
            var ftpMessage = "500 The command was not accepted.";
            mockWebClient.Setup(m => m.UploadFile(null, null));
            mockOperations.Setup(m => m.GetStatusDescription(client)).Returns(new Tuple<int, string>(500, ftpMessage));

            operations.Init(session, username, password, host, port);
            operations.ftpOther = client;

            Assert.AreEqual(operations.Upload(file), new Tuple<bool, string>(false, "Failed to extract filename from: " + ftpMessage));
            Assert.AreEqual(operations.uploadFileName, null);
        }

        [TestMethod]
        public void Upload_SplitFile_SplitFileNameIsUploaded()
        {
            var ftpMessage = "226 closing data connection; File upload success; source.csv";
            mockWebClient.Setup(m => m.UploadFile("ftp://bacon:9871/import_TestKey_splitfile_config/test.csv", file));
            mockOperations.Setup(m => m.GetStatusDescription(client)).Returns(new Tuple<int, string>(226, ftpMessage));

            operations.Init(session, username, password, host, port);
            operations.ftpOther = client;

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

            operations.Init(session, username, password, host, port);
            operations.ftpOther = client;

            Assert.AreEqual(operations.Upload(file, true), new Tuple<bool, string>(true, "test.csv has been uploaded as source.csv"));

            mockWebClient.VerifyAll();
            mockOperations.VerifyAll();
            Assert.AreEqual(operations.uploadFileName, "source.csv");
        }

        // Download

        [TestMethod]
        public void Download_Default_AnErrorReturnsFalse()
        {
            mockOperations.Setup(m => m.GetDownloadFileName()).Returns("test.csv");
            mockOperations.Setup(m => m.RemoteFileExists("/complete/test.csv")).Throws(new Exception("An Error"));

            // Setup waiting
            this.resetEvent = new AutoResetEvent(false);

            operations.Download("test.csv", pollEvery, false, delegate(bool noError, string message)
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
        public void Download_Default_FileExistsErrorReturnsFalse()
        {
            mockOperations.Setup(m => m.GetDownloadFileName()).Returns("test.csv");
            mockOperations.Setup(m => m.RemoteFileExists("/complete/test.csv")).Returns(new Tuple<bool, string>(false, "An Error"));

            // Setup waiting
            this.resetEvent = new AutoResetEvent(false);

            operations.Download("test.csv", pollEvery, false, delegate(bool noError, string message)
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
            mockOperations.Setup(m => m.GetDownloadFileName()).Returns("test.csv");
            mockOperations.Setup(m => m.RemoteFileExists("/complete/test.csv"))
                          .Returns(new Tuple<bool, string>(false, null));
            mockOperations.Setup(m => m.WaitAndDownload("test.csv", It.IsAny<System.Timers.Timer>(), It.IsAny<Action>()))
                            .Callback((string name, System.Timers.Timer timer, Action callback) =>
                            {
                                mockOperations.Setup(m => 
                                    m.Download("test.csv", pollEvery, true, It.IsAny<Action<bool, string>>()));

                                Assert.AreEqual(name, "test.csv");
                                Assert.AreEqual(timer.Interval, pollEvery * 1000);

                                callback();

                                mockOperations.Verify(m =>
                                    m.Download("test.csv", pollEvery, true, It.IsAny<Action<bool, string>>()));
                            });

            operations.Download("test.csv", pollEvery, true, delegate(bool noError, string message) { });

            mockOperations.VerifyAll();
        }

        [TestMethod]
        public void Download_Default_FileFoundAndDownloads()
        {
            mockOperations.Setup(m => m.GetDownloadFileName()).Returns("test.csv");
            mockOperations.Setup(m => m.RemoteFileExists("/complete/test.csv")).Returns(new Tuple<bool, string>(true, null));
            mockOperations.Setup(m => m.ThrowErrorIfLocalFileNotPresent("\test\\test.csv"));
            mockSession.Setup(m => m.GetFiles("/complete/test.csv", "\test\\test.csv", false));

            // Setup waiting
            this.resetEvent = new AutoResetEvent(false);

            operations.Download("\test", pollEvery, false, delegate(bool noError, string message)
            {
                //Assert.IsTrue(noError);
                Assert.AreEqual(message, "test.csv downloaded to \test");

                // Stop waiting
                this.resetEvent.Set();
            });

            // Do not pass this statement until the waiting is done
            Assert.IsTrue(this.resetEvent.WaitOne());

            mockOperations.VerifyAll();
        }

        // RemoteFileExists

        [TestMethod]
        public void RemoteFileExists_EmptyFile_ReturnsFalse()
        {
            Assert.AreEqual(operations.RemoteFileExists(""), new Tuple<bool, string>(false, null));
        }

        [TestMethod]
        public void RemoteFileExists_InitError_ReturnsFalseAndError()
        {
            mockSession.Setup(m => m.Dispose());
            mockOperations.Setup(m => m.Init(It.IsAny<ISession>(), username, password, host, port))
                          .Returns(new Tuple<bool, string>(false, "An Error"));

            Assert.AreEqual(operations.RemoteFileExists("test/test.csv"), new Tuple<bool, string>(false, "An Error"));

            mockSession.VerifyAll();
            mockOperations.VerifyAll();
        }

        [TestMethod]
        public void RemoteFileExists_FileNotFound_ReturnsFalse()
        {
            mockSession.Setup(m => m.Dispose());
            mockOperations.Setup(m => m.Init(It.IsAny<ISession>(), username, password, host, port))
                          .Returns(new Tuple<bool, string>(true, null));
            mockOperations.Setup(m => m.InDirectoryListing("/test", "test.csv")).Returns(false);
            operations.Init(session, username, password, host, port);

            Assert.AreEqual(operations.RemoteFileExists("/test/test.csv"), new Tuple<bool, string>(false, null));

            mockSession.VerifyAll();
            mockOperations.VerifyAll();
        }

        [TestMethod]
        public void RemoteFileExists_FileFound_ReturnTrue()
        {
            mockSession.Setup(m => m.Dispose());
            mockOperations.Setup(m => m.Init(It.IsAny<ISession>(), username, password, host, port))
                          .Returns(new Tuple<bool, string>(true, null));
            mockOperations.Setup(m => m.InDirectoryListing("/test", "test.csv")).Returns(true);
            operations.Init(session, username, password, host, port);

            Assert.AreEqual(operations.RemoteFileExists("/test/test.csv"), new Tuple<bool, string>(true, null));

            mockSession.VerifyAll();
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

        // Remove

        [TestMethod]
        public void Remove_Default_WithErrorReturnsFalse()
        {
            mockOperations.Setup(m => m.GetDownloadFileName()).Returns("test.zip");
            mockSession.Setup(m => m.RemoveFiles("/complete/test.zip")).Throws(new Exception("An Error"));

            Assert.AreEqual(operations.Remove(), new Tuple<bool, string>(false, "An Error"));
        }

        [TestMethod]
        public void Remove_Default_DeletedReturnsTrue()
        {
            mockOperations.Setup(m => m.GetDownloadFileName()).Returns("test.zip");
            mockSession.Setup(m => m.RemoveFiles("/complete/test.zip"));

            Assert.AreEqual(operations.Remove(), new Tuple<bool, string>(true, null));
        }

        // GetDownloadFileName

        [TestMethod]
        public void GetDownloadFileName_NoModify_ReturnsSentIn()
        {
            var operations = new FtpOperations();

            Assert.AreEqual(operations.GetDownloadFileName(), operations.uploadFileName);

            operations.uploadFileName = "";
            Assert.AreEqual(operations.GetDownloadFileName(), "");

            operations.uploadFileName = "test_test.doc";
            Assert.AreEqual(operations.GetDownloadFileName(), "test_test.doc");
        }

        [TestMethod]
        public void GetDownloadFileName_Modify_ReturnsModified()
        {
            var operations = new FtpOperations();

            operations.uploadFileName = "source_test.doc";
            Assert.AreEqual(operations.GetDownloadFileName(), "archive_test.doc");

            operations.uploadFileName = "source_source_test.csv.csv";
            Assert.AreEqual(operations.GetDownloadFileName(), "archive_source_test.csv.zip");

            operations.uploadFileName = "source_source_test.txt.txt";
            Assert.AreEqual(operations.GetDownloadFileName(), "archive_source_test.txt.zip");

            operations.uploadFileName = "source_source_test.csv.txt";
            Assert.AreEqual(operations.GetDownloadFileName(), "archive_source_test.csv.zip");
        }
    }
}
