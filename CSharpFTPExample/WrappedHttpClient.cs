using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using EasyHttp.Http;
using EasyHttp.Infrastructure;

namespace CSharpFTPExample
{
    public class WrappedHttpClient : IHttpClient
    {
        private HttpClient http;
        // Moq is limited in how deeply it can check so I will force manual checks
        private List<object[]> calls;
        private bool mocked;
        private HttpResponse mockedResponse;

        public WrappedHttpClient(bool mocked = false)
        {
            http = new HttpClient();
            calls = new List<object[]>();

            this.mocked = mocked; 
        }

        public virtual HttpResponse Post(string uri, IDictionary<string, object> formData, IList<FileData> files)
        {
            if (!mocked)
            {
                return http.Post(uri, formData, files);
            }
            else
            {
                calls.Add(new object[] { uri, formData, files });
                return mockedResponse;
            }
        }

        public virtual HttpResponse Get(string uri)
        {
            if (!mocked)
            {
                return http.Get(uri);
            }
            else
            {
                calls.Add(new object[] { uri });
                return mockedResponse;
            }
        }

        public virtual HttpResponse GetFile(string uri, string filename)
        {
            if (!mocked)
            {
                return http.GetAsFile(uri, filename);
            }
            else
            {
                calls.Add(new object[] { uri, filename });
                return mockedResponse;
            }
        }

        public virtual HttpResponse Delete(string uri)
        {
            if (!mocked)
            {
                return http.Delete(uri);
            }
            else
            {
                calls.Add(new object[] { uri });
                return mockedResponse;
            }
        }

        public HttpRequest Request
        {
            get { return http.Request; }
        }

        public List<object[]> LastCalls
        {
            get { return calls; }
        }

        public HttpResponse MockedResponse
        {
            get { return mockedResponse; }
            set { mockedResponse = value; }
        }
    }
}
