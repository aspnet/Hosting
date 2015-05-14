// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Hosting.Startup
{
    public delegate IServiceProvider ConfigureServicesDelegate(IServiceCollection services);

    public class ConfigureServicesBuilder
    {
        public ConfigureServicesBuilder([NotNull] MethodInfo configureServices)
        {
            // Only support IServiceCollection parameters
            var parameters = configureServices.GetParameters();
            if (parameters.Length > 1 ||
                parameters.Any(p => p.ParameterType != typeof(IServiceCollection)))
            {
                throw new InvalidOperationException("ConfigureServices can take at most a single IServiceCollection parameter.");
            }

            MethodInfo = configureServices;
        }

        public MethodInfo MethodInfo { get; }

        public ConfigureServicesDelegate Build(object instance)
        {
            return services => Invoke(instance, services);
        }

        private IServiceProvider Invoke(object instance, IServiceCollection exportServices)
        {
            var parameterInfos = MethodInfo.GetParameters();
            var parameters = new object[parameterInfos.Length];
            for (var index = 0; index != parameterInfos.Length; ++index)
            {
                var parameterInfo = parameterInfos[index];
                if (exportServices != null && parameterInfo.ParameterType == typeof(IServiceCollection))
                {
                    parameters[index] = exportServices;
                }
            }

            // REVIEW: We null ref if exportServices is null, cuz it should not be null
            return MethodInfo.Invoke(instance, parameters) as IServiceProvider ?? exportServices.BuildServiceProvider();
        }
    }
}