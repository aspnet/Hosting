// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.WebSockets.Protocol;
using System.Net.Http;
using System.Security.Cryptography;

namespace Microsoft.AspNet.TestHost
{
    public class WebSocketClient
    {
        private TestServer _testServer;
        internal WebSocketClient(TestServer testServer)
        {
            _testServer = testServer;
            ReceiveBufferSize = 1024 * 16;
            KeepAliveInterval = TimeSpan.FromMinutes(2);
            SubProtocols = new List<string>();
        }

        public IList<string> SubProtocols
        {
            get;
            private set;
        }

        public TimeSpan KeepAliveInterval
        {
            get;
            set;
        }

        public int ReceiveBufferSize
        {
            get;
            set;
        }

        public bool UseZeroMask
        {
            get;
            set;
        }

        public Action<HttpRequestMessage> ConfigureRequest
        {
            get;
            set;
        }

        public Action<HttpResponseMessage> InspectResponse
        {
            get;
            set;
        }

        public async Task<WebSocket> ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            HttpClient client = _testServer.CreateClient();

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add(Constants.Headers.Connection, Constants.Headers.ConnectionUpgrade);
            request.Headers.Add(Constants.Headers.Upgrade, Constants.Headers.UpgradeWebSocket);
            request.Headers.Add(Constants.Headers.SecWebSocketVersion, Constants.Headers.SupportedVersion);
            request.Headers.Add(Constants.Headers.SecWebSocketKey, CreateRequestKey());
            if (SubProtocols.Count > 0)
            {
                request.Headers.Add(Constants.Headers.SecWebSocketProtocol, string.Join(", ", SubProtocols));
            }

            if (ConfigureRequest != null)
            {
                ConfigureRequest(request);
            }

            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (InspectResponse != null)
            {
                InspectResponse(response);
            }

            HttpStatusCode statusCode = response.StatusCode;
            if (statusCode != HttpStatusCode.SwitchingProtocols)
            {
                response.Dispose();
                throw new InvalidOperationException("Incomplete handshake, invalid status code: " + statusCode);
            }
            IEnumerable<string> clientSubProtocols = null;
            string subProtocol = null;
            if (response.Headers.TryGetValues(Constants.Headers.SecWebSocketProtocol, out clientSubProtocols))
            {
                subProtocol = string.Join(", ", clientSubProtocols);
                if (!clientSubProtocols.All(s => SubProtocols.Contains(s, StringComparer.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Incomplete handshake, the server specified an unknown sub-protocol: " + subProtocol);
                }
            }

            Stream stream = await (response as ResponseMessage).GetUpgradeStreamAsync();

            return CommonWebSocket.CreateClientWebSocket(stream, subProtocol, KeepAliveInterval, ReceiveBufferSize, useZeroMask: UseZeroMask);
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