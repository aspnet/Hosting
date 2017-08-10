using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class HostingEventSourceListener : EventListener
    {
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            Console.WriteLine($"{eventData.EventName}:");
            foreach (var payload in eventData.Payload)
            {
                if (payload is IDictionary<string, object> payloadDictionary)
                {
                    foreach (var data in payloadDictionary)
                    {
                        Console.WriteLine($"{data.Key} - {data.Value}");
                    }
                }

            }
        }
    }
}
