using System;
using System.Net;

namespace CSharpFTPExample
{
    /// <summary>
    /// Interface for WebClient for testing purposes. All used properties and methods must be specified here.
    /// </summary>
    public interface IWebClient : IDisposable
    {
        byte[] UploadFile(string address, string fileName);
        void DownloadFile(string address, string fileName); 
        ICredentials Credentials { get; set; }
    }

    /// <summary>
    /// Factory of IWebClient.
    /// </summary>
    public interface IWebClientFactory
    {
        IWebClient Create();
    }
}
