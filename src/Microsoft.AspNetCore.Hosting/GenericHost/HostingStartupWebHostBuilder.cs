using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    // We use this type to capture calls to the IWebHostBuilder so the we can properly order calls to 
    // to GenericHostWebHostBuilder.
    internal class HostingStartupWebHostBuilder : IWebHostBuilder
    {
        private readonly IWebHostBuilder _builder;
        private Action<WebHostBuilderContext, IConfigurationBuilder> _configureConfiguration;
        private Action<WebHostBuilderContext, IServiceCollection> _configureServices;

        public HostingStartupWebHostBuilder(IWebHostBuilder builder)
        {
            _builder = builder;
        }

        public IWebHost Build()
        {
            throw new NotSupportedException();
        }

        public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _configureConfiguration += configureDelegate;
            return this;
        }

        public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            return ConfigureServices((context, services) => configureServices(services));
        }

        public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
        {
            _configureServices += configureServices;
            return this;
        }

        public string GetSetting(string key) => _builder.GetSetting(key);

        public IWebHostBuilder UseSetting(string key, string value)
        {
            _builder.UseSetting(key, value);
            return this;
        }

        public void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            _configureServices?.Invoke(context, services);
        }

        public void ConfigureAppConfiguration(WebHostBuilderContext context, IConfigurationBuilder builder)
        {
            _configureConfiguration?.Invoke(context, builder);
        }
    }
}
