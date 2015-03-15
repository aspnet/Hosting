// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class ApplicationStartup
    {
        public ApplicationStartup(ConfigureDelegate configure, ConfigureServicesDelegate configureServices, object startupInstance)
        {
            Instance = startupInstance;
            ConfigureMethod = configure;
            ConfigureServicesMethod = ConfigureServicesMethod;
        }

        public ConfigureServicesDelegate ConfigureServicesMethod { get; }
        public ConfigureDelegate ConfigureMethod { get; }

        public object Instance { get; }

        // REVIEW: is serviceProvider always builder.ApplicationServices here??
        public IServiceProvider ConfigureServices(IApplicationBuilder builder, IServiceCollection services)
        {
            return ConfigureServicesMethod == null
                ? (services != null) ? services.BuildServiceProvider() : builder.ApplicationServices
                : ConfigureServicesMethod.Invoke(Instance, builder, services);
        }

        // REVIEW: is serviceProvider always builder.ApplicationServices here??
        public void Configure(IApplicationBuilder builder)
        {
            ConfigureMethod.Invoke(Instance, builder);
        }

        public static ConfigureDelegate FindConfigureDelegate(Type startupType, string environmentName)
        {
            var configureMethod = FindMethod(startupType, "Configure{0}", environmentName, typeof(void), required: true);
            return new ConfigureDelegate(configureMethod);
        }

        public static ConfigureServicesDelegate FindConfigureServicesDelegate(Type startupType, string environmentName)
        {
            var servicesMethod = FindMethod(startupType, "Configure{0}Services", environmentName, typeof(IServiceProvider), required: false)
                ?? FindMethod(startupType, "Configure{0}Services", environmentName, typeof(void), required: false);
            return servicesMethod == null ? null : new ConfigureServicesDelegate(servicesMethod);
        }

        private static MethodInfo FindMethod(Type startupType, string methodName, string environmentName, Type returnType = null, bool required = true)
        {
            var methodNameWithEnv = string.Format(CultureInfo.InvariantCulture, methodName, environmentName);
            var methodNameWithNoEnv = string.Format(CultureInfo.InvariantCulture, methodName, "");
            var methodInfo = startupType.GetMethod(methodNameWithEnv, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                ?? startupType.GetMethod(methodNameWithNoEnv, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            if (methodInfo == null)
            {
                if (required)
                {
                    throw new Exception(string.Format("A method named '{0}' or '{1}' in the type '{2}' could not be found.",
                        methodNameWithEnv,
                        methodNameWithNoEnv,
                        startupType.FullName));

                }
                return null;
            }
            if (returnType != null && methodInfo.ReturnType != returnType)
            {
                if (required)
                {
                    throw new Exception(string.Format("The '{0}' method in the type '{1}' must have a return type of '{2}'.",
                        methodInfo.Name,
                        startupType.FullName,
                        returnType.Name));
                }
                return null;
            }
            return methodInfo;
        }
    }
}