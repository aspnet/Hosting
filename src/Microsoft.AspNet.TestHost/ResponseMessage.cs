using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.AspNet.TestHost
{
    class ResponseMessage : HttpResponseMessage
    {
        private ClientHandler.RequestState _requestState;

        public ResponseMessage(ClientHandler.RequestState requestState)
        {
            _requestState = requestState;
        }

        public Task PipelineCompleteAsync()
        {
            return _requestState.PipelineTask;
        }
    }
}
