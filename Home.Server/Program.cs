using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Home.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                        .UseDefaultServiceProvider((context, options) =>
                        {
                            options.ValidateScopes = true;
                        })
                        .ConfigureWebHostDefaults(webBuilder =>
                        {
                            webBuilder.UseStartup<Startup>();
                            webBuilder.UseWebRoot(Directory.GetCurrentDirectory().ToString() + "/wwwroot");
                            webBuilder.UseUrls(ConnectionExtension.LocalIP);
                            webBuilder.UseIISIntegration();
                        })
                        .Build();

            host.Run();

        }

      
    }
}
