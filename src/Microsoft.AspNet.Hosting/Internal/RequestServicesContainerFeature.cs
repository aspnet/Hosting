// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http.Features.Internal;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Internal
{
    public class RequestServicesFeature : IServiceProvidersFeature, IDisposable
    {
        private IServiceProvider _appServices;
        private IServiceProvider _requestServices;
        private IServiceScope _scope;
        private bool _requestServicesSet;

        public RequestServicesFeature(IServiceProvider applicationServices)
        {
            if (applicationServices == null)
            {
                throw new ArgumentNullException(nameof(applicationServices));
            }

            ApplicationServices = applicationServices;
        }

        public IServiceProvider ApplicationServices
        {
            get
            {
                return _appServices;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(ApplicationServices));
                }
                _appServices = value;
            }
        }

        public IServiceProvider RequestServices
        {
            get
            {
                if (!_requestServicesSet)
                {
                    _scope = ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
                    _requestServices = _scope.ServiceProvider;
                    _requestServicesSet = true;
                }
                return _requestServices;
            }

            set
            {
                _requestServicesSet = true;
                RequestServices = value;
            }
        }

        public void Dispose()
        {
            _scope?.Dispose();
            _scope = null;
            _requestServices = null;
        }
    }
}