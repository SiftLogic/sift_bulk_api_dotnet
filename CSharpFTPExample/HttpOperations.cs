using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyHttp.Http;
using EasyHttp.Infrastructure;
using JsonFx.Json;

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
    }
}
