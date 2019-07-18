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

namespace Home.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                        .ConfigureWebHostDefaults(webBuilder =>
                        {
                            webBuilder.UseStartup<Startup>();
                            webBuilder.UseWebRoot(Directory.GetCurrentDirectory().ToString() + "/wwwroot");
                            webBuilder.UseUrls("http://192.168.1.13:5000/");
                            webBuilder.UseIISIntegration();
                        })
                        .UseDefaultServiceProvider((context, options) =>
                        {
                            options.ValidateScopes = true;
                        })
                        .Build();

            host.Run();

            // CreateHostBuilder(args).Build().Run();
        }

        // public static IHostBuilder CreateHostBuilder(string[] args) =>
        //     Host.CreateDefaultBuilder(args)
        //         .ConfigureWebHostDefaults(webBuilder =>
        //         {
        //             webBuilder.UseStartup<Startup>();
        //             webBuilder.UseUrls("http://localhost:5000/");
        //             webBuilder.Build().Run();
        //         });
        // .UseDefaultServiceProvider((context, options) =>
        // {
        //     options.ValidateScopes = true;
        // });
    }
}
