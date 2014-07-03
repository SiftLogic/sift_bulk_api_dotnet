using System;
using System.IO;
using System.Collections.Generic;
using EasyHttp.Http;
using EasyHttp.Infrastructure;

namespace CSharpFTPExample
{
    /// <summary>
    /// Interface for EasyHttp for testing. All used properties and methods must be specified here.
    /// </summary>
    public interface IHttpClient
    {
        HttpResponse Post(string uri, IDictionary<string, object> formData, IList<FileData> files);
        HttpRequest Request { get; }
        List<object[]> LastCalls { get; }
        HttpResponse MockedResponse { get; set; }
    }

    /// <summary>
    /// Factory of IHttpClient.
    /// </summary>
    public interface IHttpClientFactory
    {
        IHttpClient Create();
    }
}
