using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Hosting
{
    public class AspNetCoreHostingEventSource : EventSource
    {
        private AspNetCoreHostingEventSource() { }

        public static readonly AspNetCoreHostingEventSource Log = new AspNetCoreHostingEventSource();

        public void EnterHostRun()
        {
            WriteEvent(1, "Entering host run");
        }

        public void LeaveHostRun()
        {
            WriteEvent(2, "Leaving host run");
        }

        public void StartConfigureApplicationServices()
        {
            WriteEvent(3, "Start configuring application services");
        }

        public void EndConfigureApplicationServices()
        {
            WriteEvent(4, "End configuring application services");
        }

        public void StartConfigureMiddlewarePipeline()
        {
            WriteEvent(5, "Start configuring middleware pipeline");
        }

        public void EndConfigureMiddlewarePipeline()
        {
            WriteEvent(6, "End configuring middleware pipeline");
        }

        public void RequestStart()
        {
            WriteEvent(7, "Request started");
        }

        public void RequestEnd()
        {
            WriteEvent(8, "Request ended");
        }
    }
}
