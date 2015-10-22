// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Server.Testing
{
    public class RetryHelper
    {
        /// <summary>
        /// Retries every 1 sec for 60 times by default.
        /// </summary>
        /// <param name="retryBlock"></param>
        /// <param name="logger"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="retryCount"></param>
        public static async Task<HttpResponseMessage> RetryRequest(
            Func<Task<HttpResponseMessage>> retryBlock,
            ILogger logger,
            CancellationToken cancellationToken = default(CancellationToken),
            int retryCount = 60)
        {
            for (var retry = 0; retry < retryCount; retry++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("Failed to connect, retry canceled.");
                    throw new OperationCanceledException("Failed to connect, retry canceled.", cancellationToken);
                }

                try
                {
                    logger.LogWarning("Retry count {retryCount}..", retry + 1);
                    var response = await retryBlock();

                    if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        // Automatically retry on 503. May be application is still booting.
                        logger.LogWarning("Retrying a service unavailable error.");
                        continue;
                    }

                    return response; // Went through successfully
                }
                catch (Exception exception)
                {
                    if (retry == retryCount - 1)
                    {
                        logger.LogError("Failed to connect, retry limit exceeded.", exception);
                        throw;
                    }
                    else
                    {
                        if (exception is HttpRequestException
#if DNX451 || NET451
                        || exception is System.Net.WebException
#endif
                        )
                        {
                            logger.LogWarning("Failed to complete the request : {0}.", exception.Message);
                            await Task.Delay(1 * 1000); //Wait for a while before retry.
                        }
                    }
                }
            }

            logger.LogInformation("Failed to connect, retry limit exceeded.");
            throw new OperationCanceledException("Failed to connect, retry limit exceeded.");
        }
    }
}