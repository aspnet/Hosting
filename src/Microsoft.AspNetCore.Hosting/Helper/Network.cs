using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Hosting.Helper
{
    public class Network
    {
        public static bool PortValidity(int port)
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
                        return false;
                }
                catch
                {

                }
            }

            return true;
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

    }
}
