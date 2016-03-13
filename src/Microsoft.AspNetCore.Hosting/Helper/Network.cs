using System.Net;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class Network
    {
        /// <summary>
        /// Check whether selected port is free
        /// </summary>
        /// <param name="port">Port</param>
        /// <returns></returns>
        private static bool IsPortInUse(int port)
        {
            #region WithWebRequest Method
            ////.................................With WebRequest..................................
            //try
            //{
            //    WebRequest request = WebRequest.Create(
            // "http://localhost:" + port.ToString());

            //    // Get the response.
            //    WebResponse response = request.GetResponse();

            //    return false;
            //}
            //catch
            //{
            //    return true;
            //}

            ////...................................................................
            #endregion

            System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

#if NET451

            var iph = Dns.GetHostEntry("localhost");
            foreach (System.Net.IPAddress ip in iph.AddressList)
            {
                IPEndPoint ep = new IPEndPoint(ip, port);
                try
                {
                    socket.Connect(ep);
                    if (socket.Connected)
                        return true;
                }
                catch
                {

                }
            }

            return false;
#else
            //netstandard1.3 doesn't support System.Net.DNS. What can we do?!
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
            try
            {
                socket.Connect(ep);
                if (socket.Connected)
                    return false;
            }
            catch
            {

            }
            return true;
#endif
        }

        /// <summary>
        /// Search for free port in private ports range 49152–65535.
        /// </summary>
        /// <returns>Free Port</returns>
        public static int FindFreePort()
        {
            //The range 49152–65535 (215+214 to 216−1) contains dynamic or private ports that cannot be registered with IANA
            for (int port = 49151; port < 65535; port++)
            {
                //if current port was in use. then get other port
                if (!IsPortInUse(port))
                {
                    return port;
                }
            }

            //All private ports are in use.
            throw new System.Exception("All private ports are in use.");
        }

    }
}
