using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyHttp.Http;
using EasyHttp.Infrastructure;
using JsonFx.Json;
using System.Timers;

namespace CSharpFTPExample
{
    public class HttpOperations
    {
        public string baseUrl;
        public string statusUrl;
        public string apikey;
        public IHttpClient http;

        /// <summary>
        /// Setups up the baseUrl and apikey.
        /// <param name="password">The password to get into the ftp server.</param>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <value>A Tuple in the form (<init succeeded>, <message>)</value>
        /// </summary>
        public virtual Tuple<bool, string> Init(string password, string host, int port = 80)
        {
            http = new WrappedHttpClient();
            http.Request.AddExtraHeader("x-authorization", password);
            http.Request.Accept = HttpContentTypes.ApplicationJson;

            baseUrl = string.Format("http://{0}:{1}/api/live/bulk/", host, port);
            apikey = password;

            return new Tuple<bool, string>(true, "");
        }

        /// <summary>
        /// Uploads the specified file.
        /// <param name="filename">The absolute location of the file to upload.</param>
        /// <param name="singleFile">If the file is uploaded in single file mode. Defaults to false.</param>
        /// <param name="notify"> The full email address to notify once an upload completes. If an empty value
        ///                       is sent no address will be contacted.</param>
        /// <value>A Tuple in the form (<upload succeeded>, <message>)</value>
        /// </summary>
        public virtual Tuple<bool, string> Upload(string file, bool singleFile = false, string notify = null)
        {
            IDictionary<string, object> data = new Dictionary<string, object>();

            data.Add("export_type", singleFile ? "single" : "multi");
            data.Add("notify_email", notify);

            IList<FileData> files = new List<FileData>();
            files.Add(new FileData() { FieldName = "file", ContentType = "text/csv", Filename = file });

            try
            {
                HttpResponse response = http.Post(baseUrl, data, files);

                var reader = new JsonReader();
                dynamic output = reader.Read(GetRawResponse(response));
                if (output.status == "error")
                {
                    return new Tuple<bool, string>(false, output.msg);
                }
                statusUrl = output.status_url;
            }
            catch (Exception e)
            {
                return new Tuple<bool, string>(false, e.Message);
            }

            return new Tuple<bool, string>(true, file + " was uploaded.");
        }

        /// <summary>
        /// Wraps HttpResponse's RawText so that it is writable for testing.
        /// <param name="response">The response to extract from</param>
        /// <value>The raw response in string form</value>
        /// </summary>
        public virtual string GetRawResponse(HttpResponse response)
        {
            return response.RawText;
        }

        /// <summary>
        /// Polls every pollEvery seconds until the last uploaded file can be downloaded. Then downloads.
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
            try
            {
                HttpResponse response = http.Get(statusUrl);

                var reader = new JsonReader();
                dynamic output = reader.Read(GetRawResponse(response));
                if (output.status == "error")
                {
                    callback(false, output.msg);
                }
                else if (!String.IsNullOrEmpty(output.status) && output.status == "completed")
                {
                    DownloadAndDelete(output, location, removeAfter, callback);
                }
                else
                {
                    WaitAndDownload((string)output.job, new Timer(pollEvery * 1000), delegate()
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
        /// Downloads the actual file and then deletes if specified.
        /// <param name="location">The object received from the last status retrieval</param>
        /// <param name="location">The absolute location of the file to download.</param>
        /// <param name="removeAfter"> If the results file should be removed after downloading.</param>
        /// <param name="callback">Called once the file downloads or there is an error. Called with:
        ///   noError: If an error occured.
        ///   message: Message returned, will never be empty.
        /// </param>
        /// </summary>
        public virtual void DownloadAndDelete(dynamic output, string location, bool removeAfter,
                                              Action<bool, string> callback)
        {
            var response = http.GetFile(output.download_url, @location + "\\" + output.job + ".zip");

            var reader = new JsonReader();
            dynamic output2 = reader.Read((string)GetRawResponse(response));
            if (output2 != null && output2.status == "error")
            {
                callback(false, output2.msg);
            }
            else
            {
                if (removeAfter)
                {
                    var result = Remove();
                    if (!result.Item1)
                    {
                        callback(result.Item1, result.Item2);
                    }
                    else
                    {
                        callback(true, output.job + ".zip downloaded to " + location);
                    }
                }
                else
                {
                    callback(true, output.job + ".zip downloaded to " + location);
                }
            }
        }

        /// <summary>
        /// Removes the results file from the server.
        /// <value>A Tuple in the form (<remove succeeded>, <error message>)</value>
        /// </summary>
        public virtual Tuple<bool, string> Remove()
        {
            var response = http.Delete(statusUrl);

            var reader = new JsonReader();
            dynamic output = reader.Read(GetRawResponse(response));
            if (output != null && output.status == "error")
            {
                return new Tuple<bool, string>(false, output.msg);
            }
            else
            {
                return new Tuple<bool, string>(true, "");
            }
        }
    }
}
