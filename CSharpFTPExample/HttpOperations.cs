using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpFTPExample
{
    public class HttpOperations
    {
        public string baseUrl;
        public string apikey;

        /// <summary>
        /// Setups up the baseUrl and apikey.
        /// <param name="password">The password to get into the ftp server.</param>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <value>A Tuple in the form (<init succeeded>, <message>)</value>
        /// </summary>
        public virtual Tuple<bool, string> Init(string password, string host, int port = 80)
        {
            baseUrl = string.Format("http://{0}:{1}/api/live/bulk/", host, port);
            apikey = password;

            return new Tuple<bool, string>(true, "");
        }
    }
}
