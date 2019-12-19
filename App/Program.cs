using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Net;
using System.Threading.Tasks;
using SecuredGrain;

namespace App
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args);

            host.UseOrleans(builder =>
            {
                builder
                .UseLocalhostClustering()
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .AddIncomingGrainCallFilter<AccessTokenValidationFilter>()
                .ConfigureApplicationParts(parts =>
                {
                    parts.AddApplicationPart(
                        typeof(SecureAdderGrain).Assembly)
                        .WithReferences();
                });
            });

            host.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });

            await host.RunConsoleAsync();
        }
    }
}
