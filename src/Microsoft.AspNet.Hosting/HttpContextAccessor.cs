// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Hosting
{
    public class HttpContextAccessor : IHttpContextAccessor
    {
        private ContextSource _source;
        private HttpContext _value;

        public HttpContextAccessor()
        {
            _source = new ContextSource();
        }

        public HttpContext Value
        {
            get
            {
                return _source.Access != null ? _source.Access() : _value;
            }
        }

        public HttpContext SetValue(HttpContext value)
        {
            if (_source.Exchange != null)
            {
                return _source.Exchange(value);
            }
            var prior = _value;
            _value = value;
            return prior;
        }

        public IDisposable SetSource(Func<HttpContext> access, Func<HttpContext, HttpContext> exchange)
        {
            var prior = _source;
            _source = new ContextSource(access, exchange);
            return new Disposable(this, prior);
        }

        struct ContextSource
        {
            public ContextSource(Func<HttpContext> access, Func<HttpContext, HttpContext> exchange)
            {
                Access = access;
                Exchange = exchange;
            }

            public readonly Func<HttpContext> Access;
            public readonly Func<HttpContext, HttpContext> Exchange;
        }

        class Disposable : IDisposable
        {
            private readonly HttpContextAccessor _contextAccessor;
            private readonly ContextSource _source;

            public Disposable(HttpContextAccessor contextAccessor, ContextSource source)
            {
                _contextAccessor = contextAccessor;
                _source = source;
            }

            public void Dispose()
            {
                _contextAccessor._source = _source;
            }
        }
    }
}