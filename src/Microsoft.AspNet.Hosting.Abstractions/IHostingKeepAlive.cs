// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Hosting
{
    /// <summary>
    /// Provide a way to keep the application running
    /// </summary>
    public interface IHostingKeepAlive
    {
        /// <summary>
        /// Called when the application should be kept running.
        /// When this returns, the application shuts down.
        /// </summary>
        void Hold();
    }
}
