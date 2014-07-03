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
        public string protocol;
        public string notify;
        public ISession ftp;
        // There is no way to retrieve server responses from WinSCP when the requests succeeds. So a WebClient hack.
        // See GetStatusDescription
        public IWebClient ftpOther;

        public FtpOperations ftpOperations;
        public HttpOperations httpOperations;

        /// <summary>
        /// The constructor adds properties to the object which are used in init.
        /// <param name="username">The username to get into the ftp server.</param>
        /// <param name="password">The password to get into the ftp server.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="host">The host to connect to.</param>
        /// <param name="pollEvery">Number of seconds to poll for.</param>
        /// <param name="protocol">What protocol to use to transfer data.</param>
        /// <param name="notify">The full email address to notify once an upload completes.</param>
        /// </summary>
        public Operations(string username, string password, int port, string host = "localhost",
                          int pollEvery = 300, string protocol = "http", string notify = null)
        {
            this.username = username;
            this.password = password;
            this.host = host;
            this.port = port;
            this.pollEvery = pollEvery;
            this.protocol = protocol;
            this.notify = notify;

            if (this.protocol == "ftp")
            {
                ftpOperations = new FtpOperations();
            }
            else
            {
                httpOperations = new HttpOperations();
            }
        }

        /// <summary>
        /// Initializes the connection with the connection options (username, key, host port).
        /// <value>A Tuple in the form (<init succeeded>, <message>)</value>
        /// </summary>
        public virtual Tuple<bool, string> Init()
        {
            if (this.protocol == "ftp")
            {
                return ftpOperations.Init(new WrappedSession(), username, password, host, port);
            }
            else
            {
                return httpOperations.Init(password, host, port);
            }
        }

        /// <summary>
        /// Uploads the specified file.
        /// <param name="file">The absolute location of the file to upload.</param>
        /// <param name="singleFile">If the file is uploaded in single file mode. Defaults to false.</param>
        /// <param name="notify"> The full email address to notify once an upload completes. If an empty value
        ///                       is sent no address will be contacted.</param>
        /// <value>A Tuple in the form (<upload succeeded>, <message>)</value>
        /// </summary>
        public Tuple<bool, string> Upload(string file, bool singleFile = false, string notify = null)
        {
            if (this.protocol == "ftp")
            {
                return ftpOperations.Upload(file, singleFile);
            }
            else
            {
                return httpOperations.Upload(file, singleFile, notify);
            }
        }

        /// <summary>
        /// Downloads the last uploaded file (self.uploadFileName).
        /// <param name="location">The absolute location of the file to upload.</param>
        /// <param name="removeAfter"> If the results file should be removed after downloading.</param>
        /// <param name="callback">Called once the file downloads or there is an error. Called with:
        ///   noError: If an error occured.
        ///   message: Message returned, will never be empty.
        /// </param>
        /// </summary>
        public virtual void Download(string location, bool removeAfter, Action<bool, string> callback)
        {
            ftpOperations.Download(location, pollEvery, removeAfter, callback);
        }

        /// <summary>
        /// Removes the results file from the server.
        /// <value>A Tuple in the form (<remove succeeded>, <error message>)</value>
        /// </summary>
        public virtual Tuple<bool, string> Remove()
        {
            return ftpOperations.Remove();
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
    }
}
