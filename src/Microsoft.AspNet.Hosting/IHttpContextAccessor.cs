// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public interface IHttpContextAccessor : IContextAccessor<HttpContext>
    {
        IDisposable SetSource(Func<HttpContext> access, Func<HttpContext, HttpContext> exchange);
    }
}