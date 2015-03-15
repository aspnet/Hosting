// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Startup
{

    public class ConfigureServicesDelegate
    {
        public ConfigureServicesDelegate(MethodInfo configureServices)
        {
            MethodInfo = configureServices;
        }

        public MethodInfo MethodInfo { get; }

        public IServiceProvider Invoke(object instance, IApplicationBuilder builder, IServiceCollection services)
        {
            var serviceProvider = builder.ApplicationServices;
            var parameterInfos = MethodInfo.GetParameters();
            var parameters = new object[parameterInfos.Length];
            for (var index = 0; index != parameterInfos.Length; ++index)
            {
                var parameterInfo = parameterInfos[index];
                if (parameterInfo.ParameterType == typeof(IApplicationBuilder))
                {
                    parameters[index] = builder;
                }
                else if (services != null && parameterInfo.ParameterType == typeof(IServiceCollection))
                {
                    parameters[index] = services;
                }
                else
                {
                    try
                    {
                        parameters[index] = serviceProvider.GetRequiredService(parameterInfo.ParameterType);
                    }
                    catch (Exception)
                    {
                        throw new Exception(string.Format(
                            "Could not resolve a service of type '{0}' for the parameter '{1}' of method '{2}' on type '{3}'.",
                            parameterInfo.ParameterType.FullName,
                            parameterInfo.Name,
                            MethodInfo.Name,
                            MethodInfo.DeclaringType.FullName));
                    }
                }
            }

            // IServiceProvider ConfigureServices(IServiceCollection, ...)
            if (MethodInfo.ReturnType == typeof(IServiceProvider))
            {
                return (IServiceProvider)MethodInfo.Invoke(instance, parameters);
            }
            // void ConfigureServices(IServiceCollection, ...)
            else
            {
                // REVIEW: this will work even if their ConfigureServices does nothing
                MethodInfo.Invoke(instance, parameters);

                // REVIEW: is builder.ApplicationServices the proper default if no IServiceCollection at all??
                return services != null ? services.BuildServiceProvider() : builder.ApplicationServices;
            }
        }
    }
}