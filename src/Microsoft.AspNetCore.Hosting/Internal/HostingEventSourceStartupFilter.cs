
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class HostingEventSourceStartupFilter : IStartupFilter
    {
        HostingEventSourceListener _hostingEventSourceListener;

        public HostingEventSourceStartupFilter(HostingEventSourceListener hostingEventSourceListener)
        {
            _hostingEventSourceListener = hostingEventSourceListener;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                var hostingEventSource = EventSource.GetSources().FirstOrDefault(es => es.Name == "Microsoft-AspNetCore-Hosting");

                if (hostingEventSource != null)
                {
                    _hostingEventSourceListener.EnableEvents(hostingEventSource, EventLevel.LogAlways, EventKeywords.All, new Dictionary<string, string>() { { "EventCounterIntervalSec", "1" } });
                }

                next(builder);
            };
        }
    }
}
