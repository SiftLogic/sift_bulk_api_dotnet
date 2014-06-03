using System;
using System.Linq;
using System.Collections.Specialized;
using System.Net;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;

namespace CSharpFTPExample
{
    /// <summary>
    /// Used by the Program.cs file to load and download files. This can be directly required to integrate into
    /// your own .Net application.
    /// </summary>
    public class Operations
    {
        private string username;
        private string password;
        private string host;
        private int port;

        public string uploadFileName;
        public int pollEvery;
        public IWebClient ftp;

        /// <summary>
        /// The constructor adds properties to the object which are used in init.
        /// <param name="username">The username to get into the ftp server.</param>
        /// <param name="password">The password to get into the ftp server.</param>
        /// <param name="host">The host to connect to.</param>
        /// <param name="host">The port to connect to.</param>
        /// <param name="pollEvery">Number of seconds to poll for.</param>
        /// </summary>
        public Operations(string username, string password, string host = "localhost", int port = 21, int pollEvery = 300)
        {
            this.username = username;
            this.password = password;
            this.host = host;
            this.port = port;
            this.pollEvery = pollEvery;
        }

        /// <summary>
        /// Initializes the web client was initialized with the correct credentials. Returns
        /// <value>true if this succeeded.</value>
        /// </summary>
        public bool Init()
        {
            this.ftp = new WrappedWebClient();

            ftp.Credentials = new NetworkCredential(username, password);

            return true;
        }

        /// <summary>
        /// Uploads the specified file.
        /// <param name="filename">The location of the file to upload.</param>
        /// <param name="singleFile">If the file is uploaded in single file mode. Defaults to false.</param>
        /// <value>A Tuble in the form (<upload succeeded>, <message>)</value>
        /// </summary>
        public Tuple<bool, string> Upload(string file, bool singleFile = false)
        {
            var type = singleFile ? "default" : "splitfile";
            var directory = "/import_" + username + "_" + type + "_config/";

            try
            {
                var fileName = new FileInfo(file).Name;
                Console.WriteLine(fileName);
                ftp.UploadFile("ftp://" + host + ':' + port + directory + fileName, file);

                var status = GetStatusDescription(ftp);
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
        /// <param name="location">The location of the file to upload.</param>
        /// <param name="callback">Called once the file downloads or there is an error. Called with:
        ///   noError: If an error occured.
        ///   message: Message returned, will never be empty.
        /// </param>
        /// </summary>
        public virtual void Download(string location, Action<bool, string> callback)
        {
            var directory = "ftp://" + host + ':' + port + "/complete";
            var formatted = GetDownloadFileName();
            var result = GetDirectoryListing(directory);

            if (!result.Item1)
            {
                callback(false, result.Item2);
            }
            else if (result.Item2.IndexOf(formatted) > -1)
            {
                try
                {
                    ftp.DownloadFile(directory + "/" + formatted, location + "/" + formatted);
                    callback(true, formatted + " downloaded to " + location);
                }
                catch (WebException exception)
                {
                    if (exception.Response != null)
                    {
                        callback(false, ((FtpWebResponse)exception.Response).StatusDescription);
                    }
                    callback(false,"The download location probably cannot be accessed. Full error message:" + exception.Message);
                }
            }
            else
            {
                WaitAndDownload(formatted, new Timer(pollEvery * 1000), delegate()
                {
                    Download(location, callback);
                });
            }
        }

        /// <summary>
        /// Waits the specified amount of time then calls Download again. Does not pause execution on the current thread.
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
        /// Instead of implementing multiple complex interfaces directly into the code base it is easier to just stub out this.
        /// Unfortunately, then this code must remain untested.
        /// <param name="location">Full url of the location to list e.g. ftp://bacon:5894/complete </param>
        /// <value>>A Tuple in the form (<listing succeeded>, <message>)</value>
        /// </summary>
        public virtual Tuple<bool, string> GetDirectoryListing(string location)
        {
            try
            {
                var request = (FtpWebRequest)WebRequest.Create(location);
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                request.Credentials = ftp.Credentials;

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);

                var listing = reader.ReadToEnd();

                reader.Close();
                response.Close();

                return new Tuple<bool, string>(true, listing);
            }
            catch (WebException exception)
            {
                return new Tuple<bool, string>(false, ((FtpWebResponse)exception.Response).StatusDescription);
            }
        }

        /// <summary>
        /// Returns
        /// <value>An ordered dictionary of the username, password, host and port.</value>
        /// </summary>
        public OrderedDictionary GetConnectionDetails()
        {
            OrderedDictionary dictionary = new OrderedDictionary();
            dictionary.Add("username", username);
            dictionary.Add("password", password);
            dictionary.Add("host", host);
            dictionary.Add("port", port);

            return dictionary;
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
            if(formatted.IndexOf(".csv") > -1 || formatted.IndexOf(".txt") > -1)
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
