using System;
using System.Linq;
using System.Collections.Specialized;
using System.Net;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;
using WinSCP;

namespace CSharpFTPExample
{
    /// <summary>
    /// Handles all FTP based operations across the system.
    /// </summary>
    public class FtpOperations
    {
        // Since the connection may need to be reopened later these will need to be stored.
        private SessionOptions options;

        public ISession ftp;
        public IWebClient ftpOther;
        public string uploadFileName;

        private int port;

        /// <summary>
        /// Initializes the WinSCP client and WebClient with the correct credentials. Connects to the WinSCP Client.
        /// <param name="session">A WinSCP session, empty instantiation.</param>
        /// <param name="username">The username to get into the ftp server.</param>
        /// <param name="password">The password to get into the ftp server.</param>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <value>A Tuple in the form (<init succeeded>, <message>)</value>
        /// </summary>
        public virtual Tuple<bool, string> Init(ISession session, string username, string password, string host, int port = 21)
        {
            ftp = session;
            ftpOther = new WrappedWebClient();

            try
            {
                ftpOther.Credentials = new NetworkCredential(username, password);

                // Setup session options
                options = new SessionOptions();
                options.Protocol = Protocol.Ftp;
                options.HostName = host;
                options.UserName = username;
                options.Password = password;
                this.port = port;

                ftp.Open(options);

                return new Tuple<bool, string>(true, "Initialization succeeded.");
            }
            catch (Exception e)
            {
                return new Tuple<bool, string>(false, e.Message);
            }
        }

        /// <summary>
        /// Uploads the specified file.
        /// <param name="filename">The absolute location of the file to upload.</param>
        /// <param name="singleFile">If the file is uploaded in single file mode. Defaults to false.</param>
        /// <value>A Tuple in the form (<upload succeeded>, <message>)</value>
        /// </summary>
        public virtual Tuple<bool, string> Upload(string file, bool singleFile = false)
        {
            var type = singleFile ? "default" : "splitfile";
            var directory = "/import_" + options.UserName + "_" + type + "_config/";

            try
            {
                var fileName = new FileInfo(file).Name;
                ftpOther.UploadFile("ftp://" + options.HostName + ':' + port + directory + fileName, file);

                var status = GetStatusDescription(ftpOther);
                if (status.Item1 == 226)
                {
                    uploadFileName = status.Item2.Split(';').Last().Trim();
                    return new Tuple<bool, string>(true, fileName + " has been uploaded as " + uploadFileName);
                }
                else
                {
                    return new Tuple<bool, string>(false, "Failed to extract filename from: " + status.Item2);
                }
            }
            catch (WebException exception)
            {
                if (exception.Response != null)
                {
                    return new Tuple<bool, string>(false, ((FtpWebResponse)exception.Response).StatusDescription);
                }
                return new Tuple<bool, string>(false, exception.Message);
            }
        }

        /// <summary>
        /// Polls every pollEvery seconds until the last uploaded file can be downloaded. Then downloads.
        /// Note: This must use FTPWebRequest (which is cumbersome), because WebClient does not support FTP listings.
        /// <param name="location">The absolute location of the file to upload.</param>
        /// <param name="removeAfter"> If the results file should be removed after downloading.</param>
        /// <param name="pollEvery"> Time in milleseconds to wait between each poll.</param>
        /// <param name="callback">Called once the file downloads or there is an error. Called with:
        ///   noError: If an error occured.
        ///   message: Message returned, will never be empty.
        /// </param>
        /// </summary>
        public virtual void Download(string location, int pollEvery, bool removeAfter, Action<bool, string> callback)
        {
            var formatted = GetDownloadFileName();
            var remoteFile = "/complete/" + formatted;

            try
            {
                var result = RemoteFileExists(remoteFile);
                if (result.Item1 && String.IsNullOrEmpty(result.Item2))
                {
                    ftp.GetFiles(remoteFile, @location + "\\" + formatted, removeAfter);

                    ThrowErrorIfLocalFileNotPresent(@location + "\\" + formatted);

                    callback(true, formatted + " downloaded to " + location);
                }
                // Result error
                else if (!String.IsNullOrEmpty(result.Item2))
                {
                    callback(false, result.Item2);
                }
                else
                {
                    WaitAndDownload(formatted, new Timer(pollEvery * 1000), delegate()
                    {
                        Download(location, pollEvery, removeAfter, callback);
                    });
                }
            }
            catch (Exception e)
            {
                callback(false, e.Message);
            }
        }

        /// <summary>
        /// Throws an error if the file could not be found on the file system. Not tested, it is used to isolate File's
        /// static methods.
        /// <param name="file">Full path of the file e.g. /complete/test.csv </param>
        /// </summary>
        public virtual void ThrowErrorIfLocalFileNotPresent(string file)
        {
            if (!File.Exists(file))
            {
                throw new Exception(file + " could not be saved.");
            }
        }

        /// <summary>
        /// Checks if the sent in file exists. The WinSCP version does not work.
        /// <param name="file">Full path of the file e.g. /complete/test.csv </param>
        /// <value>A Tuple in the form (<file found>, <message (only if an error)>)</value>
        /// </summary>
        public virtual Tuple<bool, string> RemoteFileExists(string file)
        {
            if (!String.IsNullOrEmpty(file))
            {
                var parts = file.Split('/');
                var filename = parts.Last();
                var location = String.Join("/", parts, 0, parts.Length - 1);

                // Connection could have dropped
                ftp.Dispose();
                var result = Init(new WrappedSession(), options.UserName, options.Password, options.HostName, port);
                if (!result.Item1)
                {
                    return new Tuple<bool, string>(false, result.Item2);
                }

                return new Tuple<bool, string>(InDirectoryListing(location, filename), null);
            }

            return new Tuple<bool, string>(false, null);
        }

        /// <summary>
        /// Mocking the return of ListDirectory is impossible, literally. So it is best to isolate the untested code.
        /// public sealed class RemoteDirectoryInfo
        /// "You can only get an instance of the class by calling Session.ListDirectory." -WinSCP
        /// <param name="location">Location of the server to check e.g. /complete</param>
        /// <param name="filename">Just the filename e.g. test.csv</param>
        /// <param name="callback">Called once the wait is over. No parameters.</param>
        /// </summary>
        public virtual bool InDirectoryListing(string location, string filename)
        {
            RemoteDirectoryInfo directory = ftp.ListDirectory(location);
            if (directory != null)
            {
                foreach (RemoteFileInfo fileInfo in directory.Files)
                {
                    if (fileInfo.Name == filename)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Waits the specified amount of time then calls Download again. Does not pause execution on the thread.
        /// <param name="file">The file name being waited on</param>
        /// <param name="timer">A timer instance with the interval time already set</param>
        /// <param name="callback">Called once the wait is over. No parameters.</param>
        /// </summary>
        public virtual void WaitAndDownload(string file, Timer timer, Action callback)
        {
            Console.WriteLine("Waiting for results file " + file);

            timer.Elapsed += (s_, e_) => callback();
            timer.AutoReset = false;
            timer.Start();
        }

        /// <summary>
        /// Removes the results file from the server.
        /// <value>A Tuple in the form (<remove succeeded>, <error message>)</value>
        /// </summary>
        public virtual Tuple<bool, string> Remove()
        {
            try
            {
                var result = ftp.RemoveFiles("/complete/" + GetDownloadFileName());

                if (result != null)
                {
                    result.Check();
                }

                return new Tuple<bool, string>(true, null);
            }
            catch (Exception e)
            {
                return new Tuple<bool, string>(false, e.Message);
            }
        }

        /// <summary>
        /// Retrieves the upload file name and transforms it to the download one.
        /// <value>The current download name of the current upload.</value>
        /// </summary>
        public virtual string GetDownloadFileName()
        {
            if (string.IsNullOrEmpty(uploadFileName))
            {
                return uploadFileName;
            }

            var formatted = new Regex("source_").Replace(uploadFileName, "archive_", 1);
            if (formatted.IndexOf(".csv") > -1 || formatted.IndexOf(".txt") > -1)
            {
                formatted = formatted.Remove(formatted.Length - 4) + ".zip";
            }

            return formatted;
        }

        /// <summary>
        /// A way of extracting ftp responses from WebClient modified from http://stackoverflow.com/a/6470446. Returns
        /// <value>A Tuple in the form (<status code>, <description>)</value>
        /// </summary>
        public virtual Tuple<int, string> GetStatusDescription(IWebClient client)
        {
            var type = client.GetType().BaseType;
            FieldInfo responseField = type.GetField("m_WebResponse", BindingFlags.Instance | BindingFlags.NonPublic);

            if (responseField != null)
            {
                FtpWebResponse response = responseField.GetValue(client) as FtpWebResponse;

                if (response != null)
                {
                    return new Tuple<int, string>((int)response.StatusCode, response.StatusDescription);
                }
                else
                {
                    throw new WebException("Could not get the status code and description from the server.");
                }
            }

            throw new WebException("Could not get the response field from the server.");
        }
    }
}
