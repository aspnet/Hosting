// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class RequestServicesFeature : IServiceProvidersFeature, IDisposable
    {
        private IServiceScopeFactory _scopeFactory;
        private IServiceProvider _requestServices;
        private IServiceScope _scope;
        private bool _requestServicesSet;

        public RequestServicesFeature(IServiceScopeFactory scopeFactory)
        {
            if (scopeFactory == null)
            {
                throw new ArgumentNullException(nameof(scopeFactory));
            }

            _scopeFactory = scopeFactory;
        }

        internal RequestServicesFeature()
        {
            // For use with pre-validated IServiceScopeFactory
        }

        internal IServiceScopeFactory ServiceScopeFactory
        {
            set
            {
                // Setting pre-validated IServiceScopeFactory
                _scopeFactory = value; 
            }
        }

        public IServiceProvider RequestServices
        {
            get
            {
                if (!_requestServicesSet)
                {
                    _scope = _scopeFactory.CreateScope();
                    _requestServices = _scope.ServiceProvider;
                    _requestServicesSet = true;
                }
                return _requestServices;
            }

            set
            {
                _requestServices = value;
                _requestServicesSet = true;
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