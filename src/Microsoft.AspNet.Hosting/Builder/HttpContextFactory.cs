// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Internal;

namespace Microsoft.AspNet.Hosting.Builder
{
    public class HttpContextFactory : IHttpContextFactory
    {
        private static int _capacity = 32 * Environment.ProcessorCount;
        private static readonly ConcurrentQueue<DefaultHttpContext> _contextPool = new ConcurrentQueue<DefaultHttpContext>();
        private static readonly ConcurrentQueue<FeatureCollection> _featureCollectionPool = new ConcurrentQueue<FeatureCollection>();

        public HttpContext CreateHttpContext(IFeatureCollection featureCollection)
        {
            DefaultHttpContext context;
            if (_contextPool.TryDequeue(out context))
            {
                context.Initalize(CreateFeatureCollection(featureCollection));
                return context;
            }
            return new DefaultHttpContext(CreateFeatureCollection(featureCollection), PoolContext);
        }

        internal FeatureCollection CreateFeatureCollection(IFeatureCollection innerFeatureCollection)
        {
            FeatureCollection featureCollection;
            if (_featureCollectionPool.TryDequeue(out featureCollection))
            {
                featureCollection.Reset(innerFeatureCollection);
                return featureCollection;
            }
            return new FeatureCollection(innerFeatureCollection, PoolFeatureCollection);
        }

        internal void PoolContext(DefaultHttpContext context)
        {
            // Benign race condition
            if (_contextPool.Count < _capacity)
            {
                _contextPool.Enqueue(context);
            }
        }
        internal void PoolFeatureCollection(FeatureCollection context)
        {
            // Benign race condition
            if (_featureCollectionPool.Count < _capacity)
            {
                _featureCollectionPool.Enqueue(context);
            }
        }
    }
}