﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore;

namespace Microsoft.AspNet.Hosting.Embedded
{
    public class EmbeddedClient
    {
        private Func<object, Task> _pipeline;
        public EmbeddedClient(Func<object, Task> pipeline)
        {
            _pipeline = pipeline;
        }

        public async Task<HttpResponse> SendAsync(string method,
                                                  string url,
                                                  IDictionary<string, string[]> headers = null,
                                                  Stream body = null,
                                                  Action<HttpRequest> onSendingRequest = null)
        {
            return await SendAsync(method, new Uri(url), headers, body, onSendingRequest);
        }

        public async Task<HttpResponse> SendAsync(string method,
                                                  Uri uri,
                                                  IDictionary<string, string[]> headers = null,
                                                  Stream body = null,
                                                  Action<HttpRequest> onSendingRequest = null)
        {
            var request = CreateRequest(method, uri, headers, body);
            var response = new ResponseInformation();

            var features = new FeatureCollection();
            features.Add(typeof(IHttpRequestInformation), request);
            features.Add(typeof(IHttpResponseInformation), response);
            var httpContext = new DefaultHttpContext(features);

            if (onSendingRequest != null)
            {
                onSendingRequest(httpContext.Request);
            }
            await _pipeline(features);

            response.Body.Seek(0, SeekOrigin.Begin);
            return httpContext.Response;
        }

        private static IHttpRequestInformation CreateRequest(
            string method,
            Uri uri,
            IDictionary<string, string[]> headers,
            Stream body)
        {
            var request = new RequestInformation();
            request.Method = method;
            request.Scheme = uri.Scheme;
            request.Path = PathString.FromUriComponent(uri).Value;
            request.QueryString = QueryString.FromUriComponent(uri).Value;
            request.Headers = headers ?? request.Headers;
            if (!request.Headers.ContainsKey("Host"))
            {
                var host = new string[1];
                if (uri.IsDefaultPort)
                {
                    host[0] = uri.Host;
                }
                else
                {
                    host[0] = uri.GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped);
                }
                request.Headers["Host"] = host;
            }

            if (body != null)
            {
                EnsureContentLength(request.Headers, body);
                request.Body = body;
            }
            else
            {
                request.Body = Stream.Null;
            }
            return request;
        }

        public async Task<HttpResponse> GetAsync(string url)
        {
            var uri = new Uri(url);
            return await GetAsync(uri);
        }

        public async Task<HttpResponse> GetAsync(Uri uri)
        {
            return await SendAsync("GET", uri);
        }

        public async Task<string> GetStringAsync(string url)
        {
            var uri = new Uri(url);
            return await GetStringAsync(uri);
        }

        public async Task<string> GetStringAsync(Uri uri)
        {
            var response = await GetAsync(uri);
            return await new StreamReader(response.Body).ReadToEndAsync();
        }

        public async Task<Stream> GetStreamAsync(string url)
        {
            var uri = new Uri(url);
            return await GetStreamAsync(uri);
        }

        public async Task<Stream> GetStreamAsync(Uri uri)
        {
            var response = await GetAsync(uri);
            return response.Body;
        }

        public async Task<HttpResponse> PostAsync(
            string url,
            string content,
            string contentType,
            Action<HttpRequest> onSedingRequest = null)
        {
            return await PostAsync(new Uri(url), content, contentType, onSedingRequest);
        }

        public async Task<HttpResponse> PostAsync(
            Uri url,
            string content,
            string contentType,
            Action<HttpRequest> onSedingRequest = null)
        {
            var bytes = GetBytes(content);
            var headers = CreateContentHeaders(contentType, bytes.Length);
            var body = new MemoryStream(bytes);

            return await SendAsync("POST", url, headers, body, onSedingRequest);
        }

        public async Task<HttpResponse> PutAsync(
            string url,
            string content,
            string contentType,
            Action<HttpRequest> onSedingRequest = null)
        {
            return await PutAsync(new Uri(url), content, contentType, onSedingRequest);
        }

        public async Task<HttpResponse> PutAsync(
            Uri url,
            string content,
            string contentType,
            Action<HttpRequest> onSedingRequest = null)
        {
            var bytes = GetBytes(content);
            var headers = CreateContentHeaders(contentType, bytes.Length);
            var body = new MemoryStream(bytes);

            return await SendAsync("PUT", url, headers, body, onSedingRequest);
        }

        public async Task<HttpResponse> DeleteAsync(string url)
        {
            return await DeleteAsync(new Uri(url));
        }

        public async Task<HttpResponse> DeleteAsync(Uri uri)
        {
            return await SendAsync("DELETE", uri);
        }

        private static void EnsureContentLength(IDictionary<string, string[]> dictionary, Stream body)
        {
            if (!dictionary.ContainsKey("Content-Length"))
            {
                dictionary["Content-Length"] = new[] { body.Length.ToString(CultureInfo.InvariantCulture) };
            }
        }

        private static byte[] GetBytes(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            return bytes;
        }

        private static Dictionary<string, string[]> CreateContentHeaders(string contentType, int contentLength)
        {
            return new Dictionary<string, string[]>
            {
                { "Content-Type", new [] { contentType } },
                { "Content-Length", new [] { contentLength.ToString(CultureInfo.InvariantCulture) } }
            };
        }
    }
}
