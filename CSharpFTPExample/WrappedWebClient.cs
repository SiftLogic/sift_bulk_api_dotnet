using System.Net;

namespace CSharpFTPExample
{
    /// <summary>
    /// Wrapped web client for testing purposes.
    /// </summary>
    public class WrappedWebClient : WebClient, IWebClient
    {

    }

    /// <summary>
    /// Factory of the wrapped web client. Put any used properties of WebClient here.
    /// </summary>
    public class WrappedWebClientFactory : IWebClientFactory
    {
        #region IWebClientFactory implementation

        private WrappedWebClient client;

        public IWebClient Create()
        {
            client = new WrappedWebClient();
            return client;
        }

        public ICredentials Credentials
        {
            get
            {
                return client.Credentials;
            }
            set
            {
                client.Credentials = value;
            }
        }

        #endregion
    }
}
