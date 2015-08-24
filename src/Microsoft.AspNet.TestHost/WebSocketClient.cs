// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Cryptography;
using Microsoft.Framework.Internal;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;

namespace Microsoft.AspNet.TestHost
{
    public class WebSocketClient
    {
        private readonly Func<IFeatureCollection, Task> _next;
        private readonly PathString _pathBase;

        internal WebSocketClient([NotNull] Func<IFeatureCollection, Task> next, PathString pathBase)
        {
            _next = next;
            
            // PathString.StartsWithSegments that we use below requires the base path to not end in a slash.
            if (pathBase.HasValue && pathBase.Value.EndsWith("/"))
            {
                pathBase = new PathString(pathBase.Value.Substring(0, pathBase.Value.Length - 1));
            }
            _pathBase = pathBase;

            SubProtocols = new List<string>();
        }

        public IList<string> SubProtocols
        {
            get;
            private set;
        }

        public Action<HttpRequestMessage> ConfigureRequest
        {
            get;
            set;
        }

        public async Task<System.Net.WebSockets.WebSocket> ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            // clientRequest
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("Connection", "Upgrade");
            request.Headers.Add("Upgrade", "websocket");
            request.Headers.Add("Sec-WebSocket-Version", "13");
            request.Headers.Add("Sec-WebSocket-Key", CreateRequestKey());
            if (SubProtocols.Count > 0)
            {
                request.Headers.Add("SecWebSocketProtocol", string.Join(", ", SubProtocols));
            }
            if (ConfigureRequest != null)
            {
                ConfigureRequest(request);
            }

            var state = new RequestState(request, _pathBase, cancellationToken);

            // Async offload, don't let the test code block the caller.
            var offload = Task.Factory.StartNew(async () =>
            {
                try
                {
                    await _next(state.FeatureCollection);
                    state.PipelineComplete();
                }
                catch (Exception ex)
                {
                    state.PipelineFailed(ex);
                }
            });

            return await state.WebSocketTask;
        }

        private class RequestState : IHttpWebSocketFeature
        {
            public IFeatureCollection FeatureCollection { get; private set; }
            public Task<System.Net.WebSockets.WebSocket> WebSocketTask { get { return _websocketTcs.Task; } }

            private TaskCompletionSource<System.Net.WebSockets.WebSocket> _websocketTcs;

            public RequestState(HttpRequestMessage clientRequest, PathString pathBase, CancellationToken cancellationToken)
            {
                _websocketTcs = new TaskCompletionSource<System.Net.WebSockets.WebSocket>();

                // HttpContext
                FeatureCollection = new FeatureCollection();
                var httpContext = new DefaultHttpContext(FeatureCollection);

                // Request
                httpContext.SetFeature<IHttpRequestFeature>(new RequestFeature());
                var serverRequest = httpContext.Request;
                serverRequest.Protocol = "HTTP/" + clientRequest.Version.ToString(2);
                serverRequest.Scheme = clientRequest.RequestUri.Scheme;
                serverRequest.Method = clientRequest.Method.ToString();
                var fullPath = PathString.FromUriComponent(clientRequest.RequestUri);
                PathString remainder;
                if (fullPath.StartsWithSegments(pathBase, out remainder))
                {
                    serverRequest.PathBase = pathBase;
                    serverRequest.Path = remainder;
                }
                else
                {
                    serverRequest.PathBase = PathString.Empty;
                    serverRequest.Path = fullPath;
                }
                serverRequest.QueryString = QueryString.FromUriComponent(clientRequest.RequestUri);
                foreach (var header in clientRequest.Headers)
                {
                    serverRequest.Headers.AppendValues(header.Key, header.Value.ToArray());
                }
                var requestContent = clientRequest.Content;
                if (requestContent != null)
                {
                    foreach (var header in clientRequest.Content.Headers)
                    {
                        serverRequest.Headers.AppendValues(header.Key, header.Value.ToArray());
                    }
                }
                serverRequest.Body = Stream.Null;

                // Response
                httpContext.SetFeature<IHttpResponseFeature>(new ResponseFeature());
                httpContext.Response.Body = Stream.Null;
                httpContext.Response.StatusCode = 200;

                // WebSocket
                httpContext.SetFeature<IHttpWebSocketFeature>(this);
            }

            public void PipelineComplete()
            {
                PipelineFailed(null);
            }

            public void PipelineFailed(Exception ex)
            {
                _websocketTcs.TrySetException(new InvalidOperationException("The websocket was not accepted.", ex));
            }

            bool IHttpWebSocketFeature.IsWebSocketRequest
            {
                get
                {
                    return true;
                }
            }

            Task<System.Net.WebSockets.WebSocket> IHttpWebSocketFeature.AcceptAsync(WebSocketAcceptContext context)
            {
                var websockets = WebSocket.CreatePair(context.SubProtocol);
                _websocketTcs.SetResult(websockets.Item1);
                return Task.FromResult<System.Net.WebSockets.WebSocket>(websockets.Item2);
            }
        }

        private string CreateRequestKey()
        {
            byte[] data = new byte[16];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(data);
            return Convert.ToBase64String(data);
        }
    }
}