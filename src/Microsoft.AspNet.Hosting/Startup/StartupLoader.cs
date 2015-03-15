// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class StartupLoader : IStartupLoader
    {
        private readonly IServiceProvider _services;

        public StartupLoader(IServiceProvider services)
        {
            _services = services;
        }

        public ApplicationStartup LoadStartup(
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
                instance = ActivatorUtilities.GetServiceOrCreateInstance(_services, type);
            }

            return new ApplicationStartup(configureMethod, servicesMethod, instance);
        }
    }
}
