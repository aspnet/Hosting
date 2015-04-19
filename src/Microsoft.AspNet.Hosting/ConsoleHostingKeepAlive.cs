using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Hosting
{
    public class ConsoleHostingKeepAlive : IHostingKeepAlive
    {
        public Task SetupAsync()
        {
            return Task.Run(()=>
            {
                Console.WriteLine("Started");
                Console.ReadLine();
            });
        }
    }
}
