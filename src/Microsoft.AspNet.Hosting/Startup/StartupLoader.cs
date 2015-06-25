// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class StartupLoader : IStartupLoader
    {
        private readonly IServiceProvider _services;
        private readonly IHostingEnvironment _hostingEnv;

        public StartupLoader(IServiceProvider services, IHostingEnvironment hostingEnv)
        {
            _services = services;
            _hostingEnv = hostingEnv;
        }

        public StartupMethods LoadMethods(
            Type startupType,
            IList<string> diagnosticMessages)
        {
            var environmentName = _hostingEnv.EnvironmentName;
            var configureMethod = FindConfigureDelegate(startupType, environmentName);
            var servicesMethod = FindConfigureServicesDelegate(startupType, environmentName);

            object instance = null;
            if (!configureMethod.MethodInfo.IsStatic || (servicesMethod != null && !servicesMethod.MethodInfo.IsStatic))
            {
                instance = ActivatorUtilities.GetServiceOrCreateInstance(_services, startupType);
            }

            return new StartupMethods(configureMethod.Build(instance), servicesMethod?.Build(instance));
        }

        public Type FindStartupType(string startupName, IList<string> diagnosticMessages)
        {
            var environmentName = _hostingEnv.EnvironmentName;
            if (string.IsNullOrEmpty(startupName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(startupName));
            }

            var nameParts = HostingUtilities.SplitTypeName(startupName);
            var typeName = nameParts.Item1;
            var assemblyName = nameParts.Item2;

            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            if (assembly == null)
            {
                throw new InvalidOperationException($"The assembly '{assemblyName}' failed to load.");
            }

            if (string.IsNullOrEmpty(typeName))
            {
                typeName = "Startup";
            }

            var typeNameWithEnv = typeName + environmentName;

            // Check the most likely places first
            var type =
                assembly.GetType(typeNameWithEnv) ??
                assembly.GetType(assemblyName + "." + typeNameWithEnv) ??
                assembly.GetType(typeName) ??
                assembly.GetType(assemblyName + "." + typeName);

            if (type == null)
            {
                // Full scan
                var definedTypes = assembly.DefinedTypes.ToList();

                var startupType1 = definedTypes.Where(info => info.Name.Equals(typeNameWithEnv, StringComparison.Ordinal));
                var startupType2 = definedTypes.Where(info => info.Name.Equals(typeName, StringComparison.Ordinal));

                var typeInfo = startupType1.Concat(startupType2).FirstOrDefault();
                if (typeInfo != null)
                {
                    type = typeInfo.AsType();
                }
            }

            if (type == null)
            {
                throw new InvalidOperationException(String.Format("A type named '{0}' or '{1}' could not be found in assembly '{2}'.",
                    typeNameWithEnv,
                    typeName,
                    assemblyName));
            }

            return type;
        }

        private static ConfigureBuilder FindConfigureDelegate(Type startupType, string environmentName)
        {
            var configureMethod = FindMethod(startupType, "Configure{0}", environmentName, typeof(void), required: true);
            return new ConfigureBuilder(configureMethod);
        }

        private static ConfigureServicesBuilder FindConfigureServicesDelegate(Type startupType, string environmentName)
        {
            var servicesMethod = FindMethod(startupType, "Configure{0}Services", environmentName, typeof(IServiceProvider), required: false)
                ?? FindMethod(startupType, "Configure{0}Services", environmentName, typeof(void), required: false);
            return servicesMethod == null ? null : new ConfigureServicesBuilder(servicesMethod);
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
                    throw new InvalidOperationException(string.Format("A method named '{0}' or '{1}' in the type '{2}' could not be found.",
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
                    throw new InvalidOperationException(string.Format("The '{0}' method in the type '{1}' must have a return type of '{2}'.",
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