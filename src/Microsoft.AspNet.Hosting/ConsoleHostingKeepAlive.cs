using System;

namespace Microsoft.AspNet.Hosting
{
    public class ConsoleHostingKeepAlive : IHostingKeepAlive
    {
        public void Hold()
        {
            Console.WriteLine("Started");
            Console.ReadLine();
        }
    }
}