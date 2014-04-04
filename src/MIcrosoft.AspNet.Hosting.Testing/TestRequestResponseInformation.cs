using Microsoft.AspNet.HttpFeature;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Hosting.Testing
{
    internal class TestRequestResponseInformation : IHttpRequestInformation, IHttpResponseInformation
    {
        public TestRequestResponseInformation()
        {
            IHttpRequestInformation request = (IHttpRequestInformation)this;
            request.Headers = new Dictionary<string, string[]>();
            request.PathBase = "";
            request.Body = Stream.Null;
            request.Protocol = "HTTP/1.1";

            IHttpResponseInformation response = (IHttpResponseInformation)this;
            response.Headers = new Dictionary<string, string[]>();
            response.Body = new MemoryStream();
        }

        // IHttpRequestInformation
        Stream IHttpRequestInformation.Body { get; set; }

        IDictionary<string, string[]> IHttpRequestInformation.Headers { get; set; }

        string IHttpRequestInformation.Method { get; set; }

        string IHttpRequestInformation.Path { get; set; }

        string IHttpRequestInformation.PathBase { get; set; }

        string IHttpRequestInformation.Protocol { get; set; }

        string IHttpRequestInformation.QueryString { get; set; }

        string IHttpRequestInformation.Scheme { get; set; }

        // IHttpResponseInformation

        int IHttpResponseInformation.StatusCode { get; set; }
        string IHttpResponseInformation.ReasonPhrase { get; set; }
        IDictionary<string, string[]> IHttpResponseInformation.Headers { get; set; }
        Stream IHttpResponseInformation.Body { get; set; }
        void IHttpResponseInformation.OnSendingHeaders(Action<object> callback, object state)
        {
            // TODO: Figure out how to implement this thing.
        }
    }
}
