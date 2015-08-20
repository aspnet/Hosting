using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.AspNet.TestHost
{
    public static class HttpResponseMessageExtensions
    {
        public static Task PipelineCompleteAsync(this HttpResponseMessage message)
        {
            return (message as ResponseMessage).PipelineCompleteAsync();
        }
    }
}
