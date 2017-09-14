using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

// Note that this sample will not run. It is only here to illustrate usage patterns.

namespace SampleStartups
{
    public class StartupConfigureOptions : StartupBase
    {
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public override void Configure(IApplicationBuilder app)
        {
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }

        // Entry point for the application.
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                //.UseKestrel()
                .UseFakeServer()
                .UseStartup<StartupConfigureOptions>()
                // Register transient IConfigureOptions
                .ConfigureOptions<ConfigureOptions>()
                // An instance will be registered as a singleton IConfigureOptions
                .ConfigureOptions(new SingletonConfigure(new SomeState { Default = "Hi" })) // Transient ConfigureOptions
                .Build();

            host.Run();
        }
    }

    // Configures multiple options types using services
    public class ConfigureOptions : IConfigureOptions<SampleOptions>, IPostConfigureOptions<SampleOptions2>
    {

        readonly IHostingEnvironment _env;
        readonly IServiceProvider _services;

        // Can consume services
        public ConfigureOptions(IHostingEnvironment env, IServiceProvider services)
        {
            _env = env;
            _services = services;
        }

        public void Configure(SampleOptions options)
        {
            options.SomeSetting = _env.ApplicationName;
        }

        public void PostConfigure(string name, SampleOptions2 options)
        {
            // Target only a specific options name
            if (name == _env.ApplicationName)
            {
                options.OtherSetting = _env.ContentRootPath;
            }
        }
    }

    public class SingletonConfigure : IConfigureNamedOptions<SampleOptions>, IPostConfigureOptions<SampleOptions>
    {
        readonly SomeState _state;

        public SingletonConfigure(SomeState state) => _state = state;

        public void Configure(string name, SampleOptions options)
        {
            if (name == _state.Default)
            {
                options.SomeSetting = "Set";
            }
        }

        public void Configure(SampleOptions options) => Configure(Options.DefaultName, options);

        // PostConfigures run after Configure, can be used to initialize to a default
        public void PostConfigure(string name, SampleOptions options)
        {
            if (options.SomeSetting == null)
            {
                options.SomeSetting = "Default";
            }
        }
    }

    public class SomeState {
        public string Default { get; set; }
    }

    public class SampleOptions
    {
        public string SomeSetting { get; set; }
    }

    public class SampleOptions2
    {
        public string OtherSetting { get; set; }
    }


}
