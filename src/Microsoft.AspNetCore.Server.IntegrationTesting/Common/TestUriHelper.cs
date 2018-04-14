// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.Common
{
    public static class TestUriHelper
    {
        public static Uri BuildTestUri()
        {
            return BuildTestUri(null);
        }

        public static Uri BuildTestUri(string hint)
        {
            return BuildTestUri(hint, statusMessagesEnabled: false);
        }

        public static Uri BuildTestUri(string hint, bool statusMessagesEnabled)
        {
            if (string.IsNullOrEmpty(hint))
            {
                if (statusMessagesEnabled)
                {
                    // Most functional tests use this codepath and should directly
                    // bind to dynamic port "0" and scrape the assigned port
                    // from the status message, which should be 100% reliable.
                    return new UriBuilder("http", "127.0.0.1", 0).Uri;
                }
                else
                {
                    // If status messages are disabled, the less reliable GetNextPort()
                    // must be used.
                    return new UriBuilder("http", "localhost", GetNextPort()).Uri;
                }
            }
            else
            {
                var uriHint = new Uri(hint);
                if (uriHint.Port == 0)
                {
                    // Only a few tests use this codepath, so it's fine to use the less reliable
                    // GetNextPort() for simplicity.  These tests could be improved to bind 
                    // to dynamic port "0" (when status messages are enabled) but the hostname
                    // would need to be "127.0.0.1" or "[::1]".  Binding to dynamic port "0" on
                    // "localhost" is not supported in Kestrel.
                    return new UriBuilder(uriHint) { Port = GetNextPort() }.Uri;
                }
                else
                {
                    // If the hint contains a specific port, return it unchanged.
                    return uriHint;
                }
            }
        }

        // Copied from https://github.com/aspnet/KestrelHttpServer/blob/47f1db20e063c2da75d9d89653fad4eafe24446c/test/Microsoft.AspNetCore.Server.Kestrel.FunctionalTests/AddressRegistrationTests.cs#L508
        //
        // This method is an attempt to safely get a free port from the OS.  Most of the time,
        // when binding to dynamic port "0" the OS increments the assigned port, so it's safe
        // to re-use the assigned port in another process.  However, occasionally the OS will reuse
        // a recently assigned port instead of incrementing, which causes flaky tests with AddressInUse
        // exceptions.  This method should only be used when the application itself cannot use
        // dynamic port "0" (e.g. IISExpress).  Most functional tests using raw Kestrel
        // (with status messages enabled) should directly bind to dynamic port "0" and scrape 
        // the assigned port from the status message, which should be 100% reliable.
        public static int GetNextPort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                // Let the OS assign the next available port. Unless we cycle through all ports
                // on a test run, the OS will always increment the port number when making these calls.
                // This prevents races in parallel test runs where a test is already bound to
                // a given port, and a new test is able to bind to the same port due to port
                // reuse being enabled by default by the OS.
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }
    }
}
