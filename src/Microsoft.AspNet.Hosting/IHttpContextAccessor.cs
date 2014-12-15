// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Hosting
{
    public interface IHttpContextAccessor
    {
        HttpContext Value { get; }

        HttpContext SetValue(HttpContext value);

        IDisposable SetSource(Func<HttpContext> access, Func<HttpContext, HttpContext> exchange);
    }
}