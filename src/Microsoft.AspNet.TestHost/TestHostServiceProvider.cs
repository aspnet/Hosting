// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.TestHost
{
    public class TestHostServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider _fallback;
        private readonly IServiceProvider _services;

        public TestHostServiceProvider(IServiceProvider fallback, IServiceProvider services)
        {
            _fallback = fallback;
            _services = services;
        }

        public object GetService(Type serviceType)
        {
            return _services.GetService(serviceType) ?? _fallback.GetService(serviceType);
        }
    }
}
