// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            ConfigureServicesMethod = configureServices;
        }

        public ConfigureServicesDelegate ConfigureServicesMethod { get; }
        public ConfigureDelegate ConfigureMethod { get; }

        public object Instance { get; }

        // REVIEW: need to revisit the import implications here (are services the exported services?)
        public IServiceProvider ConfigureServices(IApplicationBuilder builder, IServiceCollection services)
        {
            return ConfigureServicesMethod == null
                ? (services != null) ? services.BuildServiceProvider() : builder.ApplicationServices
                : ConfigureServicesMethod.Invoke(Instance, builder, services);
        }

        public void Configure(IServiceProvider services, IApplicationBuilder builder)
        {
            ConfigureMethod.Invoke(Instance, services, builder);
        }

        public static ApplicationStartup LoadStartup(
            IServiceProvider services,
            string applicationName,
            string environmentName,
            IList<string> diagnosticMessages)
        {
            if (string.IsNullOrEmpty(applicationName))
            {
                throw new ArgumentException("Value cannot be null or empty.", "applicationName");
            }

            var assembly = Assembly.Load(new AssemblyName(applicationName));
            if (assembly == null)
            {
                throw new Exception(String.Format("The assembly '{0}' failed to load.", applicationName));
            }

            var startupNameWithEnv = "Startup" + environmentName;
            var startupNameWithoutEnv = "Startup";

            // Check the most likely places first
            var type =
                assembly.GetType(startupNameWithEnv) ??
                assembly.GetType(applicationName + "." + startupNameWithEnv) ??
                assembly.GetType(startupNameWithoutEnv) ??
                assembly.GetType(applicationName + "." + startupNameWithoutEnv);

            if (type == null)
            {
                // Full scan
                var definedTypes = assembly.DefinedTypes.ToList();

                var startupType1 = definedTypes.Where(info => info.Name.Equals(startupNameWithEnv, StringComparison.Ordinal));
                var startupType2 = definedTypes.Where(info => info.Name.Equals(startupNameWithoutEnv, StringComparison.Ordinal));

                var typeInfo = startupType1.Concat(startupType2).FirstOrDefault();
                if (typeInfo != null)
                {
                    type = typeInfo.AsType();
                }
            }

            if (type == null)
            {
                throw new Exception(String.Format("A type named '{0}' or '{1}' could not be found in assembly '{2}'.",
                    startupNameWithEnv,
                    startupNameWithoutEnv,
                    applicationName));
            }

            var configureMethod = ApplicationStartup.FindConfigureDelegate(type, environmentName);
            var servicesMethod = ApplicationStartup.FindConfigureServicesDelegate(type, environmentName);

            object instance = null;
            if (!configureMethod.MethodInfo.IsStatic || (servicesMethod != null && !servicesMethod.MethodInfo.IsStatic))
            {
                instance = ActivatorUtilities.GetServiceOrCreateInstance(services, type);
            }

            return new ApplicationStartup(configureMethod, servicesMethod, instance);
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