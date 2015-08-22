using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.AspNet.TestHost
{
    class ResponseMessage : HttpResponseMessage
    {
        ClientHandler.RequestState _requestState;

        public ResponseMessage(ClientHandler.RequestState requestState)
        {
            _requestState = requestState;
        }

        public Task<Stream> GetUpgradeStreamAsync()
        {
            return _requestState.UpgradeTask;
        }
    }
}
